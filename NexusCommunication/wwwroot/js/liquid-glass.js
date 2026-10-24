/* Liquid-glass helpers: (1) enable SVG lens refraction on Chromium, (2) let the colourful backdrop drift slightly with the cursor. Presentation only. */
(function () {
  'use strict';
  var root = document.documentElement;

  // (1) backdrop-filter: url(#svg) is only honoured by Chromium
  try {
    var ua = navigator.userAgent || '';
    var chromium = /Chrome\//.test(ua) && !/Firefox/.test(ua);
    var coarse = window.matchMedia && matchMedia('(pointer: coarse)').matches;
    if (chromium && !coarse && !(window.matchMedia && matchMedia('(prefers-reduced-motion: reduce)').matches)) root.classList.add('lg-refract');
  } catch (e) { }

  // (1b) performance governor: full effects stay ON; only if frames stay slow for several seconds on this machine do we switch to a lighter blur / no lens warp (html.lg-lite)
  (function () {
    if (!window.requestAnimationFrame) return;
    var nxfx = (window.nx = window.nx || {}); nxfx.fx = nxfx.fx || {};
    var last = 0, slow = 0, n = 0, running = false;
    function tick(t) {
      if (document.hidden || root.classList.contains('nx-loading')) { last = 0; slow = 0; n = 0; return requestAnimationFrame(tick); }
      if (last) {
        var dt = t - last;
        if (dt < 250) {                       // ignore tab switches / long pauses
          n++; if (n > 30) { slow = slow * 0.97 + (dt > 34 ? 1 : 0) * 0.03; }
          if (n > 240 && slow > 0.55) {       // ~4 s of mostly-slow frames
            root.classList.add('lg-lite'); root.classList.remove('lg-refract'); nxfx.fx.lite = true; return;
          }
          if (n > 900) return;                // ran fine for ~15 s: stop measuring
        }
      }
      last = t; requestAnimationFrame(tick);
    }
    if (!running) { running = true; requestAnimationFrame(tick); }
  })();

  // (2) parallax of the backdrop orbs
  var reduce = window.matchMedia && matchMedia('(prefers-reduced-motion: reduce)').matches;
  var touch = window.matchMedia && matchMedia('(pointer: coarse)').matches;
  if (reduce || touch) return;
  var tx = 0, ty = 0, cx = 0, cy = 0, raf = 0;
  function tick() {
    cx += (tx - cx) * 0.06; cy += (ty - cy) * 0.06;
    root.style.setProperty('--lg-ox', cx.toFixed(1) + 'px');
    root.style.setProperty('--lg-oy', cy.toFixed(1) + 'px');
    raf = (Math.abs(tx - cx) > 0.1 || Math.abs(ty - cy) > 0.1) ? requestAnimationFrame(tick) : 0;
  }
  window.addEventListener('pointermove', function (e) {
    tx = (e.clientX / window.innerWidth - 0.5) * -46;
    ty = (e.clientY / window.innerHeight - 0.5) * -46;
    if (!raf) raf = requestAnimationFrame(tick);
  }, { passive: true });
})();
