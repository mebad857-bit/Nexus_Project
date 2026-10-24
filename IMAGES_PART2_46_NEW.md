# NEXUS SERVICE MARKETING SYSTEM — Part 2: 46 nayi images

Is file me har image ke liye ye sab likha hai:
1. **Image ka naam** (isi naam se save karni hai)
2. **Kis folder me rakhni hai**
3. **Kis page / tab par dikhegi**
4. **Kaun si code file me us ka path laga hua hai**
5. **Prompt** (lmarena me copy-paste karne ke liye, poori theme us ke andar likhi hai)

---

## 1) Image kahan rakhni hai (sab se zaroori)

Project ke andar yeh folder hai:

```
UI4/NexusCommunication/wwwroot/images/
```

Is ke andar **2 folder** me nayi images jayengi:

```
wwwroot/images/tabs/    <- 28 images (dashboard ki har tab)
wwwroot/images/plans/   <- 18 images (har plan card)
```

Aap ko koi code **change karne ki zarurat nahi**. Image ko neeche likhe naam se sahi folder me daal dein, bas.
Extension `.jpg`, `.png`, `.jpeg` ya `.webp` kuch bhi ho sakta hai, code khud dhoond leta hai. **Sirf naam exactly same hona chahiye.**

> Misal: Admin ki Customers tab ki image ka naam `admin-customers` hai.
> Aap file `admin-customers.jpg` bana kar `wwwroot/images/tabs/` me rakh dein. Ho gaya.

## 2) Code ke andar path kahan laga hai (sirf jankari ke liye)

| Code file | Kya karti hai |
|---|---|
| `NexusCommunication/Views/Shared/_TabBanner.cshtml` | Har tab ke upar wali wide image yahan se dikhti hai. Path banta hai: `/images/tabs/{role}-{tab}` |
| `NexusCommunication/Views/Admin/Index.cshtml` | Admin ki 11 tabs ke banner ka path (`_TabBanner` call) |
| `NexusCommunication/Views/AccountsDesk/Index.cshtml` | Accounts ki 4 tabs ke banner |
| `NexusCommunication/Views/Technical/Index.cshtml` | Technical ki 4 tabs ke banner |
| `NexusCommunication/Views/Retail/Index.cshtml` | Retail ki 4 tabs ke banner |
| `NexusCommunication/Views/Portal/Index.cshtml` | Customer portal ki 5 tabs ke banner |
| `NexusCommunication/Services/Ui.cs` | **18 plans ke image naam yahan likhe hain** (`PlanPics` list). Plan ka naam dekh kar sahi image chunta hai |
| `NexusCommunication/Views/Shared/_PlansBrowser.cshtml` | Plan cards yahan banate hain aur plan ki image yahan dikhti hai |
| `NexusCommunication/Views/Home/Index.cshtml` | Landing page — is me `_PlansBrowser` (Dial-Up / Broadband / Landline wale 3 sections) laga hai |
| `NexusCommunication/Views/Home/Plans.cshtml` | Plans page — is me bhi `_PlansBrowser` laga hai (yahan saare plans dikhte hain) |

## 3) Images ke size

| Folder | Size (width × height) | Ratio | Website me kaise dikhti hai | Subject kahan rakhna hai |
|---|---|---|---|---|
| `tabs/` (28) | **2560 × 1080 px** | 21:9 | Poori image dikhti hai (width ke hisaab se height khud barhti hai, desktop par taqreeban 1070 × 450 px) | Kahin bhi, ab kuch cut nahi hota |
| `plans/` (18) | **1920 × 1080 px** | 16:9 | Card ke upar poori 16:9 image (taqreeban 390 × 220 px) | Kahin bhi, ab kuch cut nahi hota |
| `hero/` (8) | Neeche section C me har image ke saamne | | | |
| `products/phone-cx2` | 800 × 800 px | 1:1 | Chhoti square thumbnail | Beech me |

**Baaqi zaroori baatein:**
- **Format:** JPG, quality taqreeban 85. File ka size 300 KB – 800 KB rakhein taake site tez chale (purani PNG 1.5–2.5 MB ki hain, bohot bhari hain).
- **Agar lmarena size/ratio ka option na de:** prompt me "21:9" / "16:9" likha hua hai, wo aksar kaam kar jata hai. Warna jo bhi wide image mile use rakh lein. Code image ko khud crop (`object-cover`) kar leta hai, kuch kharab nahi hota.
- Ab image ki height ke hisaab se box bana hai, is liye **upar neeche se kuch cut nahi hota** (21:9 image tabs me, 16:9 image plans me).

## 4) Theme (har image isi theme me banani hai)

Purani images jaisi hi theme hai, taake poora project ek jaisa lage. Ye sab har prompt ke andar already likhi hui hai.

| Cheez | Detail |
|---|---|
| Background | Deep dark navy `#070B16` |
| Main accent | Neon cyan + electric blue |
| Second accent | Halki violet glow |
| Look | Cinematic, premium, high contrast, ultra detailed, shallow depth of field |
| Type | Professional telecom / technology brand photography |
| Desk ka rang (sirf `tabs/`) | Admin = cyan/blue · Accounts = emerald-teal · Technical = amber · Retail = violet · Customer = sky-blue |
| Bilkul nahi | Text, letters, numbers, logo, watermark, readable screen |
| Shape / size | Upar section 3 me likha hai (tabs = 21:9, plans = 16:9) |

> lmarena purani prompt yaad nahi rakhta, is liye har prompt alag se poori likhi hai. Bas ek prompt copy karein aur paste karein.

---

## A) `tabs/` — Dashboard ki har tab ki image (28 images)

**Kaise dikhti hai:** tab kholne par uske sab se upar ek wide banner image aati hai.
Har panel ki **pehli tab** (Admin → Overview, Accounts → Overview, Technical → Feasibility Queue, Retail → Place Order, Customer → Overview) par purani 3-image strip hi rahegi, us ke liye nayi image nahi chahiye.


### Admin panel — 11 images

- **Page:** `/admin` (login ke baad)
- **Code file jisme path laga hai:** `NexusCommunication/Views/Admin/Index.cshtml`
- **Image folder:** `wwwroot/images/tabs/`

#### 1. `admin-customers`
- **Save as:** `NexusCommunication/wwwroot/images/tabs/admin-customers.jpg`
- **Size:** **2560 × 1080 px** (21:9 ultra-wide). Agar lmarena 21:9 na de to 1920 × 1080 (16:9) bhi chalega
- **Kahan dikhegi:** Admin panel → **Customers** tab ke sab se upar (`/admin#customers`)
- **Path is file me hai:** `NexusCommunication/Views/Admin/Index.cshtml` (aur `NexusCommunication/Views/Shared/_TabBanner.cshtml` isko dikhati hai)
- **Prompt:**
```
A wide row of friendly customer profile cards floating as glowing translucent glass panels above a dark reflective desk, each card showing only an abstract avatar silhouette and soft line icons, a subtle network of thin light lines connecting the cards, a blurred modern operations office behind. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, ultra-wide 21:9 horizontal composition (2560x1080) with the main subject confined to the middle horizontal third of the frame and generous empty dark space above and below it because the image will be cropped to a thin wide banner, left third slightly darker and less busy, no text, no letters, no numbers, no logos, no watermark, no readable screens, cyan and electric blue lighting.
```

#### 2. `admin-orders`
- **Save as:** `NexusCommunication/wwwroot/images/tabs/admin-orders.jpg`
- **Size:** **2560 × 1080 px** (21:9 ultra-wide). Agar lmarena 21:9 na de to 1920 × 1080 (16:9) bhi chalega
- **Kahan dikhegi:** Admin panel → **Orders** tab ke sab se upar (`/admin#orders`)
- **Path is file me hai:** `NexusCommunication/Views/Admin/Index.cshtml` (aur `NexusCommunication/Views/Shared/_TabBanner.cshtml` isko dikhati hai)
- **Prompt:**
```
A glowing horizontal pipeline of connected translucent glass stages floating over a dark desk, small parcels and application forms moving along it as soft light, a blurred control room in the background, sense of flow from left to right. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, ultra-wide 21:9 horizontal composition (2560x1080) with the main subject confined to the middle horizontal third of the frame and generous empty dark space above and below it because the image will be cropped to a thin wide banner, left third slightly darker and less busy, no text, no letters, no numbers, no logos, no watermark, no readable screens, cyan and electric blue lighting.
```

#### 3. `admin-connections`
- **Save as:** `NexusCommunication/wwwroot/images/tabs/admin-connections.jpg`
- **Size:** **2560 × 1080 px** (21:9 ultra-wide). Agar lmarena 21:9 na de to 1920 × 1080 (16:9) bhi chalega
- **Kahan dikhegi:** Admin panel → **Connections** tab ke sab se upar (`/admin#connections`)
- **Path is file me hai:** `NexusCommunication/Views/Admin/Index.cshtml` (aur `NexusCommunication/Views/Shared/_TabBanner.cshtml` isko dikhati hai)
- **Prompt:**
```
Close-up of a clean wall of fibre-optic patch panels with hundreds of tiny glowing LED indicators and neatly routed cables, cyan light trails running along the cables into the distance, very shallow depth of field. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, ultra-wide 21:9 horizontal composition (2560x1080) with the main subject confined to the middle horizontal third of the frame and generous empty dark space above and below it because the image will be cropped to a thin wide banner, left third slightly darker and less busy, no text, no letters, no numbers, no logos, no watermark, no readable screens, cyan and electric blue lighting.
```

#### 4. `admin-billing`
- **Save as:** `NexusCommunication/wwwroot/images/tabs/admin-billing.jpg`
- **Size:** **2560 × 1080 px** (21:9 ultra-wide). Agar lmarena 21:9 na de to 1920 × 1080 (16:9) bhi chalega
- **Kahan dikhegi:** Admin panel → **Billing & Payments** tab ke sab se upar (`/admin#billing`)
- **Path is file me hai:** `NexusCommunication/Views/Admin/Index.cshtml` (aur `NexusCommunication/Views/Shared/_TabBanner.cshtml` isko dikhati hai)
- **Prompt:**
```
A tidy executive desk with a stack of paper invoices, a card payment terminal and a calculator, abstract glowing coin and receipt icons floating above in light, blurred glass office behind, calm trustworthy mood. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, ultra-wide 21:9 horizontal composition (2560x1080) with the main subject confined to the middle horizontal third of the frame and generous empty dark space above and below it because the image will be cropped to a thin wide banner, left third slightly darker and less busy, no text, no letters, no numbers, no logos, no watermark, no readable screens, cyan and electric blue lighting.
```

#### 5. `admin-employees`
- **Save as:** `NexusCommunication/wwwroot/images/tabs/admin-employees.jpg`
- **Size:** **2560 × 1080 px** (21:9 ultra-wide). Agar lmarena 21:9 na de to 1920 × 1080 (16:9) bhi chalega
- **Kahan dikhegi:** Admin panel → **Employees** tab ke sab se upar (`/admin#employees`)
- **Path is file me hai:** `NexusCommunication/Views/Admin/Index.cshtml` (aur `NexusCommunication/Views/Shared/_TabBanner.cshtml` isko dikhati hai)
- **Prompt:**
```
A diverse team of five telecom staff in smart company uniforms standing in a confident row in a dark modern operations hall, seen from slightly below, soft rim light on their shoulders, faces natural and not too close, blurred screens glowing behind them. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, ultra-wide 21:9 horizontal composition (2560x1080) with the main subject confined to the middle horizontal third of the frame and generous empty dark space above and below it because the image will be cropped to a thin wide banner, left third slightly darker and less busy, no text, no letters, no numbers, no logos, no watermark, no readable screens, cyan and electric blue lighting.
```

#### 6. `admin-vendors`
- **Save as:** `NexusCommunication/wwwroot/images/tabs/admin-vendors.jpg`
- **Size:** **2560 × 1080 px** (21:9 ultra-wide). Agar lmarena 21:9 na de to 1920 × 1080 (16:9) bhi chalega
- **Kahan dikhegi:** Admin panel → **Vendors** tab ke sab se upar (`/admin#vendors`)
- **Path is file me hai:** `NexusCommunication/Views/Admin/Index.cshtml` (aur `NexusCommunication/Views/Shared/_TabBanner.cshtml` isko dikhati hai)
- **Prompt:**
```
Two business professionals shaking hands across a dark glass meeting table with a small model of a router and a fibre spool between them, city skyline at night through the window, handshake in sharp focus. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, ultra-wide 21:9 horizontal composition (2560x1080) with the main subject confined to the middle horizontal third of the frame and generous empty dark space above and below it because the image will be cropped to a thin wide banner, left third slightly darker and less busy, no text, no letters, no numbers, no logos, no watermark, no readable screens, cyan and electric blue lighting.
```

#### 7. `admin-equipment`
- **Save as:** `NexusCommunication/wwwroot/images/tabs/admin-equipment.jpg`
- **Size:** **2560 × 1080 px** (21:9 ultra-wide). Agar lmarena 21:9 na de to 1920 × 1080 (16:9) bhi chalega
- **Kahan dikhegi:** Admin panel → **Equipment & Stock** tab ke sab se upar (`/admin#equipment`)
- **Path is file me hai:** `NexusCommunication/Views/Admin/Index.cshtml` (aur `NexusCommunication/Views/Shared/_TabBanner.cshtml` isko dikhati hai)
- **Prompt:**
```
Long aisle of tall industrial warehouse shelving filled with boxed modems, routers and cable drums, a worker silhouette with a handheld scanner far down the aisle, strong perspective lines, cyan light along the shelves. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, ultra-wide 21:9 horizontal composition (2560x1080) with the main subject confined to the middle horizontal third of the frame and generous empty dark space above and below it because the image will be cropped to a thin wide banner, left third slightly darker and less busy, no text, no letters, no numbers, no logos, no watermark, no readable screens, cyan and electric blue lighting.
```

#### 8. `admin-procurement`
- **Save as:** `NexusCommunication/wwwroot/images/tabs/admin-procurement.jpg`
- **Size:** **2560 × 1080 px** (21:9 ultra-wide). Agar lmarena 21:9 na de to 1920 × 1080 (16:9) bhi chalega
- **Kahan dikhegi:** Admin panel → **Procurement** tab ke sab se upar (`/admin#procurement`)
- **Path is file me hai:** `NexusCommunication/Views/Admin/Index.cshtml` (aur `NexusCommunication/Views/Shared/_TabBanner.cshtml` isko dikhati hai)
- **Prompt:**
```
A delivery truck backing up to a loading dock at night with pallets of network equipment boxes being unloaded, a worker with a clipboard, glowing cyan dock lights and light trails, wide cinematic shot. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, ultra-wide 21:9 horizontal composition (2560x1080) with the main subject confined to the middle horizontal third of the frame and generous empty dark space above and below it because the image will be cropped to a thin wide banner, left third slightly darker and less busy, no text, no letters, no numbers, no logos, no watermark, no readable screens, cyan and electric blue lighting.
```

#### 9. `admin-shops`
- **Save as:** `NexusCommunication/wwwroot/images/tabs/admin-shops.jpg`
- **Size:** **2560 × 1080 px** (21:9 ultra-wide). Agar lmarena 21:9 na de to 1920 × 1080 (16:9) bhi chalega
- **Kahan dikhegi:** Admin panel → **Retail Shops** tab ke sab se upar (`/admin#shops`)
- **Path is file me hai:** `NexusCommunication/Views/Admin/Index.cshtml` (aur `NexusCommunication/Views/Shared/_TabBanner.cshtml` isko dikhati hai)
- **Prompt:**
```
Row of three modern glass-fronted telecom shops side by side on a city street at blue hour, soft glowing cyan strip lighting inside, a few blurred pedestrians, wet pavement reflections, no readable signage. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, ultra-wide 21:9 horizontal composition (2560x1080) with the main subject confined to the middle horizontal third of the frame and generous empty dark space above and below it because the image will be cropped to a thin wide banner, left third slightly darker and less busy, no text, no letters, no numbers, no logos, no watermark, no readable screens, cyan and electric blue lighting.
```

#### 10. `admin-plans`
- **Save as:** `NexusCommunication/wwwroot/images/tabs/admin-plans.jpg`
- **Size:** **2560 × 1080 px** (21:9 ultra-wide). Agar lmarena 21:9 na de to 1920 × 1080 (16:9) bhi chalega
- **Kahan dikhegi:** Admin panel → **Plans & Charges** tab ke sab se upar (`/admin#plans`)
- **Path is file me hai:** `NexusCommunication/Views/Admin/Index.cshtml` (aur `NexusCommunication/Views/Shared/_TabBanner.cshtml` isko dikhati hai)
- **Prompt:**
```
Three elegant glass price-card panels floating side by side in a dark space, each with a different glowing icon (an old modem, a Wi-Fi symbol, a desk phone) made of light, soft reflections on a dark glossy floor, no numbers or text on the cards. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, ultra-wide 21:9 horizontal composition (2560x1080) with the main subject confined to the middle horizontal third of the frame and generous empty dark space above and below it because the image will be cropped to a thin wide banner, left third slightly darker and less busy, no text, no letters, no numbers, no logos, no watermark, no readable screens, cyan and electric blue lighting.
```

#### 11. `admin-feedback`
- **Save as:** `NexusCommunication/wwwroot/images/tabs/admin-feedback.jpg`
- **Size:** **2560 × 1080 px** (21:9 ultra-wide). Agar lmarena 21:9 na de to 1920 × 1080 (16:9) bhi chalega
- **Kahan dikhegi:** Admin panel → **Feedback** tab ke sab se upar (`/admin#feedback`)
- **Path is file me hai:** `NexusCommunication/Views/Admin/Index.cshtml` (aur `NexusCommunication/Views/Shared/_TabBanner.cshtml` isko dikhati hai)
- **Prompt:**
```
A happy customer giving a thumbs-up to a support agent across a bright reception counter, floating glowing five-star shapes made of light above them, shallow depth of field, warm welcoming mood against the dark navy surroundings. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, ultra-wide 21:9 horizontal composition (2560x1080) with the main subject confined to the middle horizontal third of the frame and generous empty dark space above and below it because the image will be cropped to a thin wide banner, left third slightly darker and less busy, no text, no letters, no numbers, no logos, no watermark, no readable screens, cyan and electric blue lighting.
```


### Accounts panel — 4 images

- **Page:** `/accounts` (login ke baad)
- **Code file jisme path laga hai:** `NexusCommunication/Views/AccountsDesk/Index.cshtml`
- **Image folder:** `wwwroot/images/tabs/`

#### 12. `accounts-generate`
- **Save as:** `NexusCommunication/wwwroot/images/tabs/accounts-generate.jpg`
- **Size:** **2560 × 1080 px** (21:9 ultra-wide). Agar lmarena 21:9 na de to 1920 × 1080 (16:9) bhi chalega
- **Kahan dikhegi:** Accounts panel → **Generate Invoice** tab ke sab se upar (`/accounts#generate`)
- **Path is file me hai:** `NexusCommunication/Views/AccountsDesk/Index.cshtml` (aur `NexusCommunication/Views/Shared/_TabBanner.cshtml` isko dikhati hai)
- **Prompt:**
```
Hands typing on a sleek laptop beside a printed invoice just coming out of a compact printer and a desktop calculator on a dark desk, abstract glowing document and tick icons floating in the air, close crop on hands and paper. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, ultra-wide 21:9 horizontal composition (2560x1080) with the main subject confined to the middle horizontal third of the frame and generous empty dark space above and below it because the image will be cropped to a thin wide banner, left third slightly darker and less busy, no text, no letters, no numbers, no logos, no watermark, no readable screens, emerald-teal and cyan lighting.
```

#### 13. `accounts-ledger`
- **Save as:** `NexusCommunication/wwwroot/images/tabs/accounts-ledger.jpg`
- **Size:** **2560 × 1080 px** (21:9 ultra-wide). Agar lmarena 21:9 na de to 1920 × 1080 (16:9) bhi chalega
- **Kahan dikhegi:** Accounts panel → **Invoice Ledger** tab ke sab se upar (`/accounts#ledger`)
- **Path is file me hai:** `NexusCommunication/Views/AccountsDesk/Index.cshtml` (aur `NexusCommunication/Views/Shared/_TabBanner.cshtml` isko dikhati hai)
- **Prompt:**
```
A tall stack of neatly filed invoice folders and an open accounting ledger book on a dark wooden desk, a desk lamp casting a pool of teal light, rows of filing cabinets softly blurred behind, quiet organised mood. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, ultra-wide 21:9 horizontal composition (2560x1080) with the main subject confined to the middle horizontal third of the frame and generous empty dark space above and below it because the image will be cropped to a thin wide banner, left third slightly darker and less busy, no text, no letters, no numbers, no logos, no watermark, no readable screens, emerald-teal and cyan lighting.
```

#### 14. `accounts-dues`
- **Save as:** `NexusCommunication/wwwroot/images/tabs/accounts-dues.jpg`
- **Size:** **2560 × 1080 px** (21:9 ultra-wide). Agar lmarena 21:9 na de to 1920 × 1080 (16:9) bhi chalega
- **Kahan dikhegi:** Accounts panel → **Customer Dues** tab ke sab se upar (`/accounts#dues`)
- **Path is file me hai:** `NexusCommunication/Views/AccountsDesk/Index.cshtml` (aur `NexusCommunication/Views/Shared/_TabBanner.cshtml` isko dikhati hai)
- **Prompt:**
```
A pocket watch and a small stack of coins beside an unpaid invoice with a red-orange paperclip on a dark desk, soft emerald glow, hourglass sand running in the blurred background, sense of time and pending payment. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, ultra-wide 21:9 horizontal composition (2560x1080) with the main subject confined to the middle horizontal third of the frame and generous empty dark space above and below it because the image will be cropped to a thin wide banner, left third slightly darker and less busy, no text, no letters, no numbers, no logos, no watermark, no readable screens, emerald-teal and cyan lighting.
```

#### 15. `accounts-receipts`
- **Save as:** `NexusCommunication/wwwroot/images/tabs/accounts-receipts.jpg`
- **Size:** **2560 × 1080 px** (21:9 ultra-wide). Agar lmarena 21:9 na de to 1920 × 1080 (16:9) bhi chalega
- **Kahan dikhegi:** Accounts panel → **Receipts** tab ke sab se upar (`/accounts#receipts`)
- **Path is file me hai:** `NexusCommunication/Views/AccountsDesk/Index.cshtml` (aur `NexusCommunication/Views/Shared/_TabBanner.cshtml` isko dikhati hai)
- **Prompt:**
```
A long paper receipt curling out of a point-of-sale printer onto a dark counter, a hand holding a payment card near the terminal, tiny glowing check-mark icons floating above, teal and cyan light, macro-style close-up. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, ultra-wide 21:9 horizontal composition (2560x1080) with the main subject confined to the middle horizontal third of the frame and generous empty dark space above and below it because the image will be cropped to a thin wide banner, left third slightly darker and less busy, no text, no letters, no numbers, no logos, no watermark, no readable screens, emerald-teal and cyan lighting.
```


### Technical panel — 4 images

- **Page:** `/technical` (login ke baad)
- **Code file jisme path laga hai:** `NexusCommunication/Views/Technical/Index.cshtml`
- **Image folder:** `wwwroot/images/tabs/`

#### 16. `technical-provision`
- **Save as:** `NexusCommunication/wwwroot/images/tabs/technical-provision.jpg`
- **Size:** **2560 × 1080 px** (21:9 ultra-wide). Agar lmarena 21:9 na de to 1920 × 1080 (16:9) bhi chalega
- **Kahan dikhegi:** Technical panel → **Provision Deck** tab ke sab se upar (`/technical#provision`)
- **Path is file me hai:** `NexusCommunication/Views/Technical/Index.cshtml` (aur `NexusCommunication/Views/Shared/_TabBanner.cshtml` isko dikhati hai)
- **Prompt:**
```
Technician hands plugging a glowing fibre-optic connector into a router port in a dark equipment room, green and cyan LEDs lighting the scene, a laptop with abstract waveform lines blurred in the background, macro close-up. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, ultra-wide 21:9 horizontal composition (2560x1080) with the main subject confined to the middle horizontal third of the frame and generous empty dark space above and below it because the image will be cropped to a thin wide banner, left third slightly darker and less busy, no text, no letters, no numbers, no logos, no watermark, no readable screens, warm amber and cyan lighting.
```

#### 17. `technical-orders`
- **Save as:** `NexusCommunication/wwwroot/images/tabs/technical-orders.jpg`
- **Size:** **2560 × 1080 px** (21:9 ultra-wide). Agar lmarena 21:9 na de to 1920 × 1080 (16:9) bhi chalega
- **Kahan dikhegi:** Technical panel → **Order Tracker** tab ke sab se upar (`/technical#orders`)
- **Path is file me hai:** `NexusCommunication/Views/Technical/Index.cshtml` (aur `NexusCommunication/Views/Shared/_TabBanner.cshtml` isko dikhati hai)
- **Prompt:**
```
A large wall-mounted tablet-style board made of floating glass cards showing abstract order progress as glowing dots on a timeline, a technician silhouette in a helmet watching it, dark control room, amber highlights. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, ultra-wide 21:9 horizontal composition (2560x1080) with the main subject confined to the middle horizontal third of the frame and generous empty dark space above and below it because the image will be cropped to a thin wide banner, left third slightly darker and less busy, no text, no letters, no numbers, no logos, no watermark, no readable screens, warm amber and cyan lighting.
```

#### 18. `technical-grid`
- **Save as:** `NexusCommunication/wwwroot/images/tabs/technical-grid.jpg`
- **Size:** **2560 × 1080 px** (21:9 ultra-wide). Agar lmarena 21:9 na de to 1920 × 1080 (16:9) bhi chalega
- **Kahan dikhegi:** Technical panel → **Connection Grid** tab ke sab se upar (`/technical#grid`)
- **Path is file me hai:** `NexusCommunication/Views/Technical/Index.cshtml` (aur `NexusCommunication/Views/Shared/_TabBanner.cshtml` isko dikhati hai)
- **Prompt:**
```
Aerial night view of a city grid with glowing amber and cyan network lines connecting hundreds of buildings, bright junction nodes at intersections, subtle fog over the streets, strong sense of a live connected network. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, ultra-wide 21:9 horizontal composition (2560x1080) with the main subject confined to the middle horizontal third of the frame and generous empty dark space above and below it because the image will be cropped to a thin wide banner, left third slightly darker and less busy, no text, no letters, no numbers, no logos, no watermark, no readable screens, warm amber and cyan lighting.
```

#### 19. `technical-equipment`
- **Save as:** `NexusCommunication/wwwroot/images/tabs/technical-equipment.jpg`
- **Size:** **2560 × 1080 px** (21:9 ultra-wide). Agar lmarena 21:9 na de to 1920 × 1080 (16:9) bhi chalega
- **Kahan dikhegi:** Technical panel → **Equipment Bay** tab ke sab se upar (`/technical#equipment`)
- **Path is file me hai:** `NexusCommunication/Views/Technical/Index.cshtml` (aur `NexusCommunication/Views/Shared/_TabBanner.cshtml` isko dikhati hai)
- **Prompt:**
```
A technician's workbench with neatly arranged tools: crimping tool, cable tester, spools of fibre and Cat6 cable, a toolbox and a modem, dark background, warm amber task lamp mixed with cyan accent, top three-quarter view. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, ultra-wide 21:9 horizontal composition (2560x1080) with the main subject confined to the middle horizontal third of the frame and generous empty dark space above and below it because the image will be cropped to a thin wide banner, left third slightly darker and less busy, no text, no letters, no numbers, no logos, no watermark, no readable screens, warm amber and cyan lighting.
```


### Retail panel — 4 images

- **Page:** `/retail` (login ke baad)
- **Code file jisme path laga hai:** `NexusCommunication/Views/Retail/Index.cshtml`
- **Image folder:** `wwwroot/images/tabs/`

#### 20. `retail-book`
- **Save as:** `NexusCommunication/wwwroot/images/tabs/retail-book.jpg`
- **Size:** **2560 × 1080 px** (21:9 ultra-wide). Agar lmarena 21:9 na de to 1920 × 1080 (16:9) bhi chalega
- **Kahan dikhegi:** Retail panel → **Order Book** tab ke sab se upar (`/retail#book`)
- **Path is file me hai:** `NexusCommunication/Views/Retail/Index.cshtml` (aur `NexusCommunication/Views/Shared/_TabBanner.cshtml` isko dikhati hai)
- **Prompt:**
```
A thick leather-bound order book open on a retail counter with a pen resting across it, a barcode scanner and a router box beside it, soft violet and cyan lights from the shop interior blurred behind, shallow depth of field. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, ultra-wide 21:9 horizontal composition (2560x1080) with the main subject confined to the middle horizontal third of the frame and generous empty dark space above and below it because the image will be cropped to a thin wide banner, left third slightly darker and less busy, no text, no letters, no numbers, no logos, no watermark, no readable screens, violet and cyan lighting.
```

#### 21. `retail-customers`
- **Save as:** `NexusCommunication/wwwroot/images/tabs/retail-customers.jpg`
- **Size:** **2560 × 1080 px** (21:9 ultra-wide). Agar lmarena 21:9 na de to 1920 × 1080 (16:9) bhi chalega
- **Kahan dikhegi:** Retail panel → **Customers** tab ke sab se upar (`/retail#customers`)
- **Path is file me hai:** `NexusCommunication/Views/Retail/Index.cshtml` (aur `NexusCommunication/Views/Shared/_TabBanner.cshtml` isko dikhati hai)
- **Prompt:**
```
A smiling retail executive handing a new router box to a customer across a modern counter, both seen from the side at mid distance, violet ambient light, shelves of devices blurred behind, friendly natural body language. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, ultra-wide 21:9 horizontal composition (2560x1080) with the main subject confined to the middle horizontal third of the frame and generous empty dark space above and below it because the image will be cropped to a thin wide banner, left third slightly darker and less busy, no text, no letters, no numbers, no logos, no watermark, no readable screens, violet and cyan lighting.
```

#### 22. `retail-connections`
- **Save as:** `NexusCommunication/wwwroot/images/tabs/retail-connections.jpg`
- **Size:** **2560 × 1080 px** (21:9 ultra-wide). Agar lmarena 21:9 na de to 1920 × 1080 (16:9) bhi chalega
- **Kahan dikhegi:** Retail panel → **Connections** tab ke sab se upar (`/retail#connections`)
- **Path is file me hai:** `NexusCommunication/Views/Retail/Index.cshtml` (aur `NexusCommunication/Views/Shared/_TabBanner.cshtml` isko dikhati hai)
- **Prompt:**
```
A tidy retail shop wall display of routers, modems and phones with glowing status LEDs arranged in clean rows, violet and cyan backlight, a customer silhouette browsing at the far end, symmetrical perspective. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, ultra-wide 21:9 horizontal composition (2560x1080) with the main subject confined to the middle horizontal third of the frame and generous empty dark space above and below it because the image will be cropped to a thin wide banner, left third slightly darker and less busy, no text, no letters, no numbers, no logos, no watermark, no readable screens, violet and cyan lighting.
```

#### 23. `retail-collections`
- **Save as:** `NexusCommunication/wwwroot/images/tabs/retail-collections.jpg`
- **Size:** **2560 × 1080 px** (21:9 ultra-wide). Agar lmarena 21:9 na de to 1920 × 1080 (16:9) bhi chalega
- **Kahan dikhegi:** Retail panel → **Collections** tab ke sab se upar (`/retail#collections`)
- **Path is file me hai:** `NexusCommunication/Views/Retail/Index.cshtml` (aur `NexusCommunication/Views/Shared/_TabBanner.cshtml` isko dikhati hai)
- **Prompt:**
```
Close-up of a cashier's hands counting banknotes next to a card terminal and a small cash drawer at a retail counter, violet and cyan glow, tiny glowing coin icons floating above, shallow depth of field. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, ultra-wide 21:9 horizontal composition (2560x1080) with the main subject confined to the middle horizontal third of the frame and generous empty dark space above and below it because the image will be cropped to a thin wide banner, left third slightly darker and less busy, no text, no letters, no numbers, no logos, no watermark, no readable screens, violet and cyan lighting.
```


### Customer portal — 5 images

- **Page:** `/portal` (login ke baad)
- **Code file jisme path laga hai:** `NexusCommunication/Views/Portal/Index.cshtml`
- **Image folder:** `wwwroot/images/tabs/`

#### 24. `customer-orders`
- **Save as:** `NexusCommunication/wwwroot/images/tabs/customer-orders.jpg`
- **Size:** **2560 × 1080 px** (21:9 ultra-wide). Agar lmarena 21:9 na de to 1920 × 1080 (16:9) bhi chalega
- **Kahan dikhegi:** Customer portal → **My Orders** tab ke sab se upar (`/portal#orders`)
- **Path is file me hai:** `NexusCommunication/Views/Portal/Index.cshtml` (aur `NexusCommunication/Views/Shared/_TabBanner.cshtml` isko dikhati hai)
- **Prompt:**
```
A courier handing a sealed router box to a smiling person at their front door in the evening, warm hallway light behind them, a soft sky-blue glow on the box, shallow depth of field, calm everyday mood. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, ultra-wide 21:9 horizontal composition (2560x1080) with the main subject confined to the middle horizontal third of the frame and generous empty dark space above and below it because the image will be cropped to a thin wide banner, left third slightly darker and less busy, no text, no letters, no numbers, no logos, no watermark, no readable screens, soft sky-blue and cyan lighting.
```

#### 25. `customer-connections`
- **Save as:** `NexusCommunication/wwwroot/images/tabs/customer-connections.jpg`
- **Size:** **2560 × 1080 px** (21:9 ultra-wide). Agar lmarena 21:9 na de to 1920 × 1080 (16:9) bhi chalega
- **Kahan dikhegi:** Customer portal → **My Connections** tab ke sab se upar (`/portal#connections`)
- **Path is file me hai:** `NexusCommunication/Views/Portal/Index.cshtml` (aur `NexusCommunication/Views/Shared/_TabBanner.cshtml` isko dikhati hai)
- **Prompt:**
```
A cosy modern living room corner with a white Wi-Fi router on a shelf, soft glowing sky-blue Wi-Fi signal rings spreading through the room around a sofa and a laptop, evening ambience, nobody's face visible. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, ultra-wide 21:9 horizontal composition (2560x1080) with the main subject confined to the middle horizontal third of the frame and generous empty dark space above and below it because the image will be cropped to a thin wide banner, left third slightly darker and less busy, no text, no letters, no numbers, no logos, no watermark, no readable screens, soft sky-blue and cyan lighting.
```

#### 26. `customer-bills`
- **Save as:** `NexusCommunication/wwwroot/images/tabs/customer-bills.jpg`
- **Size:** **2560 × 1080 px** (21:9 ultra-wide). Agar lmarena 21:9 na de to 1920 × 1080 (16:9) bhi chalega
- **Kahan dikhegi:** Customer portal → **My Bills** tab ke sab se upar (`/portal#bills`)
- **Path is file me hai:** `NexusCommunication/Views/Portal/Index.cshtml` (aur `NexusCommunication/Views/Shared/_TabBanner.cshtml` isko dikhati hai)
- **Prompt:**
```
A person's hands holding a smartphone beside a coffee mug and a printed bill on a kitchen table, abstract glowing receipt and tick icons floating above the phone, soft sky-blue light, screen content not readable. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, ultra-wide 21:9 horizontal composition (2560x1080) with the main subject confined to the middle horizontal third of the frame and generous empty dark space above and below it because the image will be cropped to a thin wide banner, left third slightly darker and less busy, no text, no letters, no numbers, no logos, no watermark, no readable screens, soft sky-blue and cyan lighting.
```

#### 27. `customer-feedback`
- **Save as:** `NexusCommunication/wwwroot/images/tabs/customer-feedback.jpg`
- **Size:** **2560 × 1080 px** (21:9 ultra-wide). Agar lmarena 21:9 na de to 1920 × 1080 (16:9) bhi chalega
- **Kahan dikhegi:** Customer portal → **Feedback** tab ke sab se upar (`/portal#feedback`)
- **Path is file me hai:** `NexusCommunication/Views/Portal/Index.cshtml` (aur `NexusCommunication/Views/Shared/_TabBanner.cshtml` isko dikhati hai)
- **Prompt:**
```
A friendly customer-support agent with a headset smiling warmly in a bright modern contact centre, abstract floating glowing speech-bubble and heart shapes made of light beside them, sky-blue accent, blurred colleagues behind. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, ultra-wide 21:9 horizontal composition (2560x1080) with the main subject confined to the middle horizontal third of the frame and generous empty dark space above and below it because the image will be cropped to a thin wide banner, left third slightly darker and less busy, no text, no letters, no numbers, no logos, no watermark, no readable screens, soft sky-blue and cyan lighting.
```

#### 28. `customer-profile`
- **Save as:** `NexusCommunication/wwwroot/images/tabs/customer-profile.jpg`
- **Size:** **2560 × 1080 px** (21:9 ultra-wide). Agar lmarena 21:9 na de to 1920 × 1080 (16:9) bhi chalega
- **Kahan dikhegi:** Customer portal → **Profile** tab ke sab se upar (`/portal#profile`)
- **Path is file me hai:** `NexusCommunication/Views/Portal/Index.cshtml` (aur `NexusCommunication/Views/Shared/_TabBanner.cshtml` isko dikhati hai)
- **Prompt:**
```
A person seen from behind sitting at a desk at night setting up an online account on a laptop, a glowing abstract user-avatar outline made of light floating above the keyboard, soft sky-blue and cyan light, cosy room blurred behind. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, ultra-wide 21:9 horizontal composition (2560x1080) with the main subject confined to the middle horizontal third of the frame and generous empty dark space above and below it because the image will be cropped to a thin wide banner, left third slightly darker and less busy, no text, no letters, no numbers, no logos, no watermark, no readable screens, soft sky-blue and cyan lighting.
```


---

## B) `plans/` — Har plan card ki image (18 images)

**Kaise dikhti hai:** har plan card (price wale box) ke sab se upar ek patli image.
Pehle ek type ke saare plans par same image thi, ab har plan ki apni image hai.

**Ye image 3 jagah dikhti hai:**
1. **Landing page** (`/`) → "The tariff sheet" section → Dial-Up / Broadband / Landline tabs. Code: `NexusCommunication/Views/Home/Index.cshtml` (andar `_PlansBrowser`)
2. **Plans page** (`/plans`) → saare plans. Code: `NexusCommunication/Views/Home/Plans.cshtml`
3. **Admin panel** → Plans & Charges tab ki table me chhoti thumbnail. Code: `NexusCommunication/Views/Admin/Index.cshtml`

Teeno jagah plan card ka design `NexusCommunication/Views/Shared/_PlansBrowser.cshtml` me hai, aur **plan → image naam ka jor `NexusCommunication/Services/Ui.cs` (`PlanPics`)** me likha hai.

> Dhyan dein: landing page par har type ke sirf pehle 2 category wale plans dikhte hain, baaqi sirf Plans page par.
> Is liye jin plans ke saamne "sirf Plans page" likha hai wo landing par nahi dikhenge.


### Dial-Up — 7 images

#### 29. `dialup-hourly-10h`
- **Save as:** `NexusCommunication/wwwroot/images/plans/dialup-hourly-10h.jpg`
- **Size:** **1920 × 1080 px** (16:9)
- **Plan:** Dial-Up · Hourly Basis · 10 Hours Pack
- **Kahan dikhegi:** Landing page + Plans page + Admin → Plans & Charges
- **Path is file me hai:** `NexusCommunication/Services/Ui.cs` (PlanPics list) · card: `NexusCommunication/Views/Shared/_PlansBrowser.cshtml`
- **Prompt:**
```
An old beige external dial-up modem with a few blinking amber LEDs and a short telephone cable, a small analog kitchen timer beside it showing a short time, dark desk, warm amber light with cyan rim light, macro product shot. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, wide 16:9 horizontal composition (1920x1080) with the main subject centered in the middle half of the frame and empty dark space above and below it because the image will be cropped to a wide strip, no text, no letters, no numbers, no logos, no watermark, no readable screens.
```

#### 30. `dialup-hourly-30h`
- **Save as:** `NexusCommunication/wwwroot/images/plans/dialup-hourly-30h.jpg`
- **Size:** **1920 × 1080 px** (16:9)
- **Plan:** Dial-Up · Hourly Basis · 30 Hours Pack
- **Kahan dikhegi:** Landing page + Plans page + Admin → Plans & Charges
- **Path is file me hai:** `NexusCommunication/Services/Ui.cs` (PlanPics list) · card: `NexusCommunication/Views/Shared/_PlansBrowser.cshtml`
- **Prompt:**
```
A classic dial-up modem next to a medium-sized hourglass and an old corded phone, amber LEDs glowing, dark wooden desk, warm amber light with a cyan rim light, nostalgic but clean. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, wide 16:9 horizontal composition (1920x1080) with the main subject centered in the middle half of the frame and empty dark space above and below it because the image will be cropped to a wide strip, no text, no letters, no numbers, no logos, no watermark, no readable screens.
```

#### 31. `dialup-hourly-60h`
- **Save as:** `NexusCommunication/wwwroot/images/plans/dialup-hourly-60h.jpg`
- **Size:** **1920 × 1080 px** (16:9)
- **Plan:** Dial-Up · Hourly Basis · 60 Hours Pack
- **Kahan dikhegi:** Landing page + Plans page + Admin → Plans & Charges
- **Path is file me hai:** `NexusCommunication/Services/Ui.cs` (PlanPics list) · card: `NexusCommunication/Views/Shared/_PlansBrowser.cshtml`
- **Prompt:**
```
A dial-up modem with a large stack of old floppy disks and a large hourglass beside it on a dark desk, a vintage CRT monitor softly blurred behind, amber glow with cyan rim light. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, wide 16:9 horizontal composition (1920x1080) with the main subject centered in the middle half of the frame and empty dark space above and below it because the image will be cropped to a wide strip, no text, no letters, no numbers, no logos, no watermark, no readable screens.
```

#### 32. `dialup-28k-monthly`
- **Save as:** `NexusCommunication/wwwroot/images/plans/dialup-28k-monthly.jpg`
- **Size:** **1920 × 1080 px** (16:9)
- **Plan:** Dial-Up · Unlimited 28Kbps · Monthly
- **Kahan dikhegi:** Landing page + Plans page + Admin → Plans & Charges
- **Path is file me hai:** `NexusCommunication/Services/Ui.cs` (PlanPics list) · card: `NexusCommunication/Views/Shared/_PlansBrowser.cshtml`
- **Prompt:**
```
A dial-up modem with a slow single thin glowing amber line of data leaving it, a small desk calendar with one page showing, an old beige keyboard in the foreground, dark background, amber and cyan light. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, wide 16:9 horizontal composition (1920x1080) with the main subject centered in the middle half of the frame and empty dark space above and below it because the image will be cropped to a wide strip, no text, no letters, no numbers, no logos, no watermark, no readable screens.
```

#### 33. `dialup-28k-quarterly`
- **Save as:** `NexusCommunication/wwwroot/images/plans/dialup-28k-quarterly.jpg`
- **Size:** **1920 × 1080 px** (16:9)
- **Plan:** Dial-Up · Unlimited 28Kbps · Quarterly
- **Kahan dikhegi:** Landing page + Plans page + Admin → Plans & Charges
- **Path is file me hai:** `NexusCommunication/Services/Ui.cs` (PlanPics list) · card: `NexusCommunication/Views/Shared/_PlansBrowser.cshtml`
- **Prompt:**
```
A dial-up modem and an old corded telephone with three small desk calendars standing in a row behind them to suggest three months, amber glowing infinity-loop shape made of light above the modem, dark background. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, wide 16:9 horizontal composition (1920x1080) with the main subject centered in the middle half of the frame and empty dark space above and below it because the image will be cropped to a wide strip, no text, no letters, no numbers, no logos, no watermark, no readable screens.
```

#### 34. `dialup-56k-monthly`
- **Save as:** `NexusCommunication/wwwroot/images/plans/dialup-56k-monthly.jpg`
- **Size:** **1920 × 1080 px** (16:9)
- **Plan:** Dial-Up · Unlimited 56Kbps · Monthly
- **Kahan dikhegi:** **sirf** Plans page (`/plans`) + Admin → Plans & Charges
- **Path is file me hai:** `NexusCommunication/Services/Ui.cs` (PlanPics list) · card: `NexusCommunication/Views/Shared/_PlansBrowser.cshtml`
- **Prompt:**
```
A sleek retro-modern 56K dial-up modem with bright amber LEDs, a thicker brighter glowing amber data stream leaving it, a single desk calendar page, an old CRT monitor edge glowing, dark background, cyan rim light. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, wide 16:9 horizontal composition (1920x1080) with the main subject centered in the middle half of the frame and empty dark space above and below it because the image will be cropped to a wide strip, no text, no letters, no numbers, no logos, no watermark, no readable screens.
```

#### 35. `dialup-56k-quarterly`
- **Save as:** `NexusCommunication/wwwroot/images/plans/dialup-56k-quarterly.jpg`
- **Size:** **1920 × 1080 px** (16:9)
- **Plan:** Dial-Up · Unlimited 56Kbps · Quarterly (most popular)
- **Kahan dikhegi:** **sirf** Plans page (`/plans`) + Admin → Plans & Charges
- **Path is file me hai:** `NexusCommunication/Services/Ui.cs` (PlanPics list) · card: `NexusCommunication/Views/Shared/_PlansBrowser.cshtml`
- **Prompt:**
```
Hero shot of a premium retro dial-up modem on a dark glossy surface, brilliant amber LEDs, a glowing amber infinity loop and three small floating calendar tiles above it, golden rim light plus cyan accents, feels like the best dial-up pack. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, wide 16:9 horizontal composition (1920x1080) with the main subject centered in the middle half of the frame and empty dark space above and below it because the image will be cropped to a wide strip, no text, no letters, no numbers, no logos, no watermark, no readable screens.
```


### Broadband — 6 images

#### 36. `broadband-hourly-30h`
- **Save as:** `NexusCommunication/wwwroot/images/plans/broadband-hourly-30h.jpg`
- **Size:** **1920 × 1080 px** (16:9)
- **Plan:** Broadband · Hourly Basis · 30 Hours Pack
- **Kahan dikhegi:** Landing page + Plans page + Admin → Plans & Charges
- **Path is file me hai:** `NexusCommunication/Services/Ui.cs` (PlanPics list) · card: `NexusCommunication/Views/Shared/_PlansBrowser.cshtml`
- **Prompt:**
```
A compact white Wi-Fi router with a small glowing digital-style stopwatch ring of cyan light around it, a few thin speed lines, dark background, light home-user mood, product photography. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, wide 16:9 horizontal composition (1920x1080) with the main subject centered in the middle half of the frame and empty dark space above and below it because the image will be cropped to a wide strip, no text, no letters, no numbers, no logos, no watermark, no readable screens.
```

#### 37. `broadband-hourly-60h`
- **Save as:** `NexusCommunication/wwwroot/images/plans/broadband-hourly-60h.jpg`
- **Size:** **1920 × 1080 px** (16:9)
- **Plan:** Broadband · Hourly Basis · 60 Hours Pack
- **Kahan dikhegi:** Landing page + Plans page + Admin → Plans & Charges
- **Path is file me hai:** `NexusCommunication/Services/Ui.cs` (PlanPics list) · card: `NexusCommunication/Views/Shared/_PlansBrowser.cshtml`
- **Prompt:**
```
A white Wi-Fi router in front of a large glowing cyan stopwatch ring and a half-filled glowing hourglass, thin blue speed lines trailing across the dark background. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, wide 16:9 horizontal composition (1920x1080) with the main subject centered in the middle half of the frame and empty dark space above and below it because the image will be cropped to a wide strip, no text, no letters, no numbers, no logos, no watermark, no readable screens.
```

#### 38. `broadband-64k-monthly`
- **Save as:** `NexusCommunication/wwwroot/images/plans/broadband-64k-monthly.jpg`
- **Size:** **1920 × 1080 px** (16:9)
- **Plan:** Broadband · Unlimited 64Kbps · Monthly
- **Kahan dikhegi:** Landing page + Plans page + Admin → Plans & Charges
- **Path is file me hai:** `NexusCommunication/Services/Ui.cs` (PlanPics list) · card: `NexusCommunication/Views/Shared/_PlansBrowser.cshtml`
- **Prompt:**
```
A white home router on a dark shelf with a single steady cyan Wi-Fi arc glowing above it, a small glass calendar tile with one page floating beside it, a laptop softly blurred behind, calm everyday streaming mood. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, wide 16:9 horizontal composition (1920x1080) with the main subject centered in the middle half of the frame and empty dark space above and below it because the image will be cropped to a wide strip, no text, no letters, no numbers, no logos, no watermark, no readable screens.
```

#### 39. `broadband-64k-quarterly`
- **Save as:** `NexusCommunication/wwwroot/images/plans/broadband-64k-quarterly.jpg`
- **Size:** **1920 × 1080 px** (16:9)
- **Plan:** Broadband · Unlimited 64Kbps · Quarterly
- **Kahan dikhegi:** Landing page + Plans page + Admin → Plans & Charges
- **Path is file me hai:** `NexusCommunication/Services/Ui.cs` (PlanPics list) · card: `NexusCommunication/Views/Shared/_PlansBrowser.cshtml`
- **Prompt:**
```
A white router with steady glowing Wi-Fi arcs and three glass calendar tiles floating in a row behind it, a tablet and a laptop softly blurred, cyan and blue light, infinity-loop shape of light above the router. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, wide 16:9 horizontal composition (1920x1080) with the main subject centered in the middle half of the frame and empty dark space above and below it because the image will be cropped to a wide strip, no text, no letters, no numbers, no logos, no watermark, no readable screens.
```

#### 40. `broadband-128k-monthly`
- **Save as:** `NexusCommunication/wwwroot/images/plans/broadband-128k-monthly.jpg`
- **Size:** **1920 × 1080 px** (16:9)
- **Plan:** Broadband · Unlimited 128Kbps · Monthly (flagship)
- **Kahan dikhegi:** **sirf** Plans page (`/plans`) + Admin → Plans & Charges
- **Path is file me hai:** `NexusCommunication/Services/Ui.cs` (PlanPics list) · card: `NexusCommunication/Views/Shared/_PlansBrowser.cshtml`
- **Prompt:**
```
Powerful modern Wi-Fi router with four antennas and bright cyan LEDs, dramatic streams of cyan and electric-blue light streaming past it toward the camera, strong sense of speed, a single calendar tile glowing, dark background. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, wide 16:9 horizontal composition (1920x1080) with the main subject centered in the middle half of the frame and empty dark space above and below it because the image will be cropped to a wide strip, no text, no letters, no numbers, no logos, no watermark, no readable screens.
```

#### 41. `broadband-128k-quarterly`
- **Save as:** `NexusCommunication/wwwroot/images/plans/broadband-128k-quarterly.jpg`
- **Size:** **1920 × 1080 px** (16:9)
- **Plan:** Broadband · Unlimited 128Kbps · Quarterly (best value)
- **Kahan dikhegi:** **sirf** Plans page (`/plans`) + Admin → Plans & Charges
- **Path is file me hai:** `NexusCommunication/Services/Ui.cs` (PlanPics list) · card: `NexusCommunication/Views/Shared/_PlansBrowser.cshtml`
- **Prompt:**
```
Hero shot of a premium Wi-Fi 6 router in the foreground with dazzling cyan, blue and violet light trails radiating outward like a tunnel of speed, three glowing calendar tiles and an infinity loop of light above, most premium broadband look. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, wide 16:9 horizontal composition (1920x1080) with the main subject centered in the middle half of the frame and empty dark space above and below it because the image will be cropped to a wide strip, no text, no letters, no numbers, no logos, no watermark, no readable screens.
```


### Landline — 5 images

#### 42. `landline-local-annual`
- **Save as:** `NexusCommunication/wwwroot/images/plans/landline-local-annual.jpg`
- **Size:** **1920 × 1080 px** (16:9)
- **Plan:** Landline · Local Plan · Unlimited Annual
- **Kahan dikhegi:** Landing page + Plans page + Admin → Plans & Charges
- **Path is file me hai:** `NexusCommunication/Services/Ui.cs` (PlanPics list) · card: `NexusCommunication/Views/Shared/_PlansBrowser.cshtml`
- **Prompt:**
```
An elegant black desk telephone with a coiled cord on a dark desk, a twelve-page desk calendar standing beside it, soft violet and cyan glow, a notepad and pen, minimal and calm. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, wide 16:9 horizontal composition (1920x1080) with the main subject centered in the middle half of the frame and empty dark space above and below it because the image will be cropped to a wide strip, no text, no letters, no numbers, no logos, no watermark, no readable screens.
```

#### 43. `landline-local-monthly`
- **Save as:** `NexusCommunication/wwwroot/images/plans/landline-local-monthly.jpg`
- **Size:** **1920 × 1080 px** (16:9)
- **Plan:** Landline · Local Plan · Monthly Local
- **Kahan dikhegi:** Landing page + Plans page + Admin → Plans & Charges
- **Path is file me hai:** `NexusCommunication/Services/Ui.cs` (PlanPics list) · card: `NexusCommunication/Views/Shared/_PlansBrowser.cshtml`
- **Prompt:**
```
A simple desk telephone with its handset resting, a single small desk calendar page, a small glowing local-area map pin made of light above the phone, violet and cyan lighting, dark background. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, wide 16:9 horizontal composition (1920x1080) with the main subject centered in the middle half of the frame and empty dark space above and below it because the image will be cropped to a wide strip, no text, no letters, no numbers, no logos, no watermark, no readable screens.
```

#### 44. `landline-std-monthly`
- **Save as:** `NexusCommunication/wwwroot/images/plans/landline-std-monthly.jpg`
- **Size:** **1920 × 1080 px** (16:9)
- **Plan:** Landline · STD Plan · Monthly STD
- **Kahan dikhegi:** Landing page + Plans page + Admin → Plans & Charges
- **Path is file me hai:** `NexusCommunication/Services/Ui.cs` (PlanPics list) · card: `NexusCommunication/Views/Shared/_PlansBrowser.cshtml`
- **Prompt:**
```
A desk telephone with thin glowing violet call-arc lines curving from it across a dark space towards a distant softly glowing city skyline (long-distance calling), a single calendar tile, cyan accents. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, wide 16:9 horizontal composition (1920x1080) with the main subject centered in the middle half of the frame and empty dark space above and below it because the image will be cropped to a wide strip, no text, no letters, no numbers, no logos, no watermark, no readable screens.
```

#### 45. `landline-std-halfyearly`
- **Save as:** `NexusCommunication/wwwroot/images/plans/landline-std-halfyearly.jpg`
- **Size:** **1920 × 1080 px** (16:9)
- **Plan:** Landline · STD Plan · Half-Yearly STD
- **Kahan dikhegi:** Landing page + Plans page + Admin → Plans & Charges
- **Path is file me hai:** `NexusCommunication/Services/Ui.cs` (PlanPics list) · card: `NexusCommunication/Views/Shared/_PlansBrowser.cshtml`
- **Prompt:**
```
A desk phone and a glowing globe made of thin violet and cyan light lines with several call arcs between distant points, six small floating calendar tiles in an arc behind, dark reflective surface. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, wide 16:9 horizontal composition (1920x1080) with the main subject centered in the middle half of the frame and empty dark space above and below it because the image will be cropped to a wide strip, no text, no letters, no numbers, no logos, no watermark, no readable screens.
```

#### 46. `landline-std-yearly`
- **Save as:** `NexusCommunication/wwwroot/images/plans/landline-std-yearly.jpg`
- **Size:** **1920 × 1080 px** (16:9)
- **Plan:** Landline · STD Plan · Yearly STD
- **Kahan dikhegi:** Landing page + Plans page + Admin → Plans & Charges
- **Path is file me hai:** `NexusCommunication/Services/Ui.cs` (PlanPics list) · card: `NexusCommunication/Views/Shared/_PlansBrowser.cshtml`
- **Prompt:**
```
Premium executive desk phone on a dark polished desk with a large glowing globe of violet and cyan light lines and many long-distance call arcs spanning continents behind it, a twelve-month calendar wheel of light above, most premium landline look. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, wide 16:9 horizontal composition (1920x1080) with the main subject centered in the middle half of the frame and empty dark space above and below it because the image will be cropped to a wide strip, no text, no letters, no numbers, no logos, no watermark, no readable screens.
```


---

## C) `hero/` — Aap ke paas pehle se hain (sirf naam aur jagah)

Aap ne kaha hero images aap ke paas hain. In ko `NexusCommunication/wwwroot/images/hero/` folder me daal dein. Naam exactly ye hone chahiye:

| Naam | Size | Kahan dikhegi | Code file |
|---|---|---|---|
| `hero-network` | **1920 × 1080** (16:9) | Landing page ka sab se bada hero (upar) | `Views/Home/Index.cshtml` |
| `cta-engineer` | **1920 × 1080** (16:9) | Landing page ke sab se neeche "Get your connection surveyed" ka background | `Views/Home/Index.cshtml` |
| `login-visual` | **1200 × 1600** (3:4, portrait) | Login page, left side | `Views/Auth/Login.cshtml` |
| `register-visual` | **1200 × 1600** (3:4, portrait) | Register page, left side | `Views/Auth/Register.cshtml` |
| `plans-hero` | **1920 × 640** (3:1) | Plans page ka upar wala wide banner | `Views/Home/Plans.cshtml` |
| `track-order-desk` | **1200 × 800** (3:2) | Track page, neeche 3 images me pehli | `Views/Home/Track.cshtml` |
| `track-field-visit` | **1200 × 800** (3:2) | Track page, neeche 3 images me doosri | `Views/Home/Track.cshtml` |
| `track-activation` | **1200 × 800** (3:2) | Track page, neeche 3 images me teesri | `Views/Home/Track.cshtml` |

(Aap ki hero images pehle se bani hain, to unka size alag ho to koi masla nahi, code crop kar leta hai. Wide hero me subject right side par achha lagta hai kyun ke left par text aata hai.)

Aur ek image zip me nahi mili: `products/phone-cx2` (folder `wwwroot/images/products/`), size **800 × 800** (1:1). Ye Admin → Equipment & Stock aur Technical → Equipment Bay me "Corded Desk Phone CX-2" ki photo hai. Prompt: *Black corded desk telephone with keypad, studio product photo on a dark reflective surface with violet rim light, dark navy background (#070B16), neon cyan accents, ultra detailed, no text, no logo, no watermark.*

---

## Total

| | Images |
|---|---|
| `tabs/` | 28 (2560 × 1080) |
| `plans/` | 18 (1920 × 1080) |
| **Nayi images total** | **46** |

Aap ka target 55 tak tha, is liye 9 images bach gayin. Zarurat ho to bata dein, main aur jagah ke liye bana dunga.
