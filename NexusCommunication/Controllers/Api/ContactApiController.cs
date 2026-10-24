using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusCommunication.Data;
using NexusCommunication.Models;
using NexusCommunication.Services;

namespace NexusCommunication.Controllers.Api;

/// <summary>Public "Contact us" form (anyone may send) plus the admin-only "mark everything read" shortcut.</summary>
[ApiController]
[Route("api/contact")]
public class ContactApiController : ApiBase
{
    private static readonly Regex EmailShape = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    // simple spam guard: at most 5 messages per visitor address in 10 minutes
    private static readonly ConcurrentDictionary<string, List<DateTime>> Recent = new();

    private readonly AppDbContext _db;
    private readonly NotificationService _notify;
    private readonly AuditService _audit;
    public ContactApiController(AppDbContext db, NotificationService notify, AuditService audit)
    { _db = db; _notify = notify; _audit = audit; }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Send([FromBody] ContactRequest req)
    {
        // bots fill the hidden field: pretend it worked, store nothing
        if (!Blank(req.Website)) return Ok(new { ok = true });

        var name = req.Name?.Trim() ?? "";
        var email = req.Email?.Trim() ?? "";
        var message = req.Message?.Trim() ?? "";

        if (name.Length == 0) return Err(400, "Please tell us your name");
        if (name.Length > 200) return Err(400, "Name is too long (max 200 characters)");
        if (email.Length == 0 || email.Length > 200 || !EmailShape.IsMatch(email)) return Err(400, "Enter a valid e-mail address so we can reply");
        if (message.Length < 5) return Err(400, "Please write a few words so we can help");
        if (message.Length > 4000) return Err(400, "Message is too long (max 4000 characters)");

        var key = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var now = DateTime.UtcNow;
        var hits = Recent.GetOrAdd(key, _ => new List<DateTime>());
        lock (hits)
        {
            hits.RemoveAll(t => now - t > TimeSpan.FromMinutes(10));
            if (hits.Count >= 5) return Err(429, "Too many messages in a short time — please try again in a few minutes");
            hits.Add(now);
        }

        _db.ContactMessages.Add(new ContactMessage { Name = name, Email = email, Message = message });
        await _db.SaveChangesAsync();
        // the admin desk reads every message - ring the bell
        await _notify.ComplaintAsync("New complaint / contact message",
            $"{name}: {(message.Length > 140 ? message[..139] + "…" : message)}", "messages");
        return Ok(new { ok = true });
    }

    [HttpPost("read-all")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> ReadAll()
    {
        var n = await _db.ContactMessages.Where(m => !m.IsRead).ExecuteUpdateAsync(s => s.SetProperty(m => m.IsRead, true));
        if (n > 0) await _audit.LogAsync(AuditAction.Update, "Contact message", null, $"{n} contact message(s) marked read");
        return Ok(new { ok = true, updated = n });
    }
}
