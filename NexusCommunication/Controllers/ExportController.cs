using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusCommunication.Data;
using NexusCommunication.Models;
using NexusCommunication.Services;

namespace NexusCommunication.Controllers;

/// <summary>
/// Export of the dashboard tables as Excel (.xlsx), CSV or a print-friendly report (saved as PDF from the browser).
/// Admin sees everything, accounts sees billing data, a retail outlet only sees its own orders / invoices / receipts.
/// </summary>
[Authorize]
[Route("export")]
public class ExportController : Controller
{
    private const int MaxRows = 20000;
    private static readonly CultureInfo En = CultureInfo.GetCultureInfo("en-US");
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private static readonly Dictionary<string, string[]> Access = new()
    {
        ["orders"] = new[] { AppRoles.Admin, AppRoles.Retail },
        ["customers"] = new[] { AppRoles.Admin, AppRoles.Retail },
        ["connections"] = new[] { AppRoles.Admin, AppRoles.Retail },
        ["bills"] = new[] { AppRoles.Admin, AppRoles.Accounts, AppRoles.Retail },
        ["payments"] = new[] { AppRoles.Admin, AppRoles.Accounts, AppRoles.Retail },
        ["dues"] = new[] { AppRoles.Admin, AppRoles.Accounts },
    };

    private readonly AppDbContext _db;
    public ExportController(AppDbContext db) => _db = db;

    /* ------------------------------------------------------------ downloads */
    [HttpGet("{resource}")]
    public async Task<IActionResult> Download(string resource, string? format, string? status)
    {
        var gate = Gate(resource);
        if (gate != null) return gate;

        var sheet = await Build(resource, User.ToSession(), status);
        var name = $"nexus-{resource}-{DateTime.Now:yyyyMMdd-HHmm}";

        switch ((format ?? "xlsx").ToLowerInvariant())
        {
            case "csv":
                return File(ToCsv(sheet), "text/csv; charset=utf-8", name + ".csv");
            case "xlsx":
                return File(ToXlsx(sheet), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", name + ".xlsx");
            default:
                return BadRequest("Unknown format. Use xlsx or csv.");
        }
    }

    /// <summary>Invoice-style printable report - the browser's "Save as PDF" turns it into a PDF.</summary>
    [HttpGet("{resource}/print")]
    public async Task<IActionResult> Print(string resource, string? status, bool auto = false)
    {
        var gate = Gate(resource);
        if (gate != null) return gate;

        ViewData["Auto"] = auto;
        return View("Print", await Build(resource, User.ToSession(), status));
    }

    private IActionResult? Gate(string resource)
    {
        if (!Access.TryGetValue(resource, out var roles)) return NotFound();
        return roles.Contains(User.ToSession().Role) ? null : Forbid();
    }

    /* ------------------------------------------------------------ data */
    private async Task<ExportSheet> Build(string resource, SessionUser s, string? status)
    {
        var retail = s.Role == AppRoles.Retail;
        var shopId = s.ShopId ?? -1;
        var scope = retail ? (s.ScopeName ?? "Retail outlet") : s.Role == AppRoles.Accounts ? "Accounts department" : "All outlets · head office";
        status = string.IsNullOrWhiteSpace(status) || status == "all" ? null : status.Trim().ToLowerInvariant();
        ExportSheet Make(string title, string sub, string[] heads, List<object?[]> rows, int[]? sum = null, string? filter = null) =>
            new(resource, title, sub, heads, rows, sum ?? Array.Empty<int>(), s.Name, scope, filter);

        switch (resource)
        {
            case "orders":
            {
                var q = _db.Orders.AsNoTracking().Include(o => o.Customer).Include(o => o.Plan).Include(o => o.Shop).AsQueryable();
                if (retail) q = q.Where(o => o.ShopId == shopId);
                if (status != null) q = q.Where(o => o.Status == status);
                var list = await q.OrderByDescending(o => o.CreatedAt).Take(MaxRows).ToListAsync();
                return Make("Orders", "Every connection application with its feasibility status",
                    new[] { "Order ID", "Customer", "Phone", "Connection", "Plan", "Lines", "Discount %", "Source", "Status", "Placed" },
                    list.Select(o => new object?[]
                    {
                        o.OrderNo, o.Customer.Name, o.Customer.Phone, Fmt.TypeLabel(o.ConnectionType),
                        o.Plan == null ? "" : $"{o.Plan.Category} · {o.Plan.Name}", o.Quantity, o.DiscountPct,
                        o.Shop?.Name ?? "Online", Fmt.OrderStatus(o.Status), o.CreatedAt,
                    }).ToList(), null, status == null ? null : Fmt.OrderStatus(status));
            }

            case "customers":
            {
                var list = await _db.Customers.AsNoTracking().OrderBy(c => c.Name).Take(MaxRows).ToListAsync();
                return Make("Customers", "Everyone who holds an account or has applied for a connection",
                    new[] { "Name", "Phone", "Alternate phone", "Email", "Address", "City", "ID proof", "Joined" },
                    list.Select(c => new object?[] { c.Name, c.Phone, c.AltPhone, c.Email, c.Address, c.City, c.IdProof, c.CreatedAt }).ToList());
            }

            case "connections":
            {
                var q = _db.Connections.AsNoTracking().Include(c => c.Customer).Include(c => c.Plan).Include(c => c.Order).AsQueryable();
                if (retail) q = q.Where(c => c.Order != null && c.Order.ShopId == shopId);
                if (status != null) q = q.Where(c => c.Status == status);
                var list = await q.OrderByDescending(c => c.CreatedAt).Take(MaxRows).ToListAsync();
                return Make("Connections", "Provisioned accounts with plan and status",
                    new[] { "Account ID", "Customer", "Phone", "Connection", "Plan", "City", "Status", "Live since" },
                    list.Select(c => new object?[]
                    {
                        c.AccountId, c.Customer.Name, c.Customer.Phone, Fmt.TypeLabel(c.ConnectionType),
                        c.Plan == null ? "" : $"{c.Plan.Category} · {c.Plan.Name}", c.City, Fmt.ConnStatus(c.Status), c.ActivatedAt,
                    }).ToList(), null, status == null ? null : Fmt.ConnStatus(status));
            }

            case "bills":
            {
                var q = _db.Bills.AsNoTracking().Include(b => b.Connection).ThenInclude(c => c.Customer).AsQueryable();
                if (retail) q = q.Where(b => b.Connection.Order != null && b.Connection.Order.ShopId == shopId);
                if (status != null) q = q.Where(b => b.Status == status);
                var list = await q.OrderByDescending(b => b.CreatedAt).Take(MaxRows).ToListAsync();
                return Make("Invoice ledger", "Every invoice with rental, usage, tax and what is still due",
                    new[] { "Invoice", "Account ID", "Customer", "Period", "Rental", "Usage", "Discount", "Service tax", "Total", "Paid", "Due", "Status", "Due date", "Issued" },
                    list.Select(b => new object?[]
                    {
                        b.BillNo, b.Connection.AccountId, b.Connection.Customer.Name, b.Period, b.Rental, b.Usage, b.Discount,
                        b.Tax, b.Total, b.PaidAmount, b.DueAmount, Fmt.BillStatus(b.Status), b.DueDate, b.CreatedAt,
                    }).ToList(), new[] { 4, 5, 6, 7, 8, 9, 10 }, status == null ? null : Fmt.BillStatus(status));
            }

            case "payments":
            {
                var q = _db.Payments.AsNoTracking().Include(p => p.Bill).ThenInclude(b => b.Connection).ThenInclude(c => c.Customer).AsQueryable();
                if (retail) q = q.Where(p => p.Bill.Connection.Order != null && p.Bill.Connection.Order.ShopId == shopId);
                var list = await q.OrderByDescending(p => p.CreatedAt).Take(MaxRows).ToListAsync();
                return Make("Receipts & payments", "Every payment received against an invoice",
                    new[] { "Date", "Invoice", "Account ID", "Customer", "Amount", "Method", "Reference", "Channel", "Received by" },
                    list.Select(p => new object?[]
                    {
                        p.CreatedAt, p.Bill.BillNo, p.Bill.Connection.AccountId, p.Bill.Connection.Customer.Name, p.Amount,
                        p.Method.ToUpperInvariant(), p.Reference, p.Channel, p.ReceivedBy,
                    }).ToList(), new[] { 4 });
            }

            default: // dues
            {
                var open = await _db.Bills.AsNoTracking().Include(b => b.Connection).ThenInclude(c => c.Customer)
                    .Where(b => b.Status != BillStatus.Paid).ToListAsync();
                var now = DateTime.UtcNow;
                var rows = open.GroupBy(b => b.Connection.CustomerId)
                    .Select(g => new
                    {
                        Customer = g.First().Connection.Customer, Count = g.Count(), Due = g.Sum(x => x.DueAmount),
                        Oldest = g.Min(x => x.DueDate), Overdue = g.Any(x => x.DueDate != null && x.DueDate < now),
                    })
                    .OrderByDescending(r => r.Due).Take(MaxRows).ToList();
                return Make("Outstanding dues", "Customers with unpaid or partly paid invoices, largest balance first",
                    new[] { "Customer", "Phone", "Open invoices", "Amount due", "Oldest due date", "Overdue" },
                    rows.Select(r => new object?[] { r.Customer.Name, r.Customer.Phone, r.Count, r.Due, r.Oldest, r.Overdue ? "Yes" : "No" }).ToList(),
                    new[] { 3 });
            }
        }
    }

    /* ------------------------------------------------------------ formats */
    private static string Text(object? v, bool csv) => v switch
    {
        null => "",
        DateTime d => d.ToLocalTime().ToString(csv ? "yyyy-MM-dd HH:mm" : "MMM dd, yyyy", En),
        decimal m => m.ToString("0.00", Inv),
        int i => i.ToString(Inv),
        _ => v.ToString() ?? "",
    };

    /// <summary>Quote values for CSV and neutralise cells that a spreadsheet would run as a formula.</summary>
    private static string CsvCell(object? v)
    {
        var t = Text(v, true);
        if (v is string && t.Length > 0 && "=+-@\t\r".IndexOf(t[0]) >= 0) t = "'" + t;
        return t.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0 ? "\"" + t.Replace("\"", "\"\"") + "\"" : t;
    }

    private static byte[] ToCsv(ExportSheet sheet)
    {
        var sb = new StringBuilder();
        sb.Append(string.Join(",", sheet.Headers.Select(h => CsvCell(h)))).Append("\r\n");
        foreach (var row in sheet.Rows) sb.Append(string.Join(",", row.Select(CsvCell))).Append("\r\n");
        // UTF-8 byte-order mark so Excel opens non-ASCII names correctly
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    private static byte[] ToXlsx(ExportSheet sheet)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(sheet.Title.Length > 31 ? sheet.Title[..31] : sheet.Title);
        var cols = sheet.Headers.Length;

        for (var c = 0; c < cols; c++) ws.Cell(1, c + 1).Value = sheet.Headers[c];
        var head = ws.Range(1, 1, 1, cols);
        head.Style.Font.Bold = true;
        head.Style.Font.FontColor = XLColor.White;
        head.Style.Fill.BackgroundColor = XLColor.FromHtml("#4f46e5");

        for (var r = 0; r < sheet.Rows.Count; r++)
        {
            for (var c = 0; c < cols; c++)
            {
                var cell = ws.Cell(r + 2, c + 1);
                switch (sheet.Rows[r][c])
                {
                    case null: break;
                    case DateTime d:
                        cell.Value = d.ToLocalTime();
                        cell.Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
                        break;
                    case decimal m:
                        cell.Value = (double)m;
                        cell.Style.NumberFormat.Format = "$#,##0.00";
                        break;
                    case int i:
                        cell.Value = i;
                        break;
                    case var other:
                        cell.Value = other.ToString() ?? "";
                        break;
                }
            }
        }

        ws.SheetView.FreezeRows(1);
        if (sheet.Rows.Count > 0) ws.Range(1, 1, sheet.Rows.Count + 1, cols).SetAutoFilter();
        ws.Columns().AdjustToContents();
        foreach (var col in ws.ColumnsUsed()) if (col.Width > 46) col.Width = 46;

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
