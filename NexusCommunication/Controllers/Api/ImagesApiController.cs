using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusCommunication.Data;
using NexusCommunication.Models;
using NexusCommunication.Services;

namespace NexusCommunication.Controllers.Api;

/// <summary>
/// Picture CRUD for every record that can carry an image.
///   POST   /api/images/{resource}/{id}   multipart/form-data, field "file"  -> create or replace the picture
///   DELETE /api/images/{resource}/{id}                                       -> remove the picture
/// resource = customers | employees | vendors | equipment | shops | plans
/// Same permission rules as /api/crud, plus: a customer may change their own profile picture and any employee their own.
/// </summary>
[ApiController]
[Route("api/images/{resource}/{id:int}")]
public class ImagesApiController : ApiBase
{
    private static readonly Dictionary<string, string[]> WriteRoles = new()
    {
        ["customers"] = new[] { AppRoles.Admin, AppRoles.Retail },
        ["employees"] = new[] { AppRoles.Admin },
        ["vendors"] = new[] { AppRoles.Admin },
        ["equipment"] = new[] { AppRoles.Admin, AppRoles.Technical },
        ["shops"] = new[] { AppRoles.Admin },
        ["plans"] = new[] { AppRoles.Admin },
    };

    private readonly AppDbContext _db;
    private readonly ImageStore _images;
    private readonly AuditService _audit;
    public ImagesApiController(AppDbContext db, ImageStore images, AuditService audit) { _db = db; _images = images; _audit = audit; }

    private static string Kind(string resource) => resource switch
    {
        "customers" => "Customer", "employees" => "Employee", "vendors" => "Vendor", "equipment" => "Equipment",
        "shops" => "Retail shop", "plans" => "Plan", _ => resource,
    };

    [HttpPost]
    [RequestSizeLimit(4 * 1024 * 1024)]
    public async Task<IActionResult> Upload(string resource, int id, IFormFile? file)
    {
        var (entity, setter, getter, err) = await Resolve(resource, id);
        if (err != null) return err;

        var (path, error) = await _images.SaveAsync(file, ImageStore.Folders[resource]);
        if (error != null) return Err(400, error);

        var old = getter!();
        setter!(path);
        await _db.SaveChangesAsync();
        _images.Delete(old);
        await _audit.LogAsync(AuditAction.Image, Kind(resource), id.ToString(), $"Picture {(old == null ? "added to" : "replaced on")} {AuditService.Describe(Kind(resource), entity!)}");
        return Ok(new { ok = true, imageUrl = path });
    }

    [HttpDelete]
    public async Task<IActionResult> Remove(string resource, int id)
    {
        var (entity, setter, getter, err) = await Resolve(resource, id);
        if (err != null) return err;

        var old = getter!();
        setter!(null);
        await _db.SaveChangesAsync();
        _images.Delete(old);
        await _audit.LogAsync(AuditAction.Image, Kind(resource), id.ToString(), $"Picture removed from {AuditService.Describe(Kind(resource), entity!)}");
        return Ok(new { ok = true });
    }

    /// <summary>finds the record, checks the caller may change its picture, returns accessors for ImageUrl</summary>
    private async Task<(object? Entity, Action<string?>? Set, Func<string?>? Get, IActionResult? Error)> Resolve(string resource, int id)
    {
        if (!WriteRoles.TryGetValue(resource, out var roles)) return (null, null, null, Err(404, "Unknown resource"));
        if (User.Identity?.IsAuthenticated != true) return (null, null, null, Err(401, "Not authenticated"));
        var s = Session;

        switch (resource)
        {
            case "customers":
            {
                var e = await _db.Customers.FirstOrDefaultAsync(x => x.Id == id);
                if (e == null) return (null, null, null, Err(404, "Record not found"));
                var own = s.Role == AppRoles.Customer && e.UserId == s.Id;
                if (!own && !roles.Contains(s.Role)) return (null, null, null, Err(403, "Forbidden"));
                return (e, v => e.ImageUrl = v, () => e.ImageUrl, null);
            }
            case "employees":
            {
                var e = await _db.Employees.FirstOrDefaultAsync(x => x.Id == id);
                if (e == null) return (null, null, null, Err(404, "Record not found"));
                var own = e.UserId != null && e.UserId == s.Id;
                if (!own && !roles.Contains(s.Role)) return (null, null, null, Err(403, "Forbidden"));
                return (e, v => e.ImageUrl = v, () => e.ImageUrl, null);
            }
        }

        if (!roles.Contains(s.Role)) return (null, null, null, Err(403, "Forbidden"));
        switch (resource)
        {
            case "vendors": { var e = await _db.Vendors.FindAsync(id); if (e == null) break; return (e, v => e.ImageUrl = v, () => e.ImageUrl, null); }
            case "equipment": { var e = await _db.Equipment.FindAsync(id); if (e == null) break; return (e, v => e.ImageUrl = v, () => e.ImageUrl, null); }
            case "shops": { var e = await _db.RetailShops.FindAsync(id); if (e == null) break; return (e, v => e.ImageUrl = v, () => e.ImageUrl, null); }
            case "plans": { var e = await _db.Plans.FindAsync(id); if (e == null) break; return (e, v => e.ImageUrl = v, () => e.ImageUrl, null); }
        }
        return (null, null, null, Err(404, "Record not found"));
    }
}
