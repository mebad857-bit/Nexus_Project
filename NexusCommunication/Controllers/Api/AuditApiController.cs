using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusCommunication.Data;
using NexusCommunication.Models;
using NexusCommunication.Services;

namespace NexusCommunication.Controllers.Api;

/// <summary>Read-only access to the activity log - administrators only. Nothing here can edit or delete log rows.</summary>
[ApiController]
[Authorize(Roles = AppRoles.Admin)]
[Route("api/audit")]
public class AuditApiController : ApiBase
{
    private const int PageSize = 25;
    private readonly AppDbContext _db;
    public AuditApiController(AppDbContext db) => _db = db;

    /// <summary>Filter lists: everyone who appears in the log, and the available action types</summary>
    [HttpGet("meta")]
    public async Task<IActionResult> Meta()
    {
        var users = await _db.AuditLogs.AsNoTracking().Select(l => l.UserName).Distinct().OrderBy(n => n).ToListAsync();
        return Ok(new { users, actions = AuditAction.All });
    }

    /// <summary>
    /// GET /api/audit?user=&amp;action=&amp;from=2026-10-01&amp;to=2026-10-10&amp;q=&amp;tz=-300&amp;page=1
    /// from / to are calendar days in the admin's own time zone (tz = JavaScript getTimezoneOffset()).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? user, [FromQuery] string? action, [FromQuery] string? from,
        [FromQuery] string? to, [FromQuery] string? q, [FromQuery] int tz = 0, [FromQuery] int page = 1)
    {
        tz = Math.Clamp(tz, -840, 840);
        var query = _db.AuditLogs.AsNoTracking().AsQueryable();

        if (!Blank(user)) { var u = user!.Trim(); query = query.Where(l => l.UserName == u); }
        if (!Blank(action)) { var a = action!.Trim().ToLowerInvariant(); query = query.Where(l => l.Action == a); }
        if (DateTime.TryParse(from, out var f))
        {
            var start = DateTime.SpecifyKind(f.Date, DateTimeKind.Utc).AddMinutes(tz);
            query = query.Where(l => l.CreatedAt >= start);
        }
        if (DateTime.TryParse(to, out var t))
        {
            var end = DateTime.SpecifyKind(t.Date, DateTimeKind.Utc).AddDays(1).AddMinutes(tz);
            query = query.Where(l => l.CreatedAt < end);
        }
        if (!Blank(q))
        {
            var term = q!.Trim();
            query = query.Where(l => l.Summary.Contains(term) || l.Entity.Contains(term) || (l.EntityId != null && l.EntityId.Contains(term)));
        }

        var total = await query.CountAsync();
        page = Math.Max(1, page);
        var items = await query.OrderByDescending(l => l.CreatedAt).ThenByDescending(l => l.Id)
            .Skip((page - 1) * PageSize).Take(PageSize)
            .Select(l => new { l.Id, l.UserId, at = l.CreatedAt, l.UserName, l.Role, l.Action, l.Entity, l.EntityId, l.Summary, ip = l.IpAddress })
            .ToListAsync();

        // profile pictures of the people on this page (customer photo or employee photo), looked up once
        var ids = items.Where(i => i.UserId != null).Select(i => i.UserId!.Value).Distinct().ToList();
        var pics = new Dictionary<int, string>();
        if (ids.Count > 0)
        {
            foreach (var e in await _db.Employees.AsNoTracking().Where(x => x.UserId != null && ids.Contains(x.UserId.Value) && x.ImageUrl != null)
                         .Select(x => new { x.UserId, x.ImageUrl }).ToListAsync())
                pics[e.UserId!.Value] = e.ImageUrl!;
            foreach (var c in await _db.Customers.AsNoTracking().Where(x => x.UserId != null && ids.Contains(x.UserId.Value) && x.ImageUrl != null)
                         .Select(x => new { x.UserId, x.ImageUrl }).ToListAsync())
                pics[c.UserId!.Value] = c.ImageUrl!;
        }
        var rows = items.Select(i => new
        {
            i.Id, i.at, i.UserName, i.Role, i.Action, i.Entity, i.EntityId, i.Summary, i.ip,
            image = i.UserId != null && pics.TryGetValue(i.UserId.Value, out var u) ? Ui.AssetIfSeed(u) : null,
            initials = Fmt.Initials(i.UserName),
        });
        return Ok(new { total, page, pageSize = PageSize, items = rows });
    }
}
