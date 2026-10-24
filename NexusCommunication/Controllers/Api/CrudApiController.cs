using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusCommunication.Data;
using NexusCommunication.Models;
using NexusCommunication.Services;
using AppUser = NexusCommunication.Models.User;

namespace NexusCommunication.Controllers.Api;

/// <summary>Generic add / edit / delete for the admin registries (employees, vendors, equipment, shops, plans, customers, feedback).</summary>
[ApiController]
[Route("api/crud/{resource}")]
public class CrudApiController : ApiBase
{
    private sealed record Res(Type Type, string[] WriteRoles, string[] Fields);

    private static readonly Dictionary<string, Res> Map = new()
    {
        ["employees"] = new(typeof(Employee), new[] { AppRoles.Admin }, new[] { "name", "email", "phone", "department", "designation", "shopId", "status" }),
        ["vendors"] = new(typeof(Vendor), new[] { AppRoles.Admin }, new[] { "name", "contactPerson", "email", "phone", "address", "products" }),
        ["equipment"] = new(typeof(Equipment), new[] { AppRoles.Admin, AppRoles.Technical }, new[] { "name", "type", "vendorId", "unitPrice", "stock", "reorderLevel" }),
        ["shops"] = new(typeof(RetailShop), new[] { AppRoles.Admin }, new[] { "code", "name", "city", "address", "phone", "manager" }),
        ["plans"] = new(typeof(Plan), new[] { AppRoles.Admin }, new[] { "connectionType", "category", "name", "speed", "price", "validityLabel", "validityMonths", "deposit", "callRates", "description", "active" }),
        ["customers"] = new(typeof(Customer), new[] { AppRoles.Admin, AppRoles.Retail }, new[] { "name", "email", "phone", "address", "city", "cityCode", "idProof", "altPhone" }),
        ["feedback"] = new(typeof(Feedback), new[] { AppRoles.Admin }, new[] { "name", "contact", "connectionType", "rating", "message" }),
        ["faqs"] = new(typeof(Faq), new[] { AppRoles.Admin }, new[] { "question", "answer", "sortOrder", "active" }),
        // contact messages are created by the public form (api/contact); admins can only mark them read / unread or delete them
        ["messages"] = new(typeof(ContactMessage), new[] { AppRoles.Admin }, new[] { "isRead" }),
    };

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private readonly AppDbContext _db;
    private readonly IdGenerator _ids;
    private readonly ImageStore _images;
    private readonly AuditService _audit;
    private readonly NotificationService _notify;
    public CrudApiController(AppDbContext db, IdGenerator ids, ImageStore images, AuditService audit, NotificationService notify)
    { _db = db; _ids = ids; _images = images; _audit = audit; _notify = notify; }

    /// <summary>Friendly record type for the activity log</summary>
    private static readonly Dictionary<string, string> Kinds = new()
    {
        ["employees"] = "Employee", ["vendors"] = "Vendor", ["equipment"] = "Equipment", ["shops"] = "Retail shop", ["plans"] = "Plan",
        ["customers"] = "Customer", ["feedback"] = "Feedback", ["faqs"] = "FAQ", ["messages"] = "Contact message",
    };
    private static string IdOf(object entity) => entity.GetType().GetProperty("Id")?.GetValue(entity)?.ToString() ?? "";

    private bool IsSignedIn => User.Identity?.IsAuthenticated == true;

    /* ------------------------------------------------------------------ create */
    [HttpPost]
    public async Task<IActionResult> Create(string resource, [FromBody] JsonElement body)
    {
        if (!Map.TryGetValue(resource, out var res)) return Err(404, "Unknown resource");
        if (resource == "messages") return Err(405, "Messages are created from the public contact form");

        // feedback is open to the public (contact form); everything else needs a signed-in writer
        if (resource != "feedback")
        {
            if (!IsSignedIn) return Err(401, "Not authenticated");
            if (!res.WriteRoles.Contains(Session.Role)) return Err(403, "Forbidden");
        }

        if (resource == "employees") return await CreateEmployee(body);

        var entity = Activator.CreateInstance(res.Type)!;
        var bad = Apply(entity, body, res.Fields);
        if (bad != null) return Err(400, bad);

        if (entity is Feedback fb) fb.Rating = Math.Clamp(fb.Rating, 1, 5);
        if (entity is Customer newCust) SyncCityCode(newCust);
        var invalid = Validate(entity);
        if (invalid != null) return Err(400, invalid);

        _db.Add(entity);
        return await Save(() => Ok(new { ok = true, id = (int)(entity.GetType().GetProperty("Id")?.GetValue(entity) ?? 0) }),
            after: async () =>
            {
                if (entity is Feedback f)
                {
                    // public feedback: the admin desk is alerted (a low rating reads as a complaint)
                    await _notify.ComplaintAsync(f.Rating <= 2 ? "New complaint (low rating)" : "New customer feedback",
                        $"{f.Name} · {f.Rating}/5 — {Shorten(f.Message, 140)}", "feedback");
                    return;
                }
                await _audit.LogAsync(AuditAction.Create, Kinds[resource], IdOf(entity), $"{AuditService.Describe(Kinds[resource], entity)} created");
            });
    }

    private async Task<IActionResult> CreateEmployee(JsonElement body)
    {
        string Str(string key) => body.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()!.Trim() : "";
        var username = Str("username");
        var password = Str("password");
        if (Str("name") == "" || username == "" || password == "" || Str("department") == "")
            return Err(400, "Name, department, username and password are required");
        if (password.Length < 6) return Err(400, "Password must be at least 6 characters");
        if (username.Length > 50) return Err(400, "Username is too long");
        if (await _db.Users.AnyAsync(u => u.Username == username)) return Err(409, "That username is already taken");

        var emp = new Employee { EmpCode = await _ids.NextEmpCodeAsync(), Status = "active" };
        var bad = Apply(emp, body, Map["employees"].Fields.Where(f => f != "status").ToArray());
        if (bad != null) return Err(400, bad);
        var invalid = Validate(emp);
        if (invalid != null) return Err(400, invalid);

        emp.User = new AppUser
        {
            Username = username,
            PasswordHash = PasswordHasher.Hash(password),
            Name = emp.Name,
            Role = emp.Department, // department names equal role names (admin/accounts/technical/retail)
        };
        _db.Employees.Add(emp);
        return await Save(() => Ok(new { ok = true, id = emp.Id }),
            after: () => _audit.LogAsync(AuditAction.Create, "Employee", emp.Id.ToString(),
                $"Employee '{emp.Name}' ({emp.EmpCode}) onboarded in {emp.Department}, login '{username}'"));
    }

    /* ------------------------------------------------------------------ update */
    [HttpPatch]
    public async Task<IActionResult> Update(string resource, [FromBody] JsonElement body)
    {
        if (!Map.TryGetValue(resource, out var res)) return Err(404, "Unknown resource");
        if (!IsSignedIn) return Err(401, "Not authenticated");
        var s = Session;

        if (!body.TryGetProperty("id", out var idEl) || !idEl.TryGetInt32(out var id) || id <= 0)
            return Err(400, "id is required");

        var entity = await _db.FindAsync(res.Type, id);
        if (entity == null) return Err(404, "Record not found");

        string[] fields;
        if (resource == "customers" && s.Role == AppRoles.Customer)
        {
            // customers may only update their own contact fields
            if (((Customer)entity).UserId != s.Id) return Err(403, "Forbidden");
            fields = new[] { "phone", "email", "address", "altPhone" };
        }
        else
        {
            if (!res.WriteRoles.Contains(s.Role)) return Err(403, "Forbidden");
            fields = res.Fields;
        }

        // "receive stock": relative change is race-safe, unlike overwriting the absolute number
        if (resource == "equipment" && body.TryGetProperty("addStock", out var addEl) && addEl.TryGetInt32(out var add))
        {
            var eq = (Equipment)entity;
            if (eq.Stock + add < 0) return Err(400, "Stock cannot go below zero");
            eq.Stock += add;
            return await Save(() => Ok(new { ok = true, stock = eq.Stock }),
                after: () => _audit.LogAsync(AuditAction.Update, "Equipment", eq.Id.ToString(),
                    $"Equipment '{eq.Name}' (#{eq.Id}): stock {(add >= 0 ? "+" : "")}{add} (now {eq.Stock})"));
        }

        var touched = fields.Where(f => body.TryGetProperty(f, out _)).ToArray();
        if (touched.Length == 0) return Err(400, "Nothing to update");

        var bad = Apply(entity, body, touched);
        if (bad != null) return Err(400, bad);
        var invalid = Validate(entity);
        if (invalid != null) return Err(400, invalid);

        if (entity is Customer editedCust && touched.Contains("city")) SyncCityCode(editedCust);
        var changes = AuditService.Changes(_db, entity);   // old -> new values, read before saving

        // keep the login role in sync when an employee changes department
        if (entity is Employee emp && touched.Contains("department") && emp.UserId != null)
        {
            var u = await _db.Users.FindAsync(emp.UserId);
            if (u != null) u.Role = emp.Department;
        }

        return await Save(() => Ok(new { ok = true }), after: async () =>
        {
            if (changes.Length == 0 || resource == "messages") return;   // read / unread toggles are not worth a log line
            var kind = Kinds[resource];
            await _audit.LogAsync(AuditAction.Update, kind, id.ToString(), $"{AuditService.Describe(kind, entity)} edited — {changes}");
        });
    }

    /* ------------------------------------------------------------------ delete */
    [HttpDelete]
    public async Task<IActionResult> Delete(string resource, [FromQuery] int id)
    {
        if (!Map.TryGetValue(resource, out var res)) return Err(404, "Unknown resource");
        if (!IsSignedIn) return Err(401, "Not authenticated");
        if (!res.WriteRoles.Contains(Session.Role)) return Err(403, "Forbidden");
        if (id <= 0) return Err(400, "id query param is required");
        if (resource == "customers" && Session.Role != AppRoles.Admin)
            return Err(403, "Only the administrator can delete customer records");

        var entity = await _db.FindAsync(res.Type, id);
        if (entity == null) return Ok(new { ok = true });

        if (entity is Employee emp && emp.UserId == Session.Id)
            return Err(409, "You cannot delete your own account");

        var pictureToDelete = (entity.GetType().GetProperty("ImageUrl")?.GetValue(entity) as string);
        var kind = Kinds[resource];
        var deletedText = $"{AuditService.Describe(kind, entity)} deleted";   // described before it disappears
        _db.Remove(entity);
        if (entity is Employee e && e.UserId != null)
        {
            var user = await _db.Users.FindAsync(e.UserId);
            if (user != null)
            {
                _db.Notifications.RemoveRange(await _db.Notifications.Where(n => n.UserId == user.Id).ToListAsync());
                _db.Users.Remove(user);
            }
        }
        return await Save(() => { _images.Delete(pictureToDelete); return Ok(new { ok = true }); },
            "This record is referenced by other data and cannot be deleted (for plans, unpublish them instead).",
            after: () => _audit.LogAsync(AuditAction.Delete, kind, id.ToString(), deletedText));
    }

    /* ------------------------------------------------------------------ helpers */
    /// <summary>The 3-digit city code is part of every account id, so it must always match the chosen city.</summary>
    private static void SyncCityCode(Customer c)
    {
        var city = Fmt.Cities.FirstOrDefault(x => x.Name == c.City);
        if (city.Code != null) c.CityCode = city.Code;
    }

    private async Task<IActionResult> Save(Func<IActionResult> onOk, string? conflictMessage = null, Func<Task>? after = null)
    {
        try
        {
            await _db.SaveChangesAsync();
            if (after != null) await after();   // activity log / alerts: only once the change is really stored
            return onOk();
        }
        catch (DbUpdateException)
        {
            return Err(409, conflictMessage ?? "That value already exists or is invalid (codes and usernames must be unique).");
        }
    }

    private static string Shorten(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";

    private static string? Validate(object entity)
    {
        var results = new List<ValidationResult>();
        return Validator.TryValidateObject(entity, new ValidationContext(entity), results, true)
            ? null
            : results.First().ErrorMessage;
    }

    /// <summary>Copy whitelisted JSON fields (camelCase) onto the entity. Returns an error message or null.</summary>
    private static string? Apply(object entity, JsonElement body, string[] fields)
    {
        if (body.ValueKind != JsonValueKind.Object) return "Invalid request body";
        var type = entity.GetType();
        foreach (var field in fields)
        {
            if (!body.TryGetProperty(field, out var el)) continue;
            var prop = type.GetProperty(char.ToUpperInvariant(field[0]) + field[1..]);
            if (prop == null) continue;
            try
            {
                if (el.ValueKind == JsonValueKind.Null)
                {
                    var isNullable = !prop.PropertyType.IsValueType || Nullable.GetUnderlyingType(prop.PropertyType) != null;
                    if (isNullable) prop.SetValue(entity, null);
                    continue;
                }
                // JS may send numbers for text fields (and vice-versa) - be lenient for strings
                object? value = prop.PropertyType == typeof(string) && el.ValueKind != JsonValueKind.String
                    ? el.ToString()
                    : JsonSerializer.Deserialize(el.GetRawText(), prop.PropertyType, JsonOpts);
                if (value is string str) value = str.Trim();
                prop.SetValue(entity, value);
            }
            catch (Exception)
            {
                return $"Invalid value for '{field}'";
            }
        }
        return null;
    }
}
