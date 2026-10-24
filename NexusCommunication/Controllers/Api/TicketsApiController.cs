using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusCommunication.Data;
using NexusCommunication.Models;
using NexusCommunication.Services;

namespace NexusCommunication.Controllers.Api;

/// <summary>
/// Complaint tickets.
///   GET  /api/tickets                 list, scoped by role (customer: own · technical: assigned to me · admin: all)
///   GET  /api/tickets/{id}            one ticket with its conversation + the actions this person may take
///   POST /api/tickets                 customer raises a complaint (multipart: category, subject, description, optional file)
///   POST /api/tickets/{id}/reply      customer / assigned technician / admin writes a message (multipart: message, optional file)
///   POST /api/tickets/{id}/assign     admin picks a technical-desk employee            { employeeId }
///   POST /api/tickets/{id}/status     { action: start | resolve | close | reopen, note }
///   GET  /api/tickets/technicians     admin: employees who can be assigned
/// Flow: open -> assigned -> in_progress -> resolved -> closed.
/// </summary>
[ApiController]
[Authorize]
[Route("api/tickets")]
public class TicketsApiController : ApiBase
{
    private const string UploadFolder = "tickets";

    private readonly AppDbContext _db;
    private readonly IdGenerator _ids;
    private readonly ImageStore _images;
    private readonly NotificationService _notify;
    private readonly AuditService _audit;

    public TicketsApiController(AppDbContext db, IdGenerator ids, ImageStore images, NotificationService notify, AuditService audit)
    { _db = db; _ids = ids; _images = images; _notify = notify; _audit = audit; }

    /* ------------------------------- who sees what ------------------------------ */

    private async Task<int?> MyEmployeeIdAsync() =>
        await _db.Employees.AsNoTracking().Where(e => e.UserId == Session.Id).Select(e => (int?)e.Id).FirstOrDefaultAsync();

    private async Task<int?> MyCustomerIdAsync() =>
        await _db.Customers.AsNoTracking().Where(c => c.UserId == Session.Id).Select(c => (int?)c.Id).FirstOrDefaultAsync();

    /// <summary>Tickets the signed-in person is allowed to see (null = this role has no ticket access)</summary>
    private async Task<IQueryable<Ticket>?> VisibleAsync()
    {
        var q = _db.Tickets.AsQueryable();
        switch (Session.Role)
        {
            case AppRoles.Admin: return q;
            case AppRoles.Customer:
                var cid = await MyCustomerIdAsync();
                return q.Where(t => t.CustomerId == (cid ?? -1));
            case AppRoles.Technical:
                var eid = await MyEmployeeIdAsync();
                return q.Where(t => t.AssignedEmployeeId == (eid ?? -1));
            default: return null;
        }
    }

    private async Task<Ticket?> LoadAsync(int id)
    {
        var visible = await VisibleAsync();
        if (visible == null) return null;
        return await visible.Include(t => t.Customer).Include(t => t.AssignedEmployee).FirstOrDefaultAsync(t => t.Id == id);
    }

    /// <summary>The buttons this person may use on this ticket right now (the UI only shows what the server will accept)</summary>
    private static string[] ActionsFor(string role, Ticket t)
    {
        var s = t.Status;
        var list = new List<string>();
        switch (role)
        {
            case AppRoles.Admin:
                if (s is TicketStatus.Open or TicketStatus.Assigned or TicketStatus.InProgress) list.Add("assign");
                if (s != TicketStatus.Closed) list.Add("close");
                if (s is TicketStatus.Resolved or TicketStatus.Closed) list.Add("reopen");
                break;
            case AppRoles.Technical:
                if (s == TicketStatus.Assigned) list.Add("start");
                if (s is TicketStatus.Assigned or TicketStatus.InProgress) list.Add("resolve");
                break;
            case AppRoles.Customer:
                if (s is TicketStatus.Open or TicketStatus.Resolved) list.Add("close");
                if (s == TicketStatus.Resolved) list.Add("reopen");
                break;
        }
        return list.ToArray();
    }

    private static bool CanReply(string role, Ticket t) => t.Status != TicketStatus.Closed && role is AppRoles.Admin or AppRoles.Customer or AppRoles.Technical;

    private static object Summary(Ticket t, int replies) => new
    {
        t.Id, t.TicketNo, t.Category, t.Subject, t.Status, statusLabel = TicketStatus.Label(t.Status), t.ImageUrl,
        t.CreatedAt, t.UpdatedAt, t.AssignedAt, t.ResolvedAt, t.ClosedAt,
        customerId = t.CustomerId, customerName = t.Customer?.Name, customerPhone = t.Customer?.Phone,
        assigneeId = t.AssignedEmployeeId, assigneeName = t.AssignedEmployee?.Name,
        replies,
    };

    /* --------------------------------- reading --------------------------------- */

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status = null)
    {
        var visible = await VisibleAsync();
        if (visible == null) return Err(403, "Forbidden");
        var q = visible;
        if (!Blank(status) && TicketStatus.All.Contains(status!)) q = q.Where(t => t.Status == status);

        var rows = await q.AsNoTracking().OrderByDescending(t => t.UpdatedAt).ThenByDescending(t => t.Id).Take(500)
            .Select(t => new
            {
                t.Id, t.TicketNo, t.Category, t.Subject, t.Status, t.ImageUrl, t.CreatedAt, t.UpdatedAt, t.AssignedAt, t.ResolvedAt, t.ClosedAt,
                customerId = t.CustomerId, customerName = t.Customer.Name, customerPhone = t.Customer.Phone,
                assigneeId = t.AssignedEmployeeId, assigneeName = t.AssignedEmployee != null ? t.AssignedEmployee.Name : null,
                replies = t.Replies.Count(r => !r.IsSystem),
            }).ToListAsync();

        var all = await visible.AsNoTracking().GroupBy(t => t.Status).Select(g => new { g.Key, Count = g.Count() }).ToListAsync();
        var counts = TicketStatus.All.ToDictionary(k => k, k => all.FirstOrDefault(x => x.Key == k)?.Count ?? 0);

        return Ok(new
        {
            counts,
            items = rows.Select(t => new
            {
                t.Id, t.TicketNo, t.Category, t.Subject, t.Status, statusLabel = TicketStatus.Label(t.Status), t.ImageUrl,
                t.CreatedAt, t.UpdatedAt, t.AssignedAt, t.ResolvedAt, t.ClosedAt,
                t.customerId, t.customerName, t.customerPhone, t.assigneeId, t.assigneeName, t.replies,
            }),
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var t = await LoadAsync(id);
        if (t == null) return Err(404, "Ticket not found");
        var replies = await _db.TicketReplies.AsNoTracking().Where(r => r.TicketId == id)
            .OrderBy(r => r.CreatedAt).ThenBy(r => r.Id)
            .Select(r => new { r.Id, r.AuthorName, r.AuthorRole, r.Message, r.ImageUrl, r.IsSystem, r.CreatedAt }).ToListAsync();
        return Ok(new
        {
            ticket = Summary(t, replies.Count(r => !r.IsSystem)),
            description = t.Description,
            customerAddress = Session.Role == AppRoles.Customer ? null : t.Customer.Address,
            replies,
            actions = ActionsFor(Session.Role, t),
            canReply = CanReply(Session.Role, t),
        });
    }

    /// <summary>Technical-desk employees the admin can assign to, with their current workload</summary>
    [HttpGet("technicians")]
    public async Task<IActionResult> Technicians()
    {
        if (Session.Role != AppRoles.Admin) return Err(403, "Forbidden");
        var rows = await _db.Employees.AsNoTracking()
            .Where(e => e.Department == AppRoles.Technical && e.Status == "active" && e.UserId != null)
            .OrderBy(e => e.Name)
            .Select(e => new
            {
                e.Id, e.Name, e.Designation,
                openCount = _db.Tickets.Count(t => t.AssignedEmployeeId == e.Id && t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Closed),
            }).ToListAsync();
        return Ok(rows);
    }

    /* --------------------------------- raising --------------------------------- */

    [HttpPost]
    [RequestSizeLimit(4 * 1024 * 1024)]
    public async Task<IActionResult> Raise([FromForm] string? category, [FromForm] string? subject, [FromForm] string? description, IFormFile? file)
    {
        if (Session.Role != AppRoles.Customer) return Err(403, "Only customers can raise a complaint");
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.UserId == Session.Id);
        if (customer == null) return Err(400, "Your login is not linked to a customer record");

        category = category?.Trim() ?? ""; subject = subject?.Trim() ?? ""; description = description?.Trim() ?? "";
        if (!TicketCategories.All.Contains(category)) return Err(400, "Choose a category");
        if (subject.Length < 5) return Err(400, "Write a short subject (at least 5 characters)");
        if (subject.Length > 200) return Err(400, "Subject is too long (max 200 characters)");
        if (description.Length < 10) return Err(400, "Please describe the problem (at least 10 characters)");
        if (description.Length > 4000) return Err(400, "Description is too long (max 4000 characters)");

        // spam guard: 5 complaints per hour per customer
        var hourAgo = DateTime.UtcNow.AddHours(-1);
        if (await _db.Tickets.CountAsync(t => t.CustomerId == customer.Id && t.CreatedAt > hourAgo) >= 5)
            return Err(429, "You have raised several complaints in the last hour. Please wait a little before raising another.");

        string? imageUrl = null;
        if (file != null && file.Length > 0)
        {
            var (path, error) = await _images.SaveAsync(file, UploadFolder);
            if (error != null) return Err(400, error);
            imageUrl = path;
        }

        var now = DateTime.UtcNow;
        var ticket = new Ticket
        {
            TicketNo = await _ids.NextTicketNoAsync(), CustomerId = customer.Id, Category = category, Subject = subject,
            Description = description, ImageUrl = imageUrl, Status = TicketStatus.Open, CreatedAt = now, UpdatedAt = now,
        };
        _db.Tickets.Add(ticket);
        try { await _db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            // two complaints at the same moment picked the same number: try once more with a fresh one
            _db.Entry(ticket).State = EntityState.Detached;
            ticket = new Ticket
            {
                TicketNo = await _ids.NextTicketNoAsync(), CustomerId = customer.Id, Category = category, Subject = subject,
                Description = description, ImageUrl = imageUrl, Status = TicketStatus.Open, CreatedAt = now, UpdatedAt = now,
            };
            _db.Tickets.Add(ticket);
            try { await _db.SaveChangesAsync(); }
            catch (DbUpdateException) { _images.Delete(imageUrl); return Err(500, "Could not save the complaint, please try again"); }
        }

        await _audit.LogAsync(AuditAction.Create, "Ticket", ticket.Id.ToString(), $"Complaint {ticket.TicketNo} raised by {customer.Name}: {subject} ({category})");
        await _notify.TicketRaisedAsync(ticket, customer.Name);
        return Ok(new { ok = true, id = ticket.Id, ticketNo = ticket.TicketNo });
    }

    /* --------------------------------- replying -------------------------------- */

    [HttpPost("{id:int}/reply")]
    [RequestSizeLimit(4 * 1024 * 1024)]
    public async Task<IActionResult> Reply(int id, [FromForm] string? message, IFormFile? file)
    {
        var t = await LoadAsync(id);
        if (t == null) return Err(404, "Ticket not found");
        if (!CanReply(Session.Role, t)) return Err(400, "This ticket is closed - no more replies");

        message = message?.Trim() ?? "";
        if (message.Length < 2) return Err(400, "Write your message first");
        if (message.Length > 2000) return Err(400, "Message is too long (max 2000 characters)");

        string? imageUrl = null;
        if (file != null && file.Length > 0)
        {
            var (path, error) = await _images.SaveAsync(file, UploadFolder);
            if (error != null) return Err(400, error);
            imageUrl = path;
        }

        var s = Session;
        var now = DateTime.UtcNow;
        _db.TicketReplies.Add(new TicketReply
        {
            TicketId = t.Id, AuthorUserId = s.Id, AuthorName = s.Name, AuthorRole = s.Role, Message = message, ImageUrl = imageUrl, CreatedAt = now,
        });

        string? sysNote = null;
        var statusChanged = false;
        if (s.Role == AppRoles.Technical && t.Status == TicketStatus.Assigned)
        {
            t.Status = TicketStatus.InProgress; statusChanged = true; sysNote = $"{s.Name} started working on this ticket";
        }
        else if (s.Role == AppRoles.Customer && t.Status == TicketStatus.Resolved)
        {
            // the customer says it is not fixed: back to the technician (or to the queue if nobody is assigned)
            t.Status = t.AssignedEmployeeId != null ? TicketStatus.InProgress : TicketStatus.Open;
            t.ResolvedAt = null; statusChanged = true; sysNote = $"{s.Name} replied — ticket reopened";
        }
        if (sysNote != null)
            _db.TicketReplies.Add(new TicketReply { TicketId = t.Id, AuthorUserId = s.Id, AuthorName = s.Name, AuthorRole = s.Role, Message = sysNote, IsSystem = true, CreatedAt = now.AddMilliseconds(5) });
        t.UpdatedAt = now;
        await _db.SaveChangesAsync();

        await _notify.TicketReplyAsync(t, s.Role, s.Name, message);
        if (statusChanged) await _notify.TicketStatusAsync(t, s.Role);
        return Ok(new { ok = true });
    }

    /* --------------------------------- assigning ------------------------------- */

    public class AssignRequest { public int EmployeeId { get; set; } }
    public class TicketActionRequest { public string? Action { get; set; } public string? Note { get; set; } }

    [HttpPost("{id:int}/assign")]
    public async Task<IActionResult> Assign(int id, [FromBody] AssignRequest req)
    {
        if (Session.Role != AppRoles.Admin) return Err(403, "Only the admin can assign a ticket");
        var t = await LoadAsync(id);
        if (t == null) return Err(404, "Ticket not found");
        if (!ActionsFor(AppRoles.Admin, t).Contains("assign")) return Err(400, "A resolved or closed ticket cannot be assigned. Reopen it first.");

        var emp = await _db.Employees.FirstOrDefaultAsync(e => e.Id == req.EmployeeId);
        if (emp == null || emp.Department != AppRoles.Technical || emp.Status != "active" || emp.UserId == null)
            return Err(400, "Choose an active technical-desk employee who has a login");

        var now = DateTime.UtcNow;
        var previous = t.AssignedEmployee?.Name;
        t.AssignedEmployeeId = emp.Id; t.AssignedEmployee = emp; t.AssignedAt = now; t.UpdatedAt = now;
        if (t.Status == TicketStatus.Open) t.Status = TicketStatus.Assigned;
        var note = previous == null ? $"Assigned to {emp.Name} (technical desk)" : previous == emp.Name ? $"Assignment confirmed: {emp.Name}" : $"Re-assigned from {previous} to {emp.Name}";
        _db.TicketReplies.Add(new TicketReply { TicketId = t.Id, AuthorUserId = Session.Id, AuthorName = Session.Name, AuthorRole = Session.Role, Message = note, IsSystem = true, CreatedAt = now });
        await _db.SaveChangesAsync();

        await _audit.LogAsync(AuditAction.Status, "Ticket", t.Id.ToString(), $"{t.TicketNo}: {note}");
        await _notify.TicketAssignedAsync(t, emp.Name);
        return Ok(new { ok = true });
    }

    /* ---------------------------------- status --------------------------------- */

    [HttpPost("{id:int}/status")]
    public async Task<IActionResult> ChangeStatus(int id, [FromBody] TicketActionRequest req)
    {
        var t = await LoadAsync(id);
        if (t == null) return Err(404, "Ticket not found");
        var action = req.Action?.Trim().ToLowerInvariant() ?? "";
        var role = Session.Role;
        if (!ActionsFor(role, t).Contains(action)) return Err(400, "That change is not allowed for this ticket right now");

        var note = req.Note?.Trim() ?? "";
        if (note.Length > 2000) return Err(400, "Note is too long (max 2000 characters)");
        var now = DateTime.UtcNow;
        var s = Session;
        var old = t.Status;
        string line;

        switch (action)
        {
            case "start":
                t.Status = TicketStatus.InProgress;
                line = $"{s.Name} started working on this ticket";
                break;
            case "resolve":
                if (note.Length < 5) return Err(400, "Write what was done to fix it (at least 5 characters)");
                t.Status = TicketStatus.Resolved; t.ResolvedAt = now;
                line = $"{s.Name} marked this ticket resolved";
                break;
            case "close":
                t.Status = TicketStatus.Closed; t.ClosedAt = now;
                line = role == AppRoles.Customer ? $"{s.Name} closed the ticket" : $"Ticket closed by {s.Name}";
                break;
            case "reopen":
                t.Status = t.AssignedEmployeeId != null ? TicketStatus.InProgress : TicketStatus.Open;
                t.ResolvedAt = null; t.ClosedAt = null;
                line = $"Ticket reopened by {s.Name}";
                break;
            default: return Err(400, "Unknown action");
        }

        // the resolution note (or a reason for closing / reopening) is shown in the conversation as a normal message
        if (note.Length > 0)
            _db.TicketReplies.Add(new TicketReply { TicketId = t.Id, AuthorUserId = s.Id, AuthorName = s.Name, AuthorRole = role, Message = note, CreatedAt = now });
        _db.TicketReplies.Add(new TicketReply { TicketId = t.Id, AuthorUserId = s.Id, AuthorName = s.Name, AuthorRole = role, Message = line, IsSystem = true, CreatedAt = now.AddMilliseconds(5) });
        t.UpdatedAt = now;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(AuditAction.Status, "Ticket", t.Id.ToString(), $"{t.TicketNo}: {TicketStatus.Label(old)} → {TicketStatus.Label(t.Status)} by {s.Name}");
        await _notify.TicketStatusAsync(t, role);
        return Ok(new { ok = true, status = t.Status });
    }
}
