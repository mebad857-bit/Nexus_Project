using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusCommunication.Data;
using NexusCommunication.Models;
using NexusCommunication.Services;

namespace NexusCommunication.Controllers;

/// <summary>Printable invoice / receipt page. Staff can open any bill, customers only their own.</summary>
[Authorize]
public class InvoiceController : Controller
{
    private readonly AppDbContext _db;
    public InvoiceController(AppDbContext db) => _db = db;

    [HttpGet("invoice/{id:int}")]
    public async Task<IActionResult> Show(int id)
    {
        var bill = await _db.Bills.AsNoTracking()
            .Include(b => b.Payments)
            .Include(b => b.Connection).ThenInclude(c => c.Customer)
            .Include(b => b.Connection).ThenInclude(c => c.Plan)
            .FirstOrDefaultAsync(b => b.Id == id);
        if (bill == null) return NotFound();

        var s = User.ToSession();
        if (s.Role == AppRoles.Customer && bill.Connection.Customer.UserId != s.Id)
            return Forbid();

        return View(bill);
    }
}
