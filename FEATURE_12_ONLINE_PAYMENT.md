# Feature 12 — Online payment simulation (DEMO) — theme aur maujooda UI me koi change nahi

**Naya table:** `OnlinePayments` (app start par khud ban jata hai, purani DB me bhi; `Database/NexusCommunicationDb.sql` me bhi add hai).

## Customer ke liye kya hota hai
1. Portal → **My Bills** me har unpaid bill par **Pay now** button (pehle yahan "Pay at outlet / office" likha tha; wo line ab neeche note me hai). Bill-due reminder banner me bhi **Pay now** hai.
2. Dialog khulta hai: upar **DEMO PAYMENT** ka saaf notice, amount (poora balance), aur 3 methods — **Card / JazzCash / Easypaisa**.
3. Form bharne par **2–3 second "Processing…"** animation (progress bar + steps).
4. Phir **Success** (transaction ID, copy button, "View receipt") ya **Failure** (wajah + "Try again").
5. Success par:
   * Bill ka status khud **Paid** (ya Partial) ho jata hai,
   * **Accounts desk → Receipts** me payment dikhti hai (Channel badge: *Online · demo*), Overview me "Online (demo)" ka total + bar bhi,
   * Invoice page ke payments table me bhi (Reference par click karke receipt khulti hai),
   * **Notification (bell):** customer ko "Payment successful (demo)" (receipt link ke saath), accounts + admin ko "Online payment received (demo)". Fail par sirf customer ko "Payment failed (demo)".
   * Admin ke **Activity log** me entry.
6. **Receipt page:** `/receipt/DEMO-YYYYMMDD-XXXXXX` — print / Save as PDF, upar "DEMO PAYMENT" notice. Customer sirf apni receipt, admin/accounts koi bhi dekh sakte hain.

## Demo test data (form me "Fill" buttons bhi hain)
| Method | Success | Failure |
|---|---|---|
| Card | koi bhi 16 digit, jaise `4242 4242 4242 4242` | number ka aakhir `0002`, jaise `4000 0000 0000 0002` |
| JazzCash / Easypaisa | koi bhi `03XX XXXXXXX` number | number ka aakhir `0000` |

Expiry `MM/YY` (aane wali date), CVV 3 digit, wallet PIN 4 digit — kuch bhi chalega.

## Asli data kabhi store nahi hota
* Card number, expiry, CVV, mobile number, PIN **server ko bheje hi nahi jate** — browser sirf `{billId, method, outcome}` bhejta hai. Form band/submit hote hi fields DOM se hat jati hain.
* Database me sirf: transaction ID, bill, customer, method, amount, success/failed, failure reason.
* Customer sirf **apne** bill ka poora balance de sakta hai; paid bill dobara pay nahi hota; bill par RowVersion hone se double-click par dusri payment reject ho jati hai.
* 10 minute me max 10 attempts.
* **Production me band karne ke liye:** `appsettings.json` → `"DemoPayments": { "Enabled": false }` (API 403 deti hai).

## Files
| Kya | Kahan |
|---|---|
| Entity `OnlinePayment`, `OnlineMethods` | `Models/Entities.cs` |
| Table | `Data/AppDbContext.cs`, `Data/DbSeeder.cs`, `Database/NexusCommunicationDb.sql` |
| API `POST /api/payments/online` | `Controllers/Api/PaymentsApiController.cs` |
| Receipt page | `Controllers/ReceiptController.cs`, `Views/Receipt/Show.cshtml` |
| Alerts | `Services/NotificationService.cs` (`OnlinePaymentSucceededAsync`, `OnlinePaymentFailedAsync`) |
| Pay dialog | `wwwroot/js/payments.js`, `Views/Shared/_PaymentAssets.cshtml` |
| Buttons | `Views/Portal/Index.cshtml` |
| Accounts / invoice display | `Views/AccountsDesk/Index.cshtml`, `Controllers/AccountsDeskController.cs`, `Views/Invoice/Show.cshtml` |

## Note
Is machine par .NET SDK nahi tha, isliye project compile/run nahi hua (C# braces, Razor aur JS syntax manually check hua). Visual Studio me pehli dafa Build karke dekhein; koi error aaye to bata dein.
