using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusCommunication.Data;
using NexusCommunication.Models;

namespace NexusCommunication.Controllers;

[Authorize(Roles = AppRoles.Technical)]
public class TechnicalController : Controller
{
    private readonly AppDbContext _db;
    public TechnicalController(AppDbContext db) => _db = db;

    [HttpGet("technical")]
    public async Task<IActionResult> Index()
    {
        var pending = await _db.Orders.AsNoTracking().Include(o => o.Customer).Include(o => o.Plan).Include(o => o.Shop)
            .Where(o => o.Status == OrderStatus.Pending).OrderByDescending(o => o.CreatedAt).ToListAsync();
        var feasible = await _db.Orders.AsNoTracking().Include(o => o.Customer).Include(o => o.Plan)
            .Where(o => o.Status == OrderStatus.Feasible).OrderByDescending(o => o.CreatedAt).ToListAsync();
        var conns = await _db.Connections.AsNoTracking().Include(c => c.Customer).Include(c => c.Plan)
            .OrderByDescending(c => c.CreatedAt).Take(150).ToListAsync();
        var equipment = await _db.Equipment.AsNoTracking().Include(e => e.Vendor).OrderBy(e => e.Id).ToListAsync();
        var allOrders = await _db.Orders.AsNoTracking().Include(o => o.Customer).Include(o => o.Plan).Include(o => o.Shop)
            .OrderByDescending(o => o.CreatedAt).Take(300).ToListAsync();
        var plans = await _db.Plans.AsNoTracking().Where(p => p.Active).OrderBy(p => p.Id).ToListAsync();
        var vendors = await _db.Vendors.AsNoTracking().OrderBy(v => v.Name).ToListAsync();

        return View(new TechnicalVm
        {
            PendingOrders = pending, FeasibleOrders = feasible, Connections = conns, Equipment = equipment,
            AllOrders = allOrders, Plans = plans, Vendors = vendors,
            Pending = pending.Count, Feasible = feasible.Count,
            Active = conns.Count(c => c.Status == ConnStatus.Active),
            Inactive = conns.Count(c => c.Status != ConnStatus.Active),
            LowStock = equipment.Count(e => e.Stock <= e.ReorderLevel),
        });
    }
}
