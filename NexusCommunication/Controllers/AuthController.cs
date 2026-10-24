using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusCommunication.Data;
using NexusCommunication.Models;
using NexusCommunication.Services;

namespace NexusCommunication.Controllers;

public class AuthController : Controller
{
    private readonly AppDbContext _db;
    private readonly ImageStore _images;
    private readonly AuditService _audit;
    public AuthController(AppDbContext db, ImageStore images, AuditService audit) { _db = db; _images = images; _audit = audit; }

    /* ---- simple brute-force guard: 5 wrong passwords lock the username for 5 minutes ---- */
    private sealed class Attempt { public int Count; public DateTime LockedUntil; }
    private static readonly ConcurrentDictionary<string, Attempt> Attempts = new(StringComparer.OrdinalIgnoreCase);
    private const int MaxAttempts = 5;
    private static readonly TimeSpan LockFor = TimeSpan.FromMinutes(5);

    private static bool IsLocked(string username, out int minutesLeft)
    {
        minutesLeft = 0;
        if (!Attempts.TryGetValue(username, out var a) || a.LockedUntil <= DateTime.UtcNow) return false;
        minutesLeft = (int)Math.Ceiling((a.LockedUntil - DateTime.UtcNow).TotalMinutes);
        return true;
    }

    /// <summary>A successful password reset also lifts the "too many wrong passwords" lock for that username</summary>
    public static void ClearLock(string username) => Attempts.TryRemove(username, out _);

    private static void RegisterFailure(string username)
    {
        var a = Attempts.GetOrAdd(username, _ => new Attempt());
        lock (a)
        {
            if (a.LockedUntil <= DateTime.UtcNow && a.LockedUntil != default) { a.Count = 0; a.LockedUntil = default; }
            a.Count++;
            if (a.Count >= MaxAttempts) { a.LockedUntil = DateTime.UtcNow.Add(LockFor); a.Count = 0; }
        }
    }

    // no-store: browser must never keep/restore a cached login page (stops old credentials reappearing after logout / back button)
    [AllowAnonymous]
    [HttpGet("login")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
            return Redirect(Fmt.RoleHome(User.ToSession().Role));
        return View(new LoginVm());
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Login(LoginVm vm, string? returnUrl = null)
    {
        var username = (vm.Username ?? "").Trim();
        var password = vm.Password ?? "";
        if (username == "" || password == "")
        {
            vm.Error = "Username and password are required";
            return View(vm);
        }

        if (IsLocked(username, out var minutes))
        {
            vm.Error = $"Too many failed attempts. Try again in about {minutes} minute(s).";
            vm.Password = "";
            return View(vm);
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null || !PasswordHasher.Verify(password, user.PasswordHash))
        {
            RegisterFailure(username);
            vm.Error = "Invalid credentials. Please check your username and password.";
            vm.Password = "";
            return View(vm);
        }
        Attempts.TryRemove(username, out _);

        int? shopId = null;
        string? scopeName = null;
        if (user.Role != AppRoles.Customer)
        {
            var emp = await _db.Employees.Include(e => e.Shop).FirstOrDefaultAsync(e => e.UserId == user.Id);
            if (emp != null && emp.Status != "active")
            {
                vm.Error = "This staff account has been suspended. Please contact the administrator.";
                vm.Password = "";
                return View(vm);
            }
            if (user.Role == AppRoles.Retail)
            {
                shopId = emp?.ShopId;
                scopeName = emp?.Shop?.Name;
            }
        }

        await SignInAsync(user, shopId, scopeName);
        await _audit.LogAsync(new SessionUser(user.Id, user.Username, user.Name, user.Role, shopId, scopeName),
            AuditAction.Login, "Account", user.Id.ToString(), $"{user.Name} signed in ({user.Role})");

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl);
        return Redirect(Fmt.RoleHome(user.Role));
    }

    /* ------------------------------------------------------------------ customer self-registration */
    [AllowAnonymous]
    [HttpGet("register")]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return Redirect(Fmt.RoleHome(User.ToSession().Role));
        return View(new RegisterVm());
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterVm vm, IFormFile? photo)
    {
        vm.Name = (vm.Name ?? "").Trim();
        vm.Username = (vm.Username ?? "").Trim();
        vm.Phone = (vm.Phone ?? "").Trim();
        vm.Address = (vm.Address ?? "").Trim();
        vm.City = (vm.City ?? "").Trim();
        vm.Email = string.IsNullOrWhiteSpace(vm.Email) ? null : vm.Email.Trim();
        vm.OrderNo = string.IsNullOrWhiteSpace(vm.OrderNo) ? null : vm.OrderNo.Trim().ToUpperInvariant();
        var password = vm.Password ?? "";
        var confirm = vm.Confirm ?? "";
        vm.Password = ""; vm.Confirm = "";

        string? Fail(string msg) { vm.Error = msg; return msg; }

        if (vm.Name.Length < 2 || vm.Name.Length > 200) { Fail("Please enter your full name."); return View(vm); }
        if (!Regex.IsMatch(vm.Username, @"^[A-Za-z0-9._-]{3,50}$")) { Fail("Username must be 3–50 characters: letters, numbers, dot, dash or underscore."); return View(vm); }
        if (password.Length < 6) { Fail("Password must be at least 6 characters."); return View(vm); }
        if (password != confirm) { Fail("The two passwords do not match."); return View(vm); }
        if (vm.Phone.Length < 6 || vm.Phone.Length > 50) { Fail("Please enter a valid contact number."); return View(vm); }
        if (vm.Address.Length < 4 || vm.Address.Length > 400) { Fail("Please enter your address."); return View(vm); }
        var city = Fmt.Cities.FirstOrDefault(c => c.Name == vm.City);
        if (city.Code == null) { Fail("Please choose your city from the list."); return View(vm); }
        if (vm.Email != null && (vm.Email.Length > 200 || !vm.Email.Contains('@'))) { Fail("That e-mail address does not look right."); return View(vm); }
        if (await _db.Users.AnyAsync(u => u.Username == vm.Username)) { Fail("That username is already taken."); return View(vm); }

        /* Optional: claim the record a retail outlet already created for this person.
           Requires an order id that belongs to a customer with the same phone number and no login yet. */
        Customer? existing = null;
        if (vm.OrderNo != null)
        {
            var order = await _db.Orders.Include(o => o.Customer).FirstOrDefaultAsync(o => o.OrderNo == vm.OrderNo);
            if (order == null || order.Customer.UserId != null || order.Customer.Phone != vm.Phone)
            {
                Fail("We could not match that order id with your phone number. Check both, or leave the order id empty to create a fresh profile.");
                return View(vm);
            }
            existing = order.Customer;
        }

        // optional profile photo (validated by content, max 3 MB)
        string? photoPath = null;
        if (photo != null && photo.Length > 0)
        {
            var saved = await _images.SaveAsync(photo, ImageStore.Folders["customers"]);
            if (saved.Error != null) { Fail(saved.Error); return View(vm); }
            photoPath = saved.Path;
        }

        var user = new User
        {
            Username = vm.Username,
            PasswordHash = PasswordHasher.Hash(password),
            Name = vm.Name,
            Role = AppRoles.Customer,
        };
        _db.Users.Add(user);

        if (existing != null)
        {
            existing.User = user;
            if (vm.Email != null && string.IsNullOrEmpty(existing.Email)) existing.Email = vm.Email;
            if (photoPath != null && string.IsNullOrEmpty(existing.ImageUrl)) existing.ImageUrl = photoPath; else if (photoPath != null) { _images.Delete(photoPath); photoPath = null; }
        }
        else
        {
            _db.Customers.Add(new Customer
            {
                User = user,
                Name = vm.Name,
                Email = vm.Email,
                Phone = vm.Phone,
                Address = vm.Address,
                City = city.Name,
                CityCode = city.Code,
                ImageUrl = photoPath,
            });
        }

        try { await _db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            _images.Delete(photoPath);
            Fail("That username is already taken.");
            return View(vm);
        }

        await SignInAsync(user, null, null);
        await _audit.LogAsync(new SessionUser(user.Id, user.Username, user.Name, user.Role, null, null),
            AuditAction.Create, "Customer", user.Id.ToString(), $"{user.Name} registered a new customer account");
        return Redirect(Fmt.RoleHome(user.Role));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var who = User.ToSession();
            await _audit.LogAsync(who, AuditAction.Logout, "Account", who.Id.ToString(), $"{who.Name} signed out");
        }
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/login");
    }

    /// <summary>Signed in but wrong desk -> send the user to their own console (same as the original requireRole redirect)</summary>
    [AllowAnonymous]
    [HttpGet("auth/denied")]
    public IActionResult Denied() =>
        User.Identity?.IsAuthenticated == true ? Redirect(Fmt.RoleHome(User.ToSession().Role)) : Redirect("/login");

    private async Task SignInAsync(User user, int? shopId, string? scopeName)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new("username", user.Username),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Role, user.Role),
        };
        if (shopId != null) claims.Add(new Claim("shopId", shopId.Value.ToString()));
        if (scopeName != null) claims.Add(new Claim("scopeName", scopeName));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) });
    }
}
