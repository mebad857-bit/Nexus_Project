using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusCommunication.Data;
using NexusCommunication.Models;
using NexusCommunication.Services;

namespace NexusCommunication.Controllers;

/// <summary>"My account" page - available to every signed-in role (profile summary + change password)</summary>
[Authorize]
public class AccountController : Controller
{
    private readonly AppDbContext _db;
    public AccountController(AppDbContext db) => _db = db;

    [HttpGet("account")]
    public async Task<IActionResult> Index()
    {
        var s = User.ToSession();
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == s.Id);
        if (user == null) return Redirect("/login");

        var vm = new AccountVm { Name = user.Name, Username = user.Username, Role = user.Role, Scope = s.ScopeName, Since = user.CreatedAt };
        if (user.Role == AppRoles.Customer)
        {
            var c = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == user.Id);
            vm.Email = c?.Email; vm.Phone = c?.Phone;
            if (c != null) { vm.ImageUrl = c.ImageUrl; vm.ImageResource = "customers"; vm.ImageRecordId = c.Id; }
        }
        else
        {
            var e = await _db.Employees.AsNoTracking().Include(x => x.Shop).FirstOrDefaultAsync(x => x.UserId == user.Id);
            if (e != null)
            {
                vm.Email = e.Email; vm.Phone = e.Phone; vm.Designation = e.Designation; vm.EmpCode = e.EmpCode;
                vm.Since = e.JoinedAt;
                vm.Scope ??= e.Shop?.Name;
                vm.ImageUrl = e.ImageUrl; vm.ImageResource = "employees"; vm.ImageRecordId = e.Id;
            }
        }
        return View(vm);
    }

    /// <summary>"Security" page of the account: change password (old password + strength meter)</summary>
    [HttpGet("account/security")]
    public async Task<IActionResult> Security()
    {
        var s = User.ToSession();
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == s.Id);
        if (user == null) return Redirect("/login");
        return View(new AccountVm { Name = user.Name, Username = user.Username, Role = user.Role, Scope = s.ScopeName, Since = user.CreatedAt });
    }
}
