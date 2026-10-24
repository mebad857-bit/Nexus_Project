using Microsoft.EntityFrameworkCore;
using NexusCommunication.Data;

namespace NexusCommunication.Services;

/// <summary>Generates the human-readable ids used across the system (unique indexes in SQL Server guard against races).</summary>
public class IdGenerator
{
    private readonly AppDbContext _db;
    public IdGenerator(AppDbContext db) => _db = db;

    private static string Pad(long n, int width) => n.ToString().PadLeft(width, '0');

    /// <summary>Order id: letter (D/B/T) + 10 digit serial  ->  D0000000001</summary>
    public async Task<string> NextOrderNoAsync(string type)
    {
        var letter = Fmt.TypeLetter(type);
        var last = await _db.Orders.Where(o => o.OrderNo.StartsWith(letter))
            .OrderByDescending(o => o.OrderNo).Select(o => o.OrderNo).FirstOrDefaultAsync();
        long max = last != null && long.TryParse(last.AsSpan(1), out var n) ? n : 0;
        return letter + Pad(max + 1, 10);
    }

    /// <summary>Account id: letter + 3 digit city code + 12 digit serial  ->  B101000000000001</summary>
    public async Task<string> NextAccountIdAsync(string type, string cityCode)
    {
        var prefix = Fmt.TypeLetter(type) + cityCode;
        var last = await _db.Connections.Where(c => c.AccountId.StartsWith(prefix))
            .OrderByDescending(c => c.AccountId).Select(c => c.AccountId).FirstOrDefaultAsync();
        long max = last != null && long.TryParse(last.AsSpan(prefix.Length), out var n) ? n : 0;
        return prefix + Pad(max + 1, 12);
    }

    /// <summary>Reserve several consecutive account ids (bulk orders get one connection per line)</summary>
    public async Task<List<string>> NextAccountIdsAsync(string type, string cityCode, int count)
    {
        var prefix = Fmt.TypeLetter(type) + cityCode;
        var last = await _db.Connections.Where(c => c.AccountId.StartsWith(prefix))
            .OrderByDescending(c => c.AccountId).Select(c => c.AccountId).FirstOrDefaultAsync();
        long max = last != null && long.TryParse(last.AsSpan(prefix.Length), out var n) ? n : 0;
        var list = new List<string>();
        for (var i = 1; i <= count; i++) list.Add(prefix + Pad(max + i, 12));
        return list;
    }

    public async Task<string> NextBillNoAsync()
    {
        var prefix = $"INV-{DateTime.UtcNow.Year}-";
        var last = await _db.Bills.Where(b => b.BillNo.StartsWith(prefix))
            .OrderByDescending(b => b.BillNo.Length).ThenByDescending(b => b.BillNo)
            .Select(b => b.BillNo).FirstOrDefaultAsync();
        long max = last != null && long.TryParse(last.AsSpan(prefix.Length), out var n) ? n : 0;
        return prefix + Pad(max + 1, 4);
    }

    public async Task<string> NextEmpCodeAsync()
    {
        var last = await _db.Employees.Where(e => e.EmpCode.StartsWith("EMP-"))
            .OrderByDescending(e => e.EmpCode.Length).ThenByDescending(e => e.EmpCode)
            .Select(e => e.EmpCode).FirstOrDefaultAsync();
        long max = last != null && long.TryParse(last.AsSpan(4), out var n) ? n : 0;
        return "EMP-" + Pad(max + 1, 4);
    }

    /// <summary>Ticket id: TK- + 6 digit serial  ->  TK-000001</summary>
    public async Task<string> NextTicketNoAsync()
    {
        var last = await _db.Tickets.Where(t => t.TicketNo.StartsWith("TK-"))
            .OrderByDescending(t => t.TicketNo.Length).ThenByDescending(t => t.TicketNo)
            .Select(t => t.TicketNo).FirstOrDefaultAsync();
        long max = last != null && long.TryParse(last.AsSpan(3), out var n) ? n : 0;
        return "TK-" + Pad(max + 1, 6);
    }
}
