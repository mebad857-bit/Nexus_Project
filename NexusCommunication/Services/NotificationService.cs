using Microsoft.EntityFrameworkCore;
using NexusCommunication.Data;
using NexusCommunication.Models;

namespace NexusCommunication.Services;

/// <summary>One person who should receive an alert, plus the dashboard role that decides which page the alert opens.</summary>
public record Recipient(int UserId, string Role);

/// <summary>
/// Creates the alerts shown in the bell. Rules:
///  - a customer only ever receives alerts about their own orders / bills / payments;
///  - staff only receive alerts meant for their role (technical: surveys, accounts: billing, retail: their outlet, admin: oversight);
///  - the person who caused the event is never alerted about their own action.
/// Call these AFTER the real change was saved. They never throw - a failed alert must not fail the action.
/// </summary>
public class NotificationService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;
    private readonly ILogger<NotificationService> _log;

    public NotificationService(AppDbContext db, IHttpContextAccessor http, ILogger<NotificationService> log)
    {
        _db = db; _http = http; _log = log;
    }

    private int? ActorId
    {
        get
        {
            var u = _http.HttpContext?.User;
            return u?.Identity?.IsAuthenticated == true ? u.ToSession().Id : null;
        }
    }

    /* ------------------------------ who gets it ------------------------------ */

    public async Task<List<Recipient>> StaffAsync(params string[] roles) =>
        (await _db.Users.AsNoTracking().Where(u => roles.Contains(u.Role)).Select(u => new { u.Id, u.Role }).ToListAsync())
            .Select(u => new Recipient(u.Id, u.Role)).ToList();

    /// <summary>The login of a customer record (walk-in customers created at a counter may have none)</summary>
    public async Task<List<Recipient>> CustomerAsync(int customerId)
    {
        var uid = await _db.Customers.AsNoTracking().Where(c => c.Id == customerId).Select(c => c.UserId).FirstOrDefaultAsync();
        return uid is int id ? new List<Recipient> { new(id, AppRoles.Customer) } : new List<Recipient>();
    }

    /// <summary>Retail staff working at one outlet</summary>
    public async Task<List<Recipient>> ShopStaffAsync(int? shopId)
    {
        if (shopId == null) return new List<Recipient>();
        return (await _db.Employees.AsNoTracking().Where(e => e.ShopId == shopId && e.UserId != null).Select(e => e.UserId!.Value).ToListAsync())
            .Select(id => new Recipient(id, AppRoles.Retail)).ToList();
    }

    /* ------------------------------- the events ------------------------------ */

    /// <summary>A new order is waiting for its feasibility survey</summary>
    public async Task OrderPlacedAsync(Order o, string customerName)
    {
        var to = await StaffAsync(AppRoles.Technical, AppRoles.Admin);
        await SendAsync(to, NotifType.Order, "New order awaiting survey",
            $"Order {o.OrderNo} · {Fmt.TypeLabel(o.ConnectionType)} · {customerName}", OrderLink);
    }

    /// <summary>An order changed status (feasible / not feasible / cancelled / connected ...)</summary>
    public async Task OrderStatusAsync(Order o, string actorRole, string? accountInfo = null)
    {
        var statusText = Fmt.OrderStatus(o.Status);

        if (o.Status == OrderStatus.Cancelled && actorRole == AppRoles.Customer)
        {
            // the customer withdrew it: tell the people who would have worked on it
            await SendAsync(await StaffAsync(AppRoles.Technical, AppRoles.Admin), NotifType.Order,
                "Order withdrawn by customer", $"Order {o.OrderNo} was cancelled by the customer.", OrderLink);
            return;
        }

        var customerMsg = o.Status switch
        {
            OrderStatus.Feasible => $"Order {o.OrderNo}: the survey is complete and your area is feasible. Installation follows within 48 hours.",
            OrderStatus.Infeasible => $"Order {o.OrderNo}: sorry, service is not feasible at your address right now.",
            OrderStatus.Connected => $"Order {o.OrderNo} is complete — your connection is live{(accountInfo != null ? $" ({accountInfo})" : "")}.",
            OrderStatus.Cancelled => $"Order {o.OrderNo} has been cancelled.",
            _ => $"Order {o.OrderNo} is now: {statusText}.",
        };
        var customerTitle = o.Status switch
        {
            OrderStatus.Feasible => "Your area is feasible",
            OrderStatus.Infeasible => "Area not feasible",
            OrderStatus.Connected => "Your connection is live",
            OrderStatus.Cancelled => "Order cancelled",
            _ => "Order status updated",
        };
        await SendAsync(await CustomerAsync(o.CustomerId), NotifType.Order, customerTitle, customerMsg, OrderLink);

        // staff: the outlet that placed it + admin; accounts are told when a line is live and ready to bill
        var staff = await ShopStaffAsync(o.ShopId);
        staff.AddRange(await StaffAsync(AppRoles.Admin));
        if (o.Status == OrderStatus.Connected) staff.AddRange(await StaffAsync(AppRoles.Accounts));
        await SendAsync(staff, NotifType.Order, $"Order {statusText.ToLowerInvariant()}",
            $"Order {o.OrderNo} is now: {statusText}.{(accountInfo != null ? " " + accountInfo : "")}", OrderLink);
    }

    /// <summary>A bill was generated for a customer's connection</summary>
    public async Task BillGeneratedAsync(Bill b, int customerId, string accountId)
    {
        await SendAsync(await CustomerAsync(customerId), NotifType.Bill, "New bill generated",
            $"Bill {b.BillNo} for {b.Period}: {Fmt.Money(b.Total)}, due {Fmt.Date(b.DueDate)}.", BillLink);
        await SendAsync(await StaffAsync(AppRoles.Admin), NotifType.Bill, "Bill generated",
            $"Bill {b.BillNo} ({Fmt.Money(b.Total)}) was generated for account {accountId}.", BillLink);
    }

    /// <summary>A payment was recorded against a bill</summary>
    public async Task PaymentReceivedAsync(Payment p, Bill b, int customerId, string accountId)
    {
        var settled = b.Status == BillStatus.Paid;
        if (settled) await ClearBillRemindersAsync(b, customerId);
        await SendAsync(await CustomerAsync(customerId), NotifType.Payment, "Payment received",
            $"We received {Fmt.Money(p.Amount)} for bill {b.BillNo}. " + (settled ? "The bill is fully paid — thank you!" : $"Balance due: {Fmt.Money(b.DueAmount)}."), PaymentLink);
        await SendAsync(await StaffAsync(AppRoles.Accounts, AppRoles.Admin), NotifType.Payment, "Payment received",
            $"{Fmt.Money(p.Amount)} on bill {b.BillNo} (account {accountId}, {p.Method}).{(settled ? " Bill settled." : "")}", PaymentLink);
    }

    /// <summary>
    /// A customer paid a bill through the DEMO online payment. The customer gets their own alert too (with the receipt link),
    /// even though they caused the event; accounts + admin are told as usual.
    /// </summary>
    public async Task OnlinePaymentSucceededAsync(OnlinePayment op, Payment p, Bill b, int customerId, string accountId)
    {
        var settled = b.Status == BillStatus.Paid;
        if (settled) await ClearBillRemindersAsync(b, customerId);
        await SendAsync(await CustomerAsync(customerId), NotifType.Payment, "Payment successful (demo)",
            $"{Fmt.Money(p.Amount)} paid for bill {b.BillNo} via {OnlineMethods.Label(op.Method)}. Transaction {op.TransactionId}." + (settled ? " The bill is fully paid." : $" Balance due: {Fmt.Money(b.DueAmount)}."),
            role => "/receipt/" + op.TransactionId, includeActor: true);
        await SendAsync(await StaffAsync(AppRoles.Accounts, AppRoles.Admin), NotifType.Payment, "Online payment received (demo)",
            $"{Fmt.Money(p.Amount)} on bill {b.BillNo} (account {accountId}, {OnlineMethods.Label(op.Method)}, {op.TransactionId}).{(settled ? " Bill settled." : "")}", PaymentLink);
    }

    /// <summary>A demo online payment was declined - only the customer is told</summary>
    public async Task OnlinePaymentFailedAsync(OnlinePayment op, Bill b, int customerId)
    {
        await SendAsync(await CustomerAsync(customerId), NotifType.Payment, "Payment failed (demo)",
            $"{Fmt.Money(op.Amount)} for bill {b.BillNo} was not completed: {op.FailureReason}. Nothing was charged — you can try again.",
            role => "/portal#bills", includeActor: true);
    }

    /// <summary>A new complaint / contact message / feedback arrived - the admin desk reads it</summary>
    public async Task ComplaintAsync(string title, string message, string tab)
    {
        await SendAsync(await StaffAsync(AppRoles.Admin), NotifType.Complaint, title, message,
            role => role == AppRoles.Admin ? "/admin#" + tab : null);
    }

    /* ----------------------------- complaint tickets ----------------------------- */

    private static string? TicketLink(string role) => role switch
    {
        AppRoles.Customer => "/portal#tickets", AppRoles.Admin => "/admin#tickets", AppRoles.Technical => "/technical#tickets", _ => null,
    };

    private async Task<List<Recipient>> AssigneeAsync(Ticket t)
    {
        if (t.AssignedEmployeeId == null) return new List<Recipient>();
        var uid = await _db.Employees.AsNoTracking().Where(e => e.Id == t.AssignedEmployeeId).Select(e => e.UserId).FirstOrDefaultAsync();
        return uid is int id ? new List<Recipient> { new(id, AppRoles.Technical) } : new List<Recipient>();
    }

    /// <summary>A customer raised a complaint - the admin desk assigns it</summary>
    public async Task TicketRaisedAsync(Ticket t, string customerName)
    {
        await SendAsync(await StaffAsync(AppRoles.Admin), NotifType.Ticket, "New complaint ticket",
            $"{t.TicketNo} · {t.Category} · {customerName}: {t.Subject}", TicketLink);
    }

    /// <summary>The admin assigned (or re-assigned) a ticket: the technician is told, and so is the customer</summary>
    public async Task TicketAssignedAsync(Ticket t, string assigneeName)
    {
        await SendAsync(await AssigneeAsync(t), NotifType.Ticket, "Ticket assigned to you", $"{t.TicketNo}: {t.Subject}", TicketLink);
        await SendAsync(await CustomerAsync(t.CustomerId), NotifType.Ticket, "Your complaint was assigned",
            $"{t.TicketNo} is now with {assigneeName} of the technical desk.", TicketLink);
    }

    /// <summary>A ticket changed status (in progress / resolved / closed / reopened)</summary>
    public async Task TicketStatusAsync(Ticket t, string actorRole)
    {
        var label = TicketStatus.Label(t.Status).ToLowerInvariant();
        var customerMsg = t.Status switch
        {
            TicketStatus.InProgress when actorRole == AppRoles.Customer => $"{t.TicketNo} was reopened and is back with the technical desk.",
            TicketStatus.InProgress => $"{t.TicketNo}: our technical team has started working on it.",
            TicketStatus.Resolved => $"{t.TicketNo} has been marked resolved. Please check and close it, or reply if the problem is still there.",
            TicketStatus.Closed => $"{t.TicketNo} has been closed.",
            _ => $"{t.TicketNo} is now {label}.",
        };
        await SendAsync(await CustomerAsync(t.CustomerId), NotifType.Ticket, "Complaint " + label, customerMsg, TicketLink);

        // staff side: whoever did not cause it
        var staff = await StaffAsync(AppRoles.Admin);
        staff.AddRange(await AssigneeAsync(t));
        await SendAsync(staff, NotifType.Ticket, "Ticket " + label,
            actorRole == AppRoles.Customer ? $"{t.TicketNo} {label} by the customer." : $"{t.TicketNo} is now {label}.", TicketLink);
    }

    /// <summary>Someone wrote in the ticket conversation: the other side is told</summary>
    public async Task TicketReplyAsync(Ticket t, string actorRole, string authorName, string text)
    {
        var snippet = Cut(text, 140);
        if (actorRole == AppRoles.Customer)
        {
            var staff = await StaffAsync(AppRoles.Admin);
            staff.AddRange(await AssigneeAsync(t));
            await SendAsync(staff, NotifType.Ticket, "Customer replied", $"{t.TicketNo} · {authorName}: {snippet}", TicketLink);
        }
        else
        {
            await SendAsync(await CustomerAsync(t.CustomerId), NotifType.Ticket, "New reply on your complaint", $"{t.TicketNo} · {authorName}: {snippet}", TicketLink);
            if (actorRole == AppRoles.Admin) await SendAsync(await AssigneeAsync(t), NotifType.Ticket, "Admin replied on a ticket", $"{t.TicketNo} · {snippet}", TicketLink);
        }
    }

    /* --------------------------- bill due reminders --------------------------- */

    public const string DueSoonTitle = "Bill due soon";
    public const string OverdueTitle = "Bill overdue";

    /// <summary>
    /// Puts a reminder in the customer's bell for every bill that is overdue or due within <see cref="BillReminder.WarnDays"/> days.
    /// Needs no extra table: a reminder is only created if the bell does not already hold the same one (same bill, same stage),
    /// so it is safe to call as often as you like. Runs at most once per request. Never throws.
    /// </summary>
    public async Task SyncBillRemindersAsync(int userId)
    {
        try
        {
            var ctx = _http.HttpContext;
            const string key = "nx-bill-reminders-synced";
            if (ctx != null)
            {
                if (ctx.Items.ContainsKey(key)) return;
                ctx.Items[key] = true;
            }

            var customerId = await _db.Customers.AsNoTracking().Where(c => c.UserId == userId).Select(c => (int?)c.Id).FirstOrDefaultAsync();
            if (customerId == null) return;

            var now = DateTime.UtcNow;
            var due = await BillReminder.Due(_db.Bills.AsNoTracking().Where(b => b.Connection.CustomerId == customerId), now)
                .OrderBy(b => b.DueDate).ToListAsync();
            if (due.Count == 0) return;

            var existing = await _db.Notifications.AsNoTracking()
                .Where(n => n.UserId == userId && n.Type == NotifType.Bill && (n.Title == DueSoonTitle || n.Title == OverdueTitle))
                .Select(n => new { n.Title, n.Message }).ToListAsync();

            foreach (var b in due)
            {
                var overdue = BillReminder.IsOverdue(b, now);
                var title = overdue ? OverdueTitle : DueSoonTitle;
                var prefix = $"Bill {b.BillNo} (";
                if (existing.Any(e => e.Title == title && e.Message.StartsWith(prefix))) continue;

                var when = BillReminder.When(b, now);
                _db.Notifications.Add(new Notification
                {
                    UserId = userId, Type = NotifType.Bill, Title = title, Link = "/portal#bills",
                    Message = Cut($"{prefix}{b.Period}): {Fmt.Money(b.DueAmount)} due on {Fmt.Date(b.DueDate)} - {when}.", 1000),
                });
            }
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Bill reminders could not be stored");
            foreach (var e in _db.ChangeTracker.Entries<Notification>().Where(e => e.State == EntityState.Added).ToList())
                e.State = EntityState.Detached;
        }
    }

    /// <summary>A bill was paid in full: its "due soon / overdue" alerts are of no use any more, so they are marked read</summary>
    private async Task ClearBillRemindersAsync(Bill b, int customerId)
    {
        try
        {
            var uid = await _db.Customers.AsNoTracking().Where(c => c.Id == customerId).Select(c => c.UserId).FirstOrDefaultAsync();
            if (uid is not int userId) return;
            var prefix = $"Bill {b.BillNo} (";
            await _db.Notifications.Where(n => n.UserId == userId && !n.IsRead && n.Type == NotifType.Bill
                    && (n.Title == DueSoonTitle || n.Title == OverdueTitle) && n.Message.StartsWith(prefix))
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
        }
        catch (Exception ex) { _log.LogWarning(ex, "Bill reminders could not be cleared"); }
    }

    /* ------------------------------- plumbing -------------------------------- */

    private static string? OrderLink(string role) => role switch
    {
        AppRoles.Customer => "/portal#orders", AppRoles.Technical => "/technical#orders", AppRoles.Admin => "/admin#orders",
        AppRoles.Retail => "/retail#book", AppRoles.Accounts => "/accounts#generate", _ => null,
    };
    private static string? BillLink(string role) => role switch
    {
        AppRoles.Customer => "/portal#bills", AppRoles.Admin => "/admin#billing", AppRoles.Accounts => "/accounts#ledger",
        AppRoles.Retail => "/retail#collections", _ => null,
    };
    private static string? PaymentLink(string role) => role switch
    {
        AppRoles.Customer => "/portal#bills", AppRoles.Admin => "/admin#billing", AppRoles.Accounts => "/accounts#receipts",
        AppRoles.Retail => "/retail#collections", _ => null,
    };
    private static string? RoleHome(string role) => Fmt.RoleHome(role);

    /// <summary>One row per recipient (duplicates and the acting person removed)</summary>
    private async Task SendAsync(IEnumerable<Recipient> to, string type, string title, string message, Func<string, string?> link, bool includeActor = false)
    {
        try
        {
            var actor = includeActor ? null : ActorId;
            var list = to.Where(r => r.UserId != actor).GroupBy(r => r.UserId).Select(g => g.First()).ToList();
            if (list.Count == 0) return;
            foreach (var r in list)
                _db.Notifications.Add(new Notification
                {
                    UserId = r.UserId, Type = type, Title = Cut(title, 200), Message = Cut(message, 1000), Link = link(r.Role),
                });
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Notification could not be stored");
            foreach (var e in _db.ChangeTracker.Entries<Notification>().Where(e => e.State == EntityState.Added).ToList())
                e.State = EntityState.Detached;
        }
    }

    private static string Cut(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";
}
