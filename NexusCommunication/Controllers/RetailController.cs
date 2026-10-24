using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusCommunication.Data;
using NexusCommunication.Models;
using NexusCommunication.Services;

namespace NexusCommunication.Controllers;

[Authorize(Roles = AppRoles.Retail)]
public class RetailController : Controller
{
    private readonly AppDbContext _db;
    public RetailController(AppDbContext db) => _db = db;

    [HttpGet("retail")]
    public async Task<IActionResult> Index()
    {
        var session = User.ToSession();
        var shopId = session.ShopId ?? -1;

        var plans = await _db.Plans.AsNoTracking().Where(p => p.Active).OrderBy(p => p.Id).ToListAsync();
        var customers = await _db.Customers.AsNoTracking().OrderBy(c => c.Name).Take(300).ToListAsync();
        var orders = await _db.Orders.AsNoTracking().Include(o => o.Customer).Include(o => o.Plan)
            .Where(o => o.ShopId == shopId).OrderByDescending(o => o.CreatedAt).Take(200).ToListAsync();
        var conns = await _db.Connections.AsNoTracking().Include(c => c.Customer).Include(c => c.Plan).Include(c => c.Order)
            .Where(c => c.Order != null && c.Order.ShopId == shopId).OrderByDescending(c => c.CreatedAt).Take(200).ToListAsync();

        var connIds = conns.Select(c => c.Id).ToList();
        var bills = connIds.Count == 0 ? new List<Bill>() :
            await _db.Bills.AsNoTracking().Include(b => b.Connection).ThenInclude(c => c.Customer)
                .Where(b => connIds.Contains(b.ConnectionId)).OrderByDescending(b => b.CreatedAt).Take(200).ToListAsync();

        var billIds = bills.Select(b => b.Id).ToList();
        var payments = billIds.Count == 0 ? new List<Payment>() :
            await _db.Payments.AsNoTracking().Include(p => p.Bill)
                .Where(p => billIds.Contains(p.BillId)).OrderByDescending(p => p.CreatedAt).Take(120).ToListAsync();

        return View(new RetailVm
        {
            Plans = plans, Customers = customers, ShopOrders = orders, Connections = conns, Bills = bills, Payments = payments,
            Orders = orders.Count,
            Pending = orders.Count(o => o.Status == OrderStatus.Pending),
            Connected = conns.Count,
            Collected = payments.Where(p => p.Channel == "retail" && p.ReceivedBy == session.Name).Sum(p => p.Amount),
        });
    }
}
