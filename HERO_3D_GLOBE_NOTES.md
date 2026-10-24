# Landing page — Hero 3D Globe

Sirf frontend (HTML + CSS + JS). Controllers, Services, Data, Models, SQL Server database — sab bilkul unchanged.
Koi external library nahi (Three.js waghera nahi) — pura 3D khud likha hua Canvas math hai.

## Kya dikhta hai
- Frosted-glass sphere par dotted world map, aahista ghoomta hua (theme ke blue / indigo / violet colours)
- Glowing hub cities; unke beech signal arcs "comet" ki tarah chalte hain aur destination par ripple nikalta hai
- 2 tilted orbit rings, satellites jo nearest hub par beam girate hain
- Cursor ki taraf globe halka jhukta hai; mouse se drag karke ghuma sakte hain (chhodne par inertia ke baad dobara khud ghoomta hai)
- Headline "Signals that stitch cities together." ke saath theme match (arcs = cities ko stitch karna)

## Naye files
- `NexusCommunication/wwwroot/js/hero-globe.js`  — poora globe (canvas + 3D maths + land map data)
- `NexusCommunication/wwwroot/css/hero-globe.css` — sirf position / size / entrance animation / responsive

## Purani files mein sirf yeh chhoti tabdeeliyan
- `Views/Home/Index.cshtml`
  - hero mein 1 chhota block: `<div class="nx-globe" data-nx-globe> … <canvas></canvas></div>` (constellation canvas ke baad)
  - `@section Head { hero-globe.css }` aur `@section Scripts` ke andar `hero-globe.js` ka 1 script tag
- `Views/Shared/_Layout.cshtml` — 1 line: `@await RenderSectionAsync("Head", required: false)` (taake CSS sirf landing page par load ho)

Hero ka text, buttons, track form, stats, floating cards, background — kuch nahi badla. Floating cards ab globe ke upar "orbit chips" ki tarah baithte hain.

## Behaviour
- Desktop (>=1280px): globe hero ke right side par full size
- Tablet / mobile: globe soft backdrop ban jata hai (halka + left side fade) taake headline readable rahe
- Tab hidden ya hero screen se bahar ho to animation ruk jati hai (CPU/battery bachti hai)
- Machine slow ho to canvas khud quality kam kar leta hai (pehle sharpness, phir extra dots)
- `prefers-reduced-motion`: sirf ek still frame, koi animation nahi
- Touch devices: drag band, sirf animation

## Customise
- Rotation speed: `hero-globe.js` mein `update()` ke andar `var auto = 0.105 + …` (0.105 rad/sec)
- Size / position: `hero-globe.css` ke top par `--g` (globe ka size) aur `right`
- Kitne arcs ek saath: `frame()` mein `arcs.length < 6`
- Hub cities: `HUB_DEF` list (lat, lon)
- Colours: `LAND_COLORS` aur `ARC_PAIRS` (RGB)

## Band karna ho to
`Index.cshtml` se `<div class="nx-globe" …>` block hata dein — script khud exit ho jata hai, baaki page waisa hi rahega.

## Note
Is machine par .NET SDK nahi tha, isliye ASP.NET project run nahi kiya gaya. Hero ko Chromium mein static page ki tarah render karke
1920 / 1440 / 1280 / 1024 / 768 / 390 px par check kiya gaya (0 JS errors, ~2 ms/frame). Visual Studio mein ek baar Build/Run karke dekh lein.
World map ke continents chhote-resolution (1.5°) par hain — dotted look ke liye kaafi hain.
