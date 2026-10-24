# Feature 11 — Complaint / Ticket system (theme aur maujooda UI me koi change nahi)

**Naye tables:** `Tickets`, `TicketReplies` (app start par khud ban jate hain, purani DB me bhi; `Database/NexusCommunicationDb.sql` me bhi add hain).

## Status flow
`Open → Assigned → In progress → Resolved → Closed`

| Kaun | Kya kar sakta hai |
|---|---|
| **Customer** (Portal → *Complaints* tab) | Category + subject + description + optional photo ke saath complaint raise karna; apne ticket ka status/stepper aur saare replies dekhna; reply (photo ke saath bhi) likhna; *Resolved* ho jaye to "Problem solved — close" ya "Not fixed — reopen"; *Open* ticket ko withdraw (close) karna |
| **Admin** (Admin → *Complaints* tab) | Saare tickets dekhna (status chips, filter, search); naya ticket **technical desk ke employee ko assign** karna (re-assign bhi, jab tak resolved/closed na ho); reply, close, reopen |
| **Technical desk** (Technical → *My Tickets* tab) | Sirf apne assigned tickets; *Start working*, reply, **Mark resolved** (fix ka note zaroori — customer ko dikhta hai) |

Status server hi check karta hai: har role ko sirf wahi buttons milte hain jo us waqt allowed hain.
Customer ticket par resolved ke baad reply likhe to ticket khud *In progress* me wapas aa jata hai. Technician pehla reply likhe to *Assigned → In progress* ho jata hai.

## Notifications (bell) — pehle se maujood bell use hui
* Naya ticket → admin ko
* Assign → technician ko + customer ko
* Status change (in progress / resolved / closed / reopened) → customer + admin + technician (jisne khud kiya usse nahi)
* Naya reply → doosri side ko
* Har alert ka link seedha `/portal#tickets`, `/admin#tickets` ya `/technical#tickets` kholta hai.

Har create / assign / status change **Activity log** (admin) me bhi likha jata hai.

## Files
| Kya | Kahan |
|---|---|
| Entities (`Ticket`, `TicketReply`, `TicketStatus`, `TicketCategories`) | `Models/Entities.cs` |
| Tables | `Data/AppDbContext.cs`, `Data/DbSeeder.cs` (`EnsureSupportTablesAsync`), `Database/NexusCommunicationDb.sql` |
| API | `Controllers/Api/TicketsApiController.cs` (`/api/tickets`) |
| Ticket number (TK-000001) | `Services/IdGenerator.cs` |
| Alerts | `Services/NotificationService.cs` (Ticket* methods) |
| UI (teeno desk ke liye ek hi) | `Views/Shared/_TicketsPanel.cshtml`, `wwwroot/js/tickets.js` |
| Tabs | `Views/Portal/Index.cshtml`, `Views/Admin/Index.cshtml`, `Views/Technical/Index.cshtml` |

## Hifazat
* Customer sirf apne tickets dekh sakta hai; technician sirf apne assigned; admin sab. Doosre ka ticket kholne par "not found".
* Photo ka content check hota hai (JPG/PNG/WEBP/GIF, max 3 MB) — upload `wwwroot/uploads/tickets/` me save hoti hai (folder khud ban jata hai).
* Ek customer ek ghante me max 5 complaints raise kar sakta hai.

## Note
Is machine par .NET SDK nahi tha, isliye project compile/run nahi hua (C# braces, Razor aur JS syntax manually check hua). Visual Studio me pehli dafa Build karke dekhein; koi error aaye to bata dein.
Technician assign karne ke liye Employees me *technical* department ka active employee hona chahiye jiska login bana ho (demo me `Rohan Verma`, login `technical / tech123`).
