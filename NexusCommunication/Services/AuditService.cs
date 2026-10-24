using Microsoft.EntityFrameworkCore;
using NexusCommunication.Data;
using NexusCommunication.Models;

namespace NexusCommunication.Services;

/// <summary>
/// Writes the admin "Activity log". Call it AFTER the change has been saved successfully.
/// It never throws: a logging problem must not undo or fail the action the person just did.
/// </summary>
public class AuditService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;
    private readonly ILogger<AuditService> _log;

    public AuditService(AppDbContext db, IHttpContextAccessor http, ILogger<AuditService> log)
    {
        _db = db; _http = http; _log = log;
    }

    /// <summary>Log an action done by the signed-in person of the current request</summary>
    public Task LogAsync(string action, string entity, string? entityId, string summary)
    {
        var ctx = _http.HttpContext;
        var user = ctx?.User.Identity?.IsAuthenticated == true ? ctx.User.ToSession() : null;
        return LogAsync(user, action, entity, entityId, summary);
    }

    /// <summary>Log with an explicit actor (used at login, when the cookie is not set yet)</summary>
    public async Task LogAsync(SessionUser? actor, string action, string entity, string? entityId, string summary)
    {
        try
        {
            _db.AuditLogs.Add(new AuditLog
            {
                UserId = actor?.Id > 0 ? actor.Id : null,
                UserName = Cut(actor?.Name ?? "Visitor", 200),
                Role = Cut(actor?.Role ?? "", 20),
                Action = Cut(action, 30),
                Entity = Cut(entity, 50),
                EntityId = entityId == null ? null : Cut(entityId, 50),
                Summary = Cut(summary, 1500),
                IpAddress = _http.HttpContext?.Connection.RemoteIpAddress?.ToString(),
            });
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Audit log entry could not be written");
            foreach (var e in _db.ChangeTracker.Entries<AuditLog>().Where(e => e.State == EntityState.Added).ToList())
                e.State = EntityState.Detached;
        }
    }

    /// <summary>"Plan 'Home 50' (#12)" - a readable name for a record</summary>
    public static string Describe(string kind, object entity)
    {
        var t = entity.GetType();
        string? P(string n) => t.GetProperty(n)?.GetValue(entity)?.ToString();
        var name = P("Name") ?? P("Question") ?? P("OrderNo") ?? P("EmpCode");
        var id = P("Id");
        return name != null ? $"{kind} '{Cut(name, 80)}'{(id != null && id != "0" ? $" (#{id})" : "")}" : $"{kind} #{id}";
    }

    /// <summary>Old -> new values of the properties EF sees as modified (never includes passwords)</summary>
    public static string Changes(AppDbContext db, object entity)
    {
        db.ChangeTracker.DetectChanges();
        var parts = db.Entry(entity).Properties
            .Where(p => p.IsModified && p.Metadata.Name is not ("Id" or "PasswordHash" or "RowVersion"))
            .Select(p => $"{p.Metadata.Name}: {Show(p.OriginalValue)} → {Show(p.CurrentValue)}")
            .ToList();
        return parts.Count == 0 ? "" : string.Join("; ", parts);
    }

    private static string Show(object? v) => v switch
    {
        null => "(empty)",
        string s when s.Length == 0 => "(empty)",
        string s => Cut(s, 60),
        bool b => b ? "yes" : "no",
        DateTime d => d.ToString("yyyy-MM-dd HH:mm"),
        _ => Cut(Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture) ?? "", 60),
    };

    private static string Cut(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";
}
