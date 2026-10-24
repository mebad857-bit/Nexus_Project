using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusCommunication.Data;
using NexusCommunication.Models;
using NexusCommunication.Services;

namespace NexusCommunication.Controllers;

/// <summary>Printable receipt of a successful DEMO online payment. Customers can open their own, accounts / admin staff any.</summary>
[Authorize]
public class ReceiptController : Controller
{
    private readonly AppDbContext _db;
    public ReceiptController(AppDbContext db) => _db = db;

    [HttpGet("receipt/{txn}")]
    public async Task<IActionResult> Show(string txn)
    {
        var op = await _db.OnlinePayments.AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Bill).ThenInclude(b => b.Connection).ThenInclude(c => c.Plan)
            .FirstOrDefaultAsync(o => o.TransactionId == txn && o.Status == "success");
        if (op == null) return NotFound();

        var s = User.ToSession();
        var allowed = s.Role switch
        {
            AppRoles.Customer => op.Customer.UserId == s.Id,
            AppRoles.Admin or AppRoles.Accounts => true,
            _ => false,
        };
        if (!allowed) return NotFound();   // do not reveal that the receipt exists

        return View(op);
    }
}
