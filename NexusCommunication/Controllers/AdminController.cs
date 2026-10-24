using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusCommunication.Data;
using NexusCommunication.Models;
using NexusCommunication.Services;

namespace NexusCommunication.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class AdminController : Controller
{
    private readonly AppDbContext _db;
    public AdminController(AppDbContext db) => _db = db;

    [HttpGet("admin")]
    public async Task<IActionResult> Index()
    {
        var vm = new AdminVm
        {
            Employees = await _db.Employees.AsNoTracking().Include(e => e.Shop).OrderByDescending(e => e.Id).ToListAsync(),
            Vendors = await _db.Vendors.AsNoTracking().OrderByDescending(v => v.Id).ToListAsync(),
            Equipment = await _db.Equipment.AsNoTracking().Include(e => e.Vendor).OrderByDescending(e => e.Id).ToListAsync(),
            Shops = await _db.RetailShops.AsNoTracking().OrderByDescending(s => s.Id).ToListAsync(),
            Plans = await _db.Plans.AsNoTracking().OrderByDescending(p => p.Id).ToListAsync(),
            RecentOrders = await _db.Orders.AsNoTracking().Include(o => o.Customer).Include(o => o.Plan)
                .OrderByDescending(o => o.CreatedAt).Take(7).ToListAsync(),
            Customers = await _db.Customers.AsNoTracking().OrderByDescending(c => c.Id).Take(500).ToListAsync(),
            AllOrders = await _db.Orders.AsNoTracking().Include(o => o.Customer).Include(o => o.Plan).Include(o => o.Shop)
                .OrderByDescending(o => o.CreatedAt).Take(400).ToListAsync(),
            AllConnections = await _db.Connections.AsNoTracking().Include(c => c.Customer).Include(c => c.Plan)
                .OrderByDescending(c => c.CreatedAt).Take(400).ToListAsync(),
            AllBills = await _db.Bills.AsNoTracking().Include(b => b.Connection).ThenInclude(c => c.Customer)
                .OrderByDescending(b => b.CreatedAt).Take(400).ToListAsync(),
            AllPayments = await _db.Payments.AsNoTracking().Include(p => p.Bill)
                .OrderByDescending(p => p.CreatedAt).Take(300).ToListAsync(),
            Feedbacks = await _db.Feedbacks.AsNoTracking().OrderByDescending(f => f.CreatedAt).Take(200).ToListAsync(),
            Messages = await _db.ContactMessages.AsNoTracking().OrderByDescending(m => m.CreatedAt).Take(300).ToListAsync(),
            Faqs = await _db.Faqs.AsNoTracking().OrderBy(f => f.SortOrder).ThenBy(f => f.Id).ToListAsync(),
            UnreadMessages = await _db.ContactMessages.CountAsync(m => !m.IsRead),
            OrdersCount = await _db.Orders.CountAsync(),
            PendingOrders = await _db.Orders.CountAsync(o => o.Status == OrderStatus.Pending),
            LiveConnections = await _db.Connections.CountAsync(c => c.Status == ConnStatus.Active),
            Outstanding = await _db.Bills.Where(b => b.Status != BillStatus.Paid).SumAsync(b => b.DueAmount),
            Collected = await _db.Bills.SumAsync(b => b.PaidAmount),
            StockUnits = await _db.Equipment.SumAsync(e => e.Stock),
        };

        vm.OrderStatusCounts = (await _db.Orders.GroupBy(o => o.Status)
            .Select(g => new { g.Key, Count = g.Count() }).ToListAsync()).ToDictionary(x => x.Key, x => x.Count);
        vm.ConnTypeCounts = (await _db.Connections.GroupBy(c => c.ConnectionType)
            .Select(g => new { g.Key, Count = g.Count() }).ToListAsync()).ToDictionary(x => x.Key, x => x.Count);

        /* connections per plan (bar chart) - busiest plans first, plans nobody uses yet are left out */
        var perPlan = (await _db.Connections.Where(c => c.PlanId != null)
                .GroupBy(c => c.PlanId!.Value).Select(g => new { PlanId = g.Key, Count = g.Count() }).ToListAsync())
            .ToDictionary(x => x.PlanId, x => x.Count);
        vm.PlanConnections = vm.Plans.Where(p => perPlan.ContainsKey(p.Id))
            .Select(p => new PlanCount(p.Name, p.Category, p.ConnectionType, perPlan[p.Id]))
            .OrderByDescending(x => x.Count).ThenBy(x => x.Name).Take(12).ToList();

        /* lines still waiting for hardware (orders that are pending / feasible) - drives the procurement suggestions */
        var open = await _db.Orders.Where(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Feasible)
            .Select(o => new { o.ConnectionType, o.Quantity }).ToListAsync();
        vm.OpenDemandModems = open.Where(o => o.ConnectionType != ConnTypes.Landline).Sum(o => o.Quantity);
        vm.OpenDemandPhones = open.Where(o => o.ConnectionType == ConnTypes.Landline).Sum(o => o.Quantity);

        var ratings = await _db.Feedbacks.Select(f => f.Rating).ToListAsync();
        vm.AvgRating = ratings.Count == 0 ? 0 : ratings.Average();

        vm.Monthly = await MonthlySeries(_db);
        return View(vm);
    }

    /// <summary>Invoiced vs collected for the last 6 calendar months (aggregated in memory - tiny data set)</summary>
    internal static async Task<List<MonthPoint>> MonthlySeries(AppDbContext db)
    {
        var first = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-5);
        var bills = await db.Bills.AsNoTracking().Where(b => b.CreatedAt >= first).Select(b => new { b.CreatedAt, b.Total }).ToListAsync();
        var pays = await db.Payments.AsNoTracking().Where(p => p.CreatedAt >= first).Select(p => new { p.CreatedAt, p.Amount }).ToListAsync();

        var list = new List<MonthPoint>();
        for (var i = 0; i < 6; i++)
        {
            var m = first.AddMonths(i);
            var invoiced = bills.Where(b => b.CreatedAt.Year == m.Year && b.CreatedAt.Month == m.Month).Sum(b => b.Total);
            var collected = pays.Where(p => p.CreatedAt.Year == m.Year && p.CreatedAt.Month == m.Month).Sum(p => p.Amount);
            list.Add(new MonthPoint(Fmt.ShortMonth(m), invoiced, collected));
        }
        return list;
    }
}
