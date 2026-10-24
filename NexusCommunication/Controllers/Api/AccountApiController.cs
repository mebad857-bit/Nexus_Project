using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusCommunication.Data;
using NexusCommunication.Models;
using NexusCommunication.Services;

namespace NexusCommunication.Controllers.Api;

public class ChangePasswordRequest
{
    public string? CurrentPassword { get; set; }
    public string? NewPassword { get; set; }
}

[ApiController]
[Authorize]
[Route("api/account")]
public class AccountApiController : ApiBase
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;
    public AccountApiController(AppDbContext db, AuditService audit) { _db = db; _audit = audit; }

    [HttpPost("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        var current = req.CurrentPassword ?? "";
        var next = req.NewPassword ?? "";
        if (current == "" || next == "") return Err(400, "Both the current and the new password are required");
        if (next == current) return Err(400, "Choose a password different from the current one");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == Session.Id);
        if (user == null) return Err(404, "Account not found");
        var problem = PasswordRules.Check(next, user.Username);
        if (problem != null) return Err(400, problem);
        if (!PasswordHasher.Verify(current, user.PasswordHash)) return Err(403, "The current password is not correct");

        user.PasswordHash = PasswordHasher.Hash(next);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(AuditAction.Update, "Account", user.Id.ToString(), $"{user.Name} changed their password");
        return Ok(new { ok = true });
    }
}
