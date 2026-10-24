/* ============================================================================
   Nexus LOADING SCREEN — logic, real progress and music
   Markup: Views/Shared/_Layout.cshtml (#nx-loader)   Look: wwwroot/css/loader.css
   No library, no audio file needed: the music is synthesised live with the Web Audio API
   (drop wwwroot/audio/intro.mp3 next to it if you would rather play your own track).

   When it shows:  the first time a browser session opens the site  (?intro=1 replays it, ?intro=0 skips it)
   When it leaves: only when the page is really ready —
        DOM built · fonts loaded · first-screen images loaded · window "load" · every registered 3D / heavy asset ready
        (+ a minimum on-screen time so the logo animation and the music intro can play, and a maximum so it never traps anyone)

   Public API for 3D / heavy assets (the hero globe uses it; Three.js, GLTF models, textures can too):
        nxLoader.hold('model', 'Loading 3D model…');   // keep the screen up
        nxLoader.progress('model', 0.4);               // optional: 0..1
        nxLoader.release('model');                      // this asset is ready
        nxLoader.track(promise, 'label');               // hold until a promise settles
        nxLoader.onDone(fn);  window 'nx:loader-done'   // fired when the screen has lifted
   All calls are harmless no-ops when the loader is not showing.
   ============================================================================ */
(function () {
  'use strict';

  var root = document.documentElement;
  var el = document.getElementById('nx-loader');
  var active = !!el && root.classList.contains('nx-loader-on');

  /* ---------------------------------------------------------------- inactive: expose a harmless API and leave */
  if (!active) {
    window.nxLoader = {
      active: false, done: true,
      hold: function () { }, release: function () { }, progress: function () { },
      track: function (p) { return p; }, onDone: function (cb) { try { cb(); } catch (e) { } }
    };
    if (el && el.parentNode) el.parentNode.removeChild(el);
    return;
  }

  var MIN_MS = 7000;     // shortest time on screen: the screen stays at least this long, even when everything is already loaded
  var MAX_MS = 15000;    // never wait longer than this, whatever is still loading
  var T0 = performance.now();
  var finished = false, shown = 0, lastT = T0, doneCbs = [];

  function $(s) { return el.querySelector(s); }
  var elPct = $('.nxl-pct'), elStatus = $('.nxl-status'), elBar = $('.nxl-bar'), elBarFill = $('.nxl-bar i');
  var elArc = $('.nxl-ring .arc:not(.arcg)'), elArcG = $('.nxl-ring .arcg'), elHead = $('.nxl-ring .head'), elTile = $('.nxl-tile');
  /* text is changed through its text node (characterData), so per-frame updates never look like "new content" to MutationObservers such as Tailwind's browser build */
  function textNodeOf(node) { if (!node.firstChild || node.firstChild.nodeType !== 3) node.textContent = node.textContent || ' '; return node.firstChild; }
  var tnPct = textNodeOf(elPct), tnStatus = textNodeOf(elStatus);
  try { var tapTxt = $('.nxl-tap span'); if (tapTxt && window.matchMedia && matchMedia('(hover: none)').matches) tapTxt.textContent = 'Tap anywhere to start the music'; } catch (e) { }
  var reduce = false; try { reduce = window.matchMedia && matchMedia('(prefers-reduced-motion: reduce)').matches; } catch (e) { }
  if (reduce) { try { var svgs = el.querySelectorAll('svg'); for (var si = 0; si < svgs.length; si++) if (svgs[si].pauseAnimations) svgs[si].pauseAnimations(); } catch (e) { } }

  /* ================================================================ 1. progress model */
  var tasks = {}, order = [];
  function add(name, w, label) { if (!tasks[name]) { tasks[name] = { n: name, w: w, p: 0, label: label }; order.push(name); } return tasks[name]; }
  function set(name, p) { var t = tasks[name]; if (t && p > t.p) t.p = p > 1 ? 1 : p; }
  function real() {
    var sw = 0, sp = 0, i, t;
    for (i = 0; i < order.length; i++) { t = tasks[order[i]]; sw += t.w; sp += t.w * t.p; }
    return sw ? sp / sw : 0;
  }
  function pending() {
    for (var i = 0; i < order.length; i++) if (tasks[order[i]].p < 1) return tasks[order[i]];
    return null;
  }

  add('html', 0.06, 'Starting up…'); set('html', 1);
  add('dom', 0.12, 'Building the interface…');
  add('fonts', 0.10, 'Loading fonts…');
  add('images', 0.20, 'Loading images…');
  add('load', 0.20, 'Finishing up…');

  function onDom() {
    set('dom', 1);
    /* first-screen images only (lazy ones and far-below-the-fold ones are not worth waiting for) */
    var imgs = [], all = document.images, i, r;
    for (i = 0; i < all.length; i++) {
      var im = all[i];
      if (el.contains(im) || im.loading === 'lazy' || !im.getAttribute('src')) continue;
      r = im.getBoundingClientRect();
      if (r.top > window.innerHeight * 1.3) continue;
      imgs.push(im);
    }
    if (!imgs.length) { set('images', 1); return; }
    var left = imgs.length, total = imgs.length;
    function one() { left--; set('images', (total - left) / total); }
    imgs.forEach(function (im) {
      if (im.complete) { one(); return; }
      im.addEventListener('load', one, { once: true }); im.addEventListener('error', one, { once: true });
    });
    setTimeout(function () { set('images', 1); }, 6000);
  }
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', onDom); else onDom();

  if (document.fonts && document.fonts.ready) { document.fonts.ready.then(function () { set('fonts', 1); }, function () { set('fonts', 1); }); setTimeout(function () { set('fonts', 1); }, 4500); }
  else set('fonts', 1);

  if (document.readyState === 'complete') set('load', 1); else window.addEventListener('load', function () { set('load', 1); });

  /* the logo picture (only when the real logo file exists) */
  var logo = elTile && elTile.querySelector('img'), logoSrc = logo && logo.getAttribute('data-src');
  if (logo && logoSrc) {
    add('logo', 0.06, 'Loading logo…');
    logo.addEventListener('load', function () { set('logo', 1); }, { once: true });
    logo.addEventListener('error', function () { elTile.classList.remove('has-img'); set('logo', 1); }, { once: true });
    logo.src = logoSrc;
  }

  /* ================================================================ 2. public API (3D / heavy assets) */
  var holdSeq = 0;
  var api = window.nxLoader = {
    active: true, done: false,
    hold: function (name, label) { var t = add('h:' + name, 0.3, label || 'Preparing 3D assets…'); if (t.p >= 1) t.p = 0.0; return name; },
    release: function (name) { set('h:' + name, 1); },
    progress: function (name, f) { var t = tasks['h:' + name]; if (t && f > t.p && f < 1) t.p = f; },
    track: function (promise, label) {
      var n = 'p' + (++holdSeq); api.hold(n, label);
      var fin = function () { api.release(n); };
      if (promise && promise.then) promise.then(fin, fin); else fin();
      return promise;
    },
    onDone: function (cb) { if (api.done) { try { cb(); } catch (e) { } } else doneCbs.push(cb); },
    _test: {}
  };

  /* ================================================================ 3. painting */
  var CIRC = 578.05, lastAria = -1, lastLabel = '', lastPct = -1;
  function paint(p) {
    var pr = p < 0 ? 0 : p > 100 ? 100 : p;
    var ip = Math.round(pr); if (ip !== lastPct) { lastPct = ip; tnPct.data = ip + '%'; }
    var off = (CIRC * (1 - pr / 100)).toFixed(2);
    elBarFill.style.transform = 'scaleX(' + (pr / 100).toFixed(4) + ')';
    elArc.style.strokeDashoffset = off; if (elArcG) elArcG.style.strokeDashoffset = off;
    elHead.style.transform = 'rotate(' + (pr * 3.6).toFixed(2) + 'deg)';
    if (pr > 0.4) el.classList.add('has-progress');
    var a = Math.round(pr / 5) * 5; if (a !== lastAria) { lastAria = a; elBar.setAttribute('aria-valuenow', String(Math.round(pr))); }
  }
  var STAGES = [[0, 'Starting up…'], [12, 'Connecting to the network…'], [36, 'Syncing plans & services…'], [60, 'Preparing the 3D globe…'], [84, 'Almost there…']];
  function stageText(p) { var t = STAGES[0][1]; for (var i = 0; i < STAGES.length; i++) if (p >= STAGES[i][0]) t = STAGES[i][1]; return t; }
  function label(txt) { if (txt !== lastLabel) { lastLabel = txt; tnStatus.data = txt; } }

  /* ================================================================ 4. main loop */
  function step(now) {
    if (finished) return;
    var dt = Math.min(0.1, (now - lastT) / 1000); lastT = now;
    var elapsed = now - T0, r = real(), forced = elapsed > MAX_MS;
    if (forced) { r = 1; if (!step.warned) { step.warned = true; try { console.warn('[nxLoader] gave up waiting for:', order.filter(function (n) { return tasks[n].p < 1; })); } catch (e) { } } }

    var u = Math.min(1, elapsed / MIN_MS), cap = 100 * (1 - Math.pow(1 - u, 1.4));      // steady, gently easing climb: hits 100 at MIN_MS
    var target = Math.min(r * 100, cap);
    if (elapsed > MIN_MS && r < 1) target = Math.min(96, Math.max(target, shown + 0.8 * dt));  // still waiting: keep a slow, honest creep
    if (target < shown) target = shown;                                                        // never goes backwards
    shown += (target - shown) * (1 - Math.exp(-dt * (forced ? 14 : (r >= 1 && elapsed >= MIN_MS) ? 9 : 6)));   // faster once everything is ready
    if (target - shown < 0.08) shown = target;
    paint(shown);

    var pend = pending();
    var customWait = pend && pend.n.indexOf('h:') === 0 && shown >= 40;                 // a registered 3D / heavy asset is still loading: say so
    label(shown >= 99.9 ? 'Ready' : (customWait || (pend && elapsed > MIN_MS)) ? pend.label : stageText(shown));

    if (shown >= 99.9 && (r >= 1) && elapsed >= MIN_MS) finish();
  }
  function safeStep(t) {
    try { step(typeof t === 'number' ? t : performance.now()); return true; }
    catch (e) { try { console.error('[nxLoader]', e); } catch (e2) { } if (!finished) { finished = true; exit(); } return false; }   // never leave the visitor behind the screen
  }
  function loop(t) { if (safeStep(t) && !finished) requestAnimationFrame(loop); }
  requestAnimationFrame(loop);
  setInterval(function () { if (document.hidden) safeStep(performance.now()); }, 400);   // background tab: rAF is paused, keep going

  /* ================================================================ 5. finish + exit */
  function finish() {
    if (finished) return;
    finished = true; paint(100); label('Ready');
    el.classList.add('done');
    Sound.finale();
    setTimeout(exit, 520);
  }
  function exit() {
    root.classList.remove('nx-loading');                 // unlock scroll; the hero entrance animations start now
    el.classList.add('out');
    try { sessionStorage.setItem('nx.intro', '1'); } catch (e) { }
    api.active = false; api.done = true;
    try { window.dispatchEvent(new CustomEvent('nx:loader-done')); } catch (e) { }
    doneCbs.splice(0).forEach(function (cb) { try { cb(); } catch (e) { } });
    setTimeout(function () {
      root.classList.remove('nx-loader-on');
      if (el.parentNode) el.parentNode.removeChild(el);
      Sound.dispose();
    }, 1000);
  }

  /* ================================================================ 6. sound */
  var Sound = (function () {
    var AC = window.AudioContext || window.webkitAudioContext;
    var BPM = 76, STEP = 60 / BPM / 2;                           // one eighth note
    var VOL = 0.95;
    var btn = $('.nxl-sound'), btnLabel = btn && btn.querySelector('.nxl-sound-label');
    var fileSrc = el.getAttribute('data-music') || '';
    var pref = 'on'; try { pref = localStorage.getItem('nx.sound') === 'off' ? 'off' : 'on'; } catch (e) { }
    var ctx = null, G = null, started = false, ended = false, timer = 0, nextT = 0, stepNo = 0, state = 'on', audioEl = null, noiseBuf = null;

    /* ---- score ---------------------------------------------------------------------------------------------------- */
    var CH = [                                                    // C · G · Am · F  (warm, hopeful, corporate-cinematic)
      { root: 36, pad: [48, 55, 62, 64, 71], arp: [64, 67, 71, 74, 76, 79] },
      { root: 31, pad: [43, 50, 57, 59, 62], arp: [62, 67, 71, 74, 79, 81] },
      { root: 33, pad: [45, 52, 55, 59, 64], arp: [64, 67, 69, 72, 76, 79] },
      { root: 29, pad: [41, 48, 55, 57, 64], arp: [60, 65, 67, 69, 72, 76] }
    ];
    var PAT = [[0, 2, 4, 2, 5, 4, 2, 3], [0, 3, 2, 4, 3, 5, 4, 2]];
    function mtof(m) { return 440 * Math.pow(2, (m - 69) / 12); }

    /* ---- audio graph (works on a live or an offline context) ---------------------------------------------------- */
    function makeIR(c, sec, decay) {
      var n = Math.floor(c.sampleRate * sec), b = c.createBuffer(2, n, c.sampleRate), ch, i, d, lp;
      for (ch = 0; ch < 2; ch++) { d = b.getChannelData(ch); lp = 0; for (i = 0; i < n; i++) { lp += ((Math.random() * 2 - 1) - lp) * 0.55; d[i] = lp * Math.pow(1 - i / n, decay); } }
      return b;
    }
    function build(c) {
      var g = {};
      g.comp = c.createDynamicsCompressor();
      g.comp.threshold.value = -20; g.comp.knee.value = 24; g.comp.ratio.value = 3.5; g.comp.attack.value = 0.006; g.comp.release.value = 0.25;
      g.master = c.createGain(); g.master.gain.value = 0;
      g.comp.connect(g.master); g.master.connect(c.destination);
      g.dry = c.createGain(); g.dry.connect(g.comp);
      g.rev = c.createConvolver(); g.rev.buffer = makeIR(c, 2.6, 3); g.revIn = c.createGain(); g.revOut = c.createGain(); g.revOut.gain.value = 0.62;
      g.revIn.connect(g.rev); g.rev.connect(g.revOut); g.revOut.connect(g.comp);
      g.dly = c.createDelay(2); g.dly.delayTime.value = STEP * 1.5; g.dlyIn = c.createGain(); g.dlyFb = c.createGain(); g.dlyFb.gain.value = 0.36;
      g.dlyLp = c.createBiquadFilter(); g.dlyLp.type = 'lowpass'; g.dlyLp.frequency.value = 2600; g.dlyOut = c.createGain(); g.dlyOut.gain.value = 0.34;
      g.dlyIn.connect(g.dly); g.dly.connect(g.dlyLp); g.dlyLp.connect(g.dlyFb); g.dlyFb.connect(g.dly); g.dlyLp.connect(g.dlyOut); g.dlyOut.connect(g.comp); g.dlyOut.connect(g.revIn);
      var n = Math.floor(c.sampleRate * 1.2); noiseBuf = c.createBuffer(1, n, c.sampleRate); var nd = noiseBuf.getChannelData(0); for (var i = 0; i < n; i++) nd[i] = Math.random() * 2 - 1;
      return g;
    }
    function env(c, gain, t, a, peak, hold, rel) {
      gain.gain.setValueAtTime(0.0001, t);
      gain.gain.linearRampToValueAtTime(peak, t + a);
      gain.gain.setValueAtTime(peak, t + a + hold);
      gain.gain.exponentialRampToValueAtTime(0.0001, t + a + hold + rel);
    }
    function send(c, g, node, rev, dly) {
      node.connect(g.dry);
      if (rev) { var r = c.createGain(); r.gain.value = rev; node.connect(r); r.connect(g.revIn); }
      if (dly) { var d = c.createGain(); d.gain.value = dly; node.connect(d); d.connect(g.dlyIn); }
    }
    function pan(c, v) { if (!c.createStereoPanner) return null; var p = c.createStereoPanner(); p.pan.value = v; return p; }

    function padChord(c, g, t, dur, ch, prog) {
      var lp = c.createBiquadFilter(); lp.type = 'lowpass'; lp.Q.value = 0.7;
      lp.frequency.setValueAtTime(520 + 2300 * prog, t); lp.frequency.linearRampToValueAtTime(900 + 2600 * prog, t + dur);
      var out = c.createGain(); env(c, out, t, 1.1, 1, Math.max(0.1, dur - 1.0), 1.5);
      lp.connect(out); send(c, g, out, 0.42, 0);
      ch.pad.forEach(function (m, k) {
        [-7, 7].forEach(function (det) {
          var o = c.createOscillator(); o.type = 'sawtooth'; o.frequency.value = mtof(m); o.detune.value = det + (k % 2 ? 2 : -2);
          var v = c.createGain(); v.gain.value = 0.036; o.connect(v); v.connect(lp); o.start(t); o.stop(t + dur + 2.8);
        });
      });
    }
    function subBass(c, g, t, dur, ch) {
      var o = c.createOscillator(); o.type = 'sine'; o.frequency.value = mtof(ch.root);
      var o2 = c.createOscillator(); o2.type = 'triangle'; o2.frequency.value = mtof(ch.root + 12);
      var v = c.createGain(), v2 = c.createGain(); v2.gain.value = 0.5; env(c, v, t, 0.1, 0.12, Math.max(0.1, dur - 0.6), 0.7);   // octave-up triangle keeps the bass audible on laptop / phone speakers
      o.connect(v); o2.connect(v2); v2.connect(v); send(c, g, v, 0, 0); o.start(t); o2.start(t); o.stop(t + dur + 1); o2.stop(t + dur + 1);
    }
    function pluck(c, g, t, midi, vel, p) {
      var f = mtof(midi), o = c.createOscillator(), o2 = c.createOscillator(); o.type = 'triangle'; o2.type = 'sine'; o.frequency.value = f; o2.frequency.value = f * 2;
      var lp = c.createBiquadFilter(); lp.type = 'lowpass'; lp.Q.value = 1.2; lp.frequency.setValueAtTime(5200, t); lp.frequency.exponentialRampToValueAtTime(1300, t + 0.34);
      var v = c.createGain(), v2 = c.createGain(); v2.gain.value = 0.3;
      v.gain.setValueAtTime(0.0001, t); v.gain.linearRampToValueAtTime(vel, t + 0.004); v.gain.exponentialRampToValueAtTime(0.0001, t + 0.62);
      o.connect(v); o2.connect(v2); v2.connect(v); v.connect(lp);
      var pn = pan(c, p), tail = pn ? (lp.connect(pn), pn) : lp; send(c, g, tail, 0.5, 0.5);
      o.start(t); o2.start(t); o.stop(t + 0.7); o2.stop(t + 0.7);
    }
    function pulse(c, g, t, vel) {
      var o = c.createOscillator(); o.type = 'sine'; o.frequency.setValueAtTime(130, t); o.frequency.exponentialRampToValueAtTime(46, t + 0.14);
      var v = c.createGain(); v.gain.setValueAtTime(0.0001, t); v.gain.linearRampToValueAtTime(vel, t + 0.006); v.gain.exponentialRampToValueAtTime(0.0001, t + 0.42);
      o.connect(v); send(c, g, v, 0, 0); o.start(t); o.stop(t + 0.5);
    }
    function tick(c, g, t, vel) {
      var s = c.createBufferSource(); s.buffer = noiseBuf; var hp = c.createBiquadFilter(); hp.type = 'highpass'; hp.frequency.value = 7200;
      var v = c.createGain(); v.gain.setValueAtTime(vel, t); v.gain.exponentialRampToValueAtTime(0.0001, t + 0.035);
      s.connect(hp); hp.connect(v); send(c, g, v, 0.12, 0); s.start(t, Math.random() * 0.5, 0.06);
    }
    function bell(c, g, t, midi, vel, dec) {
      var f = mtof(midi), car = c.createOscillator(), mod = c.createOscillator(), mg = c.createGain(), v = c.createGain();
      car.type = 'sine'; mod.type = 'sine'; car.frequency.value = f; mod.frequency.value = f * 3.5;
      mg.gain.setValueAtTime(f * 2.2, t); mg.gain.exponentialRampToValueAtTime(f * 0.05, t + dec * 0.6);
      v.gain.setValueAtTime(0.0001, t); v.gain.linearRampToValueAtTime(vel, t + 0.004); v.gain.exponentialRampToValueAtTime(0.0001, t + dec);
      mod.connect(mg); mg.connect(car.frequency); car.connect(v); send(c, g, v, 0.7, 0.25);
      car.start(t); mod.start(t); car.stop(t + dec + 0.1); mod.stop(t + dec + 0.1);
    }

    /* one eighth-note step of the score */
    function scheduleStep(c, g, n, t, prog) {
      var bar = Math.floor(n / 8), pos = n % 8, ch = CH[bar % 4], i;
      if (pos === 0) {
        padChord(c, g, t, STEP * 8, ch, prog);
        subBass(c, g, t, STEP * 8, ch);
        bell(c, g, t + 0.02, ch.arp[5] + 12, 0.042, 2.2);
      }
      if (bar > 0 || pos >= 2) {                                    // the arpeggio enters on the 2nd beat of the very first bar
        var idx = PAT[bar % 2][pos], vel = 0.078 + 0.07 * prog + (pos % 2 === 0 ? 0.016 : 0);
        if (!(bar === 0 && pos === 3)) pluck(c, g, t, ch.arp[idx], vel, (pos % 2 ? 0.28 : -0.28));
        if (prog > 0.45 && pos % 4 === 2) pluck(c, g, t + STEP * 0.5, ch.arp[idx] + 12, 0.042 + 0.04 * prog, 0);
      }
      if (bar > 0 && (pos === 0 || pos === 4)) pulse(c, g, t, pos === 0 ? 0.15 : 0.1);
      if (bar > 0 && (pos === 3 || pos === 7)) tick(c, g, t + STEP * 0.5, 0.014);
    }

    /* the resolve: bright Cmaj9 bells + swell + sub hit + whoosh, then fade */
    function finaleAt(c, g, t) {
      [72, 76, 79, 83, 86, 91].forEach(function (m, k) { bell(c, g, t + k * 0.05, m, 0.07 - k * 0.006, 2.8); });
      var lp = c.createBiquadFilter(); lp.type = 'lowpass'; lp.frequency.value = 2600; var out = c.createGain(); env(c, out, t, 0.12, 1, 0.6, 1.9); lp.connect(out); send(c, g, out, 0.5, 0);
      [48, 55, 64, 71, 74].forEach(function (m) { [-6, 6].forEach(function (d) { var o = c.createOscillator(); o.type = 'sawtooth'; o.frequency.value = mtof(m); o.detune.value = d; var v = c.createGain(); v.gain.value = 0.03; o.connect(v); v.connect(lp); o.start(t); o.stop(t + 3); }); });
      var s = c.createOscillator(); s.type = 'sine'; s.frequency.setValueAtTime(96, t); s.frequency.exponentialRampToValueAtTime(48, t + 0.5);
      var sv = c.createGain(); sv.gain.setValueAtTime(0.0001, t); sv.gain.linearRampToValueAtTime(0.3, t + 0.01); sv.gain.exponentialRampToValueAtTime(0.0001, t + 1.1); s.connect(sv); send(c, g, sv, 0.1, 0); s.start(t); s.stop(t + 1.2);
      var w = c.createBufferSource(); w.buffer = noiseBuf; var bp = c.createBiquadFilter(); bp.type = 'bandpass'; bp.Q.value = 1.1; bp.frequency.setValueAtTime(500, t - 0.45); bp.frequency.exponentialRampToValueAtTime(7000, t + 0.1);
      var wv = c.createGain(); wv.gain.setValueAtTime(0.0001, t - 0.45); wv.gain.linearRampToValueAtTime(0.1, t); wv.gain.exponentialRampToValueAtTime(0.0001, t + 0.35); w.connect(bp); bp.connect(wv); send(c, g, wv, 0.4, 0); w.start(Math.max(0, t - 0.45), 0, 0.9);
      g.master.gain.cancelScheduledValues(t); g.master.gain.setValueAtTime(VOL, t); g.master.gain.setTargetAtTime(0, t + 1.5, 0.55);
    }

    /* ---- live playback ---------------------------------------------------------------------------------------------- */
    function progNow() { return Math.min(1, shown / 100); }
    function pump() {
      if (!ctx || ended) return;
      var horizon = ctx.currentTime + 0.7;
      while (nextT < horizon) { scheduleStep(ctx, G, stepNo, nextT, progNow()); nextT += STEP; stepNo++; }
    }
    function begin() {
      if (started || ended || !ctx || ctx.state !== 'running') return;
      started = true; setState(pref === 'off' ? 'off' : 'on');
      var t0 = ctx.currentTime + 0.08; nextT = t0; stepNo = 0;
      G.master.gain.setValueAtTime(0, t0); if (pref !== 'off') G.master.gain.linearRampToValueAtTime(VOL, t0 + 1.4);
      pump(); timer = setInterval(pump, 120);
    }
    function ensureCtx() {
      if (ctx || !AC) return;
      try { ctx = new AC({ latencyHint: 'playback' }); } catch (e) { try { ctx = new AC(); } catch (e2) { ctx = null; return; } }
      G = build(ctx);
      ctx.onstatechange = function () { if (ctx && ctx.state === 'running' && !started && !ended && pref !== 'off') begin(); };
    }
    function startAudio() {
      if (ended || pref === 'off') return;
      if (fileSrc) {
        if (!audioEl) { audioEl = new Audio(fileSrc); audioEl.loop = true; audioEl.volume = 0.7; }
        var pr = audioEl.play();
        if (pr && pr.then) pr.then(function () { started = true; setState('on'); }, function (err) {
          if (err && err.name === 'NotAllowedError') { setState('blocked'); return; }          // waiting for a click / tap
          fileSrc = ''; audioEl = null; startAudio();                                          // file missing or unreadable: use the built-in music instead
        });
        return;
      }
      ensureCtx(); if (!ctx) { setState('off'); return; }
      var rp = ctx.resume && ctx.resume(); if (rp && rp.catch) rp.catch(function () { });
      setTimeout(function () { if (ctx && ctx.state === 'running') begin(); else if (!started) setState('blocked'); }, 220);
    }

    /* ---- the toggle button ------------------------------------------------------------------------------------------- */
    function setState(s) {
      state = s; el.classList.toggle('snd-blocked', s === 'blocked');
      if (!btn) return;
      btn.setAttribute('data-state', s);
      btn.setAttribute('aria-pressed', s === 'on' ? 'true' : 'false');
      var txt = s === 'on' ? 'Sound on' : s === 'off' ? 'Sound off' : 'Tap for sound';
      if (btnLabel) btnLabel.textContent = txt;
      btn.setAttribute('aria-label', s === 'on' ? 'Turn loading music off' : 'Turn loading music on');
    }
    function setPref(v) { pref = v; try { localStorage.setItem('nx.sound', v); } catch (e) { } }
    function mute(on) {
      if (audioEl) audioEl.muted = on;
      if (ctx && G && !ended) { var t = ctx.currentTime; G.master.gain.cancelScheduledValues(t); G.master.gain.setTargetAtTime(on ? 0 : VOL, t, 0.12); }
    }
    if (btn) btn.addEventListener('click', function () {
      if (ended) return;
      if (state === 'on') { setPref('off'); mute(true); setState('off'); return; }
      setPref('on');
      if (started) { mute(false); setState('on'); } else { startAudio(); }
    });

    /* browsers keep sound blocked until the visitor does something: the first click / tap / key starts the music */
    var gestureEvents = ['pointerdown', 'keydown', 'touchstart', 'click'];
    function onGesture(e) {
      if (started || ended || pref === 'off') return;
      if (btn && e && e.target && btn.contains(e.target)) return;
      startAudio();
    }
    gestureEvents.forEach(function (n) { window.addEventListener(n, onGesture, true); });
    function unhook() { gestureEvents.forEach(function (n) { window.removeEventListener(n, onGesture, true); }); }

    setState(pref === 'off' ? 'off' : 'on');
    if (!AC && !fileSrc) { setState('off'); }
    else if (pref !== 'off') startAudio();

    /* ---- public ---------------------------------------------------------------------------------------------------------- */
    function finale() {
      if (ended) return; ended = true; clearInterval(timer);
      if (audioEl) { var v = audioEl.volume, k = setInterval(function () { v -= 0.05; if (v <= 0) { clearInterval(k); audioEl.pause(); } else audioEl.volume = v; }, 90); return; }
      if (ctx && G && started && pref !== 'off') finaleAt(ctx, G, ctx.currentTime + 0.35);
    }
    function dispose() {
      unhook(); ended = true; clearInterval(timer);
      if (audioEl) { try { audioEl.pause(); } catch (e) { } }
      if (ctx) { var c = ctx; setTimeout(function () { try { c.close(); } catch (e) { } }, 3600); }   // let the closing chime ring out
    }

    /* offline render of the same score (used for testing / previews): returns an AudioBuffer */
    function renderOffline(seconds, finaleTime) {
      var OAC = window.OfflineAudioContext || window.webkitOfflineAudioContext, sr = 44100;
      var c = new OAC(2, Math.floor(sr * seconds), sr), g = build(c);
      g.master.gain.setValueAtTime(0, 0); g.master.gain.linearRampToValueAtTime(VOL, 1.4);
      var t = 0.05, n = 0; while (t < finaleTime) { scheduleStep(c, g, n, t, Math.min(1, t / finaleTime)); t += STEP; n++; }
      finaleAt(c, g, finaleTime);
      return c.startRendering();
    }
    return { finale: finale, dispose: dispose, renderOffline: renderOffline };
  })();
  api._test.renderOffline = Sound.renderOffline;

})();
