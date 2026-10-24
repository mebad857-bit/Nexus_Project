using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusCommunication.Data;
using NexusCommunication.Models;
using NexusCommunication.Services;

namespace NexusCommunication.Controllers;

/// <summary>
/// "Forgot password": username or e-mail -> one-time link (valid 30 minutes) -> new password.
/// With SMTP set up (appsettings "Smtp") the link is e-mailed; without it (demo) the link is shown on screen.
/// The passwords are hashed with the same PasswordHasher the login uses.
/// </summary>
[AllowAnonymous]
public class PasswordController : Controller
{
    private readonly AppDbContext _db;
    private readonly EmailSender _mail;
    private readonly AuditService _audit;
    private readonly IConfiguration _cfg;
    public PasswordController(AppDbContext db, EmailSender mail, AuditService audit, IConfiguration cfg)
    { _db = db; _mail = mail; _audit = audit; _cfg = cfg; }

    private int ExpiryMinutes => Math.Clamp(_cfg.GetValue("PasswordReset:ExpiryMinutes", 30), 5, 1440);

    /* ---- at most 5 requests per 15 minutes for one address and for one username / e-mail ---- */
    private static readonly ConcurrentDictionary<string, List<DateTime>> Requests = new();
    private static bool TooMany(string key)
    {
        var list = Requests.GetOrAdd(key, _ => new List<DateTime>());
        lock (list)
        {
            var from = DateTime.UtcNow.AddMinutes(-15);
            list.RemoveAll(t => t < from);
            if (list.Count >= 5) return true;
            list.Add(DateTime.UtcNow);
            return false;
        }
    }

    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string Mask(string email)
    {
        var at = email.IndexOf('@');
        if (at < 1) return email;
        return email[0] + new string('*', Math.Min(at - 1, 6)) + email[at..];
    }

    private string BaseUrl() =>
        _cfg["PasswordReset:BaseUrl"] is { Length: > 0 } b ? b.TrimEnd('/') : $"{Request.Scheme}://{Request.Host}";

    /* ------------------------------------------------------------------ step 1: ask for a link */
    [HttpGet("forgot-password")]
    public IActionResult ForgotPassword()
    {
        if (User.Identity?.IsAuthenticated == true) return Redirect("/account/security");
        return View(new ForgotVm { ExpiryMinutes = ExpiryMinutes });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotVm vm)
    {
        vm.ExpiryMinutes = ExpiryMinutes;
        var id = (vm.Identifier ?? "").Trim();
        vm.Identifier = id;
        if (id.Length < 3 || id.Length > 200) { vm.Error = "Enter your username or the e-mail address on your account."; return View(vm); }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "?";
        if (TooMany("ip:" + ip) || TooMany("id:" + id.ToLowerInvariant()))
        {
            vm.Error = "Too many requests. Please wait a few minutes and try again.";
            return View(vm);
        }

        /* find the account(s): by e-mail (customer or staff record) or by username */
        var users = new List<User>();
        string? email = null;
        if (id.Contains('@'))
        {
            var cust = await _db.Customers.AsNoTracking().Include(c => c.User).Where(c => c.UserId != null && c.Email == id).Select(c => c.User!).ToListAsync();
            var emps = await _db.Employees.AsNoTracking().Include(e => e.User).Where(e => e.UserId != null && e.Email == id).Select(e => e.User!).ToListAsync();
            users = cust.Concat(emps).GroupBy(u => u.Id).Select(g => g.First()).ToList();
            email = id;
            if (users.Count > 1) { vm.Error = "More than one account uses that e-mail address. Please enter your username instead."; return View(vm); }
        }
        else
        {
            var u = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Username == id);
            if (u != null)
            {
                users.Add(u);
                email = await _db.Customers.AsNoTracking().Where(c => c.UserId == u.Id).Select(c => c.Email).FirstOrDefaultAsync();
                if (string.IsNullOrWhiteSpace(email))
                    email = await _db.Employees.AsNoTracking().Where(e => e.UserId == u.Id).Select(e => e.Email).FirstOrDefaultAsync();
            }
        }

        // nobody matched: same neutral answer as a real request, but no link exists
        if (users.Count == 0) { vm.Done = true; vm.MaskedEmail = id.Contains('@') ? Mask(id) : null; return View(vm); }

        var user = users[0];
        var demo = !_mail.Configured;
        if (!demo && string.IsNullOrWhiteSpace(email))
        {
            vm.Error = "This account has no e-mail address on file, so a link cannot be sent. Staff: ask the administrator. Customers: visit your nearest Nexus outlet.";
            return View(vm);
        }

        var now = DateTime.UtcNow;
        // an older link stops working as soon as a newer one is asked for
        await _db.PasswordResetTokens.Where(t => t.UserId == user.Id && t.UsedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsedAt, (DateTime?)now));

        var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        _db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id, TokenHash = Hash(token), CreatedAt = now, ExpiresAt = now.AddMinutes(ExpiryMinutes), RequestIp = ip.Length > 45 ? ip[..45] : ip,
        });
        await _db.SaveChangesAsync();

        var link = $"{BaseUrl()}/reset-password?token={token}";
        var actor = new SessionUser(user.Id, user.Username, user.Name, user.Role, null, null);
        if (demo)
        {
            vm.Link = link;     // no SMTP: show the link on screen (demo)
        }
        else
        {
            var html = $"<p>Hello {WebUtility.HtmlEncode(user.Name)},</p>" +
                       $"<p>We received a request to reset your Nexus password. This link works once and expires in {ExpiryMinutes} minutes:</p>" +
                       $"<p><a href=\"{link}\">Reset my password</a></p>" +
                       "<p>If you did not ask for this, you can ignore this e-mail - your password stays as it is.</p>";
            if (!await _mail.SendAsync(email!, "Reset your Nexus password", html))
            {
                vm.Error = "We could not send the e-mail right now. Please try again in a little while.";
                return View(vm);
            }
            vm.MaskedEmail = Mask(email!);
        }
        await _audit.LogAsync(actor, AuditAction.Update, "Account", user.Id.ToString(),
            $"Password reset link requested for {user.Username}" + (demo ? " (shown on screen - no SMTP)" : " (sent by e-mail)"));
        vm.Done = true;
        return View(vm);
    }

    /* ------------------------------------------------------------------ step 2: choose the new password */
    private async Task<(PasswordResetToken? Token, User? User)> FindAsync(string? token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 100) return (null, null);
        var h = Hash(token.Trim());
        var now = DateTime.UtcNow;
        var t = await _db.PasswordResetTokens.FirstOrDefaultAsync(x => x.TokenHash == h && x.UsedAt == null && x.ExpiresAt > now);
        if (t == null) return (null, null);
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == t.UserId);
        return u == null ? (null, null) : (t, u);
    }

    [HttpGet("reset-password")]
    public async Task<IActionResult> ResetPassword(string? token)
    {
        var (t, u) = await FindAsync(token);
        return View(new ResetVm { Token = token ?? "", Valid = t != null, Username = u?.Username });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetVm vm)
    {
        var password = vm.Password ?? "";
        var confirm = vm.Confirm ?? "";
        vm.Password = ""; vm.Confirm = "";

        var (t, user) = await FindAsync(vm.Token);
        if (t == null || user == null) { vm.Valid = false; return View(vm); }
        vm.Valid = true; vm.Username = user.Username;

        var problem = PasswordRules.Check(password, user.Username);
        if (problem != null) { vm.Error = problem; return View(vm); }
        if (password != confirm) { vm.Error = "The two passwords do not match."; return View(vm); }

        var now = DateTime.UtcNow;
        user.PasswordHash = PasswordHasher.Hash(password);
        t.UsedAt = now;
        _db.Notifications.Add(new Notification
        {
            UserId = user.Id, Type = NotifType.Info, Title = "Password changed", Link = "/account/security",
            Message = "Your password was reset with a reset link. If this was not you, change it again and tell the administrator.",
        });
        await _db.SaveChangesAsync();
        // any other link still open for this account is now useless
        await _db.PasswordResetTokens.Where(x => x.UserId == user.Id && x.UsedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.UsedAt, (DateTime?)now));

        AuthController.ClearLock(user.Username);
        await _audit.LogAsync(new SessionUser(user.Id, user.Username, user.Name, user.Role, null, null),
            AuditAction.Update, "Account", user.Id.ToString(), $"{user.Name} reset their password with a reset link");
        vm.Done = true;
        return View(vm);
    }
}
