# Complaint / Ticket feature — 4 images

Har image ko **exactly isi naam** se is folder me rakhein (`.jpg`, `.png`, `.webp` kuch bhi chalega, project khud extension dhoond leta hai):

```
UI4/NexusCommunication/wwwroot/images/tabs/     <- 3 banners
UI4/NexusCommunication/wwwroot/images/hero/     <- 1 photo
```
(Visual Studio me: `NexusCommunication` project → `wwwroot` → `images` → `tabs` / `hero`.)

Image na ho to page kharab nahi hota — banner / photo box bas chhup jata hai.

| # | Naam | Poora path | Size | Kahan dikhegi |
|---|---|---|---|---|
| 1 | `customer-tickets` | `wwwroot/images/tabs/customer-tickets.jpg` | 2560 × 1080 (21:9) | Customer Portal → Complaints tab ke upar (`/portal#tickets`) |
| 2 | `admin-tickets` | `wwwroot/images/tabs/admin-tickets.jpg` | 2560 × 1080 (21:9) | Admin → Complaints tab ke upar (`/admin#tickets`) |
| 3 | `technical-tickets` | `wwwroot/images/tabs/technical-tickets.jpg` | 2560 × 1080 (21:9) | Technical Desk → My Tickets tab ke upar (`/technical#tickets`) |
| 4 | `ticket-support` | `wwwroot/images/hero/ticket-support.jpg` | 1280 × 720 (16:9) | Customer Portal → Complaints → "Raise a complaint" form ke upar |

Theme (purani images jaisi): deep dark navy `#070B16`, neon cyan + electric blue accent, halki violet glow, cinematic telecom photography, **no text / letters / numbers / logo / watermark**. Format JPG (quality ~85).

## 1. customer-tickets
```
A friendly customer-support specialist wearing a headset at a clean modern desk, a glowing cyan ticket card and chat bubbles floating above the desk, calm helpful mood. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, ultra-wide 21:9 horizontal composition (2560x1080) with the main subject confined to the middle horizontal third of the frame and generous empty dark space above and below it because the image will be cropped to a thin wide banner, left third slightly darker and less busy, no text, no letters, no numbers, no logos, no watermark, no readable screens.
```

## 2. admin-tickets
```
A network operations manager at a large curved dashboard wall assigning work, glowing cyan task cards flowing from a central hub to several engineer silhouettes, organised and in control. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, ultra-wide 21:9 horizontal composition (2560x1080) with the main subject confined to the middle horizontal third of the frame and generous empty dark space above and below it because the image will be cropped to a thin wide banner, left third slightly darker and less busy, no text, no letters, no numbers, no logos, no watermark, no readable screens.
```

## 3. technical-tickets
```
A field network engineer in a hard hat and reflective vest repairing a fibre-optic cabinet at dusk, glowing cyan fibre strands and a tablet in hand, focused and hands-on. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, ultra-wide 21:9 horizontal composition (2560x1080) with the main subject confined to the middle horizontal third of the frame and generous empty dark space above and below it because the image will be cropped to a thin wide banner, left third slightly darker and less busy, no text, no letters, no numbers, no logos, no watermark, no readable screens.
```

## 4. ticket-support
```
Close-up of a smiling support agent with a headset in a modern call-centre, soft glowing cyan chat bubble and a checkmark-shaped light floating beside them, welcoming and reassuring. Cinematic professional telecom and technology brand photography, deep dark navy background (#070B16), neon cyan and electric blue accent lighting with a subtle violet glow, high contrast, ultra detailed, sharp focus, shallow depth of field, premium futuristic look, 16:9 horizontal composition (1280x720) with the subject slightly right of centre and calm dark space on the left, no text, no letters, no numbers, no logos, no watermark, no readable screens.
```
