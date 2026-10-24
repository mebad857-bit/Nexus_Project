using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusCommunication.Data;
using NexusCommunication.Models;
using NexusCommunication.Services;

namespace NexusCommunication.Controllers.Api;

[ApiController]
[Authorize]
[Route("api/payments")]
public class PaymentsApiController : ApiBase
{
    private static readonly string[] Methods = { "cash", "card", "upi", "cheque", "online" };
    private readonly AppDbContext _db;
    private readonly NotificationService _notify;
    private readonly AuditService _audit;
    private readonly IConfiguration _cfg;
    public PaymentsApiController(AppDbContext db, NotificationService notify, AuditService audit, IConfiguration cfg)
    { _db = db; _notify = notify; _audit = audit; _cfg = cfg; }

    public class OnlinePayRequest
    {
        public int BillId { get; set; }
        /// <summary>card | jazzcash | easypaisa</summary>
        public string? Method { get; set; }
        /// <summary>"decline" when the customer used the demo failure test data, anything else = success. Card / wallet numbers are never sent.</summary>
        public string? Outcome { get; set; }
    }

    /// <summary>
    /// DEMO online payment (no real money, no card / wallet data received or stored). The customer pays the full balance of one of their
    /// own bills; the attempt is stored in OnlinePayments, and a success also posts a normal Payment (channel "online") so the
    /// accounts desk, invoice and exports show it. Can be switched off with  "DemoPayments": { "Enabled": false }.
    /// </summary>
    [HttpPost("online")]
    public async Task<IActionResult> PayOnline([FromBody] OnlinePayRequest req)
    {
        if (!_cfg.GetValue("DemoPayments:Enabled", true)) return Err(403, "Demo online payments are switched off");
        var s = Session;
        if (s.Role != AppRoles.Customer) return Err(403, "Only customers can pay a bill online");

        var method = (req.Method ?? "").Trim().ToLowerInvariant();
        if (!OnlineMethods.All.Contains(method)) return Err(400, "Choose Card, JazzCash or Easypaisa");

        var customer = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == s.Id);
        if (customer == null) return Err(400, "Your login is not linked to a customer record");

        var bill = await _db.Bills.Include(b => b.Connection).FirstOrDefaultAsync(b => b.Id == req.BillId && b.Connection.CustomerId == customer.Id);
        if (bill == null) return Err(404, "Bill not found");
        if (bill.Status == BillStatus.Paid || bill.DueAmount <= 0) return Err(409, "This bill is already fully paid");

        // guard against a script hammering the endpoint: 10 attempts per 10 minutes
        var since = DateTime.UtcNow.AddMinutes(-10);
        if (await _db.OnlinePayments.CountAsync(o => o.CustomerId == customer.Id && o.CreatedAt > since) >= 10)
            return Err(429, "Too many payment attempts. Please wait a few minutes and try again.");

        var amount = bill.DueAmount;
        var txn = $"DEMO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        var declined = string.Equals(req.Outcome, "decline", StringComparison.OrdinalIgnoreCase);
        var label = OnlineMethods.Label(method);

        var op = new OnlinePayment { TransactionId = txn, BillId = bill.Id, CustomerId = customer.Id, Method = method, Amount = amount };

        if (declined)
        {
            op.Status = "failed";
            op.FailureReason = method == OnlineMethods.Card ? "Card declined by the demo bank" : $"{label} could not verify the account (demo)";
            _db.OnlinePayments.Add(op);
            await _db.SaveChangesAsync();
            await _audit.LogAsync(AuditAction.Payment, "Payment", txn, $"Demo online payment of {Fmt.Money(amount)} for bill {bill.BillNo} via {label} FAILED: {op.FailureReason}");
            await _notify.OnlinePaymentFailedAsync(op, bill, customer.Id);
            return Ok(new { ok = true, status = "failed", transactionId = txn, reason = op.FailureReason, amount, method, methodLabel = label });
        }

        var payment = new Payment
        {
            BillId = bill.Id, Amount = amount, Method = method, Reference = txn,
            Channel = "online", ReceivedBy = "Online payment (demo)",
        };
        bill.PaidAmount = Math.Min(bill.Total, bill.PaidAmount + amount);
        bill.DueAmount = Math.Round(bill.Total - bill.PaidAmount, 2);
        bill.Status = bill.DueAmount <= 0.004m ? BillStatus.Paid : bill.PaidAmount > 0 ? BillStatus.Partial : BillStatus.Pending;
        op.Status = "success"; op.Payment = payment;
        _db.Payments.Add(payment);
        _db.OnlinePayments.Add(op);

        try { await _db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Err(409, "This bill was just updated — please refresh the page and check its status"); }

        await _audit.LogAsync(AuditAction.Payment, "Payment", txn,
            $"Demo online payment {Fmt.Money(amount)} ({label}) on bill {bill.BillNo} · account {bill.Connection.AccountId} — bill now {Fmt.BillStatus(bill.Status)}");
        await _notify.OnlinePaymentSucceededAsync(op, payment, bill, customer.Id, bill.Connection.AccountId);
        return Ok(new
        {
            ok = true, status = "success", transactionId = txn, amount, method, methodLabel = label, paidAt = op.CreatedAt,
            billNo = bill.BillNo, balanceDue = bill.DueAmount, billStatus = bill.Status, receiptUrl = "/receipt/" + txn,
        });
    }

    /// <summary>Record a payment against a bill - retail counter or the accounts office</summary>
    [HttpPost]
    public async Task<IActionResult> Record([FromBody] RecordPaymentRequest req)
    {
        var s = Session;
        if (s.Role is not (AppRoles.Accounts or AppRoles.Retail or AppRoles.Admin))
            return Err(403, "Payments can only be recorded by retail or accounts staff");
        if (req.BillId <= 0 || req.Amount <= 0) return Err(400, "A valid bill and amount are required");

        var method = (req.Method ?? "cash").ToLowerInvariant();
        if (!Methods.Contains(method)) return Err(400, "Unknown payment method");

        var bill = await _db.Bills.FirstOrDefaultAsync(b => b.Id == req.BillId);
        if (bill == null) return Err(404, "Bill not found");
        if (bill.Status == BillStatus.Paid) return Err(409, "Bill is already fully settled");

        var amount = Math.Round(req.Amount, 2);
        if (amount > bill.DueAmount) return Err(400, $"Amount exceeds the outstanding balance of {bill.DueAmount:0.00}");

        var newPaid = Math.Min(bill.Total, bill.PaidAmount + amount);
        var due = Math.Round(bill.Total - newPaid, 2);

        var payment = new Payment
        {
            BillId = bill.Id,
            Amount = amount,
            Method = method,
            Reference = Blank(req.Reference) ? $"NX-{Random.Shared.Next(100000, 999999)}" : req.Reference!.Trim(),
            Channel = s.Role == AppRoles.Retail ? "retail" : "office",
            ReceivedBy = s.Name,
        };
        bill.PaidAmount = newPaid;
        bill.DueAmount = due;
        bill.Status = due <= 0.004m ? BillStatus.Paid : newPaid > 0 ? BillStatus.Partial : BillStatus.Pending;
        _db.Payments.Add(payment);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Err(409, "This bill was just updated by someone else — please refresh and try again");
        }

        var link = await _db.Connections.AsNoTracking().Where(c => c.Id == bill.ConnectionId)
            .Select(c => new { c.CustomerId, c.AccountId }).FirstOrDefaultAsync();
        await _audit.LogAsync(AuditAction.Payment, "Payment", payment.Reference,
            $"Payment {Fmt.Money(amount)} ({method}) recorded on bill {bill.BillNo}{(link != null ? $" · account {link.AccountId}" : "")} — bill now {Fmt.BillStatus(bill.Status)}");
        if (link != null) await _notify.PaymentReceivedAsync(payment, bill, link.CustomerId, link.AccountId);
        return Ok(new { ok = true, payment = new { id = payment.Id, reference = payment.Reference }, bill = new { id = bill.Id, status = bill.Status, dueAmount = bill.DueAmount } });
    }
}
