using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusCommunication.Data;
using NexusCommunication.Models;
using NexusCommunication.Services;

namespace NexusCommunication.Controllers.Api;

[ApiController]
[Authorize]
[Route("api/bills")]
public class BillsApiController : ApiBase
{
    private readonly AppDbContext _db;
    private readonly IdGenerator _ids;
    private readonly NotificationService _notify;
    private readonly AuditService _audit;
    public BillsApiController(AppDbContext db, IdGenerator ids, NotificationService notify, AuditService audit)
    { _db = db; _ids = ids; _notify = notify; _audit = audit; }

    /// <summary>Generate a postpaid bill (rental + usage - discount + 12.24% service tax) - accounts department only</summary>
    [HttpPost]
    public async Task<IActionResult> Generate([FromBody] GenerateBillRequest req)
    {
        var s = Session;
        if (s.Role is not (AppRoles.Accounts or AppRoles.Admin))
            return Err(403, "Only the accounts department can generate bills");
        if (req.ConnectionId <= 0) return Err(400, "connectionId is required");

        var conn = await _db.Connections.Include(c => c.Plan).FirstOrDefaultAsync(c => c.Id == req.ConnectionId);
        if (conn == null) return Err(404, "Connection not found");
        if (conn.Status == ConnStatus.PermanentlyInactive)
            return Err(409, "Cannot bill a permanently inactive connection");

        var rental = req.Rental ?? conn.Plan?.Price ?? 0m;
        var usage = req.Usage ?? 0m;
        var discount = req.Discount ?? 0m;
        if (rental < 0 || usage < 0 || discount < 0) return Err(400, "Amounts cannot be negative");

        if (!Blank(req.Period) && req.Period!.Trim().Length > 50) return Err(400, "Billing period label is too long (max 50 characters)");

        var subtotal = rental + usage - discount;
        if (subtotal < 0) return Err(400, "Discount cannot exceed rental + usage");
        var tax = Math.Round(subtotal * (Fmt.ServiceTaxRate / 100m), 2);
        var total = Math.Round(subtotal + tax, 2);

        var bill = new Bill
        {
            BillNo = await _ids.NextBillNoAsync(),
            ConnectionId = conn.Id,
            Period = Blank(req.Period) ? Fmt.MonthLabel(0) : req.Period!.Trim(),
            Rental = rental, Usage = usage, Tax = tax, Discount = discount, Total = total,
            PaidAmount = 0m, DueAmount = total, Status = BillStatus.Pending,
            DueDate = DateTime.UtcNow.AddDays(10),
            GeneratedBy = s.Name,
        };
        _db.Bills.Add(bill);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(AuditAction.Create, "Bill", bill.BillNo, $"Bill {bill.BillNo} generated for account {conn.AccountId}: {Fmt.Money(bill.Total)} ({bill.Period})");
        await _notify.BillGeneratedAsync(bill, conn.CustomerId, conn.AccountId);
        return Ok(new { ok = true, bill = new { id = bill.Id, billNo = bill.BillNo, total = bill.Total } });
    }
}
