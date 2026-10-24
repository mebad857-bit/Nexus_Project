# Nexus — Loading Screen (logo animation + progress + music)

Sirf **website pehli baar open hone par** chalti hai (browser session mein ek dafa). Theme wahi hai: light white / blue / purple "Daylight" + liquid-glass. Koi external library / asset / CDN nahi — sirf HTML, CSS, JS (Web Audio), .NET Razor.

## Files

| File | Kaam |
|---|---|
| `Views/Shared/_Layout.cshtml` | Loader ka HTML (body ke start mein) + head mein chota inline script jo decide karta hai ke loader dikhana hai ya nahi. Dono hisse `nx-loader:start/end` aur `nx-loader-head:start/end` comments ke andar hain. |
| `wwwroot/css/loader.css` | Poora design: glass logo tile, 3D orbit rings, progress ring + bar, wordmark animation, sound button, mobile, reduced-motion. |
| `wwwroot/js/loader.js` | Real progress, 3D-asset hold/release API, music (Web Audio synth), exit. |
| `wwwroot/js/hero-globe.js` | Hero globe ab loader ko batata hai: "globe ready hone tak ruko" (`hold('globe')` / `release('globe')`). |

## Kaise kaam karta hai

1. **Pehli visit**: page open hote hi loader (logo draw → NEXUS wordmark → progress) dikhta hai, page scroll lock + page ki animations ruki rehti hain.
2. **Progress asli hai** — dikhawa nahi: HTML parsed, DOM ready, fonts, pehli screen ki images, window `load`, logo animation, aur har registered 3D asset ka hissa. Bar kabhi peeche nahi jati.
3. **Sab ready** → "Ready" + halka chime → loader fade/blur hoke hat jata hai → hero ki entrance animations aur globe spin-up usi waqt shuru hoti hain.
4. **Dobara visit (same tab/session)** → loader nahi dikhta (`sessionStorage nx.intro`). Naya browser session = loader dobara.
5. **Kam se kam 7 second** dikhta hai — chahe sab kuch (3D globe, images, poora project) pehle hi load ho chuka ho (`js/loader.js` mein `MIN_MS = 7000`; kam/zyada karna ho to sirf yeh number badlo). Zyada se zyada 15 s (`MAX_MS`) — uske baad bhale koi asset atka ho, visitor ko site mil jati hai. Upar se 3 failsafes aur hain (20 s head timer, 18 s CSS animation, script load error).
6. 7 second ke dauran status text badalta rehta hai (Connecting the network → Syncing plans & services → Preparing the 3D globe → Almost there); agar koi registered 3D asset waqai atka ho to uska apna label dikhta hai.

## Address bar se control

- `?intro=1` → loader dobara dikhao (testing / client ko dikhane ke liye)
- `?intro=0` → loader skip

## Three.js / 3D models / textures ke liye (hold / release)

Is project mein abhi Three.js nahi hai; sirf hero globe hai, aur woh already hooked hai. Jab bhi Three.js ya koi heavy asset add karo:

```js
// 1) jab load shuru ho
nxLoader.hold('model', 'Loading 3D model…');   // loader is naam ke asset ka intezar karega

// 2) jab ready ho
nxLoader.release('model');
```

Three.js `LoadingManager` ke saath (progress bhi sahi chalti hai):

```js
const mgr = new THREE.LoadingManager();
mgr.onStart    = () => nxLoader.hold('three', 'Loading 3D scene…');
mgr.onProgress = (url, loaded, total) => nxLoader.progress('three', loaded / total);
mgr.onLoad     = () => nxLoader.release('three');
const gltf = new GLTFLoader(mgr);  const tex = new THREE.TextureLoader(mgr);
```

Aur short forms: `nxLoader.track(promise, 'label')` (promise resolve hote hi release), `nxLoader.onDone(fn)` ya `window.addEventListener('nx:loader-done', …)` (loader hatne par kuch chalana ho). Agar loader pehle hi hat chuka ho to `nxLoader.active === false` hota hai aur hold/release bina kuch kiye guzar jate hain — isliye code ko if-else ki zaroorat nahi.

## Music

- Default: **built-in synth music** (warm pad + soft arpeggio + sub pulse, end par bright chime) — koi file nahi chahiye.
- **Apni music lagani ho**: file `wwwroot/audio/intro.mp3` rakh do — khud-ba-khud use hogi (file kharab/missing ho to built-in music chal jata hai).
- Browser ka rule: **sound pehli click / tap / key ke baghair block hota hai** (Chrome, Safari, Edge sab) — website kholte hi music apne aap shuru karna kisi bhi code se mumkin nahi. Isliye jab tak browser block rakhe, loader ke neeche bara "Click anywhere to start the music" pill aur upar "Tap for sound" button nazar aata hai; page par kahin bhi **pehli click/tap** music shuru kar deti hai (loader 7 s rehta hai, to waqt kaafi hai). Music progress ke saath build hoti hai aur end par chime ke saath khatam.
- Music ab pehle se ~5 dB zyada loud aur laptop/phone speakers par saaf sunai dene wali mix hai (sub-bass ke saath uski octave bhi, taake bass ghayab na ho).
- Top-right button se Sound on/off; choice `localStorage nx.sound` mein yaad rehti hai.
- Loader hatte hi audio context band ho jata hai — site par koi background sound nahi rehti.

## Logo

`wwwroot/images/brand/logo-mark.(png|svg|webp)` maujood ho to tile mein wahi logo dikhega; warna gradient "N" mark draw hota hai.

## Band karna

`_Layout.cshtml` mein `nx-loader-head:start … end` aur `nx-loader:start … end` dono block hata do (aur `css/loader.css`, `js/loader.js` ki lines). Hero globe ka loader hook `window.nxLoader` na mile to khud ignore ho jata hai.

## Smoothness (lag kam karne ke liye)

- Loader ki saari animations ab sirf `transform`/`opacity` par hain (browser unhein GPU par chalata hai, isliye page ka Tailwind/JS kaam chalte hue bhi smooth rehti hain). Blur filters, backdrop-blur, animated box-shadow aur SVG SMIL hata diye; orbit dots ab pure-CSS hain.
- Progress ka text/bar DOM ko dobara scan karwane wale tareeqe se nahi badalte (Tailwind browser build har DOM change par rescan karta hai).
- Jab tak loader screen par hai, peeche hero globe aur `fx.js` ka constellation canvas **ruk jate hain** (globe sirf pehla frame bana kar intezar karta hai) — loader hatte hi dono chal paRte hain.

## Aap ki di hui files

`wwwroot/js/fx.js` (constellation optimisation) aur `wwwroot/js/hero-globe.js` (quality-adapt + restore, landStep) project mein laga di gayi hain. Sirf itna add kiya gaya: `hero-globe.js` mein loader hooks (hold/release/intro/idle-behind-loader) aur `fx.js` ke constellation `loop()` mein 1 line (`nx-loading` ke dauran idle). Naya version dobara aaye to yeh hooks wapas lagane honge ya mujhe bata dein.

## Accessibility / safety

- `role="status"` + `aria-valuenow` progress bar; `prefers-reduced-motion` mein motion band, loader chhota rehta hai.
- JavaScript band ho to loader nazar hi nahi aata (`<noscript>`).
- Mobile (390px) se 4K tak responsive; print mein hidden.
