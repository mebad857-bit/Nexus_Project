using System.Text.RegularExpressions;
using NexusCommunication.Models;

namespace NexusCommunication.Services;

/// <summary>Topics the help-desk chatbot understands.</summary>
public static class ChatTopic
{
    public const string Greeting = "greeting", Thanks = "thanks", Help = "help", About = "about", Secrets = "secrets",
        Account = "account", Plans = "plans", Charges = "charges", Outlets = "outlets", Coverage = "coverage",
        Faq = "faq", Complaint = "complaint", Orders = "orders", Connections = "connections", Billing = "billing",
        Finance = "finance", Invoicing = "invoicing", Feedback = "feedback", Employees = "employees",
        Vendors = "vendors", Procurement = "procurement", Equipment = "equipment", Feasibility = "feasibility",
        PlaceOrder = "placeorder", Customers = "customers", Overview = "overview", Unknown = "unknown";
}

/// <summary>
/// Pure (no database) part of the chatbot: which topic a message is about, and which audience may talk about it.
/// Audience = "public" (not signed in) or one of <see cref="AppRoles"/>.
/// </summary>
public static class ChatIntent
{
    public const string Public = "public";

    /// <summary>Topics that are NOT open to everybody. Anything not listed here is open to every audience
    /// (the answer itself is then always limited to what that audience may see).</summary>
    public static readonly Dictionary<string, string[]> Restricted = new()
    {
        [ChatTopic.Employees]   = new[] { AppRoles.Admin },
        [ChatTopic.Vendors]     = new[] { AppRoles.Admin },
        [ChatTopic.Procurement] = new[] { AppRoles.Admin },
        [ChatTopic.Equipment]   = new[] { AppRoles.Technical, AppRoles.Admin },
        [ChatTopic.Feasibility] = new[] { AppRoles.Technical, AppRoles.Admin },
        [ChatTopic.Invoicing]   = new[] { AppRoles.Accounts, AppRoles.Admin },
        [ChatTopic.Finance]     = new[] { AppRoles.Accounts, AppRoles.Admin, AppRoles.Retail },
        [ChatTopic.PlaceOrder]  = new[] { AppRoles.Retail, AppRoles.Admin },
        [ChatTopic.Customers]   = new[] { AppRoles.Retail, AppRoles.Admin },
        // billing words: everyone except the technical desk (public / customer get their own, limited answer)
        [ChatTopic.Billing]     = new[] { Public, AppRoles.Customer, AppRoles.Retail, AppRoles.Accounts, AppRoles.Admin },
    };

    public static bool Allowed(string audience, string topic) =>
        !Restricted.TryGetValue(topic, out var roles) || roles.Contains(audience);

    /// <summary>True when any keyword occurs at the START of a word ("plan" does not fire on "explain", "how" not on "show").</summary>
    public static bool Has(string q, params string[] keys)
    {
        foreach (var k in keys)
        {
            var i = q.IndexOf(k, StringComparison.Ordinal);
            while (i >= 0)
            {
                if (i == 0 || !char.IsLetter(q[i - 1])) return true;
                i = q.IndexOf(k, i + 1, StringComparison.Ordinal);
            }
        }
        return false;
    }

    /// <summary>Which FAQ entry a question is about (null = show the FAQ menu).</summary>
    public static string? FaqKey(string q)
    {
        if (Has(q, "how long", "how much time", "kitne din", "kitna time", "kab tak", "installation time", "activation time")) return "time";
        if (Has(q, "document")) return "docs";
        if (Has(q, "apply", "new connection", "naya connection", "get a connection", "need a connection", "want a connection")) return "apply";
        return null;
    }

    /// <summary>Order matters: the first matching rule wins.</summary>
    public static string Detect(string q)
    {
        q = q.ToLowerInvariant();

        // 1. account help vs. secrets
        if (Has(q, "change password", "forgot password", "reset password", "update password", "my password")) return ChatTopic.Account;
        if (Has(q, "password", "passwd", "credential", "secret", "api key", "apikey", "token", "connection string", "database", "sql")) return ChatTopic.Secrets;

        // 2. staff-only subjects
        if (Has(q, "customer care", "customer support", "customer service")) return ChatTopic.Complaint;
        if (Has(q, "employee", "staff", "salary", "payroll", "department", "designation")) return ChatTopic.Employees;
        if (Has(q, "vendor", "supplier")) return ChatTopic.Vendors;
        if (Has(q, "procure", "reorder", "re-order", "restock", "purchase order")) return ChatTopic.Procurement;
        if (Has(q, "equipment", "stock", "inventory", "modem", "router", "cabling", "hardware")) return ChatTopic.Equipment;
        if (Has(q, "feasib", "survey", "provision", "queue")) return ChatTopic.Feasibility;

        // 3. own data / desk work
        if (Has(q, "my order", "my application", "mere order", "mera order", "meray order", "meri application")) return ChatTopic.Orders;
        if (Has(q, "ledger", "customer dues", "defaulter", "who owes", "dues list")
            || (Has(q, "generate", "create", "make") && Has(q, "invoice", "bill"))) return ChatTopic.Invoicing;
        if (Has(q, "place order", "place an order", "new order", "take order", "take an order")) return ChatTopic.PlaceOrder;
        if (Has(q, "customers", "customer list", "all customers")) return ChatTopic.Customers;
        if (Has(q, "feedback", "review", "rating")) return ChatTopic.Feedback;
        if (Has(q, "revenue", "income", "collected", "collection", "earning")) return ChatTopic.Finance;

        // 4. complaints that mention other words ("internet is slow")
        if (Has(q, "complaint", "shikayat", "shikayet", "problem", "masla", "issue", "not working", "slow", "disconnect", "outage")) return ChatTopic.Complaint;

        // 5. public topics
        if (Has(q, "how long", "how much time", "kitne din", "kitna time", "kab tak", "installation time", "activation time",
                   "document", "apply", "new connection", "naya connection", "get a connection", "need a connection", "want a connection"))
            return ChatTopic.Faq;
        if (Has(q, "charge", "deposit", "tax", "fees", "discount", "kharcha", "kharch", "installation", "security", "bulk", "corporate")) return ChatTopic.Charges;
        if (Has(q, "bill", "due", "dues", "baqaya", "baqi", "payment", "pay", "invoice", "receipt", "outstanding", "overdue", "paisa", "paise", "cash", "paid", "balance")) return ChatTopic.Billing;
        if (Has(q, "plan", "package", "pack", "price", "pricing", "tariff", "qeemat", "qimat", "rate", "speed", "broadband",
                   "dialup", "dial-up", "dial up", "landline", "telephone", "internet", "wifi")) return ChatTopic.Plans;
        if (Has(q, "overview", "summary", "dashboard", "statistic", "stats", "how many", "total")) return ChatTopic.Overview;
        if (Has(q, "track", "status", "order", "application")) return ChatTopic.Orders;
        if (Has(q, "connection", "active", "inactive", "suspended")) return ChatTopic.Connections;
        if (Has(q, "contact", "support", "agent", "helpline")) return ChatTopic.Complaint;
        if (Has(q, "shop", "outlet", "store", "branch", "address", "location", "office", "dukan", "dukaan")) return ChatTopic.Outlets;
        if (Has(q, "city", "cities", "area", "coverage", "shehar", "shahar")) return ChatTopic.Coverage;
        if (Has(q, "what can you", "what can i ask", "help", "menu", "options", "guide me", "how to use")) return ChatTopic.Help;
        if (Has(q, "about", "what is nexus", "who are you", "what do you do", "services", "nexus")) return ChatTopic.About;
        if (Regex.IsMatch(q, @"\b(hi|hello|hey|salam|salaam|aoa|assalam\w*|hola)\b") || Has(q, "good morning", "good evening", "good afternoon")) return ChatTopic.Greeting;
        if (Has(q, "thank", "shukria", "shukriya", "jazak")) return ChatTopic.Thanks;
        if (Has(q, "faq", "how", "kaise", "kese")) return ChatTopic.Faq;
        return ChatTopic.Unknown;
    }
}
