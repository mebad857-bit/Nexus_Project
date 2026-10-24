# NEXUS — 9 features ke liye missing images (sirf 7)

Baqi sab images aap ke paas hain. Sirf yeh 7 chahiyein (3 ka path pehle se laga tha, 4 ka path ab laga diya hai).

Har image ko **exactly isi naam** se save karein (extension .jpg / .png / .webp kuch bhi chalega) is path par:

```
NexusCommunication/wwwroot/images/<folder>/<naam>.jpg
```

Image na ho to page kharab nahi hota: 3 `tabs/` wali par khali banner hide ho jata hai, baqi 4 jagah image ke bina kuch dikhta hi nahi (purana look).

## Theme (har prompt me likhi hui hai)
Purani images wali hi theme: deep dark navy `#070B16`, neon cyan + electric blue accent, halki violet glow, cinematic telecom/technology photography, **no text / letters / numbers / logo / watermark**. Format JPG (quality ~85).

| # | Naam | Folder | Size | Kahan dikhegi |
|---|---|---|---|---|
| 1 | `admin-messages` | `wwwroot/images/tabs/` | 2560 × 1080 px (21:9). 21:9 na mile to 1920 × 1080 bhi chalega | Admin panel → Contact inbox (Messages) tab ke upar (/admin#messages) |
| 2 | `admin-faqs` | `wwwroot/images/tabs/` | 2560 × 1080 px (21:9). 21:9 na mile to 1920 × 1080 bhi chalega | Admin panel → Help centre (FAQs) tab ke upar (/admin#faqs) |
| 3 | `admin-activity` | `wwwroot/images/tabs/` | 2560 × 1080 px (21:9). 21:9 na mile to 1920 × 1080 bhi chalega | Admin panel → Activity log tab ke upar (/admin#activity) |
| 4 | `faq-hero` | `wwwroot/images/hero/` | 1920 × 640 px (3:1 wide banner) | Public FAQ page (/faq) — heading ke neeche, search box se pehle |
| 5 | `contact-support` | `wwwroot/images/hero/` | 1200 × 900 px (4:3) | Public Contact page (/contact) — left column me info cards ke neeche |
| 6 | `calculator-visual` | `wwwroot/images/hero/` | 1200 × 750 px (16:10) | Home page → Charges calculator section — left column me 3 bullet points ke neeche (sirf desktop par dikhti hai) |
| 7 | `chatbot-avatar` | `wwwroot/images/brand/` | 512 × 512 px (1:1 square, JPG ya PNG) | Chatbot ke header me gol avatar (har page ka neeche-right chat button kholne par) |

## 1. `admin-messages`
- **Save as:** `NexusCommunication/wwwroot/images/tabs/admin-messages.jpg`
- **Size:** 2560 × 1080 px (21:9). 21:9 na mile to 1920 × 1080 bhi chalega
- **Kahan dikhegi:** Admin panel → Contact inbox (Messages) tab ke upar (/admin#messages)
- **Code file:** `Views/Admin/Index.cshtml (path pehle se laga hai)`
- **Prompt:**
```
A customer-care team member at a clean modern desk reading incoming customer messages on a tablet, soft glowing envelope and chat-bubble shapes made of cyan light drifting above the desk, calm focused mood, blue accent. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, ultra-wide 21:9 horizontal composition (2560x1080) with the main subject confined to the middle horizontal third of the frame and generous empty dark space above and below it because the image will be cropped to a thin wide banner, left third slightly darker and less busy, no text, no letters, no numbers, no logos, no watermark, no readable screens.
```

## 2. `admin-faqs`
- **Save as:** `NexusCommunication/wwwroot/images/tabs/admin-faqs.jpg`
- **Size:** 2560 × 1080 px (21:9). 21:9 na mile to 1920 × 1080 bhi chalega
- **Kahan dikhegi:** Admin panel → Help centre (FAQs) tab ke upar (/admin#faqs)
- **Code file:** `Views/Admin/Index.cshtml (path pehle se laga hai)`
- **Prompt:**
```
An open notebook and a tablet on a dark glass desk with a glowing lightbulb and floating translucent question-mark shaped light sculptures rising above them, knowledge-base and help-centre feeling, cyan and blue glow. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, ultra-wide 21:9 horizontal composition (2560x1080) with the main subject confined to the middle horizontal third of the frame and generous empty dark space above and below it because the image will be cropped to a thin wide banner, left third slightly darker and less busy, no text, no letters, no numbers, no logos, no watermark, no readable screens.
```

## 3. `admin-activity`
- **Save as:** `NexusCommunication/wwwroot/images/tabs/admin-activity.jpg`
- **Size:** 2560 × 1080 px (21:9). 21:9 na mile to 1920 × 1080 bhi chalega
- **Kahan dikhegi:** Admin panel → Activity log tab ke upar (/admin#activity)
- **Code file:** `Views/Admin/Index.cshtml (path pehle se laga hai)`
- **Prompt:**
```
A glowing horizontal timeline of luminous cyan nodes connected by thin light lines floating over a dark glass panel, a transparent magnifying glass hovering over one node, audit-trail and security-monitoring feeling, subtle violet reflections. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, ultra-wide 21:9 horizontal composition (2560x1080) with the main subject confined to the middle horizontal third of the frame and generous empty dark space above and below it because the image will be cropped to a thin wide banner, left third slightly darker and less busy, no text, no letters, no numbers, no logos, no watermark, no readable screens.
```

## 4. `faq-hero`
- **Save as:** `NexusCommunication/wwwroot/images/hero/faq-hero.jpg`
- **Size:** 1920 × 640 px (3:1 wide banner)
- **Kahan dikhegi:** Public FAQ page (/faq) — heading ke neeche, search box se pehle
- **Code file:** `Views/Home/Faq.cshtml (path ab laga diya hai)`
- **Prompt:**
```
A friendly modern help-desk scene with a support agent wearing a headset at a clean desk, large soft glowing speech-bubble and lightbulb shapes made of cyan light floating around them, welcoming helpful mood. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, wide panoramic 3:1 horizontal composition (1920x640) with the main subject centred and soft empty dark space on both sides, no text, no letters, no numbers, no logos, no watermark, no readable screens.
```

## 5. `contact-support`
- **Save as:** `NexusCommunication/wwwroot/images/hero/contact-support.jpg`
- **Size:** 1200 × 900 px (4:3)
- **Kahan dikhegi:** Public Contact page (/contact) — left column me info cards ke neeche
- **Code file:** `Views/Home/Contact.cshtml (path ab laga diya hai)`
- **Prompt:**
```
A smiling customer-care agent with a headset in a bright modern telecom contact centre, softly blurred colleagues and glowing cyan indicator lights behind them, a gentle ring of light around a floating chat bubble, warm welcoming mood, face not too close to the camera. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, 4:3 landscape composition (1200x900) with the agent centred, no text, no letters, no numbers, no logos, no watermark, no readable screens.
```

## 6. `calculator-visual`
- **Save as:** `NexusCommunication/wwwroot/images/hero/calculator-visual.jpg`
- **Size:** 1200 × 750 px (16:10)
- **Kahan dikhegi:** Home page → Charges calculator section — left column me 3 bullet points ke neeche (sirf desktop par dikhti hai)
- **Code file:** `Views/Home/Index.cshtml (path ab laga diya hai)`
- **Prompt:**
```
A sleek calculator and a modern white Wi-Fi router on a dark reflective glass desk, glowing cyan coins and a ribbon-shaped receipt made of light floating above them, cost-planning and transparent-pricing feeling, a hint of violet reflections. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, 16:10 landscape composition (1200x750) with the subject centred, no text, no letters, no numbers, no logos, no watermark, no readable screens. No readable numbers on the calculator or the receipt.
```

## 7. `chatbot-avatar`
- **Save as:** `NexusCommunication/wwwroot/images/brand/chatbot-avatar.jpg`
- **Size:** 512 × 512 px (1:1 square, JPG ya PNG)
- **Kahan dikhegi:** Chatbot ke header me gol avatar (har page ka neeche-right chat button kholne par)
- **Code file:** `Views/Shared/_Chatbot.cshtml (path ab laga diya hai; image na ho to purana robot icon dikhta hai)`
- **Prompt:**
```
A friendly rounded robot assistant head with a smooth glossy white and blue shell, two softly glowing cyan eyes and a tiny smile, a small antenna with a violet glow, centred, the face filling about 70 percent of the frame, plain deep dark navy background, front view, cute and trustworthy mood. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, square 1:1 composition (512x512) safe for a circular crop, no text, no letters, no numbers, no logos, no watermark, no readable screens.
```
