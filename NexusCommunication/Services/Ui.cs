using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Html;

namespace NexusCommunication.Services;

/// <summary>Small HTML helpers that mirror the React UI atoms (StatusPill, TypeChip, StatCard, EmptyState).</summary>
public static class Ui
{
    private static string E(string? s) => WebUtility.HtmlEncode(s ?? "");

    private static readonly Dictionary<string, string> PillCls = new()
    {
        ["pending"] = "border-amber-400/40 bg-amber-400/10 text-amber-300",
        ["feasible"] = "border-sky-400/40 bg-sky-400/10 text-sky-300",
        ["infeasible"] = "border-rose-400/40 bg-rose-400/10 text-rose-300",
        ["connected"] = "border-emerald-400/40 bg-emerald-400/10 text-emerald-300",
        ["cancelled"] = "border-slate-500/40 bg-slate-500/10 text-slate-400",
        ["active"] = "border-emerald-400/40 bg-emerald-400/10 text-emerald-300",
        ["temporarily_inactive"] = "border-amber-400/40 bg-amber-400/10 text-amber-300",
        ["permanently_inactive"] = "border-rose-400/40 bg-rose-400/10 text-rose-300",
        ["paid"] = "border-emerald-400/40 bg-emerald-400/10 text-emerald-300",
        ["partial"] = "border-amber-400/40 bg-amber-400/10 text-amber-300",
        ["inactive"] = "border-slate-500/40 bg-slate-500/10 text-slate-400",
    };

    private static readonly Dictionary<string, string> DotCls = new()
    {
        ["pending"] = "bg-amber-400", ["feasible"] = "bg-sky-400", ["infeasible"] = "bg-rose-400",
        ["connected"] = "bg-emerald-400", ["cancelled"] = "bg-slate-500", ["active"] = "bg-emerald-400",
        ["temporarily_inactive"] = "bg-amber-400", ["permanently_inactive"] = "bg-rose-400",
        ["paid"] = "bg-emerald-400", ["partial"] = "bg-amber-400", ["inactive"] = "bg-slate-500",
    };

    public static IHtmlContent Pill(string? status, string? label = null)
    {
        var s = status ?? "pending";
        var cls = PillCls.TryGetValue(s, out var c) ? c : PillCls["pending"];
        var dot = DotCls.TryGetValue(s, out var d) ? d : "bg-slate-400";
        var text = label ?? s.Replace('_', ' ');
        return new HtmlString(
            $"<span class=\"inline-flex items-center gap-1.5 rounded-full border px-3 py-1 text-[11px] font-semibold uppercase tracking-[0.12em] {cls}\">" +
            $"<span class=\"h-1.5 w-1.5 rounded-full {dot}\"></span>{E(text)}</span>");
    }

    public static IHtmlContent TypeChip(string? type)
    {
        var (icon, cls, label) = type switch
        {
            "dialup" => ("radio", "border-amber-400/30 bg-amber-400/10 text-amber-300", "Dial-Up"),
            "landline" => ("phone", "border-violet-400/30 bg-violet-400/10 text-violet-300", "Landline"),
            _ => ("wifi", "border-cyan-400/30 bg-cyan-400/10 text-cyan-300", "Broadband"),
        };
        return new HtmlString(
            $"<span class=\"inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-[11px] font-semibold {cls}\">" +
            $"<i data-lucide=\"{icon}\" class=\"h-3 w-3\"></i>{label}</span>");
    }

    public static IHtmlContent StatCard(string icon, string label, string value, string? sub = null, string tone = "cyan")
    {
        var t = tone switch
        {
            "violet" => "from-violet-500/20 to-fuchsia-500/5 text-violet-300",
            "emerald" => "from-emerald-500/20 to-teal-500/5 text-emerald-300",
            "amber" => "from-amber-500/20 to-orange-500/5 text-amber-300",
            "rose" => "from-rose-500/20 to-pink-500/5 text-rose-300",
            _ => "from-cyan-500/20 to-sky-500/5 text-cyan-300",
        };
        var subHtml = sub is null ? "" : $"<p class=\"mt-1 text-xs text-slate-500\">{E(sub)}</p>";
        return new HtmlString(
            "<div class=\"glass card-hover rounded-2xl p-5\"><div class=\"flex items-center justify-between\">" +
            $"<p class=\"text-xs font-semibold uppercase tracking-[0.16em] text-slate-500\">{E(label)}</p>" +
            $"<span class=\"grid h-9 w-9 place-items-center rounded-xl bg-gradient-to-br {t}\"><i data-lucide=\"{icon}\" class=\"h-[18px] w-[18px]\"></i></span></div>" +
            $"<p data-stat class=\"mt-3 font-display text-3xl font-bold tracking-tight text-slate-100\">{E(value)}</p>{subHtml}</div>");
    }

    /// <summary>
    /// Small round / rounded-square picture with a text fallback (initials). If the file is missing or the record has no picture,
    /// the initials show instead of a broken-image icon.
    /// </summary>
    public static IHtmlContent Thumb(string? url, string? alt, string fallback, string size = "h-9 w-9", string rounded = "rounded-xl", string tone = "bg-cyan-400/10 text-cyan-300")
    {
        var img = string.IsNullOrWhiteSpace(url) ? "" :
            $"<img src=\"{E(AssetIfSeed(url))}\" alt=\"{E(alt)}\" loading=\"lazy\" onerror=\"this.remove()\" class=\"absolute inset-0 h-full w-full object-cover\" />";
        return new HtmlString($"<span class=\"relative grid {size} shrink-0 place-items-center overflow-hidden {rounded} {tone} text-xs font-bold\">{E(fallback)}{img}</span>");
    }

    /// <summary>wwwroot folder, set once at start-up (Program.cs) so views can look for files on disk</summary>
    public static string? WebRoot { get; set; }
    private static readonly string[] ImgExts = { ".png", ".jpg", ".jpeg", ".webp", ".gif" };

    /// <summary>
    /// Finds a picture by its path WITHOUT caring about the extension: Asset("/images/hero/hero-network") returns
    /// "/images/hero/hero-network.png" or ".jpg" / ".jpeg" / ".webp" - whichever file really exists in wwwroot.
    /// A path that already has an extension is tried as given first. Nothing found -> returns the .png path (the page hides it).
    /// </summary>
    public static string Asset(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "";
        var p = path.StartsWith("/") ? path : "/" + path;
        if (WebRoot == null) return Path.HasExtension(p) ? p : p + ".png";
        var stem = Path.HasExtension(p) && ImgExts.Contains(Path.GetExtension(p).ToLowerInvariant()) ? p[..^Path.GetExtension(p).Length] : p;
        string Full(string web) => Path.Combine(WebRoot, web.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (Path.HasExtension(p) && File.Exists(Full(p))) return p;
        foreach (var e in ImgExts) if (File.Exists(Full(stem + e))) return stem + e;
        return stem + ".png";
    }

    /// <summary>Own picture of every seeded plan: images/plans/{file}. Key = connection type | category | plan name.</summary>
    private static readonly Dictionary<string, string> PlanPics = new(StringComparer.OrdinalIgnoreCase)
    {
        ["dialup|Hourly Basis|10 Hours Pack"] = "dialup-hourly-10h",
        ["dialup|Hourly Basis|30 Hours Pack"] = "dialup-hourly-30h",
        ["dialup|Hourly Basis|60 Hours Pack"] = "dialup-hourly-60h",
        ["dialup|Unlimited 28Kbps|Monthly Unlimited"] = "dialup-28k-monthly",
        ["dialup|Unlimited 28Kbps|Quarterly Unlimited"] = "dialup-28k-quarterly",
        ["dialup|Unlimited 56Kbps|Monthly Unlimited"] = "dialup-56k-monthly",
        ["dialup|Unlimited 56Kbps|Quarterly Unlimited"] = "dialup-56k-quarterly",
        ["broadband|Hourly Basis|30 Hours Pack"] = "broadband-hourly-30h",
        ["broadband|Hourly Basis|60 Hours Pack"] = "broadband-hourly-60h",
        ["broadband|Unlimited 64Kbps|Monthly Unlimited"] = "broadband-64k-monthly",
        ["broadband|Unlimited 64Kbps|Quarterly Unlimited"] = "broadband-64k-quarterly",
        ["broadband|Unlimited 128Kbps|Monthly Unlimited"] = "broadband-128k-monthly",
        ["broadband|Unlimited 128Kbps|Quarterly Unlimited"] = "broadband-128k-quarterly",
        ["landline|Local Plan|Unlimited Annual"] = "landline-local-annual",
        ["landline|Local Plan|Monthly Local"] = "landline-local-monthly",
        ["landline|STD Plan|Monthly STD"] = "landline-std-monthly",
        ["landline|STD Plan|Half-Yearly STD"] = "landline-std-halfyearly",
        ["landline|STD Plan|Yearly STD"] = "landline-std-yearly",
    };

    /// <summary>true when a picture with this path (any extension) really exists in wwwroot</summary>
    public static bool AssetExists(string path)
    {
        if (WebRoot == null || string.IsNullOrWhiteSpace(path)) return false;
        var p = path.StartsWith("/") ? path : "/" + path;
        var stem = Path.HasExtension(p) && ImgExts.Contains(Path.GetExtension(p).ToLowerInvariant()) ? p[..^Path.GetExtension(p).Length] : p;
        foreach (var e in ImgExts) if (File.Exists(Path.Combine(WebRoot, (stem + e).TrimStart('/').Replace('/', Path.DirectorySeparatorChar)))) return true;
        return false;
    }

    /// <summary>Picture for a plan: its own upload, else its own images/plans/{plan} picture, else the shared picture for its connection type.</summary>
    public static string PlanImage(string? imageUrl, string? connectionType, string? category = null, string? name = null)
    {
        if (!string.IsNullOrWhiteSpace(imageUrl)) return AssetIfSeed(imageUrl);
        if (PlanPics.TryGetValue($"{connectionType}|{category}|{name}", out var file) && AssetExists("/images/plans/" + file))
            return Asset("/images/plans/" + file);
        return Asset($"/images/plans/{(connectionType is "dialup" or "landline" ? connectionType : "broadband")}");
    }

    /// <summary>Default picture for a retail outlet.</summary>
    public static string ShopImage(string? imageUrl) => !string.IsNullOrWhiteSpace(imageUrl) ? AssetIfSeed(imageUrl) : Asset("/images/shops/outlet-default");

    /// <summary>pictures shipped with the project (/images/...) are resolved on disk; uploads (/uploads/...) are used as they are</summary>
    public static string AssetIfSeed(string url) => url.StartsWith("/images/", StringComparison.OrdinalIgnoreCase) ? Asset(url) : url;

    public static IHtmlContent Empty(string title, string? hint = null)
    {
        var h = hint is null ? "" : $"<p class=\"max-w-sm text-xs text-slate-500\">{E(hint)}</p>";
        return new HtmlString(
            "<div class=\"flex flex-col items-center justify-center gap-3 rounded-2xl border border-dashed border-slate-700/70 py-14 text-center\">" +
            "<i data-lucide=\"inbox\" class=\"h-8 w-8 text-slate-600\"></i>" +
            $"<p class=\"font-display text-sm font-semibold text-slate-300\">{E(title)}</p>{h}</div>");
    }

    /// <summary>Serialise to JSON that is safe to place inside a &lt;script type="application/json"&gt; block.</summary>
    public static IHtmlContent Json(object o) =>
        new HtmlString(JsonSerializer.Serialize(o, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
}
