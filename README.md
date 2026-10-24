# ✅ v2 update — requirement coverage + UI upgrade (isi file ko pehle parhein)

## Doc ki requirement vs project (ab kya kya hai)

| Requirement (doc se) | Kahan hai |
|---|---|
| Admin: employees, vendors, retail shops, stock, plans (insert/update/delete/search) | `/admin` → Employees, Vendors, Equipment & Stock, Retail Shops, Plans & Charges — ab **Edit dialog**, search, sort, paging ke saath |
| Admin: customers / orders / connections / billing ka overview | **NEW** `/admin` → Customers, Orders, Connections, Billing & Payments panels |
| Admin: stock dekh kar vendors se equipment order karna | **NEW** `/admin` → Procurement (open demand vs stock, reorder suggestions, Receive stock) |
| Customer feedback collect karna | Public form + **NEW** customer portal Feedback tab + **NEW** admin Feedback panel (average rating) |
| Retail employee: order place, order status, connections, billing, payments | `/retail` — **NEW** Customers panel (add/edit), order remarks, invoice links |
| Technical: feasibility, connection create, active/temp/perm inactive, equipment | `/technical` — **NEW** Order Tracker, Plan change, Add equipment, Receive stock, **bulk order = N alag account ids** |
| Accounts: bill generate (plan + equipment + 12.24% tax), payments | `/accounts` — **NEW** Overview charts, Customer Dues, printable **Invoice** page (`/invoice/{id}`) |
| Customer: contact details, order, order status, bills | `/portal` — **NEW** Feedback, order withdraw, invoice view, progress bar |
| Customer self sign-up | **NEW** `/register` |
| Search by order id / account id + advanced search (name, type, date, contact) | `/track` — advanced search ab **orders aur connections dono** me search karta hai |
| Corporate discount 10-15 / 15-25 / 25-50 / 50+ | Fix: pehle "50 lines" ko 100% mil jata tha, ab sirf **50 se zyada** (backend + JS) |
| 16-digit account id, 11-digit order id | Pehle jaisa; bulk orders ke liye har line ko apna id |

## Flaws jo fix hue
* Public Track page par naam/phone sab ko dikhte the → ab **mask** (staff login par full).
* Bulk order me sirf 1 connection banta tha → ab quantity ke barabar connections + stock check.
* Advanced-search form hamesha khula tha aur toggle ulta tha → fix.
* Demo passwords login page par hamesha dikhte the → ab sirf `Development` me.
* Brute-force: 5 galat password par 5 minute lock.
* `alert()/confirm()` hata kar toast + confirm dialogs; har table me search/sort/paging.
* 404/403 ka proper page; mobile par bhi sab panels (sidebar + tab bar).
* Customer ki city badalne par account-id wala city code sync nahi hota tha → fix.
* Rupee icon ($ currency ke saath) → calculator icon; "Billing discipline score" jaisa fake text hata diya.

## Naye features
Command palette (`Ctrl+K` ya `/`), animated stat cards, revenue bar chart + donut charts, password strength meter + show/hide,
change-password page (`/account`), printable invoice, copy-to-clipboard par ids, reduced-motion + print styles.

## Note
Is machine par .NET SDK nahi tha, isliye code **compile/run nahi kiya gaya** (JS syntax check aur view balance check hua hai).
Visual Studio me open karke pehli dafa Build karein; koi chhoti compile error aaye to bata dein, fix kar dunga.
Database schema me **koi change nahi** hua — purani DB bhi chalegi.

---

# Nexus Communication System — ASP.NET Core MVC + SQL Server

Yeh project original Next.js/React app ka **ASP.NET Core MVC (.NET 8)** conversion hai.
UI (Tailwind classes, colors, fonts, animations, layout) bilkul same rakha gaya hai — sirf backend badla hai.

## Structure

```
NexusCommunication.sln
NexusCommunication/
├── NexusCommunication.csproj
├── Program.cs                     # DI, cookie auth, antiforgery, DB init
├── appsettings.json               # SQL Server connection string
├── Controllers/                   # MVC page controllers (Home, Auth, Admin, AccountsDesk, Technical, Retail, Portal)
│   └── Api/                       # JSON endpoints (/api/orders, /api/connections, /api/bills, /api/payments, /api/crud/{resource})
├── Models/                        # Entities.cs (EF Core entities) + ViewModels.cs
├── Data/                          # AppDbContext.cs + DbSeeder.cs (demo data)
├── Services/                      # PasswordHasher, IdGenerator, Fmt (formatting), Ui (HTML helpers)
├── Views/                         # Razor views (Shared/_Layout, _Nav, _Footer, dashboards ...)
├── wwwroot/js/                    # site.js + per-desk scripts (admin/accounts/technical/retail/portal)
├── wwwroot/images/                # <-- apni images yahan copy karein (neeche dekhein)
└── Database/NexusCommunicationDb.sql   # optional manual SQL Server schema
```

## Run karne ka tareeqa

1. **Visual Studio 2022** me `NexusCommunication.sln` kholein (ya `dotnet` CLI use karein). .NET 8 SDK chahiye.
2. `NexusCommunication/appsettings.json` me connection string apne SQL Server ke hisab se set karein.
   Default LocalDB hai: `Server=(localdb)\MSSQLLocalDB;Database=NexusCommunicationDb;Trusted_Connection=True;...`
   SQL Server Express ke liye: `Server=.\SQLEXPRESS;Database=NexusCommunicationDb;Trusted_Connection=True;TrustServerCertificate=True;`
3. Run (F5 / `dotnet run --project NexusCommunication`). Pehli dafa app khud database + tables banati hai
   (`Database:AutoCreate`) aur demo data daal deti hai (`Database:SeedDemoData`).
   Agar tables manually banana chahein to `Database/NexusCommunicationDb.sql` chalayen aur `AutoCreate` ko `false` kar dein.

### Demo logins
| Desk | Username | Password |
|---|---|---|
| Admin | admin | admin123 |
| Accounts | accounts | accounts123 |
| Technical | technical | tech123 |
| Retail | retail | retail123 |
| Customer | customer | customer123 |

## Images
Original zip me `public/images` folder included nahi tha. Apni purani images
(`hero-network.jpg, dialup.jpg, broadband.jpg, landline.jpg, retail-store.jpg, engineer.jpg, login-visual.jpg`)
`NexusCommunication/wwwroot/images/` me copy kar dein — pages automatically wahi images use karenge.

## Mapping (Next.js -> ASP.NET MVC)
- `src/app/*/page.tsx` -> `Controllers/*Controller.cs` + `Views/*/*.cshtml`
- `src/app/api/*` -> `Controllers/Api/*`
- `src/db/schema.ts` (Drizzle) -> `Models/Entities.cs` + `Data/AppDbContext.cs` (EF Core, SQL Server)
- `src/lib/auth.ts` (session cookie) -> ASP.NET cookie authentication + `[Authorize(Roles=...)]`
- `src/lib/format.ts`, `ids.ts` -> `Services/Fmt.cs`, `Services/IdGenerator.cs`
- React client components -> Razor views + vanilla JS in `wwwroot/js`

## Notes
- Passwords ab PBKDF2 se hashed store hote hain (`Services/PasswordHasher.cs`).
- Tailwind CSS v4 browser build CDN se load hoti hai (original jaisi classes). Internet chahiye; production ke liye
  Tailwind CLI se static CSS build karna behtar hai.
- Icons `lucide` CDN se load hote hain (same icon set jo React me tha).


---

# 🖼️ v3 — Images + logo + Image CRUD

* Logo: `wwwroot/images/brand/logo.png`, `logo-mark.png`, `wwwroot/favicon.png`
* Image CRUD (upload / change / remove): customers, employees, vendors, equipment (products), shops, plans
  API: `POST|DELETE /api/images/{resource}/{id}` · uploads are saved in `wwwroot/uploads/`
* New `ImageUrl` column on 6 tables — added automatically on start (or run the UPGRADE block at the end of `Database/NexusCommunicationDb.sql`)
* Put generated pictures in `wwwroot/images/<folder>/<name>.jpg` — exact names are in `IMAGES_TO_GENERATE.md`
* Not compiled on the machine that made this change: Build once in Visual Studio and report any error.

---

# v4 — image path fix
* Image paths no longer depend on the file extension: `Ui.Asset("/images/hero/hero-network")` finds .png / .jpg / .jpeg / .webp on disk.
* Wrong `.jpg` paths already saved in the database are repaired automatically on start-up.
* Still to add (not generated yet): `hero/` (8 images) and `products/phone-cx2` — see IMAGES_TO_GENERATE.md.

## Notifications bell + Activity log
- **Bell** (sidebar / mobile bar / public navbar, signed-in only): alerts for order status changes, new bills, received payments, new complaints / contact messages / feedback and (ready for) ticket assignment. Each person only sees their own alerts; staff get alerts for their role. "Mark all read" included. Table: `Notifications`.
- **Activity log** (Admin → Activity Log): who changed what and when, with old → new values; filter by user, action, date and text. Read-only. Table: `AuditLogs`.
- Both tables are created automatically on start-up (same mechanism as Faqs / ContactMessages) - no `Add-Migration` needed. `Database/NexusCommunicationDb.sql` also contains them for manual setup.
