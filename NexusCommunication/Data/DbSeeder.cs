using System.Globalization;
using Microsoft.EntityFrameworkCore;
using NexusCommunication.Models;
using NexusCommunication.Services;

namespace NexusCommunication.Data;

public static class DbSeeder
{
    private static DateTime Ago(double days) => DateTime.UtcNow.AddDays(-days);

    public static async Task InitializeAsync(AppDbContext db, IConfiguration cfg)
    {
        if (cfg.GetValue("Database:AutoCreate", true))
        {
            await db.Database.EnsureCreatedAsync();
            await EnsureSupportTablesAsync(db);
            await AddImageColumnsAsync(db);
            await RepairImagePathsAsync(db);
        }

        if (!cfg.GetValue("Database:SeedDemoData", true)) return;
        await SeedFaqsAsync(db);
        await SeedContactMessagesAsync(db);
        if (await db.Users.AnyAsync()) { await SeedDemoTicketsAsync(db); return; }

        /* ------------------------------ users ------------------------------ */
        User U(string username, string pw, string name, string role) =>
            new() { Username = username, PasswordHash = PasswordHasher.Hash(pw), Name = name, Role = role };

        var users = new Dictionary<string, User>
        {
            ["admin"] = U("admin", "admin123", "Aarav Mehta", AppRoles.Admin),
            ["accounts"] = U("accounts", "accounts123", "Priya Nair", AppRoles.Accounts),
            ["technical"] = U("technical", "tech123", "Rohan Verma", AppRoles.Technical),
            ["retail"] = U("retail", "retail123", "Sara Fernandes", AppRoles.Retail),
            ["retail2"] = U("retail2", "retail123", "David Chen", AppRoles.Retail),
            ["customer"] = U("customer", "customer123", "Meera Joshi", AppRoles.Customer),
        };
        db.Users.AddRange(users.Values);

        /* --------------------------- retail shops --------------------------- */
        RetailShop S(string code, string name, string city, string addr, string phone, string mgr) =>
            new() { Code = code, Name = name, City = city, Address = addr, Phone = phone, Manager = mgr };
        var shops = new Dictionary<string, RetailShop>
        {
            ["NX-101"] = S("NX-101", "Nexus Flagship", "Meridian Central", "14 Circuit Avenue, Meridian Central", "+1 555 0101", "Sara Fernandes"),
            ["NX-102"] = S("NX-102", "Nexus Harbor Hub", "Harbor Point", "Pier 9, Dockside Road, Harbor Point", "+1 555 0102", "David Chen"),
            ["NX-103"] = S("NX-103", "Nexus Tech Park", "Tech Park", "Block C, Innovation Drive, Tech Park", "+1 555 0103", "Nina Kapoor"),
            ["NX-104"] = S("NX-104", "Nexus Express", "Riverside", "221 Riverside Walk, Riverside", "+1 555 0104", "Omar Sheikh"),
        };
        db.RetailShops.AddRange(shops.Values);

        /* ----------------------------- employees ---------------------------- */
        Employee Emp(string code, User? user, string name, string email, string phone, string dept, string desig, RetailShop? shop, double joined) =>
            new() { EmpCode = code, User = user, Name = name, Email = email, Phone = phone, Department = dept, Designation = desig, Shop = shop, Status = "active", JoinedAt = Ago(joined) };
        db.Employees.AddRange(
            Emp("EMP-0001", users["admin"], "Aarav Mehta", "aarav@nexuscomm.com", "+1 555 9001", "admin", "General Manager", null, 900),
            Emp("EMP-0002", users["accounts"], "Priya Nair", "priya@nexuscomm.com", "+1 555 9002", "accounts", "Accounts Officer", null, 620),
            Emp("EMP-0003", users["technical"], "Rohan Verma", "rohan@nexuscomm.com", "+1 555 9003", "technical", "Network Engineer", null, 540),
            Emp("EMP-0004", users["retail"], "Sara Fernandes", "sara@nexuscomm.com", "+1 555 9004", "retail", "Retail Executive", shops["NX-101"], 410),
            Emp("EMP-0005", users["retail2"], "David Chen", "david@nexuscomm.com", "+1 555 9005", "retail", "Retail Executive", shops["NX-102"], 320),
            Emp("EMP-0006", null, "Nina Kapoor", "nina@nexuscomm.com", "+1 555 9006", "retail", "Store Manager", shops["NX-103"], 210));

        /* ------------------------------ vendors ----------------------------- */
        var vZyx = new Vendor { Name = "Zyxelwave Technologies", ContactPerson = "Marcus Lee", Email = "sales@zyxelwave.com", Phone = "+1 555 7001", Address = "Unit 8, Meridian Industrial Estate", Products = "ADSL & VDSL modems, wireless routers" };
        var vFib = new Vendor { Name = "FiberLink Industries", ContactPerson = "Anna Petrova", Email = "orders@fiberlink.io", Phone = "+1 555 7002", Address = "42 Harbor Logistics Park", Products = "GPON ONTs, Cat6 cabling, splitters" };
        var vCom = new Vendor { Name = "ComSet Devices", ContactPerson = "Raj Patel", Email = "b2b@comset.dev", Phone = "+1 555 7003", Address = "Plot 19, Tech Park SEZ", Products = "Corded & cordless telephones, PBX units" };
        db.Vendors.AddRange(vZyx, vFib, vCom);

        /* ----------------------------- equipment ---------------------------- */
        Equipment Eq(string name, string type, Vendor v, decimal price, int stock, int reorder) =>
            new() { Name = name, Type = type, Vendor = v, UnitPrice = price, Stock = stock, ReorderLevel = reorder };
        db.Equipment.AddRange(
            Eq("NW-5600 VDSL Modem", "modem", vZyx, 45m, 140, 25),
            Eq("NW-AX3200 WiFi 6 Router", "router", vFib, 78m, 96, 20),
            Eq("Dual-Band GPON ONT", "router", vFib, 92m, 60, 15),
            Eq("Corded Desk Phone CX-2", "telephone", vCom, 22m, 210, 40),
            Eq("ADSL2+ Micro Modem", "modem", vZyx, 28m, 8, 30),
            Eq("Cat6 Drop Cable (305m)", "cabling", vFib, 65m, 45, 10));

        /* ------------------------------- plans ------------------------------ */
        Plan P(string type, string cat, string name, string? speed, decimal price, string validity, int months, decimal deposit, string? calls, string desc) =>
            new() { ConnectionType = type, Category = cat, Name = name, Speed = speed, Price = price, ValidityLabel = validity, ValidityMonths = months, Deposit = deposit, CallRates = calls, Description = desc };
        var plans = new Dictionary<string, Plan>
        {
            // Dial-Up - deposit $325
            ["d10"] = P("dialup", "Hourly Basis", "10 Hours Pack", "56 Kbps", 50m, "1 Month", 1, 325m, null, "10 hours of dial-up surfing, valid for one month. Ideal for occasional browsing and e-mail."),
            ["d30"] = P("dialup", "Hourly Basis", "30 Hours Pack", "56 Kbps", 130m, "3 Months", 3, 325m, null, "30 hours of dial-up access spread across a full quarter."),
            ["d60"] = P("dialup", "Hourly Basis", "60 Hours Pack", "56 Kbps", 260m, "6 Months", 6, 325m, null, "60 hours of dial-up connectivity with a half-year validity window."),
            ["d28m"] = P("dialup", "Unlimited 28Kbps", "Monthly Unlimited", "28 Kbps", 75m, "1 Month", 1, 325m, null, "Truly unlimited 28 Kbps dial-up, billed monthly."),
            ["d28q"] = P("dialup", "Unlimited 28Kbps", "Quarterly Unlimited", "28 Kbps", 150m, "3 Months", 3, 325m, null, "Unlimited 28 Kbps dial-up at a two-month price."),
            ["d56m"] = P("dialup", "Unlimited 56Kbps", "Monthly Unlimited", "56 Kbps", 100m, "1 Month", 1, 325m, null, "Unlimited full-speed 56 Kbps dial-up, billed monthly."),
            ["d56q"] = P("dialup", "Unlimited 56Kbps", "Quarterly Unlimited", "56 Kbps", 180m, "3 Months", 3, 325m, null, "Unlimited 56 Kbps for a full quarter — our most popular dial-up pack."),
            // Broadband - deposit $500
            ["b30"] = P("broadband", "Hourly Basis", "30 Hours Pack", "128 Kbps", 175m, "1 Month", 1, 500m, null, "30 hours of always-fast broadband for light home users."),
            ["b60"] = P("broadband", "Hourly Basis", "60 Hours Pack", "128 Kbps", 315m, "6 Months", 6, 500m, null, "60 hours of broadband with a generous six-month validity."),
            ["b64m"] = P("broadband", "Unlimited 64Kbps", "Monthly Unlimited", "64 Kbps", 225m, "1 Month", 1, 500m, null, "Unlimited 64 Kbps broadband for everyday streaming."),
            ["b64q"] = P("broadband", "Unlimited 64Kbps", "Quarterly Unlimited", "64 Kbps", 400m, "3 Months", 3, 500m, null, "A quarter of unlimited 64 Kbps broadband at a bundle rate."),
            ["b128m"] = P("broadband", "Unlimited 128Kbps", "Monthly Unlimited", "128 Kbps", 350m, "1 Month", 1, 500m, null, "Our flagship — unlimited 128 Kbps broadband, billed monthly."),
            ["b128q"] = P("broadband", "Unlimited 128Kbps", "Quarterly Unlimited", "128 Kbps", 445m, "3 Months", 3, 500m, null, "Flagship speed at our best quarterly value."),
            // Landline - deposit $250
            ["llu"] = P("landline", "Local Plan", "Unlimited Annual", null, 75m, "12 Months", 12, 250m, "Local calls: 55¢ / minute", "Annual rental for light users with metered local calling."),
            ["llm"] = P("landline", "Local Plan", "Monthly Local", null, 35m, "1 Month", 1, 250m, "Local calls: 75¢ / minute", "Low monthly rental with straightforward metered local calls."),
            ["stdm"] = P("landline", "STD Plan", "Monthly STD", null, 125m, "1 Month", 1, 250m, "Local: 70¢/min · STD: $2.25/min · Mobile messaging: $1.00/min", "Monthly STD rental with regional calling and mobile messaging."),
            ["stdh"] = P("landline", "STD Plan", "Half-Yearly STD", null, 420m, "6 Months", 6, 250m, "Local: 60¢/min · STD: $2.00/min · Mobile messaging: $1.15/min", "Half-year rental with reduced STD and local call tariffs."),
            ["stdy"] = P("landline", "STD Plan", "Yearly STD", null, 750m, "12 Months", 12, 250m, "Local: 60¢/min · STD: $1.75/min · Mobile messaging: $1.25/min", "Our best long-distance rates, locked in for a full year."),
        };
        db.Plans.AddRange(plans.Values);

        /* ----------------------------- customers ---------------------------- */
        Customer C(User? user, string name, string email, string phone, string? alt, string addr, string city, string code, string idp, double created) =>
            new() { User = user, Name = name, Email = email, Phone = phone, AltPhone = alt, Address = addr, City = city, CityCode = code, IdProof = idp, CreatedAt = Ago(created) };
        var cust = new Dictionary<string, Customer>
        {
            ["Meera Joshi"] = C(users["customer"], "Meera Joshi", "meera.joshi@mail.com", "+1 555 3301", "+1 555 3302", "Apt 4B, Lumen Towers, Circuit Avenue", "Meridian Central", "101", "National ID · MC-4482910", 95),
            ["Kabir Anand"] = C(null, "Kabir Anand", "kabir.anand@mail.com", "+1 555 4402", null, "9 Dockside Lane", "Harbor Point", "102", "Passport · HP-9021764", 70),
            ["Elena D'Souza"] = C(null, "Elena D'Souza", "elena.dsouza@mail.com", "+1 555 5503", null, "Suite 210, Innovation Drive", "Tech Park", "103", "National ID · TP-7734521", 80),
            ["Farhan Qureshi"] = C(null, "Farhan Qureshi", "farhan.q@mail.com", "+1 555 6604", null, "221 Riverside Walk, Floor 2", "Riverside", "104", "Driver License · RV-1029384", 50),
            ["Tanya Bhatt"] = C(null, "Tanya Bhatt", "tanya.bhatt@mail.com", "+1 555 7705", null, "Villa 7, Northgate Enclave", "Northgate", "105", "Passport · NG-6612034", 12),
            ["Vikram Rao"] = C(null, "Vikram Rao", "vikram.rao@mail.com", "+1 555 8806", null, "16 Meridian Square", "Meridian Central", "101", "National ID · MC-5501283", 120),
        };
        db.Customers.AddRange(cust.Values);

        /* ------------------------------- orders ------------------------------ */
        Order O(string no, string customer, string type, string planKey, string status, string? remark, string shop, string by, double created, int qty = 1, bool corp = false, int disc = 0) =>
            new() { OrderNo = no, Customer = cust[customer], ConnectionType = type, Plan = plans[planKey], Quantity = qty, Corporate = corp, DiscountPct = disc, Status = status, Remark = remark, Shop = shops[shop], CreatedBy = by, CreatedAt = Ago(created) };
        var orders = new Dictionary<string, Order>();
        void AddOrder(Order o) { orders[o.OrderNo] = o; db.Orders.Add(o); }
        AddOrder(O("D0000000001", "Vikram Rao", "dialup", "d56m", "connected", "Feasible — installed NW-5600 modem at premises.", "NX-101", "Sara Fernandes", 118));
        AddOrder(O("D0000000002", "Elena D'Souza", "dialup", "d30", "connected", "Line + internet feasibility cleared same-day.", "NX-103", "Sara Fernandes", 76));
        AddOrder(O("D0000000003", "Farhan Qureshi", "dialup", "d28m", "pending", null, "NX-104", "David Chen", 2));
        AddOrder(O("B0000000001", "Meera Joshi", "broadband", "b128m", "connected", "Fiber drop within range — connection activated.", "NX-101", "Sara Fernandes", 92));
        AddOrder(O("B0000000002", "Tanya Bhatt", "broadband", "b64q", "feasible", "Node 500m away — ready for installation visit.", "NX-102", "David Chen", 7));
        AddOrder(O("B0000000003", "Vikram Rao", "broadband", "b64m", "infeasible", "Feeder length 4.2 km — beyond broadband reach.", "NX-101", "Sara Fernandes", 20));
        AddOrder(O("B0000000004", "Elena D'Souza", "broadband", "b128q", "feasible", "Corporate bulk — 12 lines for Innovation Drive office. 25% slab applied.", "NX-103", "David Chen", 5, qty: 12, corp: true, disc: 25));
        AddOrder(O("T0000000001", "Kabir Anand", "landline", "stdm", "connected", "Copper pair available — voice line live.", "NX-102", "David Chen", 62));
        AddOrder(O("T0000000002", "Farhan Qureshi", "landline", "llm", "cancelled", "Duplicate application withdrawn by customer.", "NX-104", "David Chen", 45));
        AddOrder(O("T0000000003", "Kabir Anand", "landline", "llu", "pending", null, "NX-102", "David Chen", 12));

        /* ---------------------------- connections ---------------------------- */
        Connection Cn(string acct, string orderNo, string customer, string planKey, string type, string city, string code, string status, double activated) =>
            new() { AccountId = acct, Order = orders[orderNo], Customer = cust[customer], Plan = plans[planKey], ConnectionType = type, City = city, CityCode = code, Status = status, ActivatedAt = Ago(activated), CreatedAt = Ago(activated) };
        var conns = new Dictionary<string, Connection>
        {
            ["D101000000000001"] = Cn("D101000000000001", "D0000000001", "Vikram Rao", "d56m", "dialup", "Meridian Central", "101", "temporarily_inactive", 115),
            ["D103000000000001"] = Cn("D103000000000001", "D0000000002", "Elena D'Souza", "d30", "dialup", "Tech Park", "103", "active", 74),
            ["B101000000000001"] = Cn("B101000000000001", "B0000000001", "Meera Joshi", "b128m", "broadband", "Meridian Central", "101", "active", 90),
            ["T102000000000001"] = Cn("T102000000000001", "T0000000001", "Kabir Anand", "stdm", "landline", "Harbor Point", "102", "active", 60),
        };
        db.Connections.AddRange(conns.Values);

        /* ------------------------------- bills ------------------------------- */
        int billSeq = 0;
        void AddBill(string acct, double createdDaysAgo, decimal rental, string status, decimal usage = 0m, decimal discount = 0m,
                     decimal paidFraction = 0.5m, string method = "upi", string channel = "retail", string receivedBy = "Sara Fernandes")
        {
            billSeq++;
            var createdAt = Ago(createdDaysAgo);
            var subtotal = rental + usage - discount;
            var tax = Math.Round(subtotal * (Fmt.ServiceTaxRate / 100m), 2);
            var total = Math.Round(subtotal + tax, 2);
            var paid = status == "paid" ? total : status == "partial" ? Math.Round(total * paidFraction, 2) : 0m;
            var bill = new Bill
            {
                BillNo = $"INV-{createdAt.Year}-{billSeq:0000}",
                Connection = conns[acct],
                Period = createdAt.ToString("MMMM yyyy", CultureInfo.GetCultureInfo("en-US")),
                Rental = rental, Usage = usage, Tax = tax, Discount = discount, Total = total,
                PaidAmount = paid, DueAmount = total - paid, Status = status,
                DueDate = createdAt.AddDays(10), GeneratedBy = "Priya Nair", CreatedAt = createdAt,
            };
            db.Bills.Add(bill);
            if (paid > 0)
            {
                db.Payments.Add(new Payment
                {
                    Bill = bill, Amount = paid, Method = method,
                    Reference = $"NX-{Random.Shared.Next(100000, 999999)}",
                    Channel = channel, ReceivedBy = receivedBy, CreatedAt = createdAt.AddDays(3),
                });
            }
        }

        // Broadband B101000000000001 — $350/mo
        AddBill("B101000000000001", 75, 350m, "paid", method: "card", channel: "office", receivedBy: "Priya Nair");
        AddBill("B101000000000001", 45, 350m, "paid", method: "upi");
        AddBill("B101000000000001", 8, 350m, "pending");
        // Dial-Up D103000000000001 — 30hr pack amortized $43.33/mo
        AddBill("D103000000000001", 40, 43.33m, "partial", paidFraction: 0.6m);
        AddBill("D103000000000001", 6, 43.33m, "pending");
        // Landline T102000000000001 — $125 rental + metered calls
        AddBill("T102000000000001", 38, 125m, "paid", usage: 82.4m, method: "cash");
        AddBill("T102000000000001", 5, 125m, "pending", usage: 134.8m, discount: 20m);
        // Dial-Up D101000000000001 — Vikram $100/mo — overdue
        AddBill("D101000000000001", 35, 100m, "pending");

        /* ------------------------------ feedback ----------------------------- */
        db.Feedbacks.AddRange(
            new Feedback { Name = "Meera Joshi", Contact = "+1 555 3301", ConnectionType = "broadband", Rating = 5, Message = "Installation was done within 48 hours and the 128K line has been rock solid.", CreatedAt = Ago(60) },
            new Feedback { Name = "Kabir Anand", Contact = "+1 555 4402", ConnectionType = "landline", Rating = 4, Message = "Clear voice quality on the STD plan. Billing to office pickup is smooth.", CreatedAt = Ago(30) },
            new Feedback { Name = "Elena D'Souza", Contact = "+1 555 5503", ConnectionType = "dialup", Rating = 4, Message = "The 30-hour pack is perfect for our backup line at the office.", CreatedAt = Ago(15) },
            new Feedback { Name = "Tanya Bhatt", Contact = "+1 555 7705", ConnectionType = "broadband", Rating = 5, Message = "Feasibility report came back in a day — very impressed so far.", CreatedAt = Ago(3) });

        await db.SaveChangesAsync();
        await ApplySeedImagesAsync(db);
        await SeedDemoTicketsAsync(db);
    }

    /// <summary>
    /// Pictures that ship with the project are saved in the database as /images/... paths. If such a path points to a file that does not
    /// exist (for example saved as .jpg while the real file is .png), switch it to the file that does exist. Runs on every start, cheap.
    /// Uploaded pictures (/uploads/...) are never touched.
    /// </summary>
    public static async Task RepairImagePathsAsync(AppDbContext db)
    {
        if (Ui.WebRoot == null) return;
        static string? Fix(string? url)
        {
            if (string.IsNullOrEmpty(url) || !url.StartsWith("/images/", StringComparison.OrdinalIgnoreCase)) return url;
            var full = Path.Combine(Ui.WebRoot!, url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(full)) return url;
            var fixedUrl = Ui.Asset(url);
            var fixedFull = Path.Combine(Ui.WebRoot!, fixedUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            return File.Exists(fixedFull) ? fixedUrl : url;
        }
        foreach (var x in await db.RetailShops.Where(x => x.ImageUrl != null).ToListAsync()) x.ImageUrl = Fix(x.ImageUrl);
        foreach (var x in await db.Employees.Where(x => x.ImageUrl != null).ToListAsync()) x.ImageUrl = Fix(x.ImageUrl);
        foreach (var x in await db.Vendors.Where(x => x.ImageUrl != null).ToListAsync()) x.ImageUrl = Fix(x.ImageUrl);
        foreach (var x in await db.Equipment.Where(x => x.ImageUrl != null).ToListAsync()) x.ImageUrl = Fix(x.ImageUrl);
        foreach (var x in await db.Plans.Where(x => x.ImageUrl != null).ToListAsync()) x.ImageUrl = Fix(x.ImageUrl);
        foreach (var x in await db.Customers.Where(x => x.ImageUrl != null).ToListAsync()) x.ImageUrl = Fix(x.ImageUrl);
        await db.SaveChangesAsync();
    }

    /* ------------------------------ FAQ + contact messages ------------------------------ */

    /// <summary>
    /// EnsureCreated never adds tables to a database that already exists, so databases made before the
    /// FAQ / contact feature get the two new tables here. Safe to run on every start.
    /// </summary>
    private static async Task EnsureSupportTablesAsync(AppDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync(
            "IF OBJECT_ID(N'[Faqs]', N'U') IS NULL CREATE TABLE [Faqs] (" +
            "[Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Faqs] PRIMARY KEY, " +
            "[Question] NVARCHAR(300) NOT NULL, [Answer] NVARCHAR(2000) NOT NULL, " +
            "[SortOrder] INT NOT NULL, [Active] BIT NOT NULL, [CreatedAt] DATETIME2 NOT NULL)");
        await db.Database.ExecuteSqlRawAsync(
            "IF OBJECT_ID(N'[ContactMessages]', N'U') IS NULL BEGIN " +
            "CREATE TABLE [ContactMessages] (" +
            "[Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_ContactMessages] PRIMARY KEY, " +
            "[Name] NVARCHAR(200) NOT NULL, [Email] NVARCHAR(200) NOT NULL, [Message] NVARCHAR(4000) NOT NULL, " +
            "[IsRead] BIT NOT NULL, [CreatedAt] DATETIME2 NOT NULL); " +
            "CREATE INDEX [IX_ContactMessages_IsRead] ON [ContactMessages] ([IsRead]); END");

        // notifications (the bell) + audit log (admin "Activity log" tab) - no foreign keys on purpose
        await db.Database.ExecuteSqlRawAsync(
            "IF OBJECT_ID(N'[Notifications]', N'U') IS NULL BEGIN " +
            "CREATE TABLE [Notifications] (" +
            "[Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Notifications] PRIMARY KEY, " +
            "[UserId] INT NOT NULL, [Type] NVARCHAR(30) NOT NULL, [Title] NVARCHAR(200) NOT NULL, " +
            "[Message] NVARCHAR(1000) NOT NULL, [Link] NVARCHAR(300) NULL, " +
            "[IsRead] BIT NOT NULL, [CreatedAt] DATETIME2 NOT NULL); " +
            "CREATE INDEX [IX_Notifications_UserId_IsRead] ON [Notifications] ([UserId], [IsRead]); " +
            "CREATE INDEX [IX_Notifications_CreatedAt] ON [Notifications] ([CreatedAt]); END");
        await db.Database.ExecuteSqlRawAsync(
            "IF OBJECT_ID(N'[AuditLogs]', N'U') IS NULL BEGIN " +
            "CREATE TABLE [AuditLogs] (" +
            "[Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_AuditLogs] PRIMARY KEY, " +
            "[UserId] INT NULL, [UserName] NVARCHAR(200) NOT NULL, [Role] NVARCHAR(20) NOT NULL, " +
            "[Action] NVARCHAR(30) NOT NULL, [Entity] NVARCHAR(50) NOT NULL, [EntityId] NVARCHAR(50) NULL, " +
            "[Summary] NVARCHAR(1500) NOT NULL, [IpAddress] NVARCHAR(45) NULL, [CreatedAt] DATETIME2 NOT NULL); " +
            "CREATE INDEX [IX_AuditLogs_CreatedAt] ON [AuditLogs] ([CreatedAt]); " +
            "CREATE INDEX [IX_AuditLogs_UserId] ON [AuditLogs] ([UserId]); " +
            "CREATE INDEX [IX_AuditLogs_Action] ON [AuditLogs] ([Action]); END");

        // complaint / ticket system (Tickets + TicketReplies)
        await db.Database.ExecuteSqlRawAsync(
            "IF OBJECT_ID(N'[Tickets]', N'U') IS NULL BEGIN " +
            "CREATE TABLE [Tickets] (" +
            "[Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Tickets] PRIMARY KEY, " +
            "[TicketNo] NVARCHAR(20) NOT NULL, [CustomerId] INT NOT NULL, [Category] NVARCHAR(60) NOT NULL, " +
            "[Subject] NVARCHAR(200) NOT NULL, [Description] NVARCHAR(4000) NOT NULL, [ImageUrl] NVARCHAR(300) NULL, " +
            "[Status] NVARCHAR(20) NOT NULL, [AssignedEmployeeId] INT NULL, [AssignedAt] DATETIME2 NULL, " +
            "[ResolvedAt] DATETIME2 NULL, [ClosedAt] DATETIME2 NULL, [CreatedAt] DATETIME2 NOT NULL, [UpdatedAt] DATETIME2 NOT NULL, " +
            "CONSTRAINT [FK_Tickets_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers]([Id]), " +
            "CONSTRAINT [FK_Tickets_Employees_AssignedEmployeeId] FOREIGN KEY ([AssignedEmployeeId]) REFERENCES [Employees]([Id])); " +
            "CREATE UNIQUE INDEX [IX_Tickets_TicketNo] ON [Tickets] ([TicketNo]); " +
            "CREATE INDEX [IX_Tickets_Status] ON [Tickets] ([Status]); " +
            "CREATE INDEX [IX_Tickets_CustomerId] ON [Tickets] ([CustomerId]); " +
            "CREATE INDEX [IX_Tickets_AssignedEmployeeId] ON [Tickets] ([AssignedEmployeeId]); END");
        await db.Database.ExecuteSqlRawAsync(
            "IF OBJECT_ID(N'[TicketReplies]', N'U') IS NULL BEGIN " +
            "CREATE TABLE [TicketReplies] (" +
            "[Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_TicketReplies] PRIMARY KEY, " +
            "[TicketId] INT NOT NULL, [AuthorUserId] INT NULL, [AuthorName] NVARCHAR(200) NOT NULL, [AuthorRole] NVARCHAR(20) NOT NULL, " +
            "[Message] NVARCHAR(2000) NOT NULL, [ImageUrl] NVARCHAR(300) NULL, [IsSystem] BIT NOT NULL, [CreatedAt] DATETIME2 NOT NULL, " +
            "CONSTRAINT [FK_TicketReplies_Tickets_TicketId] FOREIGN KEY ([TicketId]) REFERENCES [Tickets]([Id])); " +
            "CREATE INDEX [IX_TicketReplies_TicketId] ON [TicketReplies] ([TicketId]); END");

        // demo online payment attempts (no card / wallet data is stored - only the method and the outcome)
        await db.Database.ExecuteSqlRawAsync(
            "IF OBJECT_ID(N'[OnlinePayments]', N'U') IS NULL BEGIN " +
            "CREATE TABLE [OnlinePayments] (" +
            "[Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_OnlinePayments] PRIMARY KEY, " +
            "[TransactionId] NVARCHAR(40) NOT NULL, [BillId] INT NOT NULL, [CustomerId] INT NOT NULL, " +
            "[Method] NVARCHAR(20) NOT NULL, [Amount] DECIMAL(12,2) NOT NULL, [Status] NVARCHAR(10) NOT NULL, " +
            "[FailureReason] NVARCHAR(200) NULL, [PaymentId] INT NULL, [CreatedAt] DATETIME2 NOT NULL, " +
            "CONSTRAINT [FK_OnlinePayments_Bills_BillId] FOREIGN KEY ([BillId]) REFERENCES [Bills]([Id]), " +
            "CONSTRAINT [FK_OnlinePayments_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers]([Id]), " +
            "CONSTRAINT [FK_OnlinePayments_Payments_PaymentId] FOREIGN KEY ([PaymentId]) REFERENCES [Payments]([Id])); " +
            "CREATE UNIQUE INDEX [IX_OnlinePayments_TransactionId] ON [OnlinePayments] ([TransactionId]); " +
            "CREATE INDEX [IX_OnlinePayments_BillId] ON [OnlinePayments] ([BillId]); " +
            "CREATE INDEX [IX_OnlinePayments_CustomerId] ON [OnlinePayments] ([CustomerId]); END");

        // forgot-password links (only the hash of each token is stored)
        await db.Database.ExecuteSqlRawAsync(
            "IF OBJECT_ID(N'[PasswordResetTokens]', N'U') IS NULL BEGIN " +
            "CREATE TABLE [PasswordResetTokens] (" +
            "[Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_PasswordResetTokens] PRIMARY KEY, " +
            "[UserId] INT NOT NULL, [TokenHash] NVARCHAR(64) NOT NULL, [ExpiresAt] DATETIME2 NOT NULL, " +
            "[UsedAt] DATETIME2 NULL, [CreatedAt] DATETIME2 NOT NULL, [RequestIp] NVARCHAR(45) NULL); " +
            "CREATE UNIQUE INDEX [IX_PasswordResetTokens_TokenHash] ON [PasswordResetTokens] ([TokenHash]); " +
            "CREATE INDEX [IX_PasswordResetTokens_UserId] ON [PasswordResetTokens] ([UserId]); END");
    }


    /// <summary>Demo messages for the admin "Contact inbox" (same shape as the public Contact us form). Only when the inbox is empty.</summary>
    private static async Task SeedContactMessagesAsync(AppDbContext db)
    {
        if (await db.ContactMessages.AnyAsync()) return;
        ContactMessage M(string name, string email, string msg, double days, bool read) =>
            new() { Name = name, Email = email, Message = msg, CreatedAt = Ago(days), IsRead = read };
        db.ContactMessages.AddRange(
            M("Meera Joshi", "meera.joshi@mail.com", "Hello, my broadband speed drops every evening between 8 and 10 pm. Could someone from the technical desk please check the line?", 0.2, false),
            M("Farhan Qureshi", "farhan.q@mail.com", "I applied for a dial-up connection two days ago and the order is still pending. Can you tell me when the feasibility survey will happen?", 0.6, false),
            M("Tanya Bhatt", "tanya.bhatt@mail.com", "Is it possible to shift my broadband connection to a new address in Northgate? Please let me know the charges and the process.", 1.5, false),
            M("Imran Siddiqui", "imran.siddiqui@mail.com", "We run a small office with 14 staff. Please share the corporate scheme details and how the 25% bulk discount is applied on the deposit.", 2.4, false),
            M("Kabir Anand", "kabir.anand@mail.com", "My landline bill for this month looks higher than usual. Can the accounts desk send me the call-wise breakup?", 4, true),
            M("Sana Malik", "sana.malik@mail.com", "I paid my bill at the Harbor Point counter yesterday but it still shows as pending in my portal. Receipt number is attached in my original visit.", 5.5, true),
            M("Elena D'Souza", "elena.dsouza@mail.com", "Thank you for the quick installation at our Tech Park office. The team was professional and the 30-hour pack works well for us.", 9, true),
            M("Bilal Ahmed", "bilal.ahmed@mail.com", "Which retail outlet is open on Sundays? I would like to collect a new modem and pay my pending invoice.", 12, true));
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Demo complaints (Tickets + TicketReplies) so the Complaints tabs are not empty. Only when there are no tickets yet
    /// and the demo customers exist. Uses the same ticket numbers / statuses the live system produces.
    /// </summary>
    private static async Task SeedDemoTicketsAsync(AppDbContext db)
    {
        if (await db.Tickets.AnyAsync()) return;
        var customers = await db.Customers.ToDictionaryAsync(c => c.Name);
        if (customers.Count == 0) return;
        var tech = await db.Employees.FirstOrDefaultAsync(e => e.Department == "technical");

        int seq = 0;
        Ticket T(string customer, string category, string subject, string desc, string status, double daysAgo, Employee? assign = null)
        {
            var created = Ago(daysAgo);
            var t = new Ticket
            {
                TicketNo = $"TK-{++seq:000000}", Customer = customers[customer], Category = category, Subject = subject, Description = desc,
                Status = status, CreatedAt = created, UpdatedAt = created.AddHours(5),
            };
            if (assign != null && status != TicketStatus.Open) { t.AssignedEmployee = assign; t.AssignedAt = created.AddHours(2); }
            if (status is TicketStatus.Resolved or TicketStatus.Closed) t.ResolvedAt = created.AddDays(1);
            if (status == TicketStatus.Closed) t.ClosedAt = created.AddDays(1.5);
            db.Tickets.Add(t);
            return t;
        }
        void Sys(Ticket t, string msg, DateTime at) => t.Replies.Add(new TicketReply { AuthorName = "System", AuthorRole = "system", Message = msg, IsSystem = true, CreatedAt = at });
        void Say(Ticket t, string name, string role, string msg, DateTime at) => t.Replies.Add(new TicketReply { AuthorName = name, AuthorRole = role, Message = msg, CreatedAt = at });

        string C(string n) => customers.ContainsKey(n) ? n : customers.Keys.First();
        var meera = C("Meera Joshi"); var kabir = C("Kabir Anand"); var elena = C("Elena D'Souza");
        var farhan = C("Farhan Qureshi"); var tanya = C("Tanya Bhatt"); var vikram = C("Vikram Rao");

        var t1 = T(meera, "Internet / connectivity", "Speed drops every evening", "Between 8 pm and 10 pm my 128 Kbps broadband is almost unusable. Pages take minutes to load. This has happened for the last five days.", TicketStatus.Open, 0.3);

        var t2 = T(tanya, "Landline / call quality", "Line dead since yesterday", "There is no dial tone and the phone line is dead since yesterday morning. I have not changed anything at home.", TicketStatus.Assigned, 1.2, tech);
        Sys(t2, "Assigned to " + (tech?.Name ?? "technical desk"), t2.AssignedAt ?? t2.CreatedAt);

        var t3 = T(kabir, "Billing & payments", "Landline bill higher than usual", "This month's invoice shows a much higher usage amount than my normal calls. Please verify the metered call charges.", TicketStatus.InProgress, 3, tech);
        Sys(t3, "Assigned to " + (tech?.Name ?? "technical desk"), t3.AssignedAt ?? t3.CreatedAt);
        Say(t3, tech?.Name ?? "Technical desk", "technical", "We pulled the call records for your line and are checking the STD minutes against the invoice. Will update you shortly.", t3.CreatedAt.AddDays(1));
        Say(t3, kabir, "customer", "Thank you. Most of the extra minutes look like calls I did not make, please check carefully.", t3.CreatedAt.AddDays(1.2));

        var t4 = T(elena, "Installation / equipment", "Modem keeps restarting", "The NW-5600 modem at our Tech Park office restarts by itself several times a day and drops the dial-up session.", TicketStatus.Resolved, 6, tech);
        Sys(t4, "Assigned to " + (tech?.Name ?? "technical desk"), t4.AssignedAt ?? t4.CreatedAt);
        Say(t4, tech?.Name ?? "Technical desk", "technical", "Visited the site and replaced the power adapter. The modem has been stable for 24 hours.", t4.CreatedAt.AddDays(1));
        Sys(t4, "Marked resolved", t4.ResolvedAt ?? t4.CreatedAt);

        var t5 = T(vikram, "Account & profile", "Reactivate my dial-up account", "My dial-up account became temporarily inactive after an unpaid bill. I have now paid at the counter, please reactivate it.", TicketStatus.Closed, 14, tech);
        Sys(t5, "Assigned to " + (tech?.Name ?? "technical desk"), t5.AssignedAt ?? t5.CreatedAt);
        Say(t5, tech?.Name ?? "Technical desk", "technical", "Payment confirmed by the accounts desk. Your line has been reactivated.", t5.CreatedAt.AddDays(1));
        Sys(t5, "Marked resolved", t5.ResolvedAt ?? t5.CreatedAt);
        Sys(t5, "Closed", t5.ClosedAt ?? t5.CreatedAt);

        var t6 = T(farhan, "Other", "Order pending for two days", "My dial-up application is still waiting for the feasibility survey. Please let me know the expected date.", TicketStatus.Open, 0.8);

        await db.SaveChangesAsync();
    }

    /// <summary>Starter FAQ entries, written from the rules the system already enforces. Only when the table is empty.</summary>
    private static async Task SeedFaqsAsync(AppDbContext db)
    {
        if (await db.Faqs.AnyAsync()) return;
        Faq F(int order, string q, string a) => new() { SortOrder = order, Question = q, Answer = a };
        db.Faqs.AddRange(
            F(10, "How do I apply for a new connection?",
                "Walk into any Nexus retail outlet or create a free account on the website, pick a plan and submit your details. You receive an 11-digit order ID straight away."),
            F(20, "What happens after I place an order?",
                "The technical desk runs a feasibility survey for your address. Feasible orders are provisioned within 48 hours and your 16-digit account ID goes live."),
            F(30, "How can I track my order or connection?",
                "Open Track Status and enter your order ID (letter + 10 digits) or account ID (letter + 15 digits). Signed-in customers can also follow everything from their portal."),
            F(40, "What is the security deposit and do I get it back?",
                "The deposit is $325 for Dial-Up, $500 for Broadband and $250 for Landline. It is fully refundable when you withdraw the connection."),
            F(50, "Is there any tax on my bill?",
                "Yes. A 12.24% service tax applies on the whole bill (rental + usage - discounts), and every invoice shows the exact tax line."),
            F(60, "Do you offer discounts for several connections?",
                "Yes. Ordering 10-15 lines earns 25% off the advance rental and deposit, 15-25 lines 50%, 25-50 lines 75%, and more than 50 lines 100%. It is applied automatically."),
            F(70, "Where can I pay my bill?",
                "At any Nexus retail outlet counter or at the accounts desk of the city head office. Keep your invoice number handy, and you can print or save any invoice as a PDF."),
            F(80, "Can I cancel an order?",
                "Customers can withdraw an application while it is still waiting for the feasibility survey. After that, please contact your retail outlet."));
        await db.SaveChangesAsync();
    }

    /* ------------------------------ picture support ------------------------------ */

    private static readonly string[] ImageTables = { "RetailShops", "Employees", "Vendors", "Equipment", "Plans", "Customers" };

    /// <summary>
    /// Databases created before the picture feature have no ImageUrl column (EnsureCreated never alters tables).
    /// Add it once, then give the demo records their default pictures. Safe to run on every start.
    /// </summary>
    private static async Task AddImageColumnsAsync(AppDbContext db)
    {
        var addedAny = false;
        foreach (var t in ImageTables)
        {
            var exists = (await db.Database.SqlQueryRaw<int>($"SELECT CASE WHEN COL_LENGTH('{t}', 'ImageUrl') IS NULL THEN 0 ELSE 1 END AS [Value]").ToListAsync()).First() == 1;
            if (exists) continue;
            await db.Database.ExecuteSqlRawAsync($"ALTER TABLE [{t}] ADD [ImageUrl] NVARCHAR(300) NULL");
            addedAny = true;
        }
        // only right after the column was created - otherwise a picture the user deleted would come back on the next start
        if (addedAny) await ApplySeedImagesAsync(db);
    }

    /// <summary>Default pictures for the demo records (files live in wwwroot/images/...). Only fills records that have no picture yet.</summary>
    public static async Task ApplySeedImagesAsync(AppDbContext db)
    {
        var shopImg = new Dictionary<string, string>
        {
            ["NX-101"] = "/images/shops/nx-101-flagship", ["NX-102"] = "/images/shops/nx-102-harbor-hub",
            ["NX-103"] = "/images/shops/nx-103-tech-park", ["NX-104"] = "/images/shops/nx-104-express",
        };
        var empImg = new Dictionary<string, string>
        {
            ["EMP-0001"] = "/images/people/emp-aarav-mehta", ["EMP-0002"] = "/images/people/emp-priya-nair",
            ["EMP-0003"] = "/images/people/emp-rohan-verma", ["EMP-0004"] = "/images/people/emp-sara-fernandes",
            ["EMP-0005"] = "/images/people/emp-david-chen", ["EMP-0006"] = "/images/people/emp-nina-kapoor",
        };
        var vendorImg = new Dictionary<string, string>
        {
            ["Zyxelwave Technologies"] = "/images/vendors/zyxelwave", ["FiberLink Industries"] = "/images/vendors/fiberlink",
            ["ComSet Devices"] = "/images/vendors/comset",
        };
        var equipImg = new Dictionary<string, string>
        {
            ["NW-5600 VDSL Modem"] = "/images/products/modem-nw5600", ["NW-AX3200 WiFi 6 Router"] = "/images/products/router-ax3200",
            ["Dual-Band GPON ONT"] = "/images/products/ont-gpon", ["Corded Desk Phone CX-2"] = "/images/products/phone-cx2",
            ["ADSL2+ Micro Modem"] = "/images/products/modem-adsl-micro",
            ["Cat6 Drop Cable (305m)"] = "/images/products/cable-cat6",
        };
        var custImg = new Dictionary<string, string>
        {
            ["Meera Joshi"] = "/images/people/cust-meera-joshi", ["Kabir Anand"] = "/images/people/cust-kabir-anand",
            ["Elena D'Souza"] = "/images/people/cust-elena-dsouza", ["Farhan Qureshi"] = "/images/people/cust-farhan-qureshi",
            ["Tanya Bhatt"] = "/images/people/cust-tanya-bhatt", ["Vikram Rao"] = "/images/people/cust-vikram-rao",
        };

        foreach (var x in await db.RetailShops.Where(x => x.ImageUrl == null).ToListAsync()) if (shopImg.TryGetValue(x.Code, out var v)) x.ImageUrl = Ui.Asset(v);
        foreach (var x in await db.Employees.Where(x => x.ImageUrl == null).ToListAsync()) if (empImg.TryGetValue(x.EmpCode, out var v)) x.ImageUrl = Ui.Asset(v);
        foreach (var x in await db.Vendors.Where(x => x.ImageUrl == null).ToListAsync()) if (vendorImg.TryGetValue(x.Name, out var v)) x.ImageUrl = Ui.Asset(v);
        foreach (var x in await db.Equipment.Where(x => x.ImageUrl == null).ToListAsync()) if (equipImg.TryGetValue(x.Name, out var v)) x.ImageUrl = Ui.Asset(v);
        foreach (var x in await db.Customers.Where(x => x.ImageUrl == null).ToListAsync()) if (custImg.TryGetValue(x.Name, out var v)) x.ImageUrl = Ui.Asset(v);
        await db.SaveChangesAsync();
    }
}
