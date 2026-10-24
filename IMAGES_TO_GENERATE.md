# NEXUS SERVICE MARKETING SYSTEM — Images jo generate karni hain (62 images)

Har image ko **exactly isi naam** se save karein aur is folder me rakhein:

```
NexusCommunication/wwwroot/images/<folder>/<naam>.jpg
```

Code me in sab ke path pehle se lage hue hain. Agar koi image na ho to page kharab nahi hota
(initials / gradient dikhta hai), is liye aap thoda thoda karke bhi daal sakte hain.

**Logo already laga hua hai:** `wwwroot/images/brand/logo.png` (aap ka diya hua logo) aur
`logo-mark.png` (sirf "N", navbar/sidebar ke liye) + `wwwroot/favicon.png`.
Logo navbar, sidebar, footer, login, register, invoice aur error page par use ho raha hai.

---

## Sab prompts ke saath ye "style" line lagayein (lmarena par)

> **STYLE (har prompt ke end me paste karein):**
> `dark navy background (#070B16), neon cyan and electric blue accents with a hint of violet, cinematic lighting, ultra detailed, sharp focus, professional telecom / technology brand photography, no text, no letters, no logos, no watermark`

Kyun: website dark theme (navy + cyan + violet) ki hai, to images isi mood me aani chahiyein warna alag dikhengi.
Images me **text/logo na ho** (AI text galat likhta hai).
Format: **JPG**, quality ~85. Size har section me likhi hai.

> Tip: wide images (hero/banner) me main subject **right side** rakhein, kyun ke left side par text aur dark gradient aata hai.

---

## 1. `hero/` — bade banners (1920×1080, 16:9)

| # | Path | Kahan use hota hai | Description / Prompt |
|---|------|--------------------|----------------------|
| 1 | `hero/hero-network.jpg` | Home page — pehla bada hero (poori screen) | Aerial night view of a futuristic smart city with glowing cyan fibre-optic network lines connecting rooftops and towers, light trails on roads, subject concentrated on the right half, left side darker and empty |
| 2 | `hero/login-visual.jpg` | Login page — left side (portrait crop, 1200×1600) | Abstract constellation of glowing cyan nodes connected by thin light lines forming a network sphere floating in dark space, subtle violet glow, vertical composition |
| 3 | `hero/register-visual.jpg` | Register page — left side (1200×1600) | Smiling young customer holding a smartphone in a modern living room at night with a soft cyan glow of Wi-Fi signal rings around them, cinematic, vertical composition, face not too close |
| 4 | `hero/cta-engineer.jpg` | Home page — sabse neeche "Get your connection surveyed" background | Telecom engineer in safety helmet and reflective vest checking a fibre-optic cabinet at dusk, cyan indicator lights glowing, shallow depth of field, wide shot |
| 5 | `hero/plans-hero.jpg` | Plans page — upar wala wide banner (1920×640) | Wide panoramic banner of three glowing connection types: a vintage dial-up modem, a modern Wi-Fi 6 router and a desk landline phone arranged side by side on a dark reflective surface, cyan rim light |
| 6 | `hero/track-order-desk.jpg` | Track page — neeche 3 images me pehli (1200×800) | Friendly customer-service executive at a retail counter with a computer screen showing a tracking dashboard (screen content blurred, no readable text), cyan ambient light |
| 7 | `hero/track-field-visit.jpg` | Track page — doosri | Two technicians surveying a street junction box with a tablet, evening light, blue-cyan tones |
| 8 | `hero/track-activation.jpg` | Track page — teesri | Close-up of a fibre router with green and cyan LEDs glowing on a clean desk, shallow depth of field, a laptop softly blurred behind |

---

## 2. `services/` — Home page ke service cards (1200×800)

| # | Path | Kahan | Description / Prompt |
|---|------|-------|----------------------|
| 9 | `services/dialup.jpg` | Home → "Dial-Up Access" card | Retro-modern composition: beige external dial-up modem with blinking amber LEDs next to a classic corded telephone and an old CRT monitor, warm amber light mixed with cyan rim light, dark background |
| 10 | `services/broadband.jpg` | Home → "Broadband Fiber" card (featured) | Bundle of glowing fibre-optic cables with cyan and blue light streaming toward the camera, modern white Wi-Fi router in the foreground, speed / data feeling |
| 11 | `services/landline.jpg` | Home → "Landline Voice" card | Elegant desk telephone with cord on a dark wooden desk, soft violet and cyan glow, a notepad and a coffee cup, office mood |
| 12 | `services/retail-store.jpg` | Home → "Retail network" section (portrait 4:5, 1000×1250) | Modern telecom retail outlet interior, glass storefront, shelves with routers and phones, glowing cyan signage strip (no readable text), welcoming staff at counter |
| 13 | `services/corporate-office.jpg` | Home → "Bulk & corporate schemes" (1200×700) | Open-plan corporate office with rows of employees at desks, many monitors, subtle cyan network lines overlaid between workstations |

## 3. `process/` — Home page "How it works" ke 4 steps (900×600)

| # | Path | Step | Description / Prompt |
|---|------|------|----------------------|
| 14 | `process/application.jpg` | 01 Application | Hands signing an application form on a clipboard at a retail counter, pen and ID card on the table, cyan accent light |
| 15 | `process/survey.jpg` | 02 Feasibility survey | Engineer with a tablet and a measuring device beside a telecom street cabinet, topographic network map hologram glowing in cyan |
| 16 | `process/activation.jpg` | 03 Activation | Technician installing a modem on a wall in a living room, cables neatly routed, green "online" LED glowing |
| 17 | `process/billing.jpg` | 04 Transparent billing | Printed invoice and a card payment terminal on a counter, calculator, subtle glowing cyan receipt icons floating above (no readable text) |

---

## 4. `plans/` — Plans page + Home plan cards (1000×500, wide)

Ye 3 images **har plan card ke upar** type ke hisaab se dikhti hain (agar admin ne us plan ki apni image upload nahi ki).

| # | Path | Description / Prompt |
|---|------|----------------------|
| 18 | `plans/dialup.jpg` | Close-up of an old modem and phone cable with amber glow, nostalgic yet clean, dark background |
| 19 | `plans/broadband.jpg` | Abstract speed lines of cyan and blue light streaming across a dark background with a Wi-Fi signal symbol made of light (symbol only, no text) |
| 20 | `plans/landline.jpg` | Elegant desk phone handset with violet and cyan reflections, dark background, minimal |

---

## 5. `shops/` — Retail outlets (1200×800)

Home page ke outlet cards, Admin → Retail Shops table. (Admin naye outlet ki image khud upload kar sakta hai.)

| # | Path | Description / Prompt |
|---|------|----------------------|
| 21 | `shops/nx-101-flagship.jpg` | Nexus Flagship — large modern glass-fronted telecom flagship store on a city avenue at dusk, cyan LED strip lighting, people walking by |
| 22 | `shops/nx-102-harbor-hub.jpg` | Nexus Harbor Hub — telecom shop in a harbor district near the water, cranes and ships softly blurred in the background, blue hour |
| 23 | `shops/nx-103-tech-park.jpg` | Nexus Tech Park — sleek outlet inside a glass office-park building, reflections, futuristic architecture |
| 24 | `shops/nx-104-express.jpg` | Nexus Express — small compact express kiosk-style store on a riverside walkway, evening lights |
| 25 | `shops/outlet-default.jpg` | Default picture for any **new** outlet — generic modern retail counter interior with shelves, cyan accent light, no signage text |

---

## 6. `products/` — Equipment / Products (800×800, square)

Admin → Equipment & Stock, Technical → Equipment bay. Product ek clean dark studio background par, soft cyan rim light.

| # | Path | Product | Description / Prompt |
|---|------|---------|----------------------|
| 26 | `products/modem-nw5600.jpg` | NW-5600 VDSL Modem | Sleek black VDSL modem with front LED row glowing cyan, product photo on dark reflective surface, three-quarter angle |
| 27 | `products/router-ax3200.jpg` | NW-AX3200 WiFi 6 Router | Modern white Wi-Fi 6 router with four antennas, glowing cyan status light, product photo on dark background |
| 28 | `products/ont-gpon.jpg` | Dual-Band GPON ONT | Compact white fibre ONT box with a green fibre cable plugged in and small LED indicators, studio product photo |
| 29 | `products/phone-cx2.jpg` | Corded Desk Phone CX-2 | Black corded desk telephone with keypad, studio product photo with violet rim light |
| 30 | `products/modem-adsl-micro.jpg` | ADSL2+ Micro Modem | Very small compact ADSL micro modem dongle with a telephone cable, macro product photo, dark background |
| 31 | `products/cable-cat6.jpg` | Cat6 Drop Cable (305m) | Large coil / drum of blue Cat6 network cable with RJ45 connectors on top, product photo, dark background |

## 7. `vendors/` — Vendor pictures (600×600, square)

Admin → Vendors. Company-style emblem image **bina text ke**.

| # | Path | Vendor | Description / Prompt |
|---|------|--------|----------------------|
| 32 | `vendors/zyxelwave.jpg` | Zyxelwave Technologies | Abstract blue wave-shaped emblem made of glowing lines on a dark navy rounded square, tech company badge, no letters |
| 33 | `vendors/fiberlink.jpg` | FiberLink Industries | Abstract emblem of interlinked fibre strands forming a loop, cyan and green glow, dark rounded square, no letters |
| 34 | `vendors/comset.jpg` | ComSet Devices | Abstract emblem of a stylised handset and signal arcs in violet and cyan on a dark rounded square, no letters |

---

## 8. `people/` — Profile photos (800×800, square, chehra center me)

Employees aur demo customers ki photos. Realistic professional portrait headshots, soft studio light, neutral dark-blue
background, friendly expression, shoulders up. **Ye sab AI-generated imaginary log hon, kisi real insaan jaise nahi.**

| # | Path | Kaun | Description / Prompt |
|---|------|------|----------------------|
| 35 | `people/emp-aarav-mehta.jpg` | Aarav Mehta — General Manager (Admin) | Confident man in his 40s, dark suit, no tie, calm smile |
| 36 | `people/emp-priya-nair.jpg` | Priya Nair — Accounts Officer | Woman in her 30s, smart blazer, glasses, warm smile |
| 37 | `people/emp-rohan-verma.jpg` | Rohan Verma — Network Engineer | Man in his 30s, casual company polo, short beard, headset around neck |
| 38 | `people/emp-sara-fernandes.jpg` | Sara Fernandes — Retail Executive | Woman in her late 20s, company shirt, friendly bright smile |
| 39 | `people/emp-david-chen.jpg` | David Chen — Retail Executive | Man in his 30s, East-Asian features, shirt with open collar, relaxed smile |
| 40 | `people/emp-nina-kapoor.jpg` | Nina Kapoor — Store Manager | Woman in her 40s, elegant blazer, confident expression |
| 41 | `people/cust-meera-joshi.jpg` | Meera Joshi (demo customer login) | Woman in her early 30s, casual cardigan, natural smile |
| 42 | `people/cust-kabir-anand.jpg` | Kabir Anand | Man in his 50s, grey stubble, polo shirt |
| 43 | `people/cust-elena-dsouza.jpg` | Elena D'Souza | Woman in her late 30s, business casual, shoulder-length hair |
| 44 | `people/cust-farhan-qureshi.jpg` | Farhan Qureshi | Young man in his mid 20s, hoodie, friendly look |
| 45 | `people/cust-tanya-bhatt.jpg` | Tanya Bhatt | Woman in her 20s, bright smile, casual top |
| 46 | `people/cust-vikram-rao.jpg` | Vikram Rao | Man in his 40s, formal shirt, moustache |

---

## 9. `dashboard/` — Har desk ke upar 3 images ki strip (1600×900)

Har dashboard (aur "My account" page) ke upar **1 bada + 2 chhote** picture dikhte hain.
Is liye har role ki 3 images chahiyein: `-1` (bada, wide), `-2` aur `-3` (chhote, wide crop theek hai).
Mood: dark, premium, cyan/violet glow, **koi text nahi**.

### Admin Desk — `dashboard/admin-1.jpg`, `admin-2.jpg`, `admin-3.jpg`
| # | Path | Description / Prompt |
|---|------|----------------------|
| 47 | `dashboard/admin-1.jpg` | Network operations command centre with a large video wall of glowing maps and graphs (unreadable), operators silhouetted, cyan light |
| 48 | `dashboard/admin-2.jpg` | Warehouse shelves stacked with boxed modems and routers, inventory scanner in hand, blue light |
| 49 | `dashboard/admin-3.jpg` | Aerial map-style view of a city with glowing shop pins connected by cyan lines (no text) |

### Accounts Desk — `dashboard/accounts-1.jpg` … `accounts-3.jpg`
| # | Path | Description / Prompt |
|---|------|----------------------|
| 50 | `dashboard/accounts-1.jpg` | Finance office desk with laptop showing abstract bar charts (no readable numbers), invoices stacked, emerald-teal and cyan light |
| 51 | `dashboard/accounts-2.jpg` | Cashier counter taking a card payment on a terminal, hands close-up, teal glow |
| 52 | `dashboard/accounts-3.jpg` | Abstract glowing revenue growth chart rising over a dark background, emerald and cyan lines, no numbers or text |

### Technical Desk — `dashboard/technical-1.jpg` … `technical-3.jpg`
| # | Path | Description / Prompt |
|---|------|----------------------|
| 53 | `dashboard/technical-1.jpg` | Field engineer on a utility pole with climbing gear at sunset, fibre cable in hand, amber and cyan light |
| 54 | `dashboard/technical-2.jpg` | Server / network rack with neatly routed patch cables and blinking LEDs, amber and cyan glow |
| 55 | `dashboard/technical-3.jpg` | Technician with a tablet doing a line feasibility test beside an open street cabinet, dusk |

### Retail Desk — `dashboard/retail-1.jpg` … `retail-3.jpg`
| # | Path | Description / Prompt |
|---|------|----------------------|
| 56 | `dashboard/retail-1.jpg` | Retail executive welcoming a customer at a modern telecom counter, violet and cyan ambient light |
| 57 | `dashboard/retail-2.jpg` | Customer service representative with headset helping a customer with an application form, violet accent light |
| 58 | `dashboard/retail-3.jpg` | Order desk close-up: hand placing a SIM-style card and a router box on a counter next to a barcode scanner, violet glow |

### Customer Portal — `dashboard/customer-1.jpg` … `customer-3.jpg`
| # | Path | Description / Prompt |
|---|------|----------------------|
| 59 | `dashboard/customer-1.jpg` | Cosy modern living room at night, family relaxed on the sofa with a laptop and tablet, soft sky-blue and cyan Wi-Fi glow |
| 60 | `dashboard/customer-2.jpg` | Friendly support agent with a headset smiling, blurred office behind, sky-blue light |
| 61 | `dashboard/customer-3.jpg` | Person paying a bill on a smartphone at a kitchen table, phone screen glowing (content not readable), sky-blue accent |

---

## 10. `misc/`

| # | Path | Kahan | Description / Prompt |
|---|------|-------|----------------------|
| 62 | `misc/error-signal-lost.jpg` | 404 / 403 error page (800×400) | Lone antenna tower in a dark foggy landscape with a broken, flickering cyan signal arc above it, moody, "signal lost" feeling |

---

## Page-by-page: kis page par kitni images aati hain

| Page | Images |
|------|--------|
| **Home** | logo + hero-network + 3 service cards + 4 process steps + corporate-office + retail-store + outlet card photos (har outlet ki) + cta-engineer + plan card images ≈ **15+** |
| **Plans** | logo + plans-hero + har plan card par type image (3 types) ≈ **5+** |
| **Track** | logo + 3 images (order desk, field visit, activation) = **4** |
| **Login** | logo (2 jagah) + login-visual = **3** |
| **Register** | logo (2 jagah) + register-visual + **profile photo upload (preview)** = **4** |
| **Admin** | logo + avatar + 3 dashboard images + har table me thumbnails (customers, employees, vendors, products, shops, plans) = **10+** |
| **Accounts** | logo + avatar + 3 dashboard images + dues table me customer photos = **5+** |
| **Technical** | logo + avatar + 3 dashboard images + product photos (equipment bay) = **5+** |
| **Retail** | logo + avatar + 3 dashboard images + customer photos = **5+** |
| **Customer portal** | logo + avatar + 3 dashboard images + **profile photo (upload/change/remove)** = **5+** |
| **My account** | logo + avatar (photo manager) + 3 role images = **5** |
| **Invoice (print)** | logo |
| **Error 404/403** | logo + error-signal-lost |

## Image CRUD (upload / change / delete) — kahan kahan ho sakta hai

| Kya | Kaun karega | Kahan se |
|-----|-------------|----------|
| **Customer ki profile photo** | Customer khud | Register page (optional) · Portal → Profile tab · My account |
| **Customer ki photo (counter customer)** | Admin / Retail | Admin → Customers (Add / Edit) · Retail → Customers (Add / Edit) |
| **Employee ki photo** | Admin; ya employee khud | Admin → Employees (Add / Edit) · My account |
| **Product (equipment) ki image** | Admin / Technical | Admin → Equipment (Add / Edit) · Technical → Equipment bay (Add + "Photo" button) |
| **Vendor ki image** | Admin | Admin → Vendors (Add / Edit) |
| **Outlet (shop) ki image** | Admin | Admin → Retail Shops (Add / Edit) |
| **Plan ki image** | Admin | Admin → Plans & Charges (Add / Edit) — na ho to type wali default image |

Edit dialog ke upar photo box hota hai: **Upload / Change photo** aur **Remove**. Upload ki gayi files
`wwwroot/uploads/...` me save hoti hain (JPG/PNG/WEBP/GIF, max 3 MB, file ka content bhi check hota hai).
Record delete karne par uski uploaded image bhi server se delete ho jati hai.
