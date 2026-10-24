using System.Globalization;

namespace NexusCommunication.Services;

/// <summary>Formatting helpers + business constants shared by controllers and views.</summary>
public static class Fmt
{
    private static readonly CultureInfo En = CultureInfo.GetCultureInfo("en-US");

    public const decimal ServiceTaxRate = 12.24m; // % on the whole bill

    public static readonly (string Code, string Name)[] Cities =
    {
        ("101", "Meridian Central"), ("102", "Harbor Point"), ("103", "Tech Park"),
        ("104", "Riverside"), ("105", "Northgate"), ("106", "Lakeview"),
    };

    public static string Money(decimal? n) => "$" + (n ?? 0m).ToString("N2", En);
    public static string Num(long n) => n.ToString("N0", En);

    public static string Date(DateTime? d) =>
        d is null ? "—" : d.Value.ToLocalTime().ToString("MMM dd, yyyy", En);

    public static string Stamp(DateTime? d) =>
        d is null ? "—" : d.Value.ToLocalTime().ToString("MMM dd, hh:mm tt", En);

    public static string MonthLabel(int monthsBack = 0) =>
        DateTime.Now.AddMonths(-monthsBack).ToString("MMMM yyyy", En);

    /// <summary>Corporate slabs from the spec: 10-15 lines 25%, 15-25 lines 50%, 25-50 lines 75%, above 50 lines 100%.</summary>
    public static int BulkDiscount(int quantity) =>
        quantity > 50 ? 100 : quantity >= 25 ? 75 : quantity >= 15 ? 50 : quantity >= 10 ? 25 : 0;

    /// <summary>"Meera Joshi" -> "M**** J****" (used on the public search so strangers cannot read personal data)</summary>
    public static string MaskName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "—";
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", parts.Select(p => p.Length <= 1 ? p : p[0] + new string('*', Math.Min(p.Length - 1, 5))));
    }

    /// <summary>"+1 555 3301" -> "••••3301"</summary>
    public static string MaskPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return "—";
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        return digits.Length <= 4 ? "••••" : "••••" + digits[^4..];
    }

    public static string Initials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "?";
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return (parts.Length == 1 ? parts[0][..1] : parts[0][..1] + parts[^1][..1]).ToUpperInvariant();
    }

    public static string ShortMonth(DateTime d) => d.ToString("MMM yy", En);

    public static string TypeLetter(string type) => type switch { "broadband" => "B", "landline" => "T", _ => "D" };

    public static string TypeLabel(string? type) => type switch
    {
        "dialup" => "Dial-Up", "broadband" => "Broadband", "landline" => "Landline", _ => type ?? "—",
    };

    public static decimal Deposit(string type) => type switch { "dialup" => 325m, "broadband" => 500m, _ => 250m };

    public static string OrderStatus(string s) => s switch
    {
        "pending" => "Feasibility Pending", "feasible" => "Feasible", "infeasible" => "Not Feasible",
        "connected" => "Connected", "cancelled" => "Cancelled", _ => s,
    };

    public static string ConnStatus(string s) => s switch
    {
        "active" => "Active", "temporarily_inactive" => "Temporarily Inactive",
        "permanently_inactive" => "Permanently Inactive", _ => s,
    };

    public static string BillStatus(string s) => s switch
    {
        "pending" => "Unpaid", "partial" => "Partially Paid", "paid" => "Paid", _ => s,
    };

    public static string RoleHome(string role) => role switch
    {
        "admin" => "/admin", "accounts" => "/accounts", "technical" => "/technical",
        "retail" => "/retail", "customer" => "/portal", _ => "/",
    };

    /// <summary>Round-trippable decimal for HTML attributes / JS (always '.' as separator)</summary>
    public static string Dec(decimal n) => n.ToString("0.00", CultureInfo.InvariantCulture);
}
