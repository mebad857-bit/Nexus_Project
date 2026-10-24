using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusCommunication.Data;
using NexusCommunication.Models;
using NexusCommunication.Services;

namespace NexusCommunication.Controllers;

[AllowAnonymous]
public class HomeController : Controller
{
    private readonly AppDbContext _db;
    public HomeController(AppDbContext db) => _db = db;

    [HttpGet("/")]
    public async Task<IActionResult> Index()
    {
        var plans = await _db.Plans.AsNoTracking().Where(p => p.Active).OrderBy(p => p.Id).ToListAsync();
        var shops = await _db.RetailShops.AsNoTracking().OrderBy(s => s.Id).ToListAsync();
        var quotes = await _db.Feedbacks.AsNoTracking().OrderByDescending(f => f.CreatedAt).Take(6).ToListAsync();
        var connCount = await _db.Connections.CountAsync();
        var orderCount = await _db.Orders.CountAsync();
        return View(new HomeVm(plans, shops, quotes, connCount, orderCount, shops.Count, Fmt.Cities.Length));
    }

    [HttpGet("faq")]
    public async Task<IActionResult> FaqPage()
    {
        var faqs = await _db.Faqs.AsNoTracking().Where(f => f.Active).OrderBy(f => f.SortOrder).ThenBy(f => f.Id).ToListAsync();
        return View("Faq", faqs);
    }

    [HttpGet("contact")]
    public IActionResult ContactPage() => View("Contact");

    [HttpGet("plans")]
    public async Task<IActionResult> Plans()
    {
        var plans = await _db.Plans.AsNoTracking().Where(p => p.Active).OrderBy(p => p.Id).ToListAsync();
        return View(plans);
    }

    /// <summary>Public track &amp; trace: quick lookup by order/account id, or advanced field search.</summary>
    [HttpGet("track")]
    public async Task<IActionResult> Track(string? q, string? name, string? phone, string? type, string? from, string? to, string? adv)
    {
        var vm = new TrackVm
        {
            Q = q?.Trim() ?? "",
            Advanced = adv == "1",
            IsStaff = User.Identity?.IsAuthenticated == true && User.ToSession().Role != AppRoles.Customer,
        };
        var id = vm.Q.ToUpperInvariant();

        /* ---------- quick lookup by unique id ---------- */
        if (id.Length > 0)
        {
            var isAccount = Regex.IsMatch(id, @"^[DBT]\d{15}$");
            var isOrder = Regex.IsMatch(id, @"^[DBT]\d{10}$");

            if (isAccount)
            {
                var conn = await _db.Connections.AsNoTracking()
                    .Include(c => c.Customer).Include(c => c.Plan)
                    .FirstOrDefaultAsync(c => c.AccountId == id);
                if (conn == null) return Fail(vm, $"No connection found for account id {id}");

                var openBills = await _db.Bills.AsNoTracking()
                    .Where(b => b.ConnectionId == conn.Id && b.Status != BillStatus.Paid).ToListAsync();
                vm.Kind = "connection";
                vm.Connection = conn;
                vm.Due = openBills.Sum(b => b.DueAmount);
                vm.OpenBills = openBills.Count;
                vm.RecentBills = await _db.Bills.AsNoTracking().Where(b => b.ConnectionId == conn.Id)
                    .OrderByDescending(b => b.CreatedAt).Take(3).ToListAsync();
                vm.LastPayment = await _db.Payments.AsNoTracking().Where(p => p.Bill.ConnectionId == conn.Id)
                    .OrderByDescending(p => p.CreatedAt).FirstOrDefaultAsync();
                return View(vm);
            }

            if (isOrder || id.Length <= 11)
            {
                var order = await _db.Orders.AsNoTracking()
                    .Include(o => o.Customer).Include(o => o.Plan)
                    .FirstOrDefaultAsync(o => o.OrderNo == id);
                if (order == null) return Fail(vm, $"No order found for id {id}");

                vm.Kind = "order";
                vm.Order = order;
                vm.Connection = await _db.Connections.AsNoTracking().FirstOrDefaultAsync(c => c.OrderId == order.Id);
                return View(vm);
            }

            return Fail(vm, "Enter a valid Order ID (e.g. B0000000001) or Account ID (e.g. B101000000000001)");
        }

        /* ---------- advanced search (orders AND connections) ---------- */
        if (vm.Advanced)
        {
            name = name?.Trim(); phone = phone?.Trim(); type = type?.Trim();
            var orderQuery = _db.Orders.AsNoTracking().Include(o => o.Customer).Include(o => o.Plan).AsQueryable();
            var connQuery = _db.Connections.AsNoTracking().Include(c => c.Customer).Include(c => c.Plan).AsQueryable();
            var any = false;

            if (!string.IsNullOrEmpty(name))
            {
                orderQuery = orderQuery.Where(o => o.Customer.Name.Contains(name));
                connQuery = connQuery.Where(c => c.Customer.Name.Contains(name));
                any = true;
            }
            if (!string.IsNullOrEmpty(phone))
            {
                orderQuery = orderQuery.Where(o => o.Customer.Phone.Contains(phone) || o.Customer.AltPhone!.Contains(phone));
                connQuery = connQuery.Where(c => c.Customer.Phone.Contains(phone) || c.Customer.AltPhone!.Contains(phone));
                any = true;
            }
            if (!string.IsNullOrEmpty(type) && ConnTypes.All.Contains(type))
            {
                orderQuery = orderQuery.Where(o => o.ConnectionType == type);
                connQuery = connQuery.Where(c => c.ConnectionType == type);
                any = true;
            }
            if (DateTime.TryParse(from, out var f))
            {
                var start = DateTime.SpecifyKind(f.Date, DateTimeKind.Utc);
                orderQuery = orderQuery.Where(o => o.CreatedAt >= start);
                connQuery = connQuery.Where(c => c.ActivatedAt >= start);
                any = true;
            }
            if (DateTime.TryParse(to, out var t))
            {
                var end = DateTime.SpecifyKind(t.Date.AddDays(1), DateTimeKind.Utc);
                orderQuery = orderQuery.Where(o => o.CreatedAt < end);
                connQuery = connQuery.Where(c => c.ActivatedAt < end);
                any = true;
            }
            if (!any) return Fail(vm, "Provide at least one search field — name, contact number, type, or a date range.");

            vm.Kind = "list";
            vm.Results = await orderQuery.OrderByDescending(o => o.CreatedAt).Take(50).ToListAsync();
            vm.ConnResults = await connQuery.OrderByDescending(c => c.ActivatedAt).Take(50).ToListAsync();
        }

        return View(vm);
    }

    private IActionResult Fail(TrackVm vm, string message)
    {
        vm.Kind = "error";
        vm.Error = message;
        return View(vm);
    }

    [Route("error")]
    [IgnoreAntiforgeryToken]
    public IActionResult Error(int? code) => View(new ErrorVm { Code = code is >= 400 and < 600 ? code.Value : 500 });
}
