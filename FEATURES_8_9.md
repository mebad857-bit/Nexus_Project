# Features 8 & 9 (UI / theme / database schema mein koi change nahi)

## 8. Dashboard charts (Chart.js)
| Jagah | Chart | Data kahan se |
|---|---|---|
| Admin → Overview | Monthly revenue (line: Invoiced vs Collected) | Bills + Payments (last 6 months) |
| Admin → Overview | Orders by status (doughnut) | Orders table |
| Admin → Overview (naya card) | Connections by plan (bar, type ke rang me) | Connections + Plans |
| Accounts → Overview (naya row) | Collections vs dues (bar, 6 months) + Collected vs outstanding (doughnut) | Payments + unpaid Bills |

* Sab data asli DB records se aata hai (seed data bhi wahi records hain).
* Chart.js CDN se load hota hai (`cdn.jsdelivr.net`, v4.4.7). Internet na ho to purane SVG charts (revenue/status) fallback ban jate hain.
* Script: `wwwroot/js/dashboard-charts.js`

## 9. Bill due reminder
* Customer portal ke upar banner: `Bill INV-… · $… due on <date>` + "due in N days / due today / overdue by N days".
  Rule: bill unpaid ho aur due date guzar chuki ho ya 5 din ke andar ho (`BillReminder.WarnDays` se badal sakte hain).
* Notification bell me bhi alert: "Bill due soon" / "Bill overdue" (har bill ke liye har stage par sirf ek dafa).
* Bill poora pay ho jaye to uske reminder alerts khud read ho jate hain.
* Naya table nahi — sirf maujooda Bills aur Notifications.
* Code: `Services/BillReminder.cs`, `NotificationService.SyncBillRemindersAsync`, `PortalController`, `Views/Portal/Index.cshtml`.

## Note
Is machine par .NET SDK nahi tha, isliye project compile/run nahi hua (brace-balance + JS syntax check hua hai).
Visual Studio me pehli dafa Build karke dekhein; koi error aaye to bata dein.

## 10. Password reset / change password + Security page (UI aur theme wahi, sirf zaroorat ki cheezein add hui)

**Naya table:** `PasswordResetTokens` (app start par khud ban jata hai, purani DB me bhi). Token ka sirf SHA-256 hash save hota hai; link ek dafa chalta hai aur 30 minute me khatam.
Password hash wahi purana `PasswordHasher` karta hai.

| Kya | Kahan |
|---|---|
| Profile ke andar **Security** page (`/account/security`): purana password + naya password, strength bar + checklist, "passwords match" line | `Views/Account/Security.cshtml`, `AccountController.Security` |
| Profile page par Profile / Security tabs; purani change-password form ab Security page par | `Views/Account/Index.cshtml`, `Views/Shared/_AccountTabs.cshtml` |
| Customer portal → Profile tab me "Password & security" button | `Views/Portal/Index.cshtml` |
| **Forgot password** (`/forgot-password`): username YA email se link banta hai | `Controllers/PasswordController.cs`, `Views/Password/ForgotPassword.cshtml` |
| **Reset page** (`/reset-password?token=…`) | `Views/Password/ResetPassword.cshtml` |
| Login page par "Forgot password?" link | `Views/Auth/Login.cshtml` |

### SMTP (asli email)
`appsettings.json` me `Smtp` section hai (Host khali = **demo mode**: link screen par dikhta hai, email nahi jati).
Asli email ke liye bharein:
```json
"Smtp": { "Host": "smtp.gmail.com", "Port": 587, "EnableSsl": true, "User": "aap@gmail.com", "Password": "app-password", "From": "aap@gmail.com", "FromName": "Nexus Service Marketing" },
"PasswordReset": { "ExpiryMinutes": 30, "BaseUrl": "https://aap-ki-site.com" }
```
`BaseUrl` zaroor set karein jab email chalaen (link usi address se banta hai). Gmail ke liye normal password nahi, "App password" chahiye.

### Hifazati intezam
* Ek username/email/IP se 15 minute me max 5 request.
* Naya link maangne par purana link band; reset ke baad baqi links bhi band.
* Reset hone par login ka "5 galat password" lock bhi khul jata hai; bell me "Password changed" alert; Activity log me entry.
* Password username ke barabar nahi ho sakta (change aur reset dono me).
