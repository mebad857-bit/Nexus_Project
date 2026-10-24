using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusCommunication.Data;
using NexusCommunication.Models;
using NexusCommunication.Services;

namespace NexusCommunication.Controllers.Api;

/// <summary>The bell. Every call is scoped to the signed-in person - nobody can read or change another person's alerts.</summary>
[ApiController]
[Authorize]
[Route("api/notifications")]
public class NotificationsApiController : ApiBase
{
    private readonly AppDbContext _db;
    private readonly NotificationService _notify;
    public NotificationsApiController(AppDbContext db, NotificationService notify) { _db = db; _notify = notify; }

    /// <summary>Latest alerts (newest first) and the unread count</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int take = 30)
    {
        var me = Session.Id;
        if (Session.Role == AppRoles.Customer) await _notify.SyncBillRemindersAsync(me);   // bill due reminders
        take = Math.Clamp(take, 1, 100);
        var items = await _db.Notifications.AsNoTracking().Where(n => n.UserId == me)
            .OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id).Take(take)
            .Select(n => new { n.Id, n.Type, n.Title, n.Message, n.Link, n.IsRead, n.CreatedAt }).ToListAsync();
        var unread = await _db.Notifications.CountAsync(n => n.UserId == me && !n.IsRead);
        return Ok(new { unread, items });
    }

    /// <summary>Just the number for the badge (polled every so often)</summary>
    [HttpGet("count")]
    public async Task<IActionResult> Count()
    {
        var me = Session.Id;
        if (Session.Role == AppRoles.Customer) await _notify.SyncBillRemindersAsync(me);   // bill due reminders
        return Ok(new { unread = await _db.Notifications.CountAsync(n => n.UserId == me && !n.IsRead) });
    }

    [HttpPost("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var me = Session.Id;
        await _db.Notifications.Where(n => n.Id == id && n.UserId == me && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
        return Ok(new { ok = true });
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var me = Session.Id;
        var n = await _db.Notifications.Where(x => x.UserId == me && !x.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsRead, true));
        // housekeeping: read alerts older than 90 days are of no use any more
        var old = DateTime.UtcNow.AddDays(-90);
        await _db.Notifications.Where(x => x.UserId == me && x.IsRead && x.CreatedAt < old).ExecuteDeleteAsync();
        return Ok(new { ok = true, updated = n });
    }
}
