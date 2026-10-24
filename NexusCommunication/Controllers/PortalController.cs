using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusCommunication.Data;
using NexusCommunication.Models;
using NexusCommunication.Services;

namespace NexusCommunication.Controllers;

[Authorize(Roles = AppRoles.Customer)]
public class PortalController : Controller
{
    private readonly AppDbContext _db;
    private readonly NotificationService _notify;
    public PortalController(AppDbContext db, NotificationService notify) { _db = db; _notify = notify; }

    [HttpGet("portal")]
    public async Task<IActionResult> Index()
    {
        var session = User.ToSession();
        var customer = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == session.Id);
        if (customer == null) return View(new PortalVm());

        var orders = await _db.Orders.AsNoTracking().Include(o => o.Plan)
            .Where(o => o.CustomerId == customer.Id).OrderByDescending(o => o.CreatedAt).ToListAsync();
        var conns = await _db.Connections.AsNoTracking().Include(c => c.Plan)
            .Where(c => c.CustomerId == customer.Id).OrderByDescending(c => c.CreatedAt).ToListAsync();
        var plans = await _db.Plans.AsNoTracking().Where(p => p.Active).OrderBy(p => p.Id).ToListAsync();

        var connIds = conns.Select(c => c.Id).ToList();
        var bills = connIds.Count == 0 ? new List<Bill>() :
            await _db.Bills.AsNoTracking().Include(b => b.Connection)
                .Where(b => connIds.Contains(b.ConnectionId)).OrderByDescending(b => b.CreatedAt).ToListAsync();

        // bills that are overdue / due within 5 days: banner on this page + a reminder in the bell (created once per bill and stage)
        var now = DateTime.UtcNow;
        var reminders = bills.Where(b => BillReminder.IsDue(b, now)).OrderBy(b => b.DueDate).ToList();
        await _notify.SyncBillRemindersAsync(session.Id);

        return View(new PortalVm
        {
            DueReminders = reminders,
            Customer = customer, OrderList = orders, ConnectionList = conns, Bills = bills, Plans = plans,
            Connections = conns.Count,
            Live = conns.Count(c => c.Status == ConnStatus.Active),
            Orders = orders.Count,
            PendingOrders = orders.Count(o => o.Status == OrderStatus.Pending),
            Dues = bills.Where(b => b.Status != BillStatus.Paid).Sum(b => b.DueAmount),
        });
    }
}
