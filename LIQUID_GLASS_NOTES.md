# Liquid Glass UI

Sirf frontend. Controllers, Services, Data, Models aur saari views ka markup bilkul unchanged.

## Naye files
- `wwwroot/css/liquid-glass.css` : poora liquid-glass look (fx.css ke baad load hota hai)
- `wwwroot/js/liquid-glass.js`   : Chromium par lens-refraction on, aur background orbs cursor ke saath halka hilte hain

## Purani file mein sirf yeh tabdeeli
- `Views/Shared/_Layout.cshtml` : 1 css link, 1 chhota SVG filter (`#lg-warp`), 1 script tag

## Kya badla
- Colourful drifting backdrop (blue / violet / pink / cyan) taake glass ke peeche kuch bend ho
- Cards, sidebar, drawer, toasts, modals, command palette, compare pill, chatbot: frosted blur + saturation, bright rim, inner highlight, soft shadow, cursor-follow specular glow
- Buttons: glossy gel capsules; outline buttons: clear glass; inputs: inset glass wells
- Active tabs / sidebar items: glossy tinted glass; tables, dividers, chips softened
- Header scroll par glass bar + sliding nav pill

## Band karna ho to
`_Layout.cshtml` se liquid-glass.css link, `<svg id="lg-defs">` block aur liquid-glass.js script hata dein.
Blur kam/zyada: `liquid-glass.css` ke top par `--lg-blur`, transparency: `--lg-fill`.
`prefers-reduced-motion` / `prefers-reduced-transparency` / touch devices par effects khud kam ho jate hain.
