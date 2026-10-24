# UI FX layer (interactive polish)

Sirf frontend: theme (white · blue · purple) aur backend (Controllers, Services, Data, Models) bilkul unchanged.

## Naye files
- `NexusCommunication/wwwroot/css/fx.css`  — saari animations / transitions / hover effects
- `NexusCommunication/wwwroot/js/fx.js`    — cursor-reactive effects, constellation engine, page transitions

## Purani files mein sirf yeh chhoti tabdeeliyan
- `Views/Shared/_Layout.cshtml` — fx.css link, fx.js script, 1 chhota head script (page transition handshake)
- `wwwroot/js/site.js`           — `initNetwork()` ab naye constellation engine ko use karta hai (purana code fallback ke taur par maujood)
- `Views/Home/Index.cshtml`      — hero ke 2 floating cards par `data-depth` attribute (parallax)

## Kya add hua (poore project par)
- Constellation ab har page par halka backdrop hai; cursor ki taraf nodes khinchte hain, click par shock-wave, kabhi kabhi shooting star
- Cursor aura + follower ring (links par bada, text fields par I-beam), click par pulse
- Cards par cursor-spotlight aur halka 3D tilt; buttons par magnetic pull, shine, ripple
- Background grid cursor ke neeche roshan hota hai
- Hero cards / images / blurred blobs par parallax
- Page transition (gradient curtain wipe), scroll progress bar, API calls ke dauran bar par shimmer
- Dashboard: tab panels staggered entrance, table rows glide-in + accent edge, sidebar icons motion
- Nav: sliding pill, active underline, scroll par hide/show header
- Back-to-top (progress ring), toast progress bar, calculator numbers pop, text-gradient flow, section eyebrow lines draw-in

## Band karna ho to
- Pura layer: `_Layout.cshtml` se fx.css aur fx.js ki lines hata dein.
- Sirf cursor ring/aura: `fx.css` mein `.fx-aura, .fx-ring { display:none !important; }` add karein.
- Page transition band: `fx.js` ke `boot()` se `initTransitions();` hata dein.
- `prefers-reduced-motion` aur touch devices par zyada-tar effects khud band ho jate hain.
