using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusCommunication.Data;
using NexusCommunication.Models;
using NexusCommunication.Services;

namespace NexusCommunication.Controllers.Api;

/// <summary>
/// Role-aware help-desk chatbot (no external AI / API, no new database table). Live data and permissions come from the rule-based
/// engine below; Roman Urdu / English wording and the written answers come from the knowledge file Data/chat-knowledge.json
/// (Services/ChatKnowledge.cs), which also answers in Roman Urdu when the person writes Roman Urdu.
///
/// WHO MAY ASK WHAT (the audience is taken from the signed-in session on the SERVER, never from the browser):
///   public     (landing page, not signed in) : plans, charges, outlets, coverage, FAQ, order / account status, support
///   customer   : the public topics + their OWN orders, connections and bills
///   accounts   : billing desk only (invoices, dues, receipts, collections, tax) + plans / charges
///   technical  : technical desk only (feasibility, provisioning, order tracker, connections, equipment stock)
///   retail     : retail desk only (place order, THEIR shop's orders, customers, connections, collections)
///   admin      : the whole project (every panel: employees, vendors, stock, procurement, billing, feedback ...)
/// A question that belongs to another desk gets a polite "outside my scope" answer, never the data.
/// The topic keywords live in Services/ChatIntent.cs (pure, unit-testable).
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/chatbot")]
public class ChatbotApiController : ApiBase
{
    private readonly AppDbContext _db;
    private readonly ChatKnowledge _kb;
    public ChatbotApiController(AppDbContext db, ChatKnowledge kb) { _db = db; _kb = kb; }

    /* ------------------------------ wire types ------------------------------ */

    public record ChatRequest(string? Message);

    public class BotItem
    {
        public string Title { get; set; } = "";
        public string? Badge { get; set; }
        public List<string> Lines { get; set; } = new();
    }

    public record BotStep(string Label, string State);          // State: done | current | fail | todo
    public record BotButton(string Label, string? Text = null, string? Href = null); // Text = send as message, Href = open link

    public class BotReply
    {
        public string Text { get; set; } = "";
        public List<BotItem> Items { get; set; } = new();
        public List<BotStep> Steps { get; set; } = new();
        public List<BotButton> Buttons { get; set; } = new();
    }

    private const string Pub = ChatIntent.Public;
    private static readonly HashSet<string> KnownRoles = new()
    { AppRoles.Admin, AppRoles.Accounts, AppRoles.Technical, AppRoles.Retail, AppRoles.Customer };

    /// <summary>"public" for visitors, otherwise the signed-in role</summary>
    private string Audience => User.Identity?.IsAuthenticated == true && KnownRoles.Contains(Session.Role) ? Session.Role : Pub;
    private int ShopIdValue => Session.ShopId ?? -1;

    /* ------------------------------- endpoints ------------------------------ */

    [HttpGet("init")]
    public async Task<IActionResult> Init()
    {
        var role = Audience;
        string name = "";
        if (role == AppRoles.Customer) name = First((await CurrentCustomerAsync())?.Name ?? Session.Name);
        else if (role != Pub) name = First(Session.Name);
        var hello = name.Length > 0 ? $"Hello {name}!" : "Hello!";
        var text = role switch
        {
            Pub => $"{hello} I'm the Nexus Assistant. Ask me about plans, charges, retail outlets, how to apply, or track an order — or pick an option below.",
            AppRoles.Customer => $"{hello} I'm the Nexus Assistant. I can help with your orders, connections and bills, plus plans, charges and support.",
            _ => $"{hello} I'm the Nexus {Desk(role)} assistant. I can help with {ScopeText(role)}.",
        };
        return Ok(new BotReply { Text = text, Buttons = Menu(role) });
    }

    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] ChatRequest? req)
    {
        var raw = (req?.Message ?? "").Trim();
        var role = Audience;
        if (raw.Length == 0) return Ok(Fallback(role));
        if (raw.Length > 300) raw = raw[..300];
        var q = raw.ToLowerInvariant();
        var up = raw.ToUpperInvariant();

        /* 1. an id typed in the message wins (each lookup is limited to what this audience may see) */
        var acc = Regex.Match(up, @"\b[DBT]\d{15}\b");
        if (acc.Success) return Ok(await AccountLookupAsync(role, acc.Value));
        var ord = Regex.Match(up, @"\b[DBT]\d{10}\b");
        if (ord.Success) return Ok(await OrderLookupAsync(role, ord.Value));
        var inv = Regex.Match(up, @"\bINV-\d{4}-\d{4,}\b");
        if (inv.Success) return Ok(await BillLookupAsync(role, inv.Value));

        /* 2. FAQ buttons ("faq apply", ...) */
        if (q.StartsWith("faq "))
        {
            var key = q[4..].Trim();
            if (Faq.ContainsKey(key)) return Ok(FaqAnswer(key));
        }

        /* 3. the knowledge file understands Roman Urdu / English wording; it never goes beyond what this audience may see:
              an entry of another desk is refused, and live data still comes only from the role-limited lookups below */
        var topic = ChatIntent.Detect(q);
        var kb = _kb.Match(raw, role);
        var need = topic == ChatTopic.Unknown ? ChatKnowledge.Low : ChatKnowledge.High;
        if (kb.Denied != null && kb.Denied.Score >= need) return Ok(KbRefusal(role, kb.RomanUrdu));
        if (kb.Best != null && kb.Best.Score >= need)
        {
            var answered = await KbAnswerAsync(role, q, kb, kb.Best.Entry);
            if (answered != null) return Ok(answered);
        }

        /* 4. otherwise the keyword engine: detect the topic, check the audience may talk about it, then answer */
        if (topic == ChatTopic.Unknown) return Ok(KbFallback(role, kb));
        return Ok(await RouteAsync(role, topic, q));
    }

    /* ---------------------------- knowledge-file answers --------------------------- */

    private async Task<BotReply?> KbAnswerAsync(string role, string q, KbMatch kb, KbEntry e)
    {
        var text = _kb.TextFor(e, role, kb.RomanUrdu);
        BotReply reply;

        if (!string.IsNullOrWhiteSpace(e.Route))
        {
            var route = e.Route!.Trim().ToLowerInvariant();
            switch (route)
            {
                case "secrets": return SecretsReply(role);
                case "cheapest": reply = await CheapestAsync(role, kb.RomanUrdu); break;
                case "citycheck": reply = CityCheck(q, kb.RomanUrdu); break;
                default:
                    if (!ChatIntent.Allowed(role, route)) return KbRefusal(role, kb.RomanUrdu);
                    reply = await RouteAsync(role, route, string.IsNullOrWhiteSpace(e.RouteQ) ? q : e.RouteQ!);
                    break;
            }
            if (!string.IsNullOrWhiteSpace(text)) reply.Text = text;   // an entry with its own wording replaces the English lead-in; cards stay
        }
        else
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            reply = new BotReply { Text = text, Buttons = Menu(role) };
        }

        var buttons = _kb.ButtonsFor(e, role);
        if (buttons is { Count: > 0 })
        {
            reply.Buttons = buttons
                .Where(b => !string.IsNullOrWhiteSpace(b.Label) && (!string.IsNullOrWhiteSpace(b.Text) || !string.IsNullOrWhiteSpace(b.Href)))
                .Where(b => b.Href == null || (b.Href.StartsWith("/") && !b.Href.StartsWith("//")))
                .Select(b => new BotButton(b.Label, string.IsNullOrWhiteSpace(b.Text) ? null : b.Text, string.IsNullOrWhiteSpace(b.Href) ? null : b.Href))
                .ToList();
        }
        return reply;
    }

    /// <summary>The question belongs to another desk: a polite refusal (in Roman Urdu when the person wrote Roman Urdu), never the content.</summary>
    private BotReply KbRefusal(string role, bool urdu)
    {
        var who = role == Pub ? "the public assistant" : $"the {Desk(role)} assistant";
        var r = new BotReply
        {
            Text = urdu
                ? $"Maaf kijiye, yeh jankari aap ke login ({Desk(role)}) ke liye maujood nahi hai. Main in cheezon mein madad kar sakta hoon: {ScopeTextUr(role)}."
                : $"Sorry, that's outside what I can help with here. As {who} I can help with {ScopeText(role)}.",
            Buttons = Menu(role),
        };
        if (role == Pub) r.Buttons.Add(new BotButton("Sign in", Href: "/login"));
        return r;
    }

    /// <summary>Nothing matched: offer the closest questions as buttons ("did you mean"), or the usual help text.</summary>
    private BotReply KbFallback(string role, KbMatch kb)
    {
        var urdu = kb.RomanUrdu;
        var near = kb.Suggestions.Where(x => x.Score >= ChatKnowledge.Suggest).Take(3).ToList();
        if (near.Count > 0)
        {
            var r = new BotReply
            {
                Text = urdu ? "Mujhe pakka samajh nahi aaya. Kya aap yeh poochna chahte hain?" : "I'm not sure I understood. Did you mean one of these?",
            };
            foreach (var h in near) r.Buttons.Add(new BotButton(_kb.Label(h.Entry), _kb.AskText(h.Entry, urdu)));
            if (role is Pub or AppRoles.Customer) r.Buttons.Add(new BotButton("Contact us", "complaint"));
            return r;
        }
        if (!urdu) return Fallback(role);
        var f = new BotReply
        {
            Text = $"Maaf kijiye, mujhe samajh nahi aaya. Neeche se koi option chunein, ya in ke baare mein poochein: {ScopeTextUr(role)}.",
            Buttons = Menu(role),
        };
        if (role is Pub or AppRoles.Customer) f.Buttons.Add(new BotButton("Contact us", "complaint"));
        return f;
    }

    private static string ScopeTextUr(string role) => role switch
    {
        AppRoles.Admin => "poora project: orders, connections, billing, employees, vendors, equipment stock, procurement, retail shops, plans aur feedback",
        AppRoles.Accounts => "billing: invoices, customer dues, receipts, collections, service tax aur discounts, aur plans aur charges",
        AppRoles.Technical => "technical desk: feasibility queue, provisioning, order tracker, connection status aur equipment stock",
        AppRoles.Retail => "retail desk: order place karna, apne outlet ki order book, customers, connections aur collections",
        AppRoles.Customer => "aap ke apne orders, connections aur bills, aur plans, charges, outlets aur support",
        _ => "plans, charges, coverage, retail outlets, naya connection kaise lagwayen, aur order ya account track karna",
    };

    /// <summary>"Cheapest plan": the cheapest active plan of each connection type, from the live plan table.</summary>
    private async Task<BotReply> CheapestAsync(string role, bool urdu)
    {
        var plans = await _db.Plans.AsNoTracking().Where(p => p.Active).ToListAsync();
        if (plans.Count == 0) return new BotReply { Text = urdu ? "Abhi koi active plan nahi hai." : "There are no active plans right now.", Buttons = Menu(role) };
        var overall = plans.OrderBy(p => p.Price).First();
        var r = new BotReply
        {
            Text = urdu
                ? $"Sab se sasta plan {overall.Name} hai ({Fmt.Money(overall.Price)}). Har connection type ka sab se sasta plan:"
                : $"The cheapest plan overall is {overall.Name} ({Fmt.Money(overall.Price)}). The cheapest plan of each connection type:",
        };
        foreach (var t in ConnTypes.All)
        {
            var p = plans.Where(x => x.ConnectionType == t).OrderBy(x => x.Price).ThenBy(x => x.Id).FirstOrDefault();
            if (p == null) continue;
            var item = new BotItem { Title = p.Name, Badge = Fmt.Money(p.Price) };
            item.Lines.Add($"{Fmt.TypeLabel(t)} · " + (string.IsNullOrWhiteSpace(p.Speed) ? p.Category : $"{p.Category} · {p.Speed}"));
            item.Lines.Add($"Validity: {p.ValidityLabel}");
            if (p.Deposit > 0) item.Lines.Add($"Deposit: {Fmt.Money(p.Deposit)}");
            r.Items.Add(item);
        }
        r.Buttons.Add(new BotButton("Full tariff sheet", Href: "/plans"));
        r.Buttons.Add(new BotButton("Charges", "charges"));
        r.Buttons.Add(new BotButton("How to apply", "how do i apply"));
        return r;
    }

    /// <summary>"Is service available in X?": looks for one of the covered city / area names in the message.</summary>
    private BotReply CityCheck(string q, bool urdu)
    {
        var hit = Fmt.Cities.FirstOrDefault(c => q.Contains(c.Name.ToLowerInvariant()));
        if (hit.Name == null)
        {
            var cov = Coverage();
            cov.Text = urdu
                ? "Aap ke message mein mujhe shehar ka naam nahi mila. Hum in shehron / ilaqon mein service dete hain:"
                : "I couldn't spot a city name in your message. We provide service in these cities / areas:";
            return cov;
        }
        var r = new BotReply
        {
            Text = urdu
                ? $"Ji haan, hum {hit.Name} mein service dete hain. Aap order place kar sakte hain ya qareebi outlet dekh sakte hain."
                : $"Yes, we provide service in {hit.Name}. You can place an order or look at the nearest outlet.",
        };
        r.Buttons.Add(new BotButton("View plans", "plans"));
        r.Buttons.Add(new BotButton("Find an outlet", "outlets"));
        return r;
    }

    private async Task<BotReply> RouteAsync(string role, string topic, string q)
    {
        if (topic == ChatTopic.Secrets) return SecretsReply(role);
        if (!ChatIntent.Allowed(role, topic)) return Outside(role, topic);

        switch (topic)
        {
            case ChatTopic.Greeting: return new BotReply { Text = "Hi! How can I help you today?", Buttons = Menu(role) };
            case ChatTopic.Thanks: return new BotReply { Text = "Glad I could help! Ask me anything else any time.", Buttons = Menu(role) };
            case ChatTopic.Help: return HelpReply(role);
            case ChatTopic.About: return AboutReply(role);
            case ChatTopic.Account: return AccountHelp(role);

            case ChatTopic.Plans: return await PlansAsync(q, role);
            case ChatTopic.Charges: return await ChargesAsync(role);
            case ChatTopic.Outlets: return await OutletsAsync(role);
            case ChatTopic.Coverage: return Coverage();
            case ChatTopic.Faq:
                var fk = ChatIntent.FaqKey(q);
                return fk != null ? FaqAnswer(fk) : FaqMenu();
            case ChatTopic.Complaint: return ComplaintReply(role);

            case ChatTopic.Orders: return await OrdersAsync(role);
            case ChatTopic.Connections: return await ConnectionsAsync(role, q);
            case ChatTopic.Billing: return await BillingAsync(role, q);
            case ChatTopic.Finance: return role == AppRoles.Retail ? await ShopCollectionsAsync() : await BillingSummaryAsync();
            case ChatTopic.Invoicing: return await InvoicingAsync(q);
            case ChatTopic.Feedback: return await FeedbackAsync(role);
            case ChatTopic.Overview: return await OverviewAsync(role);

            case ChatTopic.Feasibility: return await FeasibilityAsync(role);
            case ChatTopic.Equipment: return await EquipmentAsync(role);
            case ChatTopic.PlaceOrder: return PlaceOrderGuide(role);
            case ChatTopic.Customers: return await CustomersAsync(role);

            case ChatTopic.Employees: return await EmployeesAsync();
            case ChatTopic.Vendors: return await VendorsAsync();
            case ChatTopic.Procurement: return await ProcurementAsync();

            default: return Fallback(role);
        }
    }

    /* ------------------------- scope / audience texts ------------------------ */

    private static string Desk(string role) => role switch
    {
        AppRoles.Admin => "Admin", AppRoles.Accounts => "Accounts", AppRoles.Technical => "Technical",
        AppRoles.Retail => "Retail", AppRoles.Customer => "Customer", _ => "Public",
    };

    private static string ScopeText(string role) => role switch
    {
        AppRoles.Admin => "everything in the project — orders, connections, billing, employees, vendors, equipment stock, procurement, retail shops, plans and feedback",
        AppRoles.Accounts => "billing — invoices, customer dues, receipts and collections, service tax and discounts — plus plans and charges",
        AppRoles.Technical => "the technical desk — the feasibility queue, provisioning, the order tracker, connection status and equipment stock",
        AppRoles.Retail => "the retail desk — placing orders, your shop's order book, customers, connections and collections",
        AppRoles.Customer => "your own orders, connections and bills, plus plans, charges, outlets and support",
        _ => "plans, charges, coverage, retail outlets, how to apply, and order or account tracking",
    };

    private static string[] Examples(string role) => role switch
    {
        AppRoles.Admin => new[] { "Overview of orders, connections, billing and stock", "Employees by department, vendors, procurement needs", "Low-stock equipment, retail shops, plans and charges", "Customer feedback and average rating", "Any order, account or invoice ID" },
        AppRoles.Accounts => new[] { "Billing summary — outstanding, overdue, collected", "Customer dues and recent receipts", "How to generate an invoice (12.24% service tax)", "Look up an invoice (INV-…), order or account ID", "Plans, charges and bulk discounts" },
        AppRoles.Technical => new[] { "Feasibility queue and how provisioning works", "Order tracker — pending, feasible, connected", "Connection status (active / inactive)", "Equipment stock and low-stock items", "Look up an order or account ID" },
        AppRoles.Retail => new[] { "How to place an order for a customer", "Your shop's order book and connections", "Collections taken at your counter", "Customers panel, plans and charges", "Look up an order or account ID from your shop" },
        AppRoles.Customer => new[] { "My orders and their status", "My connections and my bills", "Plans, charges, outlets and how to apply", "Feedback and support", "My account (photo and password)" },
        _ => new[] { "Plans, tariffs and speeds", "Charges, deposits, tax and bulk discounts", "Retail outlets and the cities we cover", "How to apply for a new connection", "Track an order or account by its ID" },
    };

    private static List<BotButton> Menu(string role) => role switch
    {
        AppRoles.Admin => new() { new("Overview", "overview"), new("Orders", "orders"), new("Billing", "billing"), new("Low stock", "low stock"), new("Employees", "employees"), new("Feedback", "feedback") },
        AppRoles.Accounts => new() { new("Billing summary", "billing summary"), new("Customer dues", "customer dues"), new("Receipts", "receipts"), new("Generate invoice", "generate invoice"), new("Charges", "charges") },
        AppRoles.Technical => new() { new("Feasibility queue", "feasibility queue"), new("Order tracker", "order tracker"), new("Connections", "connections"), new("Low stock", "low stock"), new("Provisioning guide", "provisioning guide") },
        AppRoles.Retail => new() { new("Place order", "place order"), new("Order book", "order book"), new("Collections", "collections"), new("Customers", "customers"), new("Plans", "plans") },
        AppRoles.Customer => new() { new("My orders", "my orders"), new("My bills", "my bills"), new("My connections", "my connections"), new("Plans", "plans"), new("Charges", "charges"), new("Feedback", "feedback") },
        _ => new() { new("Plans", "plans"), new("Charges", "charges"), new("Track order", "track order"), new("Outlets", "outlets"), new("How to apply", "how do i apply"), new("Complaint", "complaint") },
    };

    private static string TabLink(string role, string tab) => Fmt.RoleHome(role) + "#" + tab;
    private static string First(string? name) => (name ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
    private static string Cap(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];
    private static bool CanSeeDue(string role) => role is AppRoles.Admin or AppRoles.Accounts or AppRoles.Retail or AppRoles.Customer;

    /* ----------------------- refusals, help, small talk ---------------------- */

    private BotReply Outside(string role, string topic)
    {
        // public-facing questions that merely use a desk word get the matching FAQ instead of a refusal
        if (topic == ChatTopic.Feasibility) return FaqAnswer("time");
        if (topic == ChatTopic.PlaceOrder) return FaqAnswer("apply");

        var who = role == Pub ? "the public assistant" : $"the {Desk(role)} assistant";
        var r = new BotReply
        {
            Text = $"Sorry, that's outside what I can help with here. As {who} I can help with {ScopeText(role)}.",
            Buttons = Menu(role),
        };
        if (role == Pub) r.Buttons.Add(new BotButton("Sign in", Href: "/login"));
        return r;
    }

    private static BotReply SecretsReply(string role)
    {
        var r = new BotReply
        {
            Text = "I can't help with passwords, credentials or system configuration. " +
                   (role == Pub
                       ? "If you need access, sign in with your own account."
                       : "You can change your own password from the My account page."),
        };
        r.Buttons.Add(role == Pub ? new BotButton("Sign in", Href: "/login") : new BotButton("My account", Href: "/account"));
        return r;
    }

    private static BotReply HelpReply(string role)
    {
        var r = new BotReply { Text = "Here is what I can help you with:" };
        r.Items.Add(new BotItem { Title = role == Pub ? "Public assistant" : $"{Desk(role)} assistant", Lines = Examples(role).ToList() });
        r.Buttons = Menu(role);
        return r;
    }

    private static BotReply AboutReply(string role)
    {
        var r = new BotReply
        {
            Text = $"Nexus Communication provides dial-up, broadband and landline connections across {Fmt.Cities.Length} service areas. " +
                   "You can browse plans, apply through a retail outlet or the customer portal, and track every order and account by its ID.",
            Buttons = Menu(role),
        };
        return r;
    }

    private static BotReply AccountHelp(string role)
    {
        if (role == Pub)
            return new BotReply
            {
                Text = "Sign in to your account, or create a customer account to place orders and follow your connections and bills.",
                Buttons = { new BotButton("Sign in", Href: "/login"), new BotButton("Create account", Href: "/register") },
            };
        var r = new BotReply { Text = "Open the My account page to change your password or profile photo." };
        r.Buttons.Add(new BotButton("My account", Href: "/account"));
        if (role == AppRoles.Customer) r.Buttons.Add(new BotButton("Profile", Href: "/portal#profile"));
        return r;
    }

    private static BotReply Fallback(string role)
    {
        var r = new BotReply
        {
            Text = $"Sorry, I didn't understand that. Pick an option below, or ask me about {ScopeText(role)}.",
            Buttons = Menu(role),
        };
        if (role is Pub or AppRoles.Customer) r.Buttons.Add(new BotButton("Contact us", "complaint"));
        return r;
    }

    private static BotReply ComplaintReply(string role)
    {
        if (role is not (Pub or AppRoles.Customer))
            return new BotReply
            {
                Text = "Customer feedback and complaints come from the public site and the customer portal. For a problem with this system, please contact your administrator.",
                Buttons = Menu(role),
            };
        return new BotReply
        {
            Text = "To send a complaint or question to our team, use the option below. The network desk reads every message.",
            Buttons =
            {
                new BotButton("Contact us / Complaint", Href: role == AppRoles.Customer ? "/portal#feedback" : "/#feedback-form"),
                new BotButton("Find an outlet", "outlets"),
            },
        };
    }

    /* ------------------------------ public topics ---------------------------- */

    private async Task<BotReply> PlansAsync(string q, string role)
    {
        string? type = ChatIntent.Has(q, "dial") ? ConnTypes.Dialup
            : ChatIntent.Has(q, "broadband", "internet", "wifi", "speed") ? ConnTypes.Broadband
            : ChatIntent.Has(q, "landline", "telephone", "std") ? ConnTypes.Landline
            : null;

        var active = _db.Plans.AsNoTracking().Where(p => p.Active);

        if (type == null)
        {
            var summary = await active.GroupBy(p => p.ConnectionType)
                .Select(g => new { Type = g.Key, Count = g.Count(), From = g.Min(p => p.Price) }).ToListAsync();
            var r = new BotReply { Text = "We offer three kinds of connection. Which one would you like to see plans for?" };
            foreach (var t in ConnTypes.All)
            {
                var s = summary.FirstOrDefault(x => x.Type == t);
                if (s == null) continue;
                r.Items.Add(new BotItem { Title = Fmt.TypeLabel(t), Badge = $"{s.Count} plans", Lines = { $"From {Fmt.Money(s.From)}" } });
            }
            foreach (var t in ConnTypes.All)
                if (summary.Any(x => x.Type == t)) r.Buttons.Add(new BotButton(Fmt.TypeLabel(t), t == ConnTypes.Dialup ? "dialup plans" : t + " plans"));
            r.Buttons.Add(new BotButton("Full tariff sheet", Href: "/plans"));
            return r;
        }

        var plans = await active.Where(p => p.ConnectionType == type).OrderBy(p => p.Price).ThenBy(p => p.Id).ToListAsync();
        if (plans.Count == 0)
            return new BotReply { Text = $"There are no active {Fmt.TypeLabel(type)} plans right now.", Buttons = Menu(role) };

        var reply = new BotReply { Text = $"{Fmt.TypeLabel(type)} plans ({plans.Count}):" };
        foreach (var p in plans.Take(6))
        {
            var item = new BotItem { Title = p.Name, Badge = Fmt.Money(p.Price) };
            item.Lines.Add(string.IsNullOrWhiteSpace(p.Speed) ? p.Category : $"{p.Category} · {p.Speed}");
            item.Lines.Add($"Validity: {p.ValidityLabel}");
            if (p.Deposit > 0) item.Lines.Add($"Deposit: {Fmt.Money(p.Deposit)}");
            if (!string.IsNullOrWhiteSpace(p.CallRates)) item.Lines.Add($"Call rates: {p.CallRates}");
            reply.Items.Add(item);
        }
        if (plans.Count > 6) reply.Text += " (cheapest first — showing the first 6)";
        reply.Buttons.Add(new BotButton("Full tariff sheet", Href: "/plans"));
        reply.Buttons.Add(new BotButton("Charges", "charges"));
        reply.Buttons.Add(new BotButton("How to apply", "how do i apply"));
        return reply;
    }

    private async Task<BotReply> ChargesAsync(string role)
    {
        var deposits = await _db.Plans.AsNoTracking().Where(p => p.Active && p.Deposit > 0)
            .GroupBy(p => p.ConnectionType).Select(g => new { Type = g.Key, Min = g.Min(p => p.Deposit), Max = g.Max(p => p.Deposit) }).ToListAsync();

        var r = new BotReply { Text = "Summary of connection charges:" };
        foreach (var t in ConnTypes.All)
        {
            var d = deposits.FirstOrDefault(x => x.Type == t);
            var dep = d == null ? Fmt.Money(Fmt.Deposit(t)) : d.Min == d.Max ? Fmt.Money(d.Min) : $"{Fmt.Money(d.Min)} – {Fmt.Money(d.Max)}";
            r.Items.Add(new BotItem { Title = Fmt.TypeLabel(t), Badge = dep, Lines = { "Refundable security deposit" } });
        }
        r.Items.Add(new BotItem
        {
            Title = "Service tax",
            Badge = Fmt.ServiceTaxRate.ToString("0.##") + "%",
            Lines = { "Applied to the whole bill (rental + usage − discount)" },
        });
        r.Items.Add(new BotItem
        {
            Title = "Corporate / bulk discount",
            Lines = { "10–14 lines: 25%", "15–24 lines: 50%", "25–50 lines: 75%", "More than 50 lines: 100%" },
        });
        r.Buttons.Add(new BotButton("View plans", "plans"));
        r.Buttons.Add(role == AppRoles.Customer ? new BotButton("My bills", "my bills") : new BotButton("How to apply", "how do i apply"));
        return r;
    }

    private async Task<BotReply> OutletsAsync(string role)
    {
        var shops = await _db.RetailShops.AsNoTracking().OrderBy(s => s.City).ThenBy(s => s.Name).Take(8).ToListAsync();
        if (shops.Count == 0) return new BotReply { Text = "No retail outlets are listed yet.", Buttons = Menu(role) };
        var r = new BotReply { Text = "Our retail outlets:" };
        foreach (var s in shops)
        {
            var item = new BotItem { Title = s.Name, Badge = s.City, Lines = { s.Address, $"Phone: {s.Phone}" } };
            if (role == AppRoles.Admin && !string.IsNullOrWhiteSpace(s.Manager)) item.Lines.Add($"Manager: {s.Manager}");
            r.Items.Add(item);
        }
        r.Buttons.Add(new BotButton("View plans", "plans"));
        r.Buttons.Add(new BotButton("Contact us", "complaint"));
        return r;
    }

    private static BotReply Coverage()
    {
        var r = new BotReply { Text = "We provide service in these cities / areas:" };
        r.Items.Add(new BotItem { Title = "Coverage", Lines = Fmt.Cities.Select(c => c.Name).ToList() });
        r.Buttons.Add(new BotButton("Find an outlet", "outlets"));
        r.Buttons.Add(new BotButton("View plans", "plans"));
        return r;
    }

    /* ----------------------------------- FAQ -------------------------------- */

    private static readonly Dictionary<string, (string Question, string Answer)> Faq = new()
    {
        ["apply"] = ("How do I get a new connection?",
            "Pick a plan on the Plans page, then sign in and place the order from the customer portal — or visit any retail outlet. After ordering you receive an Order ID that you can use to track the status."),
        ["time"] = ("How long does a connection take?",
            "After your order, the technical team first runs a feasibility survey. If the area is feasible, the installation team contacts you within 48 hours and the connection is then activated."),
        ["docs"] = ("Which documents do I need?",
            "Your name, phone number, full address, city and an ID proof. Corporate orders (10+ lines) also get a bulk discount."),
        ["pay"] = ("How do I pay my bill?",
            "You can pay at any retail outlet or at the accounts desk. Signed-in customers can see their bills and the amount due under My Bills in the portal."),
        ["tax"] = ("How much is the service tax?",
            $"A service tax of {Fmt.ServiceTaxRate:0.##}% applies to the whole bill."),
        ["bulk"] = ("What is the corporate / bulk discount?",
            "10–14 lines get 25%, 15–24 lines 50%, 25–50 lines 75%, and more than 50 lines 100%."),
    };

    private static BotReply FaqMenu()
    {
        var r = new BotReply { Text = "Frequently asked questions — pick one:" };
        foreach (var kv in Faq) r.Buttons.Add(new BotButton(kv.Value.Question, "faq " + kv.Key));
        return r;
    }

    private static BotReply FaqAnswer(string key)
    {
        var f = Faq[key];
        var r = new BotReply { Text = f.Answer };
        r.Items.Add(new BotItem { Title = f.Question });
        r.Buttons.Add(new BotButton("More FAQs", "faq"));
        r.Buttons.Add(new BotButton("View plans", "plans"));
        r.Buttons.Add(new BotButton("Contact us", "complaint"));
        return r;
    }

    /* --------------------- order / account / invoice look-ups ----------------- */

    private async Task<BotReply> OrderLookupAsync(string role, string orderNo)
    {
        var query = _db.Orders.AsNoTracking().Include(x => x.Plan).Include(x => x.Customer).Where(x => x.OrderNo == orderNo);
        if (role == AppRoles.Customer)
        {
            var cust = await CurrentCustomerAsync();
            var cid = cust?.Id ?? -1;
            query = query.Where(x => x.CustomerId == cid);
        }
        else if (role == AppRoles.Retail)
        {
            var shop = ShopIdValue;
            query = query.Where(x => x.ShopId == shop);
        }
        var o = await query.FirstOrDefaultAsync();
        if (o == null)
            return new BotReply
            {
                Text = role switch
                {
                    AppRoles.Customer => $"I couldn't find order {orderNo} on your account. Check the ID, or ask for \"my orders\".",
                    AppRoles.Retail => $"I couldn't find order {orderNo} for your shop. Check the ID and try again.",
                    _ => $"No order was found with ID {orderNo}. Please check it (for example B0000000001).",
                },
                Buttons = Menu(role),
            };

        var r = new BotReply { Text = $"Order {o.OrderNo}: {ChatStatus(o.Status)}" };
        r.Steps = o.Status switch
        {
            OrderStatus.Pending => Steps("done", "current", "todo"),
            OrderStatus.Feasible => Steps("done", "done", "current"),
            OrderStatus.Connected => Steps("done", "done", "done"),
            OrderStatus.Infeasible => Steps("done", "fail", "todo"),
            _ => Steps("done", "todo", "todo"),
        };

        var item = new BotItem { Title = o.OrderNo, Badge = ChatStatus(o.Status) };
        item.Lines.Add(o.Plan != null ? $"{Fmt.TypeLabel(o.ConnectionType)} · {o.Plan.Name}" : Fmt.TypeLabel(o.ConnectionType));
        item.Lines.Add($"Applied: {Fmt.Date(o.CreatedAt)}");
        if (o.Quantity > 1) item.Lines.Add($"Lines: {o.Quantity}");
        if (role is not (Pub or AppRoles.Customer)) item.Lines.Add($"Customer: {o.Customer.Name}"); // staff only — never on the public side
        if (o.Status == OrderStatus.Connected)
        {
            var conn = await _db.Connections.AsNoTracking().FirstOrDefaultAsync(c => c.OrderId == o.Id);
            if (conn != null)
            {
                item.Lines.Add($"Account ID: {conn.AccountId}");
                item.Lines.Add($"Activated: {Fmt.Date(conn.ActivatedAt)}");
            }
        }
        else if (o.Status == OrderStatus.Feasible) item.Lines.Add("The installation team will contact the customer within 48 hours.");
        else if (o.Status == OrderStatus.Pending) item.Lines.Add("The technical team's feasibility survey is still pending.");
        else if (o.Status == OrderStatus.Infeasible) item.Lines.Add("Service is not currently possible in this area.");
        r.Items.Add(item);

        r.Buttons.Add(new BotButton("Full track page", Href: "/track?q=" + Uri.EscapeDataString(o.OrderNo)));
        r.Buttons.Add(new BotButton("Contact us", "complaint"));
        return r;
    }

    private async Task<BotReply> AccountLookupAsync(string role, string accountId)
    {
        var query = _db.Connections.AsNoTracking().Include(x => x.Plan).Where(x => x.AccountId == accountId);
        if (role == AppRoles.Customer)
        {
            var cust = await CurrentCustomerAsync();
            var cid = cust?.Id ?? -1;
            query = query.Where(x => x.CustomerId == cid);
        }
        else if (role == AppRoles.Retail)
        {
            var shop = ShopIdValue;
            query = query.Where(x => x.Order != null && x.Order.ShopId == shop);
        }
        var c = await query.FirstOrDefaultAsync();
        if (c == null)
            return new BotReply
            {
                Text = role switch
                {
                    AppRoles.Customer => $"I couldn't find account {accountId} on your profile. Check the ID, or ask for \"my connections\".",
                    AppRoles.Retail => $"I couldn't find account {accountId} for your shop. Check the ID and try again.",
                    _ => $"No connection was found with account ID {accountId}. Please check it and try again.",
                },
                Buttons = Menu(role),
            };

        var item = new BotItem { Title = c.AccountId, Badge = Fmt.ConnStatus(c.Status) };
        item.Lines.Add(c.Plan != null ? $"{Fmt.TypeLabel(c.ConnectionType)} · {c.Plan.Name}" : Fmt.TypeLabel(c.ConnectionType));
        item.Lines.Add($"Activated: {Fmt.Date(c.ActivatedAt)}");
        if (CanSeeDue(role))
        {
            var open = await _db.Bills.AsNoTracking().Where(b => b.ConnectionId == c.Id && b.Status != BillStatus.Paid).ToListAsync();
            item.Lines.Add(open.Count == 0 ? "No bills due" : $"Due: {Fmt.Money(open.Sum(b => b.DueAmount))} ({open.Count} bill{(open.Count == 1 ? "" : "s")})");
        }
        var r = new BotReply { Text = $"Account {c.AccountId}:" };
        r.Items.Add(item);
        if (role == Pub) r.Text += " Sign in to see billing details for your own account.";
        r.Buttons.Add(new BotButton("Full track page", Href: "/track?q=" + Uri.EscapeDataString(c.AccountId)));
        r.Buttons.Add(new BotButton("Contact us", "complaint"));
        return r;
    }

    private async Task<BotReply> BillLookupAsync(string role, string billNo)
    {
        if (role == AppRoles.Technical) return Outside(role, ChatTopic.Billing);
        if (role == Pub) return await BillingAsync(role, "bill");

        var query = _db.Bills.AsNoTracking().Include(b => b.Connection).Where(b => b.BillNo == billNo);
        if (role == AppRoles.Customer)
        {
            var cust = await CurrentCustomerAsync();
            var cid = cust?.Id ?? -1;
            query = query.Where(b => b.Connection.CustomerId == cid);
        }
        else if (role == AppRoles.Retail)
        {
            var shop = ShopIdValue;
            query = query.Where(b => b.Connection.Order != null && b.Connection.Order.ShopId == shop);
        }
        var b = await query.FirstOrDefaultAsync();
        if (b == null)
            return new BotReply { Text = $"I couldn't find invoice {billNo}{(role is AppRoles.Customer or AppRoles.Retail ? " in your scope" : "")}. Check the number and try again.", Buttons = Menu(role) };

        var overdue = b.Status != BillStatus.Paid && b.DueDate != null && b.DueDate.Value < DateTime.UtcNow;
        var item = new BotItem
        {
            Title = b.BillNo,
            Badge = Fmt.BillStatus(b.Status),
            Lines =
            {
                $"Account: {b.Connection.AccountId} · {b.Period}",
                $"Rental {Fmt.Money(b.Rental)} + usage {Fmt.Money(b.Usage)} − discount {Fmt.Money(b.Discount)} + tax {Fmt.Money(b.Tax)}",
                $"Total {Fmt.Money(b.Total)} · paid {Fmt.Money(b.PaidAmount)} · due {Fmt.Money(b.DueAmount)}",
                b.DueDate == null ? "Due date: —" : $"Due date: {Fmt.Date(b.DueDate)}{(overdue ? " (overdue)" : "")}",
            },
        };
        var r = new BotReply { Text = $"Invoice {b.BillNo}:" };
        r.Items.Add(item);
        r.Buttons.Add(new BotButton("Open invoice", Href: "/invoice/" + b.Id));
        r.Buttons.Add(new BotButton("Menu", "help"));
        return r;
    }

    private static BotReply TrackPrompt(string role)
    {
        var r = new BotReply
        {
            Text = "Type an Order ID (for example B0000000001) and I'll show its status — Applied → Survey → Activated. " +
                   "An Account ID (for example B101000000000001) gives you the connection details.",
        };
        if (role == Pub) r.Buttons.Add(new BotButton("Sign in", Href: "/login"));
        r.Buttons.Add(new BotButton("Track page", Href: "/track"));
        return r;
    }

    private static List<BotStep> Steps(string applied, string survey, string activated) =>
        new() { new("Applied", applied), new("Survey", survey), new("Activated", activated) };

    private static string ChatStatus(string s) => s switch
    {
        OrderStatus.Pending => "Applied — survey pending",
        OrderStatus.Feasible => "Survey complete — installation pending",
        OrderStatus.Connected => "Activated",
        OrderStatus.Infeasible => "Survey: area not feasible",
        OrderStatus.Cancelled => "Cancelled",
        _ => Fmt.OrderStatus(s),
    };

    /* ----------------------------- orders (per role) -------------------------- */

    private async Task<BotReply> OrdersAsync(string role) => role switch
    {
        AppRoles.Customer => await MyOrdersAsync(),
        AppRoles.Retail => await OrdersOverviewAsync(role, ShopIdValue),
        AppRoles.Technical or AppRoles.Admin => await OrdersOverviewAsync(role, null),
        _ => TrackPrompt(role),   // public + accounts: status by ID only
    };

    private async Task<BotReply> MyOrdersAsync()
    {
        var cust = await CurrentCustomerAsync();
        if (cust == null) return new BotReply { Text = "Your customer profile isn't set up yet, so there are no orders to show.", Buttons = Menu(AppRoles.Customer) };

        var orders = await _db.Orders.AsNoTracking().Include(o => o.Plan)
            .Where(o => o.CustomerId == cust.Id).OrderByDescending(o => o.CreatedAt).Take(5).ToListAsync();
        if (orders.Count == 0)
            return new BotReply { Text = "You haven't placed any orders yet.", Buttons = { new BotButton("View plans", "plans"), new BotButton("How to apply", "how do i apply") } };

        var total = await _db.Orders.CountAsync(o => o.CustomerId == cust.Id);
        var r = new BotReply { Text = total > orders.Count ? $"Your latest {orders.Count} orders (of {total}):" : $"Your {orders.Count} order{(orders.Count == 1 ? "" : "s")}:" };
        foreach (var o in orders)
        {
            r.Items.Add(new BotItem
            {
                Title = o.OrderNo,
                Badge = ChatStatus(o.Status),
                Lines =
                {
                    o.Plan != null ? $"{Fmt.TypeLabel(o.ConnectionType)} · {o.Plan.Name}" : Fmt.TypeLabel(o.ConnectionType),
                    $"Applied: {Fmt.Date(o.CreatedAt)}",
                },
            });
        }
        r.Text += " Type an order's ID for its details.";
        r.Buttons.Add(new BotButton("Open My Orders", Href: "/portal#orders"));
        r.Buttons.Add(new BotButton("My bills", "my bills"));
        return r;
    }

    /// <summary>Order counts by status + the 5 most recent orders. shopId = limit to one retail shop.</summary>
    private async Task<BotReply> OrdersOverviewAsync(string role, int? shopId)
    {
        IQueryable<Order> baseQ = _db.Orders.AsNoTracking();
        if (shopId != null) baseQ = baseQ.Where(o => o.ShopId == shopId);

        var counts = (await baseQ.GroupBy(o => o.Status).Select(g => new { g.Key, Count = g.Count() }).ToListAsync())
            .ToDictionary(x => x.Key, x => x.Count);
        int N(string s) => counts.TryGetValue(s, out var n) ? n : 0;
        var total = counts.Values.Sum();
        var recent = await baseQ.Include(o => o.Customer).Include(o => o.Plan).OrderByDescending(o => o.CreatedAt).Take(5).ToListAsync();

        var r = new BotReply { Text = total == 0 ? (shopId != null ? "Your shop has no orders yet." : "There are no orders yet.") : $"{(shopId != null ? "Your shop has" : "There are")} {total} order{(total == 1 ? "" : "s")} in total:" };
        r.Items.Add(new BotItem
        {
            Title = shopId != null ? "Your shop's orders by status" : "Orders by status",
            Badge = total.ToString(),
            Lines =
            {
                $"Feasibility pending: {N(OrderStatus.Pending)}",
                $"Feasible (awaiting installation): {N(OrderStatus.Feasible)}",
                $"Connected: {N(OrderStatus.Connected)}",
                $"Not feasible: {N(OrderStatus.Infeasible)} · Cancelled: {N(OrderStatus.Cancelled)}",
            },
        });
        foreach (var o in recent)
            r.Items.Add(new BotItem
            {
                Title = o.OrderNo,
                Badge = ChatStatus(o.Status),
                Lines =
                {
                    o.Plan != null ? $"{Fmt.TypeLabel(o.ConnectionType)} · {o.Plan.Name}" : Fmt.TypeLabel(o.ConnectionType),
                    $"Customer: {o.Customer.Name}",
                    $"Applied: {Fmt.Date(o.CreatedAt)}",
                },
            });
        if (recent.Count > 0) r.Text += " Latest orders below. Type an order ID for its details.";

        if (role == AppRoles.Retail)
        {
            r.Buttons.Add(new BotButton("Open Order Book", Href: TabLink(role, "book")));
            r.Buttons.Add(new BotButton("Place order", "place order"));
        }
        else if (role == AppRoles.Technical)
        {
            r.Buttons.Add(new BotButton("Open Order Tracker", Href: TabLink(role, "orders")));
            r.Buttons.Add(new BotButton("Feasibility queue", "feasibility queue"));
        }
        else
        {
            r.Buttons.Add(new BotButton("Open Orders", Href: TabLink(role, "orders")));
            r.Buttons.Add(new BotButton("Overview", "overview"));
        }
        return r;
    }

    /* --------------------------- connections (per role) ----------------------- */

    private async Task<BotReply> ConnectionsAsync(string role, string q) => role switch
    {
        Pub => await PlansAsync(q, role),
        AppRoles.Customer => await MyConnectionsAsync(),
        AppRoles.Retail => await ConnectionsOverviewAsync(role, ShopIdValue),
        _ => await ConnectionsOverviewAsync(role, null),
    };

    private async Task<BotReply> MyConnectionsAsync()
    {
        var cust = await CurrentCustomerAsync();
        if (cust == null) return new BotReply { Text = "Your customer profile isn't set up yet, so there are no connections to show.", Buttons = Menu(AppRoles.Customer) };

        var conns = await _db.Connections.AsNoTracking().Include(c => c.Plan)
            .Where(c => c.CustomerId == cust.Id).OrderByDescending(c => c.CreatedAt).Take(6).ToListAsync();
        if (conns.Count == 0)
            return new BotReply { Text = "You don't have any active connections yet. Once an order is activated it appears here.", Buttons = { new BotButton("My orders", "my orders"), new BotButton("View plans", "plans") } };

        var r = new BotReply { Text = $"Your connection{(conns.Count == 1 ? "" : "s")}:" };
        foreach (var c in conns)
            r.Items.Add(new BotItem
            {
                Title = c.AccountId,
                Badge = Fmt.ConnStatus(c.Status),
                Lines =
                {
                    c.Plan != null ? $"{Fmt.TypeLabel(c.ConnectionType)} · {c.Plan.Name}" : Fmt.TypeLabel(c.ConnectionType),
                    $"Activated: {Fmt.Date(c.ActivatedAt)}",
                },
            });
        r.Buttons.Add(new BotButton("Open My Connections", Href: "/portal#connections"));
        r.Buttons.Add(new BotButton("My bills", "my bills"));
        return r;
    }

    private async Task<BotReply> ConnectionsOverviewAsync(string role, int? shopId)
    {
        IQueryable<Connection> baseQ = _db.Connections.AsNoTracking();
        if (shopId != null) baseQ = baseQ.Where(c => c.Order != null && c.Order.ShopId == shopId);

        var byStatus = (await baseQ.GroupBy(c => c.Status).Select(g => new { g.Key, Count = g.Count() }).ToListAsync()).ToDictionary(x => x.Key, x => x.Count);
        var byType = (await baseQ.GroupBy(c => c.ConnectionType).Select(g => new { g.Key, Count = g.Count() }).ToListAsync()).ToDictionary(x => x.Key, x => x.Count);
        int S(string k) => byStatus.TryGetValue(k, out var n) ? n : 0;
        int T(string k) => byType.TryGetValue(k, out var n) ? n : 0;
        var total = byStatus.Values.Sum();
        var recent = await baseQ.Include(c => c.Plan).OrderByDescending(c => c.CreatedAt).Take(5).ToListAsync();

        var r = new BotReply { Text = total == 0 ? "There are no connections yet." : $"{(shopId != null ? "Your shop's" : "All")} connections — {total} in total:" };
        r.Items.Add(new BotItem
        {
            Title = "Connections by status",
            Badge = total.ToString(),
            Lines =
            {
                $"Active: {S(ConnStatus.Active)}",
                $"Temporarily inactive: {S(ConnStatus.TemporarilyInactive)} · Permanently inactive: {S(ConnStatus.PermanentlyInactive)}",
                $"Dial-Up {T(ConnTypes.Dialup)} · Broadband {T(ConnTypes.Broadband)} · Landline {T(ConnTypes.Landline)}",
            },
        });
        foreach (var c in recent)
            r.Items.Add(new BotItem
            {
                Title = c.AccountId,
                Badge = Fmt.ConnStatus(c.Status),
                Lines =
                {
                    c.Plan != null ? $"{Fmt.TypeLabel(c.ConnectionType)} · {c.Plan.Name}" : Fmt.TypeLabel(c.ConnectionType),
                    $"Activated: {Fmt.Date(c.ActivatedAt)}",
                },
            });
        if (recent.Count > 0) r.Text += " Latest below. Type an account ID for its details.";

        var tab = role switch { AppRoles.Technical => "grid", AppRoles.Retail => "connections", AppRoles.Admin => "connections", _ => (string?)null };
        if (tab != null) r.Buttons.Add(new BotButton("Open Connections", Href: TabLink(role, tab)));
        r.Buttons.Add(new BotButton("Menu", "help"));
        return r;
    }

    /* ------------------------------ billing (per role) ------------------------ */

    private async Task<BotReply> BillingAsync(string role, string q)
    {
        switch (role)
        {
            case Pub:
                return new BotReply
                {
                    Text = "Please sign in to see your bills and payments. You can pay at any retail outlet or at the accounts desk.",
                    Buttons = { new BotButton("Sign in", Href: "/login"), new BotButton("How to pay", "faq pay"), new BotButton("Find an outlet", "outlets") },
                };
            case AppRoles.Customer: return await CustomerBillsAsync();
            case AppRoles.Retail: return await ShopCollectionsAsync();
            case AppRoles.Accounts:
            case AppRoles.Admin:
                if (ChatIntent.Has(q, "receipt", "payment", "paid", "cash")) return await ReceiptsAsync(role);
                if (ChatIntent.Has(q, "overdue", "outstanding", "due", "dues")) return await DuesAsync(role);
                return await BillingSummaryAsync();
            default: return Outside(role, ChatTopic.Billing);
        }
    }

    private async Task<BotReply> CustomerBillsAsync()
    {
        var cust = await CurrentCustomerAsync();
        if (cust == null) return new BotReply { Text = "Your customer profile isn't set up yet, so there are no bills to show.", Buttons = Menu(AppRoles.Customer) };

        var connIds = await _db.Connections.AsNoTracking().Where(c => c.CustomerId == cust.Id).Select(c => c.Id).ToListAsync();
        if (connIds.Count == 0)
            return new BotReply { Text = "You don't have an active connection yet, so there are no bills.", Buttons = Menu(AppRoles.Customer) };

        var open = await _db.Bills.AsNoTracking().Include(b => b.Connection)
            .Where(b => connIds.Contains(b.ConnectionId) && b.Status != BillStatus.Paid)
            .OrderBy(b => b.DueDate).ToListAsync();

        if (open.Count == 0)
            return new BotReply { Text = "You have no bills due. Thank you!", Buttons = { new BotButton("My orders", "my orders"), new BotButton("Open My Bills", Href: "/portal#bills") } };

        var r = new BotReply { Text = $"Your total due is {Fmt.Money(open.Sum(b => b.DueAmount))} ({open.Count} bill{(open.Count == 1 ? "" : "s")})." };
        foreach (var b in open.Take(5))
        {
            var overdue = b.DueDate != null && b.DueDate.Value < DateTime.UtcNow;
            r.Items.Add(new BotItem
            {
                Title = b.BillNo,
                Badge = Fmt.Money(b.DueAmount),
                Lines =
                {
                    $"{b.Period} · {Fmt.BillStatus(b.Status)}",
                    $"Account: {b.Connection.AccountId}",
                    b.DueDate == null ? "Due date: —" : $"Due date: {Fmt.Date(b.DueDate)}{(overdue ? " (overdue)" : "")}",
                },
            });
        }
        r.Buttons.Add(new BotButton("Open My Bills", Href: "/portal#bills"));
        r.Buttons.Add(new BotButton("How to pay", "faq pay"));
        return r;
    }

    /// <summary>Accounts desk (and admin): invoices, outstanding, collected</summary>
    private async Task<BotReply> BillingSummaryAsync()
    {
        var now = DateTime.UtcNow;
        var invoices = await _db.Bills.CountAsync();
        var open = await _db.Bills.CountAsync(b => b.Status != BillStatus.Paid);
        var overdue = await _db.Bills.CountAsync(b => b.Status != BillStatus.Paid && b.DueDate != null && b.DueDate < now);
        var outstanding = await _db.Bills.Where(b => b.Status != BillStatus.Paid).SumAsync(b => b.DueAmount);
        var collected = await _db.Payments.SumAsync(p => p.Amount);
        var counter = await _db.Payments.Where(p => p.Channel == "retail").SumAsync(p => p.Amount);
        var office = await _db.Payments.Where(p => p.Channel == "office").SumAsync(p => p.Amount);

        var r = new BotReply { Text = "Billing summary:" };
        r.Items.Add(new BotItem { Title = "Invoices", Badge = invoices.ToString(), Lines = { $"{open} open · {overdue} overdue" } });
        r.Items.Add(new BotItem { Title = "Outstanding", Badge = Fmt.Money(outstanding), Lines = { $"Across {open} open invoice{(open == 1 ? "" : "s")}" } });
        r.Items.Add(new BotItem
        {
            Title = "Collected",
            Badge = Fmt.Money(collected),
            Lines = { $"At retail counters: {Fmt.Money(counter)}", $"At the accounts office: {Fmt.Money(office)}" },
        });
        r.Items.Add(new BotItem { Title = "Service tax", Badge = Fmt.ServiceTaxRate.ToString("0.##") + "%", Lines = { "Applied on rental + usage − discount" } });
        r.Buttons.Add(new BotButton("Customer dues", "customer dues"));
        r.Buttons.Add(new BotButton("Receipts", "receipts"));
        r.Buttons.Add(new BotButton("Generate invoice", "generate invoice"));
        return r;
    }

    private async Task<BotReply> DuesAsync(string role)
    {
        var now = DateTime.UtcNow;
        var open = await _db.Bills.AsNoTracking().Include(b => b.Connection).ThenInclude(c => c.Customer)
            .Where(b => b.Status != BillStatus.Paid).ToListAsync();
        if (open.Count == 0) return new BotReply { Text = "No customer has any dues right now.", Buttons = Menu(role) };

        var rows = open.GroupBy(b => b.Connection.CustomerId).Select(g => new
        {
            Name = g.First().Connection.Customer.Name,
            Count = g.Count(),
            Due = g.Sum(x => x.DueAmount),
            Earliest = g.Min(x => x.DueDate),
            Overdue = g.Any(x => x.DueDate != null && x.DueDate < now),
        }).OrderByDescending(x => x.Due).ToList();

        var r = new BotReply { Text = $"{rows.Count} customer{(rows.Count == 1 ? "" : "s")} with dues — {Fmt.Money(open.Sum(b => b.DueAmount))} in total. Highest first:" };
        foreach (var x in rows.Take(5))
            r.Items.Add(new BotItem
            {
                Title = x.Name,
                Badge = Fmt.Money(x.Due),
                Lines = { $"{x.Count} open bill{(x.Count == 1 ? "" : "s")}", x.Earliest == null ? "Earliest due date: —" : $"Earliest due date: {Fmt.Date(x.Earliest)}{(x.Overdue ? " (overdue)" : "")}" },
            });
        if (role == AppRoles.Accounts) r.Buttons.Add(new BotButton("Open Customer Dues", Href: TabLink(role, "dues")));
        r.Buttons.Add(new BotButton("Billing summary", "billing summary"));
        return r;
    }

    private async Task<BotReply> ReceiptsAsync(string role)
    {
        var pays = await _db.Payments.AsNoTracking().Include(p => p.Bill).OrderByDescending(p => p.CreatedAt).Take(5).ToListAsync();
        if (pays.Count == 0) return new BotReply { Text = "No payments have been received yet.", Buttons = Menu(role) };

        var r = new BotReply { Text = "Most recent receipts:" };
        foreach (var p in pays)
        {
            var item = new BotItem
            {
                Title = p.Bill.BillNo,
                Badge = Fmt.Money(p.Amount),
                Lines = { $"{Cap(p.Method)} · {(p.Channel == "retail" ? "Retail counter" : p.Channel == "online" ? "Online (demo)" : "Accounts office")}", $"Received: {Fmt.Stamp(p.CreatedAt)}" },
            };
            if (!string.IsNullOrWhiteSpace(p.Reference)) item.Lines.Add($"Reference: {p.Reference}");
            r.Items.Add(item);
        }
        if (role == AppRoles.Accounts) r.Buttons.Add(new BotButton("Open Receipts", Href: TabLink(role, "receipts")));
        r.Buttons.Add(new BotButton("Billing summary", "billing summary"));
        return r;
    }

    private async Task<BotReply> InvoicingAsync(string q)
    {
        var role = Audience;
        if (ChatIntent.Has(q, "customer dues", "defaulter", "who owes", "dues list")) return await DuesAsync(role);

        var r = new BotReply { Text = "How to generate an invoice:" };
        r.Items.Add(new BotItem
        {
            Title = "Generate Invoice",
            Lines =
            {
                "1. Open the Generate Invoice tab and choose the connection (permanently inactive connections can't be billed).",
                "2. The rental defaults to the plan price; add usage or a discount if needed and set the billing period.",
                $"3. Service tax of {Fmt.ServiceTaxRate:0.##}% is added on rental + usage − discount.",
                "4. The invoice is due 10 days after it is generated and appears in the Invoice Ledger.",
                "5. Payments (cash, card, UPI, cheque or online) are recorded against the invoice and show under Receipts.",
            },
        });
        if (role == AppRoles.Accounts) r.Buttons.Add(new BotButton("Open Generate Invoice", Href: TabLink(role, "generate")));
        r.Buttons.Add(new BotButton("Customer dues", "customer dues"));
        r.Buttons.Add(new BotButton("Billing summary", "billing summary"));
        return r;
    }

    /* --------------------------------- retail desk ---------------------------- */

    private async Task<BotReply> ShopCollectionsAsync()
    {
        var shop = ShopIdValue;
        var name = Session.Name;
        var open = await _db.Bills.AsNoTracking()
            .Where(b => b.Status != BillStatus.Paid && b.Connection.Order != null && b.Connection.Order.ShopId == shop).ToListAsync();
        var mine = await _db.Payments
            .Where(p => p.Channel == "retail" && p.ReceivedBy == name && p.Bill.Connection.Order != null && p.Bill.Connection.Order.ShopId == shop)
            .SumAsync(p => p.Amount);

        var r = new BotReply { Text = "Collections for your shop:" };
        r.Items.Add(new BotItem { Title = "Collected by you at the counter", Badge = Fmt.Money(mine) });
        r.Items.Add(new BotItem { Title = "Still due on your shop's connections", Badge = Fmt.Money(open.Sum(b => b.DueAmount)), Lines = { $"{open.Count} open bill{(open.Count == 1 ? "" : "s")}" } });
        r.Buttons.Add(new BotButton("Open Collections", Href: TabLink(AppRoles.Retail, "collections")));
        r.Buttons.Add(new BotButton("Order book", "order book"));
        return r;
    }

    private BotReply PlaceOrderGuide(string role)
    {
        if (role == AppRoles.Admin)
            return new BotReply
            {
                Text = "Orders are placed by retail staff on behalf of customers, or by customers themselves in the portal. You can review every order in the Orders tab.",
                Buttons = { new BotButton("Open Orders", Href: TabLink(role, "orders")), new BotButton("Orders", "orders") },
            };

        var r = new BotReply { Text = "How to place an order for a customer:" };
        r.Items.Add(new BotItem
        {
            Title = "Place Order",
            Lines =
            {
                "1. Open the Place Order tab and pick an existing customer, or enter the new customer's name, phone, address and city.",
                "2. Choose the plan (it decides the connection type) and the number of lines.",
                "3. Orders of 10+ lines are corporate: 25% (10–14), 50% (15–24), 75% (25–50), 100% (more than 50 lines).",
                "4. Submit — an Order ID is generated. Track it in the Order Book.",
            },
        });
        r.Buttons.Add(new BotButton("Open Place Order", Href: TabLink(role, "order")));
        r.Buttons.Add(new BotButton("Order book", "order book"));
        return r;
    }

    private async Task<BotReply> CustomersAsync(string role)
    {
        var since = DateTime.UtcNow.AddDays(-30);
        var total = await _db.Customers.CountAsync();
        var recent = await _db.Customers.CountAsync(c => c.CreatedAt >= since);
        var cities = await _db.Customers.AsNoTracking().GroupBy(c => c.City)
            .Select(g => new { City = g.Key, Count = g.Count() }).OrderByDescending(x => x.Count).Take(4).ToListAsync();

        var r = new BotReply { Text = "Customers:" };
        var item = new BotItem { Title = "Customer base", Badge = total.ToString(), Lines = { $"{recent} new in the last 30 days" } };
        foreach (var c in cities) item.Lines.Add($"{c.City}: {c.Count}");
        r.Items.Add(item);
        if (role == AppRoles.Retail)
        {
            r.Items.Add(new BotItem { Title = "Customers tab", Lines = { "Search by name or phone, and add or edit a customer's contact details before placing an order." } });
            r.Buttons.Add(new BotButton("Open Customers", Href: TabLink(role, "customers")));
            r.Buttons.Add(new BotButton("Place order", "place order"));
        }
        else
        {
            r.Buttons.Add(new BotButton("Open Customers", Href: TabLink(role, "customers")));
            r.Buttons.Add(new BotButton("Overview", "overview"));
        }
        return r;
    }

    /* ------------------------------- technical desk --------------------------- */

    private async Task<BotReply> FeasibilityAsync(string role)
    {
        var pending = await _db.Orders.CountAsync(o => o.Status == OrderStatus.Pending);
        var feasible = await _db.Orders.CountAsync(o => o.Status == OrderStatus.Feasible);
        var latest = await _db.Orders.AsNoTracking().Include(o => o.Customer).Include(o => o.Plan)
            .Where(o => o.Status == OrderStatus.Pending).OrderByDescending(o => o.CreatedAt).Take(5).ToListAsync();

        var r = new BotReply { Text = pending == 0 ? "The feasibility queue is empty." : $"{pending} order{(pending == 1 ? " is" : "s are")} waiting for a feasibility survey." };
        r.Items.Add(new BotItem { Title = "Feasibility queue", Badge = pending.ToString(), Lines = { $"Feasible, ready to provision: {feasible}" } });
        foreach (var o in latest)
            r.Items.Add(new BotItem
            {
                Title = o.OrderNo,
                Badge = $"{o.Quantity} line{(o.Quantity == 1 ? "" : "s")}",
                Lines = { o.Plan != null ? $"{Fmt.TypeLabel(o.ConnectionType)} · {o.Plan.Name}" : Fmt.TypeLabel(o.ConnectionType), $"{o.Customer.Name} · {o.Customer.City}", $"Applied: {Fmt.Date(o.CreatedAt)}" },
            });
        r.Items.Add(new BotItem
        {
            Title = "How provisioning works",
            Lines =
            {
                "1. Feasibility Queue: review each pending order and mark it feasible or not feasible.",
                "2. Provision Deck: for feasible orders, create the connection — an account ID is generated.",
                "3. A bulk order creates one account per line, so enough equipment must be in stock.",
                "4. Connection Grid: set a connection to active, temporarily inactive or permanently inactive.",
            },
        });
        if (role == AppRoles.Technical)
        {
            r.Buttons.Add(new BotButton("Open Feasibility Queue", Href: TabLink(role, "queue")));
            r.Buttons.Add(new BotButton("Open Provision Deck", Href: TabLink(role, "provision")));
        }
        r.Buttons.Add(new BotButton("Low stock", "low stock"));
        return r;
    }

    private async Task<BotReply> EquipmentAsync(string role)
    {
        var items = await _db.Equipment.AsNoTracking().Include(e => e.Vendor).OrderBy(e => e.Name).ToListAsync();
        if (items.Count == 0) return new BotReply { Text = "No equipment is on record yet.", Buttons = Menu(role) };

        var low = items.Where(e => e.Stock <= e.ReorderLevel).ToList();
        var r = new BotReply { Text = low.Count == 0 ? "All equipment is above its reorder level." : $"{low.Count} item{(low.Count == 1 ? " is" : "s are")} at or below the reorder level:" };

        foreach (var e in low.Take(6))
            r.Items.Add(new BotItem
            {
                Title = e.Name,
                Badge = $"{e.Stock} in stock · reorder at {e.ReorderLevel}",
                Lines = { Cap(e.Type), e.Vendor != null ? $"Vendor: {e.Vendor.Name}" : "No vendor set" },
            });

        var byType = items.GroupBy(e => e.Type).OrderBy(g => g.Key)
            .Select(g => $"{Cap(g.Key)}: {g.Sum(x => x.Stock)} units").ToList();
        r.Items.Add(new BotItem { Title = "Stock by type", Badge = $"{items.Sum(e => e.Stock)} units", Lines = byType });

        if (role == AppRoles.Technical)
        {
            r.Buttons.Add(new BotButton("Open Equipment Bay", Href: TabLink(role, "equipment")));
            r.Buttons.Add(new BotButton("Feasibility queue", "feasibility queue"));
        }
        else
        {
            r.Buttons.Add(new BotButton("Procurement", "procurement"));
            r.Buttons.Add(new BotButton("Open Equipment & Stock", Href: TabLink(role, "equipment")));
        }
        return r;
    }

    /* ------------------------------------ admin ------------------------------- */

    private async Task<BotReply> OverviewAsync(string role)
    {
        switch (role)
        {
            case AppRoles.Admin: return await AdminOverviewAsync();
            case AppRoles.Accounts: return await BillingSummaryAsync();
            case AppRoles.Retail: return await OrdersOverviewAsync(role, ShopIdValue);
            case AppRoles.Technical: return await TechOverviewAsync();
            case AppRoles.Customer: return await CustomerOverviewAsync();
            default: return AboutReply(role);
        }
    }

    private async Task<BotReply> AdminOverviewAsync()
    {
        var orders = await _db.Orders.CountAsync();
        var pending = await _db.Orders.CountAsync(o => o.Status == OrderStatus.Pending);
        var feasible = await _db.Orders.CountAsync(o => o.Status == OrderStatus.Feasible);
        var connected = await _db.Orders.CountAsync(o => o.Status == OrderStatus.Connected);
        var live = await _db.Connections.CountAsync(c => c.Status == ConnStatus.Active);
        var conns = await _db.Connections.CountAsync();
        var outstanding = await _db.Bills.Where(b => b.Status != BillStatus.Paid).SumAsync(b => b.DueAmount);
        var collected = await _db.Bills.SumAsync(b => b.PaidAmount);
        var customers = await _db.Customers.CountAsync();
        var employees = await _db.Employees.CountAsync();
        var vendors = await _db.Vendors.CountAsync();
        var shops = await _db.RetailShops.CountAsync();
        var stock = await _db.Equipment.SumAsync(e => e.Stock);
        var lowStock = await _db.Equipment.CountAsync(e => e.Stock <= e.ReorderLevel);
        var ratings = await _db.Feedbacks.Select(f => f.Rating).ToListAsync();

        var r = new BotReply { Text = "Project overview:" };
        r.Items.Add(new BotItem { Title = "Orders", Badge = orders.ToString(), Lines = { $"Feasibility pending: {pending} · Feasible: {feasible} · Connected: {connected}" } });
        r.Items.Add(new BotItem { Title = "Connections", Badge = conns.ToString(), Lines = { $"{live} active · {conns - live} inactive" } });
        r.Items.Add(new BotItem { Title = "Billing", Lines = { $"Outstanding: {Fmt.Money(outstanding)}", $"Collected: {Fmt.Money(collected)}" } });
        r.Items.Add(new BotItem { Title = "Equipment stock", Badge = $"{stock} units", Lines = { lowStock == 0 ? "No item is below its reorder level" : $"{lowStock} item{(lowStock == 1 ? "" : "s")} at or below reorder level" } });
        r.Items.Add(new BotItem
        {
            Title = "People & places",
            Lines = { $"{customers} customers · {employees} employees", $"{vendors} vendors · {shops} retail shops" },
        });
        r.Items.Add(new BotItem { Title = "Feedback", Badge = ratings.Count == 0 ? "—" : ratings.Average().ToString("0.0") + " / 5", Lines = { $"{ratings.Count} message{(ratings.Count == 1 ? "" : "s")}" } });
        r.Buttons.Add(new BotButton("Open Overview", Href: TabLink(AppRoles.Admin, "overview")));
        r.Buttons.Add(new BotButton("Billing", "billing"));
        r.Buttons.Add(new BotButton("Procurement", "procurement"));
        return r;
    }

    private async Task<BotReply> TechOverviewAsync()
    {
        var pending = await _db.Orders.CountAsync(o => o.Status == OrderStatus.Pending);
        var feasible = await _db.Orders.CountAsync(o => o.Status == OrderStatus.Feasible);
        var active = await _db.Connections.CountAsync(c => c.Status == ConnStatus.Active);
        var inactive = await _db.Connections.CountAsync(c => c.Status != ConnStatus.Active);
        var low = await _db.Equipment.CountAsync(e => e.Stock <= e.ReorderLevel);

        var r = new BotReply { Text = "Technical desk overview:" };
        r.Items.Add(new BotItem { Title = "Feasibility queue", Badge = pending.ToString(), Lines = { $"Feasible, ready to provision: {feasible}" } });
        r.Items.Add(new BotItem { Title = "Connections", Badge = (active + inactive).ToString(), Lines = { $"{active} active · {inactive} inactive" } });
        r.Items.Add(new BotItem { Title = "Equipment", Badge = low.ToString(), Lines = { low == 0 ? "Nothing is below its reorder level" : $"{low} item{(low == 1 ? "" : "s")} at or below reorder level" } });
        r.Buttons.Add(new BotButton("Feasibility queue", "feasibility queue"));
        r.Buttons.Add(new BotButton("Low stock", "low stock"));
        r.Buttons.Add(new BotButton("Connections", "connections"));
        return r;
    }

    private async Task<BotReply> CustomerOverviewAsync()
    {
        var cust = await CurrentCustomerAsync();
        if (cust == null) return new BotReply { Text = "Your customer profile isn't set up yet.", Buttons = Menu(AppRoles.Customer) };

        var orders = await _db.Orders.CountAsync(o => o.CustomerId == cust.Id);
        var pending = await _db.Orders.CountAsync(o => o.CustomerId == cust.Id && o.Status == OrderStatus.Pending);
        var conns = await _db.Connections.CountAsync(c => c.CustomerId == cust.Id);
        var live = await _db.Connections.CountAsync(c => c.CustomerId == cust.Id && c.Status == ConnStatus.Active);
        var dues = await _db.Bills.Where(b => b.Connection.CustomerId == cust.Id && b.Status != BillStatus.Paid).SumAsync(b => b.DueAmount);

        var r = new BotReply { Text = "Your account at a glance:" };
        r.Items.Add(new BotItem { Title = "Orders", Badge = orders.ToString(), Lines = { $"{pending} waiting for a survey" } });
        r.Items.Add(new BotItem { Title = "Connections", Badge = conns.ToString(), Lines = { $"{live} active" } });
        r.Items.Add(new BotItem { Title = "Amount due", Badge = Fmt.Money(dues) });
        r.Buttons.Add(new BotButton("My orders", "my orders"));
        r.Buttons.Add(new BotButton("My bills", "my bills"));
        r.Buttons.Add(new BotButton("My connections", "my connections"));
        return r;
    }

    private async Task<BotReply> EmployeesAsync()
    {
        var emps = await _db.Employees.AsNoTracking().Include(e => e.Shop).OrderByDescending(e => e.Id).ToListAsync();
        if (emps.Count == 0) return new BotReply { Text = "No employees are on record yet.", Buttons = Menu(AppRoles.Admin) };

        var r = new BotReply { Text = $"{emps.Count} employee{(emps.Count == 1 ? "" : "s")} on record:" };
        var byDept = new BotItem { Title = "By department", Badge = emps.Count.ToString() };
        foreach (var g in emps.GroupBy(e => e.Department).OrderBy(g => g.Key))
            byDept.Lines.Add($"{Cap(g.Key)}: {g.Count()} ({g.Count(x => x.Status == "active")} active)");
        r.Items.Add(byDept);
        foreach (var e in emps.Take(5))
            r.Items.Add(new BotItem
            {
                Title = e.Name,
                Badge = Cap(e.Department),
                Lines = { $"{e.Designation} · {Cap(e.Status)}", e.Shop != null ? $"Shop: {e.Shop.Name}" : $"Code: {e.EmpCode}" },
            });
        r.Text += " Most recently added shown below.";
        r.Buttons.Add(new BotButton("Open Employees", Href: TabLink(AppRoles.Admin, "employees")));
        r.Buttons.Add(new BotButton("Vendors", "vendors"));
        return r;
    }

    private async Task<BotReply> VendorsAsync()
    {
        var vendors = await _db.Vendors.AsNoTracking().OrderBy(v => v.Name).Take(6).ToListAsync();
        var total = await _db.Vendors.CountAsync();
        if (total == 0) return new BotReply { Text = "No vendors are on record yet.", Buttons = Menu(AppRoles.Admin) };

        var r = new BotReply { Text = $"{total} vendor{(total == 1 ? "" : "s")} on record{(total > vendors.Count ? " (first 6 shown)" : "")}:" };
        foreach (var v in vendors)
            r.Items.Add(new BotItem { Title = v.Name, Badge = v.ContactPerson, Lines = { $"Supplies: {v.Products}", $"Phone: {v.Phone}" } });
        r.Buttons.Add(new BotButton("Open Vendors", Href: TabLink(AppRoles.Admin, "vendors")));
        r.Buttons.Add(new BotButton("Procurement", "procurement"));
        return r;
    }

    private async Task<BotReply> ProcurementAsync()
    {
        var open = await _db.Orders.Where(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Feasible)
            .Select(o => new { o.ConnectionType, o.Quantity }).ToListAsync();
        var needModems = open.Where(o => o.ConnectionType != ConnTypes.Landline).Sum(o => o.Quantity);
        var needPhones = open.Where(o => o.ConnectionType == ConnTypes.Landline).Sum(o => o.Quantity);
        var equipment = await _db.Equipment.AsNoTracking().Include(e => e.Vendor).ToListAsync();
        var modemStock = equipment.Where(e => e.Type is "modem" or "router").Sum(e => e.Stock);
        var phoneStock = equipment.Where(e => e.Type is "telephone" or "cabling").Sum(e => e.Stock);
        var low = equipment.Where(e => e.Stock <= e.ReorderLevel).ToList();

        string Gap(int need, int stock) => need > stock ? $"Short by {need - stock} — order more" : "Stock covers the open demand";
        var r = new BotReply { Text = "Procurement view — open demand (pending and feasible orders) against stock:" };
        r.Items.Add(new BotItem { Title = "Modems / routers", Badge = $"need {needModems} · in stock {modemStock}", Lines = { Gap(needModems, modemStock) } });
        r.Items.Add(new BotItem { Title = "Phones / cabling", Badge = $"need {needPhones} · in stock {phoneStock}", Lines = { Gap(needPhones, phoneStock) } });
        foreach (var e in low.Take(5))
            r.Items.Add(new BotItem
            {
                Title = e.Name,
                Badge = $"{e.Stock} left · reorder at {e.ReorderLevel}",
                Lines = { e.Vendor != null ? $"Vendor: {e.Vendor.Name}" : "No vendor set" },
            });
        if (low.Count > 0) r.Text += " Items at or below the reorder level are listed below.";
        r.Buttons.Add(new BotButton("Open Procurement", Href: TabLink(AppRoles.Admin, "procurement")));
        r.Buttons.Add(new BotButton("Vendors", "vendors"));
        return r;
    }

    private async Task<BotReply> FeedbackAsync(string role)
    {
        if (role == AppRoles.Admin)
        {
            var all = await _db.Feedbacks.AsNoTracking().OrderByDescending(f => f.CreatedAt).Take(200).ToListAsync();
            if (all.Count == 0) return new BotReply { Text = "No customer feedback has been received yet.", Buttons = Menu(role) };
            var r = new BotReply { Text = $"{all.Count} feedback message{(all.Count == 1 ? "" : "s")}, average rating {all.Average(f => f.Rating):0.0} / 5. Latest:" };
            foreach (var f in all.Take(3))
            {
                var msg = f.Message.Length > 120 ? f.Message[..120] + "…" : f.Message;
                r.Items.Add(new BotItem { Title = f.Name, Badge = $"{f.Rating} / 5", Lines = { msg, Fmt.Date(f.CreatedAt) } });
            }
            r.Buttons.Add(new BotButton("Open Feedback", Href: TabLink(role, "feedback")));
            return r;
        }
        if (role == AppRoles.Customer)
            return new BotReply
            {
                Text = "You can send us your feedback or a complaint from the Feedback tab in your portal.",
                Buttons = { new BotButton("Open Feedback", Href: "/portal#feedback"), new BotButton("My orders", "my orders") },
            };
        return ComplaintReply(role);
    }

    /* ---------------------------------- helpers ------------------------------- */

    /// <summary>The signed-in customer's profile, or null (visitor or a staff account)</summary>
    private async Task<Customer?> CurrentCustomerAsync()
    {
        if (User.Identity?.IsAuthenticated != true) return null;
        var s = Session;
        if (s.Role != AppRoles.Customer) return null;
        return await _db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == s.Id);
    }
}
