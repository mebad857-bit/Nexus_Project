using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusCommunication.Data;
using NexusCommunication.Models;

namespace NexusCommunication.Controllers;

[Authorize(Roles = AppRoles.Accounts)]
public class AccountsDeskController : Controller
{
    private readonly AppDbContext _db;
    public AccountsDeskController(AppDbContext db) => _db = db;

    [HttpGet("accounts")]
    public async Task<IActionResult> Index()
    {
        var billable = await _db.Connections.AsNoTracking().Include(c => c.Customer).Include(c => c.Plan).Include(c => c.Order)
            .Where(c => c.Status != ConnStatus.PermanentlyInactive).OrderByDescending(c => c.CreatedAt).Take(300).ToListAsync();
        var bills = await _db.Bills.AsNoTracking().Include(b => b.Connection).ThenInclude(c => c.Customer)
            .Include(b => b.Connection).ThenInclude(c => c.Plan)
            .OrderByDescending(b => b.CreatedAt).Take(250).ToListAsync();
        var payments = await _db.Payments.AsNoTracking().Include(p => p.Bill).ThenInclude(b => b.Connection).ThenInclude(c => c.Customer)
            .OrderByDescending(p => p.CreatedAt).Take(120).ToListAsync();

        // totals are computed over ALL open bills / payments, not only the rows shown in the tables
        var openBills = await _db.Bills.AsNoTracking().Include(b => b.Connection).ThenInclude(c => c.Customer)
            .Where(b => b.Status != BillStatus.Paid).ToListAsync();
        var now = DateTime.UtcNow;

        var dueRows = openBills.GroupBy(b => b.Connection.CustomerId)
            .Select(g => new DueRow(
                g.First().Connection.Customer, g.Count(), g.Sum(x => x.DueAmount),
                g.Min(x => x.DueDate), g.Any(x => x.DueDate != null && x.DueDate < now)))
            .OrderByDescending(r => r.Due).ToList();

        // collections (payments received) vs dues (still unpaid on the invoices raised) for each of the last 6 months
        var monthly = await AdminController.MonthlySeries(_db);
        var firstMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-5);
        var collectionsVsDues = monthly.Select((m, i) =>
        {
            var start = firstMonth.AddMonths(i);
            var dues = openBills.Where(b => b.CreatedAt.Year == start.Year && b.CreatedAt.Month == start.Month).Sum(b => b.DueAmount);
            return new CollectionPoint(m.Label, m.Collected, dues);
        }).ToList();

        var vm = new AccountsVm
        {
            Billable = billable, Bills = bills, Payments = payments,
            Invoices = await _db.Bills.CountAsync(),
            Open = openBills.Count,
            Overdue = openBills.Count(b => b.DueDate != null && b.DueDate < now),
            Outstanding = openBills.Sum(b => b.DueAmount),
            Collected = await _db.Payments.SumAsync(p => p.Amount),
            AtCounter = await _db.Payments.Where(p => p.Channel == "retail").SumAsync(p => p.Amount),
            AtOffice = await _db.Payments.Where(p => p.Channel == "office").SumAsync(p => p.Amount),
            AtOnline = await _db.Payments.Where(p => p.Channel == "online").SumAsync(p => p.Amount),
            DueRows = dueRows,
            DueByConnection = openBills.GroupBy(b => b.ConnectionId).ToDictionary(g => g.Key, g => g.Sum(x => x.DueAmount)),
            Monthly = monthly,
            CollectionsVsDues = collectionsVsDues,
        };
        return View(vm);
    }
}
