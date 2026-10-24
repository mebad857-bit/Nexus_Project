/* ============================================================================
   Nexus HERO — 3D network globe
   Pure Canvas 2D + hand-written 3D maths (no library, no WebGL, no network request).
   Presentation only: it draws into its own <canvas> inside [data-nx-globe] and touches nothing else.

   What you see
   - a frosted-glass sphere covered in a dotted world map, slowly turning
   - glowing "hub" cities; signal arcs are launched between them, travel as comets and ripple out at the destination
   - two tilted orbit rings with satellites that beam down to the nearest hub
   - drifting depth-dust around the globe; the globe leans towards the cursor and can be dragged (mouse) to spin it

   Switch off: remove the [data-nx-globe] block from Views/Home/Index.cshtml (the script then exits immediately).
   prefers-reduced-motion: a single still frame is drawn, nothing animates.
   ============================================================================ */
(function () {
  'use strict';

  var host = document.querySelector('[data-nx-globe]');
  if (!host) return;
  var canvas = host.querySelector('canvas');
  var ctx = canvas && canvas.getContext ? canvas.getContext('2d') : null;
  if (!ctx) return;

  var TAU = Math.PI * 2, DEG = Math.PI / 180;
  function mq(q) { try { return window.matchMedia ? window.matchMedia(q).matches : false; } catch (e) { return false; } }
  function clamp(v, a, b) { return v < a ? a : v > b ? b : v; }
  function lerp(a, b, t) { return a + (b - a) * t; }
  function easeInOut(t) { return t < 0.5 ? 2 * t * t : 1 - Math.pow(-2 * t + 2, 2) / 2; }
  function easeOut(t) { return 1 - Math.pow(1 - t, 3); }

  var reduce = mq('(prefers-reduced-motion: reduce)');
  var hero = host.closest('section') || host.parentElement || document.body;

  /* ---------------------------------------------------------------- loading screen hook (js/loader.js)
     While the loader is up the globe is a "3D asset that must be ready": the screen stays until the first frame is drawn.
     The spin-up (fast turn that settles) is saved for the moment the screen lifts, so visitors actually see it. */
  var NL = window.nxLoader, loaderUp = !!(NL && NL.active), globeReady = false;
  if (loaderUp) { NL.hold('globe', 'Preparing the 3D globe…'); setTimeout(markReady, 9000); }
  function markReady() { if (globeReady) return; globeReady = true; if (NL && NL.release) NL.release('globe'); }

  /* ---------------------------------------------------------------- palette (matches the blue → indigo → violet theme) */
  var BLUE = [37, 99, 235], SKY = [14, 165, 233], INDIGO = [79, 70, 229], VIOLET = [124, 58, 237], CYAN = [6, 182, 212];
  var LAND_COLORS = [[37, 99, 235], [58, 92, 238], [79, 70, 229], [101, 64, 233], [124, 58, 237]];
  var LAND_ALPHA = [0.34, 0.52, 0.72, 0.92];
  var ARC_PAIRS = [[BLUE, VIOLET], [CYAN, INDIGO], [INDIGO, VIOLET], [SKY, BLUE], [BLUE, INDIGO]];

  /* ---------------------------------------------------------------- land mask: 240 x 120 cells (1.5°), bit-packed, north → south */
  var LAND_B64 = 'AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAPgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAP//D///4AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAD//7////8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAH///////+AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAD/4P/////AAAAAAAAAAAAAOAAAAAAAAAAAAAAAAAAAAAf////AAAAAAAAAAAAP/gAAAAAAAAAAAAAAAAAAAAD///+AAAAAAAAAAAH//4AAAAAAAAAAAAAD/8AA4AB///+AAAAAAAAAAP/////gAAAAAAAAAAAf/4AB/AB///8AAAAAAAAAD//////7/+AAAAA//3/D/x8D/wA///wAAAA/4AAAH/////////8/ggD/////8f/+D8A//+AAAAD//h///////////////+H////////8D/A//gAAAAP//5////////////////H////////wP/Af+APwAAfx+D///////////////AH////////wB+AP8AHAAA/n////////////////4AD////////AAAAH4AAAAD/P////////////////gAB/9/////8APwABwAAAAH+H//////////////h8AAAfgD////8AH/AAAAAAAD/A////////////+ADgAAAfAA////8AH/AAAAAAIAOD////////////8AHgAAA4AAP////gH/gAAAAAOAuP////////////wAPAAAAAAAH////4P/wAAAAAyAgf////////////wAOAAAAAAAD////+f/8AAAABzH//////////////8AMAAAAAAAB/////f/+AAAAAHn//////////////8AIAAAAAAAA///////3AAAAAAf//////////////4AAAAAAAAAAP/////4HgAAAAB///////////////8AAAAAAAAAAf//+H/wDAAAAAD///////////////4AAAAAAAAAAf///x/8AAAAAAB///zfx/////////wAAAAAAAAAAf///8/4AAAAAAB/z/gPg/////////jAAAAAAAAAAf///+fgAAAAAA/wY/gDw////////8DAAAAAAAAAAf/////AAAAAAA/gGfHnw////////4AAAAAAAAAAAf////8AAAAAAA/ADY//4///////3wEAAAAAAAAAAP////8AAAAAAA/ACM//4///////AwEAAAAAAAAAAH////4AAAAAAAOPgAe/////////4wcAAAAAAAAAAH////4AAAAAAAN/gAA/////////wz4AAAAAAAAAAB////wAAAAAAAf/gAA/////////wDAAAAAAAAAAAA////gAAAAAAA///gB/////////4EAAAAAAAAAAAAf///AAAAAAAB///v9/////////4AAAAAAAAAAAAAX/5BAAAAAAAB////9/z///////4AAAAAAAAAAAAAL/wBgAAAAAAD////9/5///////4AAAAAAAAAAAAAB/gAgAAAAAAH////+/4P//////wAAAAAAAAAAAAAA/gAAAAAAAAP////+f8wH/////gAAAAAAAAAAAAAAfgAAAAAAAAP/////P/4D/////IAAAAAAAAAAAAAAPgA4AAAAAAf/////P/8D/+f/4AAAAAAAAAAAAAAAPgwMAAAAAAf/////n/4A/8P/AAAAAAAAAAAAAAAAHxgBwAAAAAf/////n/wAfwH+AAAAAAAAAAAAAAAAB/gAAAAAAAf/////z/gAfgH/AIAAAAAAAAAAAAAAADwAAAAAAAf/////7+AAfAB/AMAAAAAAAAAAAAAAAB8AAAAAAAf/////94AAPAA/gEAAAAAAAAAAAAAAAAMAAAAAAAf/////+AAAOAAvgAAAAAAAAAAAAAAAAAEBAAAAAAP//////8AAGAAvAAAAAAAAAAAAAAAAAACH/gAAAAH//////8AAGAAiAAAAAAAAAAAAAAAAAAB//wAAAAD//////4AABAAgADAAAAAAAAAAAAAAAAAP/4AAAAB//////4AABAAQADAAAAAAAAAAAAAAAAAP//AAAAA+D////wAAAAAYDgAAAAAAAAAAAAAAAAAP//wAAAAAA////gAAAACYHgAAAAAAAAAAAAAAAAAP//wAAAAAA////AAAAABIPgAAAAAAAAAAAAAAAAAf//4AAAAAA///+AAAAAAwfOAAAAAAAAAAAAAAAAA///4AAAAAA///8AAAAAAYfIAAAAAAAAAAAAAAAAAf///AAAAAAf//4AAAAAAMPID4AAAAAAAAAAAAAAA////8AAAAAf//4AAAAAAOCMA/AAAAAAAAAAAAAAA/////AAAAAP//wAAAAAACAAAPwAAAAAAAAAAAAAAf////gAAAAP//wAAAAAADwAAP4AAAAAAAAAAAAAAf////gAAAAP//wAAAAAAAMAAH4AAAAAAAAAAAAAAP////AAAAAH//wAAAAAAAAAAAMAAAAAAAAAAAAAAP////AAAAAH//4AAAAAAAAAAAAAAAAAAAAAAAAAAH///+AAAAAH//4AAAAAAAAAHiAAAAAAAAAAAAAAAD///+AAAAAH//4IAAAAAAAAfjAAAAAAAAAAAAAAAB///8AAAAAP//wYAAAAAAAB/jgAAAAAAAAAAAAAAA///8AAAAAP//h4AAAAAAAD/3wAAAAAAAAAAAAAAAf//8AAAAAP//B4AAAAAAAH//wAAAAAAAAAAAAAAAf//4AAAAAH/+BwAAAAAAAf//4AAAAAAAAAAAAAAAf//wAAAAAH/+BwAAAAAAD///8AAAAAAAAAAAAAAAf//gAAAAAD//BwAAAAAAD///+AAAAAAAAAAAAAAAf/+AAAAAAD/+BgAAAAAAD////AAAAAAAAAAAAAAAf/8AAAAAAD/8AAAAAAAAD////AAAAAAAAAAAAAAA//8AAAAAAD/8AAAAAAAAD////AAAAAAAAAAAAAAA//4AAAAAAB/4AAAAAAAAD////AAAAAAAAAAAAAAA//wAAAAAAA/wAAAAAAAAD////AAAAAAAAAAAAAAA//gAAAAAAA/gAAAAAAAAB/j/+AAAAAAAAAAAAAAA//AAAAAAAA+AAAAAAAAAB8Af+AAAAAAAAAAAAAAB/8AAAAAAAAAAAAAAAAAAAAAP+AAQAAAAAAAAAAAB/8AAAAAAAAAAAAAAAAAAAAAH8AAYAAAAAAAAAAAB/4AAAAAAAAAAAAAAAAAAAAABwAAOAAAAAAAAAAAB/gAAAAAAAAAAAAAAAAAAAAAAAAAEAAAAAAAAAAAB+AAAAAAAAAAAAAAAAAAAAAAAYAAQAAAAAAAAAAAB+AAAAAAAAAAAAAAAAAAAAAAAYAAwAAAAAAAAAAAB+AAAAAAAAAAAAAAAAAAAAAAAAADgAAAAAAAAAAAD8AAAAAAAAAAAAAAAAAAAAAAAAAHAAAAAAAAAAAAD4AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAD4AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAADwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAADgAAAAAAAAAAAAAAD///////AAAAAAAAAAAAAAAA/AAAAAAAAAAAA////////////gAAAAAAAAAAAAA/+AAAAAAAB/////////////////gAAAAAAAAAAA//+AAAAA/////////////////////w8AAAAAAAA////AAA/////////////////////////+AAAAH//////gB////////////////////////////h/////////3//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////';
  var LW = 240, LH = 120, landBits = null;
  try {
    var bin = atob(LAND_B64);
    landBits = new Uint8Array(bin.length);
    for (var bi = 0; bi < bin.length; bi++) landBits[bi] = bin.charCodeAt(bi);
  } catch (e) { landBits = null; }
  function isLand(lat, lon) {
    if (!landBits) return false;
    var col = Math.floor((lon + 180) / 360 * LW), row = Math.floor((90 - lat) / 180 * LH);
    col = ((col % LW) + LW) % LW; row = clamp(row, 0, LH - 1);
    var idx = row * LW + col;
    return (landBits[idx >> 3] >> (7 - (idx & 7))) & 1;
  }

  /* ---------------------------------------------------------------- small 3D helpers (row-major 3x3) */
  function mul(a, b) {
    return [
      a[0] * b[0] + a[1] * b[3] + a[2] * b[6], a[0] * b[1] + a[1] * b[4] + a[2] * b[7], a[0] * b[2] + a[1] * b[5] + a[2] * b[8],
      a[3] * b[0] + a[4] * b[3] + a[5] * b[6], a[3] * b[1] + a[4] * b[4] + a[5] * b[7], a[3] * b[2] + a[4] * b[5] + a[5] * b[8],
      a[6] * b[0] + a[7] * b[3] + a[8] * b[6], a[6] * b[1] + a[7] * b[4] + a[8] * b[7], a[6] * b[2] + a[7] * b[5] + a[8] * b[8]
    ];
  }
  function rotY(a) { var c = Math.cos(a), s = Math.sin(a); return [c, 0, s, 0, 1, 0, -s, 0, c]; }
  function rotX(a) { var c = Math.cos(a), s = Math.sin(a); return [1, 0, 0, 0, c, -s, 0, s, c]; }
  function rotZ(a) { var c = Math.cos(a), s = Math.sin(a); return [c, -s, 0, s, c, 0, 0, 0, 1]; }
  function ll(lat, lon) { var la = lat * DEG, lo = lon * DEG, c = Math.cos(la); return [c * Math.sin(lo), Math.sin(la), c * Math.cos(lo)]; }
  function dot(a, b) { return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]; }
  function cross(a, b) { return [a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0]]; }
  function norm(a) { var l = Math.sqrt(dot(a, a)) || 1; return [a[0] / l, a[1] / l, a[2] / l]; }

  /* ---------------------------------------------------------------- the dotted sphere (Fibonacci lattice, split into land / sea) */
  var landPts = [], seaPts = [];
  (function buildSphere() {
    var N = 5600, golden = Math.PI * (3 - Math.sqrt(5));
    for (var i = 0; i < N; i++) {
      var y = 1 - 2 * (i + 0.5) / N, r = Math.sqrt(1 - y * y), th = golden * i;
      var x = Math.cos(th) * r, z = Math.sin(th) * r;
      var lat = Math.asin(y) / DEG, lon = Math.atan2(x, z) / DEG;
      (isLand(lat, lon) ? landPts : seaPts).push(x, y, z);
    }
  })();
  var landN = landPts.length / 3, seaN = seaPts.length / 3;
  var landXYZ = new Float32Array(landPts), seaXYZ = new Float32Array(seaPts);

  /* graticule: parallels every 30° and meridians every 30° */
  var gratLines = (function () {
    var lines = [], k, j, pts;
    for (k = -60; k <= 60; k += 30) { pts = []; for (j = 0; j <= 72; j++) pts.push(ll(k, -180 + j * 5)); lines.push(pts); }
    for (k = 0; k < 360; k += 30) { pts = []; for (j = 0; j <= 36; j++) pts.push(ll(-90 + j * 5, k)); lines.push(pts); }
    return lines;
  })();

  /* ---------------------------------------------------------------- network hubs */
  var HUB_DEF = [
    [40.7, -74.0], [37.8, -122.4], [19.4, -99.1], [4.7, -74.1], [-23.5, -46.6], [51.5, -0.1], [50.1, 8.7], [55.7, 37.6],
    [6.5, 3.4], [30.0, 31.2], [-1.3, 36.8], [-26.2, 28.0], [25.2, 55.3], [24.9, 67.0], [19.1, 72.9], [1.35, 103.8],
    [39.9, 116.4], [22.3, 114.2], [35.7, 139.7], [-33.9, 151.2]
  ];
  var hubs = HUB_DEF.map(function (h, i) {
    var v = ll(h[0], h[1]);
    var e1 = norm(cross(v, Math.abs(v[1]) > 0.95 ? [1, 0, 0] : [0, 1, 0]));
    var e2 = cross(v, e1);
    return { v: v, e1: e1, e2: e2, heat: 0, ph: i * 1.37, vx: 0, vy: 0, vz: 0 };
  });

  /* ---------------------------------------------------------------- orbit rings + satellites (rings do not spin with the Earth) */
  var RING_N = 120;
  var RINGS = [
    { rr: 1.34, inc: 62 * DEG, node: -28 * DEG, dashed: false },
    { rr: 1.56, inc: 112 * DEG, node: 36 * DEG, dashed: true }
  ];
  RINGS.forEach(function (r) {
    r.m = mul(rotY(r.node), rotX(r.inc));
    r.pts = new Float32Array((RING_N + 1) * 3);
    for (var i = 0; i <= RING_N; i++) {
      var th = i / RING_N * TAU, p = [Math.cos(th), 0, Math.sin(th)];
      r.pts[i * 3] = r.m[0] * p[0] + r.m[2] * p[2];
      r.pts[i * 3 + 1] = r.m[3] * p[0] + r.m[5] * p[2];
      r.pts[i * 3 + 2] = r.m[6] * p[0] + r.m[8] * p[2];
    }
  });
  var SATS = [
    { ring: 0, ph: 0.4, sp: 0.34 }, { ring: 0, ph: 3.5, sp: 0.34 },
    { ring: 1, ph: 1.7, sp: -0.23 }, { ring: 1, ph: 4.6, sp: -0.23 }
  ];
  function ringPoint(ring, th) {
    var m = RINGS[ring].m, c = Math.cos(th), s = Math.sin(th);
    return [m[0] * c + m[2] * s, m[3] * c + m[5] * s, m[6] * c + m[8] * s];
  }

  /* ---------------------------------------------------------------- depth dust */
  var dust = [];
  (function () {
    for (var i = 0; i < 90; i++) {
      var u = Math.random() * 2 - 1, th = Math.random() * TAU, r = Math.sqrt(1 - u * u), rad = 1.22 + Math.random() * 1.15;
      dust.push({ v: [r * Math.cos(th), u, r * Math.sin(th)], rad: rad, size: 0.5 + Math.random() * 1.1, ph: Math.random() * TAU, sp: 0.6 + Math.random() * 1.6, c: Math.random() < 0.55 ? BLUE : (Math.random() < 0.5 ? VIOLET : CYAN) });
    }
  })();

  /* ---------------------------------------------------------------- glow sprites (drawn instead of per-frame gradients) */
  var glowCache = {};
  function glowSprite(rgb) {
    var k = rgb.join(',');
    if (glowCache[k]) return glowCache[k];
    var c = document.createElement('canvas'); c.width = c.height = 64;
    var g = c.getContext('2d'), gr = g.createRadialGradient(32, 32, 0, 32, 32, 32);
    gr.addColorStop(0, 'rgba(' + k + ',1)'); gr.addColorStop(0.22, 'rgba(' + k + ',0.55)');
    gr.addColorStop(0.55, 'rgba(' + k + ',0.14)'); gr.addColorStop(1, 'rgba(' + k + ',0)');
    g.fillStyle = gr; g.fillRect(0, 0, 64, 64);
    return (glowCache[k] = c);
  }
  function glow(x, y, r, rgb, a) {
    if (a <= 0.004 || r <= 0.2) return;
    ctx.globalAlpha = a > 1 ? 1 : a;
    ctx.drawImage(glowSprite(rgb), x - r, y - r, r * 2, r * 2);
    ctx.globalAlpha = 1;
  }

  /* ---------------------------------------------------------------- state */
  var S = { size: 0, cx: 0, cy: 0, R: 0, dotR: 1 };
  var BASE_PITCH = 0.42, BASE_ROLL = -0.28;
  var view = { yaw: -67 * DEG, userPitch: 0 };
  var mouse = { tx: 0, ty: 0, x: 0, y: 0 };
  var drag = { on: false, x: 0, y: 0, vyaw: 0, endAt: -9 };
  var arcs = [], ripples = [];
  var t = 0, last = 0, rafId = 0, running = false, liteFrame = false, nextSpawn = 0, M = rotY(0), Mv = rotY(0), introT0 = loaderUp ? 1e9 : 0;
  var quality = 2, slowAvg = 16, lastAdapt = 0, warm = 0, goodSince = 0;   // quality 2 = full, 1 = lighter canvas, 0 = lightest (adapts to slow machines)
  var LX = -0.42, LY = 0.52, LZ = 0.74;
  (function () { var l = Math.sqrt(LX * LX + LY * LY + LZ * LZ); LX /= l; LY /= l; LZ /= l; })();

  /* ---------------------------------------------------------------- arcs & ripples */
  function spawnArc(still) {
    for (var tries = 0; tries < 40; tries++) {
      var ai = (Math.random() * hubs.length) | 0, bi2 = (Math.random() * hubs.length) | 0;
      if (ai === bi2) continue;
      var A = hubs[ai], B = hubs[bi2];
      if (!still && (A.vz < 0.0 || B.vz < -0.3)) continue;
      var om = Math.acos(clamp(dot(A.v, B.v), -1, 1));
      if (om < 0.42 || om > 2.05) continue;
      var n = 44, pts = new Float32Array((n + 1) * 3), so = Math.sin(om), hgt = 0.05 + 0.27 * (om / Math.PI);
      for (var i = 0; i <= n; i++) {
        var u = i / n, s1 = Math.sin((1 - u) * om) / so, s2 = Math.sin(u * om) / so, rad = 1 + hgt * Math.sin(Math.PI * u);
        pts[i * 3] = (A.v[0] * s1 + B.v[0] * s2) * rad;
        pts[i * 3 + 1] = (A.v[1] * s1 + B.v[1] * s2) * rad;
        pts[i * 3 + 2] = (A.v[2] * s1 + B.v[2] * s2) * rad;
      }
      arcs.push({ a: ai, b: bi2, pts: pts, n: n, t0: t, dur: 4.2 + Math.random() * 1.6, c: ARC_PAIRS[(Math.random() * ARC_PAIRS.length) | 0], arrived: false, still: !!still });
      if (!still) { addRipple(ai, 1.9, 0.12); A.heat = 1; }
      return true;
    }
    return false;
  }
  function addRipple(hubIdx, dur, max) { ripples.push({ h: hubIdx, t0: t, dur: dur, max: max }); }

  /* ---------------------------------------------------------------- sizing */
  function resize() {
    var w = host.clientWidth, h = host.clientHeight, size = Math.round(Math.min(w, h));
    if (size < 40) { S.size = 0; return; }
    var dpr = Math.min(window.devicePixelRatio || 1, quality >= 2 ? 2 : quality === 1 ? 1.25 : 1);
    if (size * size * dpr * dpr > 4.2e6) dpr = Math.sqrt(4.2e6 / (size * size));
    canvas.width = canvas.height = Math.max(1, Math.round(size * dpr));
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    S.size = size; S.cx = S.cy = size / 2; S.R = size * 0.305; S.dotR = S.R * 0.0098;
    if (!running) render(t);
  }

  /* ---------------------------------------------------------------- update (physics of the view) */
  function update(dt) {
    var intro = clamp((t - introT0) / 2.6, 0, 1), e = easeOut(intro);
    var auto = 0.105 + 2.1 * (1 - e) * (1 - e);                      // fast spin-up that settles into a calm turn
    if (drag.on) {
      /* position is set by the pointer handler */
    } else {
      var resume = clamp((t - drag.endAt - 0.6) / 1.6, 0, 1);
      view.yaw += (drag.vyaw + auto * (drag.endAt < 0 ? 1 : resume)) * dt;
      drag.vyaw *= Math.exp(-dt * 2.4);
      view.userPitch *= Math.exp(-dt * 0.8);
    }
    var k = 1 - Math.exp(-dt * 3.2);
    mouse.x += (mouse.tx - mouse.x) * k; mouse.y += (mouse.ty - mouse.y) * k;
  }

  /* ---------------------------------------------------------------- render */
  function render(time) {
    var size = S.size; if (!size) return;
    var cx = S.cx, cy = S.cy, R = S.R, i, j, g;

    var pitch = BASE_PITCH + view.userPitch - mouse.y * 0.11, yawM = mouse.x * 0.17;
    var Rzx = mul(rotZ(BASE_ROLL), rotX(pitch));
    M = mul(Rzx, rotY(view.yaw + yawM));
    Mv = mul(Rzx, rotY(yawM));
    var m0 = M[0], m1 = M[1], m2 = M[2], m3 = M[3], m4 = M[4], m5 = M[5], m6 = M[6], m7 = M[7], m8 = M[8];

    ctx.clearRect(0, 0, size, size);
    ctx.lineCap = 'round'; ctx.lineJoin = 'round';

    /* hubs in view space (needed by arcs, satellites, spawn logic) */
    for (i = 0; i < hubs.length; i++) {
      var hv = hubs[i].v;
      hubs[i].vx = m0 * hv[0] + m1 * hv[1] + m2 * hv[2];
      hubs[i].vy = m3 * hv[0] + m4 * hv[1] + m5 * hv[2];
      hubs[i].vz = m6 * hv[0] + m7 * hv[1] + m8 * hv[2];
    }

    /* 1 — atmosphere */
    g = ctx.createRadialGradient(cx, cy, R * 0.9, cx, cy, R * 1.45);
    g.addColorStop(0, 'rgba(99,102,241,0.30)'); g.addColorStop(0.4, 'rgba(129,140,248,0.13)'); g.addColorStop(1, 'rgba(129,140,248,0)');
    ctx.fillStyle = g; ctx.beginPath(); ctx.arc(cx, cy, R * 1.45, 0, TAU); ctx.fill();

    /* 2 — back half of the dust + orbit rings (behind the glass) */
    drawDust(time, false);
    drawRings(time, false);

    /* 3 — glass body */
    g = ctx.createRadialGradient(cx - R * 0.34, cy - R * 0.38, R * 0.05, cx, cy, R);
    g.addColorStop(0, 'rgba(255,255,255,0.82)'); g.addColorStop(0.45, 'rgba(226,232,255,0.54)');
    g.addColorStop(0.82, 'rgba(165,180,252,0.40)'); g.addColorStop(1, 'rgba(99,102,241,0.36)');
    ctx.fillStyle = g; ctx.beginPath(); ctx.arc(cx, cy, R, 0, TAU); ctx.fill();

    ctx.save();
    ctx.beginPath(); ctx.arc(cx, cy, R, 0, TAU); ctx.clip();
    g = ctx.createLinearGradient(cx - R * 0.6, cy - R * 0.6, cx + R * 0.8, cy + R * 0.95);
    g.addColorStop(0, 'rgba(255,255,255,0)'); g.addColorStop(0.55, 'rgba(124,58,237,0)'); g.addColorStop(1, 'rgba(91,33,182,0.24)');
    ctx.fillStyle = g; ctx.fillRect(cx - R, cy - R, R * 2, R * 2);

    /* 4 — graticule */
    ctx.strokeStyle = 'rgba(79,70,229,0.13)'; ctx.lineWidth = 0.8; ctx.beginPath();
    for (i = 0; i < gratLines.length; i++) {
      var ln = gratLines[i], pen = false;
      for (j = 0; j < ln.length; j++) {
        var q = ln[j], gx = m0 * q[0] + m1 * q[1] + m2 * q[2], gy = m3 * q[0] + m4 * q[1] + m5 * q[2], gz = m6 * q[0] + m7 * q[1] + m8 * q[2];
        if (gz > 0.02) { if (pen) ctx.lineTo(cx + gx * R, cy - gy * R); else { ctx.moveTo(cx + gx * R, cy - gy * R); pen = true; } } else pen = false;
      }
    }
    ctx.stroke();

    /* 5 — sea dots (faint), back-side land (ghost through the glass), front land (bright, bucketed by colour/brightness) */
    var dr = S.dotR, px, py, pz, x, y, z, sx, sy;
    ctx.fillStyle = 'rgba(79,70,229,0.11)'; ctx.beginPath();
    for (i = 0; quality >= 2 && i < seaN; i++) {
      px = seaXYZ[i * 3]; py = seaXYZ[i * 3 + 1]; pz = seaXYZ[i * 3 + 2];
      z = m6 * px + m7 * py + m8 * pz; if (z < 0.06) continue;
      x = m0 * px + m1 * py + m2 * pz; y = m3 * px + m4 * py + m5 * pz;
      sx = cx + x * R; sy = cy - y * R; var sr = dr * 0.34 * (0.5 + 0.5 * z);
      ctx.moveTo(sx + sr, sy); ctx.arc(sx, sy, sr, 0, TAU);
    }
    ctx.fill();

    var buckets = drawLandBuckets, bk;
    for (bk = 0; bk < buckets.length; bk++) buckets[bk].length = 0;
    ctx.fillStyle = 'rgba(99,102,241,0.085)'; ctx.beginPath();
    var landStep = quality >= 2 ? 1 : quality === 1 ? 2 : 3;
    for (i = 0; i < landN; i += landStep) {
      px = landXYZ[i * 3]; py = landXYZ[i * 3 + 1]; pz = landXYZ[i * 3 + 2];
      z = m6 * px + m7 * py + m8 * pz;
      x = m0 * px + m1 * py + m2 * pz; y = m3 * px + m4 * py + m5 * pz;
      sx = cx + x * R; sy = cy - y * R;
      if (z <= 0) { var gr2 = dr * 0.55; ctx.moveTo(sx + gr2, sy); ctx.arc(sx, sy, gr2, 0, TAU); continue; }
      var b = x * LX + y * LY + z * LZ; if (b < 0) b = 0;
      var a = 0.26 + 0.74 * b, ab = a > 0.82 ? 3 : a > 0.62 ? 2 : a > 0.43 ? 1 : 0;
      var tc = 0.5 + x * 0.36 - y * 0.3, cb = tc < 0.2 ? 0 : tc < 0.4 ? 1 : tc < 0.6 ? 2 : tc < 0.8 ? 3 : 4;
      buckets[ab * 5 + cb].push(sx, sy, dr * (0.5 + 0.5 * Math.pow(z, 0.55)));
    }
    ctx.fill();
    for (bk = 0; bk < buckets.length; bk++) {
      var arr = buckets[bk]; if (!arr.length) continue;
      var col = LAND_COLORS[bk % 5], al = LAND_ALPHA[(bk / 5) | 0];
      ctx.fillStyle = 'rgba(' + col[0] + ',' + col[1] + ',' + col[2] + ',' + al + ')'; ctx.beginPath();
      for (j = 0; j < arr.length; j += 3) { ctx.moveTo(arr[j] + arr[j + 2], arr[j + 1]); ctx.arc(arr[j], arr[j + 1], arr[j + 2], 0, TAU); }
      ctx.fill();
    }

    /* 6 — ripples, arcs, hubs (all follow the Earth) */
    drawRipples(time);
    drawArcs(time);
    drawHubs(time);
    ctx.restore();

    /* 7 — glass finish: fresnel rim, outline, specular highlight */
    g = ctx.createRadialGradient(cx, cy, R * 0.82, cx, cy, R);
    g.addColorStop(0, 'rgba(255,255,255,0)'); g.addColorStop(1, 'rgba(255,255,255,0.46)');
    ctx.fillStyle = g; ctx.beginPath(); ctx.arc(cx, cy, R, 0, TAU); ctx.fill();
    g = ctx.createLinearGradient(cx - R, cy - R, cx + R, cy + R);
    g.addColorStop(0, 'rgba(255,255,255,0.95)'); g.addColorStop(0.5, 'rgba(165,180,252,0.55)'); g.addColorStop(1, 'rgba(99,102,241,0.6)');
    ctx.strokeStyle = g; ctx.lineWidth = 1.3; ctx.beginPath(); ctx.arc(cx, cy, R - 0.6, 0, TAU); ctx.stroke();

    ctx.save();
    ctx.beginPath(); ctx.arc(cx, cy, R, 0, TAU); ctx.clip();
    ctx.translate(cx - R * 0.37, cy - R * 0.52); ctx.rotate(-0.62); ctx.scale(1, 0.42);
    g = ctx.createRadialGradient(0, 0, 0, 0, 0, R * 0.46);
    g.addColorStop(0, 'rgba(255,255,255,0.72)'); g.addColorStop(1, 'rgba(255,255,255,0)');
    ctx.fillStyle = g; ctx.beginPath(); ctx.arc(0, 0, R * 0.46, 0, TAU); ctx.fill();
    ctx.restore();

    /* 8 — front half of rings, satellites, dust */
    drawRings(time, true);
    drawSatellites(time);
    drawDust(time, true);
  }
  var drawLandBuckets = []; for (var bq = 0; bq < 20; bq++) drawLandBuckets.push([]);

  /* ---- ripples: circles on the surface around a hub, foreshortened by the 3D rotation ---- */
  function drawRipples(time) {
    var cx = S.cx, cy = S.cy, R = S.R, i, k;
    ctx.lineWidth = 1.3;
    /* idle pulse on every hub */
    for (i = 0; i < hubs.length; i++) {
      var h = hubs[i]; if (h.vz < 0.05) continue;
      var ph = ((time * 0.28 + h.ph) % 1);
      ringOnSurface(h, 0.02 + ph * 0.075, (1 - ph) * 0.34 * Math.min(1, h.vz * 2), INDIGO);
    }
    /* event ripples (arc launch / arrival) */
    for (i = ripples.length - 1; i >= 0; i--) {
      var r = ripples[i], p = (time - r.t0) / r.dur;
      if (p >= 1) { ripples.splice(i, 1); continue; }
      var hb = hubs[r.h]; if (hb.vz < 0.0) continue;
      ringOnSurface(hb, 0.015 + easeOut(p) * r.max, Math.pow(1 - p, 1.4) * 0.8, VIOLET);
      if (p < 0.5) ringOnSurface(hb, 0.015 + easeOut(p * 1.6) * r.max * 0.6, Math.pow(1 - p * 2, 1.2) * 0.55, BLUE);
    }
  }
  function ringOnSurface(h, rho, alpha, rgb) {
    if (alpha <= 0.01) return;
    var cx = S.cx, cy = S.cy, R = S.R, N = 40, k, pen = false, c = Math.cos(rho), s = Math.sin(rho);
    var m = M;
    ctx.strokeStyle = 'rgba(' + rgb[0] + ',' + rgb[1] + ',' + rgb[2] + ',' + alpha.toFixed(3) + ')';
    ctx.beginPath();
    for (k = 0; k <= N; k++) {
      var f = k / N * TAU, cf = Math.cos(f), sf = Math.sin(f);
      var vx = c * h.v[0] + s * (cf * h.e1[0] + sf * h.e2[0]), vy = c * h.v[1] + s * (cf * h.e1[1] + sf * h.e2[1]), vz = c * h.v[2] + s * (cf * h.e1[2] + sf * h.e2[2]);
      var x = m[0] * vx + m[1] * vy + m[2] * vz, y = m[3] * vx + m[4] * vy + m[5] * vz, z = m[6] * vx + m[7] * vy + m[8] * vz;
      if (z > 0.02) { if (pen) ctx.lineTo(cx + x * R, cy - y * R); else { ctx.moveTo(cx + x * R, cy - y * R); pen = true; } } else pen = false;
    }
    ctx.stroke();
  }

  /* ---- arcs: comets that grow from source to destination, then fade from the tail ---- */
  function drawArcs(time) {
    var cx = S.cx, cy = S.cy, R = S.R, m = M, i, j;
    for (i = arcs.length - 1; i >= 0; i--) {
      var A = arcs[i], p = (time - A.t0) / A.dur, head, tail;
      if (A.still) { head = 1; tail = 0; p = 0.5; }
      else {
        if (p >= 1) { arcs.splice(i, 1); continue; }
        head = easeInOut(clamp(p / 0.46, 0, 1)); tail = easeInOut(clamp((p - 0.5) / 0.5, 0, 1));
        if (!A.arrived && head >= 0.999) { A.arrived = true; addRipple(A.b, 2.5, 0.2); hubs[A.b].heat = 1; }
      }
      var n = A.n, i0 = Math.floor(tail * n), i1 = Math.min(n, Math.ceil(head * n)), pts = A.pts;
      if (i1 <= i0) continue;
      var span = Math.max(0.0001, head - tail), cA = A.c[0], cB = A.c[1];
      var px0 = 0, py0 = 0, vis0 = false, have = false;
      for (j = i0; j <= i1; j++) {
        var ax = pts[j * 3], ay = pts[j * 3 + 1], az = pts[j * 3 + 2];
        var x = m[0] * ax + m[1] * ay + m[2] * az, y = m[3] * ax + m[4] * ay + m[5] * az, z = m[6] * ax + m[7] * ay + m[8] * az;
        var sx = cx + x * R, sy = cy - y * R, vis = z > 0 || (x * x + y * y) > 1;
        if (have) {
          var u = j / n, w = clamp((u - tail) / span, 0, 1), fade = A.still ? 0.8 : 1;
          var al = (0.1 + 0.82 * Math.pow(w, 1.25)) * fade * ((vis && vis0) ? 1 : 0.13);
          var rr = Math.round(lerp(cA[0], cB[0], u)), gg = Math.round(lerp(cA[1], cB[1], u)), bb = Math.round(lerp(cA[2], cB[2], u));
          ctx.strokeStyle = 'rgba(' + rr + ',' + gg + ',' + bb + ',' + al.toFixed(3) + ')';
          ctx.lineWidth = (0.9 + 1.5 * w) * (R / 260 > 0.6 ? 1 : 0.8);
          ctx.beginPath(); ctx.moveTo(px0, py0); ctx.lineTo(sx, sy); ctx.stroke();
        }
        px0 = sx; py0 = sy; vis0 = vis; have = true;
        if (j === i1 && !A.still && head < 0.999 && vis) {            // comet head
          glow(sx, sy, 15 * (R / 260), cB, 0.85);
          ctx.fillStyle = 'rgba(255,255,255,0.95)'; ctx.beginPath(); ctx.arc(sx, sy, 1.9 * (R / 260 > 0.6 ? 1 : 0.8), 0, TAU); ctx.fill();
        }
      }
    }
  }

  /* ---- hub markers ---- */
  function drawHubs(time) {
    var cx = S.cx, cy = S.cy, R = S.R, i, k = R / 260;
    for (i = 0; i < hubs.length; i++) {
      var h = hubs[i]; h.heat *= 0.985;
      if (h.vz < 0.03) continue;
      var sx = cx + h.vx * R, sy = cy - h.vy * R, f = Math.min(1, h.vz * 1.6);
      glow(sx, sy, (11 + 17 * h.heat) * k, h.heat > 0.3 ? VIOLET : BLUE, (0.42 + 0.5 * h.heat) * f);
      ctx.fillStyle = 'rgba(79,70,229,' + (0.9 * f).toFixed(3) + ')'; ctx.beginPath(); ctx.arc(sx, sy, 3.5 * k * (0.7 + 0.3 * f) + h.heat * 1.3, 0, TAU); ctx.fill();
      ctx.fillStyle = 'rgba(255,255,255,' + (0.98 * f).toFixed(3) + ')'; ctx.beginPath(); ctx.arc(sx, sy, 1.35 * k * (0.7 + 0.3 * f) + 0.3, 0, TAU); ctx.fill();
    }
  }

  /* ---- orbit rings ---- */
  function drawRings(time, front) {
    var cx = S.cx, cy = S.cy, R = S.R, m = Mv, ri, i;
    for (ri = 0; ri < RINGS.length; ri++) {
      var ring = RINGS[ri], rr = ring.rr, pts = ring.pts, pen = false;
      ctx.beginPath();
      for (i = 0; i <= RING_N; i++) {
        var ax = pts[i * 3] * rr, ay = pts[i * 3 + 1] * rr, az = pts[i * 3 + 2] * rr;
        var x = m[0] * ax + m[1] * ay + m[2] * az, y = m[3] * ax + m[4] * ay + m[5] * az, z = m[6] * ax + m[7] * ay + m[8] * az;
        var isFront = z >= 0;
        if (isFront === front) { if (pen) ctx.lineTo(cx + x * R, cy - y * R); else { ctx.moveTo(cx + x * R, cy - y * R); pen = true; } } else pen = false;
      }
      if (ring.dashed) { ctx.setLineDash([1.2, 7.5]); ctx.lineWidth = 1.9; ctx.strokeStyle = front ? 'rgba(109,64,235,0.62)' : 'rgba(109,64,235,0.26)'; }
      else { ctx.setLineDash([]); ctx.lineWidth = 1.05; ctx.strokeStyle = front ? 'rgba(59,92,240,0.42)' : 'rgba(59,92,240,0.17)'; }
      ctx.stroke(); ctx.setLineDash([]);
    }
  }

  /* ---- satellites + the beam each one drops on the nearest hub ---- */
  function drawSatellites(time) {
    var cx = S.cx, cy = S.cy, R = S.R, m = Mv, k = R / 260, si, i;
    for (si = 0; si < SATS.length; si++) {
      var sat = SATS[si], rr = RINGS[sat.ring].rr, th = sat.ph + sat.sp * time;
      var p = ringPoint(sat.ring, th);
      var x = (m[0] * p[0] + m[1] * p[1] + m[2] * p[2]) * rr, y = (m[3] * p[0] + m[4] * p[1] + m[5] * p[2]) * rr, z = (m[6] * p[0] + m[7] * p[1] + m[8] * p[2]) * rr;
      var sx = cx + x * R, sy = cy - y * R;
      var hidden = z < 0 && (x * x + y * y) < 1;
      if (hidden) continue;
      var front = z >= 0, a = front ? 1 : 0.45;

      /* trail */
      var sgn = sat.sp > 0 ? -1 : 1, tpx = sx, tpy = sy;
      for (i = 1; i <= 14; i++) {
        var q = ringPoint(sat.ring, th + sgn * i * 0.045);
        var qx = (m[0] * q[0] + m[1] * q[1] + m[2] * q[2]) * rr, qy = (m[3] * q[0] + m[4] * q[1] + m[5] * q[2]) * rr, qz = (m[6] * q[0] + m[7] * q[1] + m[8] * q[2]) * rr;
        if (qz < 0 && (qx * qx + qy * qy) < 1) break;
        var qsx = cx + qx * R, qsy = cy - qy * R;
        ctx.strokeStyle = 'rgba(99,102,241,' + (0.5 * (1 - i / 15) * a).toFixed(3) + ')'; ctx.lineWidth = 1.6 * k * (1 - i / 18) + 0.3;
        ctx.beginPath(); ctx.moveTo(tpx, tpy); ctx.lineTo(qsx, qsy); ctx.stroke(); tpx = qsx; tpy = qsy;
      }

      /* beam to nearest visible hub (front side only) */
      if (front) {
        var len = Math.sqrt(x * x + y * y + z * z), nx = x / len, ny = y / len, nz = z / len, best = -1, bd = 0.93;
        for (i = 0; i < hubs.length; i++) {
          var h = hubs[i]; if (h.vz < 0.12) continue;
          var d = nx * h.vx + ny * h.vy + nz * h.vz; if (d > bd) { bd = d; best = i; }
        }
        if (best >= 0) {
          var hb = hubs[best], hx = cx + hb.vx * R, hy = cy - hb.vy * R, pulse = 0.5 + 0.5 * Math.sin(time * 3 + si);
          var gl = ctx.createLinearGradient(sx, sy, hx, hy);
          gl.addColorStop(0, 'rgba(124,58,237,' + (0.5 * (0.6 + 0.4 * pulse)).toFixed(3) + ')'); gl.addColorStop(1, 'rgba(37,99,235,0.08)');
          ctx.strokeStyle = gl; ctx.lineWidth = 1; ctx.setLineDash([3, 4]); ctx.lineDashOffset = -time * 14;
          ctx.beginPath(); ctx.moveTo(sx, sy); ctx.lineTo(hx, hy); ctx.stroke(); ctx.setLineDash([]); ctx.lineDashOffset = 0;
          glow(hx, hy, 12 * k, VIOLET, 0.25 * pulse);
        }
      }

      /* body: glowing diamond with solar-panel wings */
      glow(sx, sy, 13 * k, front ? VIOLET : INDIGO, 0.55 * a);
      ctx.save(); ctx.translate(sx, sy); ctx.rotate(0.785 + th * 0.2);
      var s = 3.0 * k + 1.0;
      ctx.fillStyle = 'rgba(255,255,255,' + (0.96 * a).toFixed(3) + ')'; ctx.strokeStyle = 'rgba(79,70,229,' + (0.95 * a).toFixed(3) + ')'; ctx.lineWidth = 1.1;
      ctx.beginPath(); ctx.rect(-s, -s, s * 2, s * 2); ctx.fill(); ctx.stroke();
      ctx.strokeStyle = 'rgba(99,102,241,' + (0.8 * a).toFixed(3) + ')'; ctx.lineWidth = 1.4 * k + 0.4;
      ctx.beginPath(); ctx.moveTo(-s * 2.9, 0); ctx.lineTo(-s * 1.2, 0); ctx.moveTo(s * 1.2, 0); ctx.lineTo(s * 2.9, 0); ctx.stroke();
      ctx.restore();
    }
  }

  /* ---- drifting depth dust ---- */
  function drawDust(time, front) {
    var cx = S.cx, cy = S.cy, R = S.R, m = Mv, half = S.size / 2, i;
    var spin = rotY(time * 0.045), d, p;
    for (i = 0; i < dust.length; i++) {
      d = dust[i]; p = d.v;
      var rx = spin[0] * p[0] + spin[2] * p[2], ry = p[1], rz = spin[6] * p[0] + spin[8] * p[2];
      var x = (m[0] * rx + m[1] * ry + m[2] * rz) * d.rad, y = (m[3] * rx + m[4] * ry + m[5] * rz) * d.rad, z = (m[6] * rx + m[7] * ry + m[8] * rz) * d.rad;
      if ((z >= 0) !== front) continue;
      if (z < 0 && (x * x + y * y) < 1) continue;
      var sx = cx + x * R, sy = cy - y * R, edge = clamp((1 - Math.hypot(sx - cx, sy - cy) / half) / 0.2, 0, 1);
      if (edge <= 0) continue;
      var tw = 0.5 + 0.5 * Math.sin(time * d.sp + d.ph), depth = front ? 1 : 0.55;
      var a = (0.18 + 0.5 * tw) * edge * depth, sz = d.size * (R / 260 > 0.6 ? 1 : 0.75) * (0.8 + 0.4 * (z / d.rad + 1) / 2 * 1.6);
      ctx.fillStyle = 'rgba(' + d.c[0] + ',' + d.c[1] + ',' + d.c[2] + ',' + a.toFixed(3) + ')';
      ctx.beginPath(); ctx.arc(sx, sy, sz, 0, TAU); ctx.fill();
    }
  }

  /* ---------------------------------------------------------------- loop */
  function frame(now) {
    rafId = 0;
    if (!running) return;
    var raw = now - last, dt = Math.min(0.05, raw / 1000); last = now; t += dt;
    if (loaderUp && globeReady && window.nxLoader && window.nxLoader.active) { rafId = requestAnimationFrame(frame); return; }   // first frame is ready: stay still behind the loading screen (keeps it smooth)
    adapt(now, raw);
    update(dt);
    if (arcs.length < 6 && t >= nextSpawn) { spawnArc(false); nextSpawn = t + 0.45 + Math.random() * 0.9; }
    var fx = window.nx && window.nx.fx;
    if (fx && fx.lite) {
      liteFrame = !liteFrame;
      if (liteFrame) { rafId = requestAnimationFrame(frame); return; }
    } else {
      liteFrame = false;
    }
    render(t);
    markReady();
    rafId = requestAnimationFrame(frame);
  }
  /* Adapt the detail level quickly under sustained load, then restore it after the device recovers. */
  function adapt(now, raw) {
    if (raw > 250 || (window.nxLoader && window.nxLoader.active)) return;   // the loading screen phase is not representative
    warm++; if (warm < 45) return;
    slowAvg += (raw - slowAvg) * 0.05;
    if (slowAvg > 42 && quality > 0 && now - lastAdapt > 2500) {
      quality--; lastAdapt = now; slowAvg = 16; goodSince = 0; resize();
    } else if (quality < 2 && slowAvg < 22) {
      if (!goodSince) goodSince = now;
      if (now - goodSince > 7000 && now - lastAdapt > 10000) {
        quality++; lastAdapt = now; slowAvg = 30; goodSince = 0; resize();
      }
    } else {
      goodSince = 0;
    }
  }
  function start() {
    if (running || reduce || !S.size) return;
    running = true; last = performance.now();
    if (!rafId) rafId = requestAnimationFrame(frame);
  }
  function stop() { running = false; if (rafId) { cancelAnimationFrame(rafId); rafId = 0; } }

  /* ---------------------------------------------------------------- interaction (mouse tilt + drag-to-spin) */
  function hostCenter() { var r = canvas.getBoundingClientRect(); return { x: r.left + r.width / 2, y: r.top + r.height / 2, k: S.size ? r.width / S.size : 1 }; }
  if (!reduce && mq('(hover: hover) and (pointer: fine)')) {
    window.addEventListener('pointermove', function (e) {
      if (e.pointerType && e.pointerType !== 'mouse') return;
      var c = hostCenter();
      mouse.tx = clamp((e.clientX - c.x) / (window.innerWidth * 0.5), -1, 1);
      mouse.ty = clamp((e.clientY - c.y) / (window.innerHeight * 0.5), -1, 1);
    }, { passive: true });

    var INTERACTIVE = 'a,button,input,select,textarea,label,form,summary,h1,h2,h3,p,[data-count]';
    hero.addEventListener('pointerdown', function (e) {
      if (e.pointerType !== 'mouse' || e.button !== 0 || !S.size || !running) return;
      if (e.target.closest && e.target.closest(INTERACTIVE)) return;
      var c = hostCenter();
      if (Math.hypot(e.clientX - c.x, e.clientY - c.y) > S.R * c.k * 1.12) return;
      drag.on = true; drag.x = e.clientX; drag.y = e.clientY; drag.vyaw = 0;
      document.documentElement.classList.add('nx-globe-dragging');
    });
    window.addEventListener('pointermove', function (e) {
      if (!drag.on) return;
      var dx = e.clientX - drag.x, dy = e.clientY - drag.y; drag.x = e.clientX; drag.y = e.clientY;
      var k = 1 / Math.max(1, S.R) * 1.15;
      view.yaw += dx * k; view.userPitch = clamp(view.userPitch + dy * k * 0.7, -0.75, 0.75);
      drag.vyaw = lerp(drag.vyaw, dx * k * 60, 0.4);
    }, { passive: true });
    var endDrag = function () { if (!drag.on) return; drag.on = false; drag.endAt = t; drag.vyaw = clamp(drag.vyaw, -4, 4); document.documentElement.classList.remove('nx-globe-dragging'); };
    window.addEventListener('pointerup', endDrag); window.addEventListener('pointercancel', endDrag); window.addEventListener('blur', endDrag);
  }

  /* ---------------------------------------------------------------- boot */
  function boot() {
    resize();
    if (reduce) {
      /* one still frame: a calm pose with a few arcs already drawn */
      view.yaw = -62 * DEG;
      render(0);
      var made = 0, guard = 0; while (made < 5 && guard++ < 200) { if (spawnArc(true)) made++; }
      render(0);
      markReady();
      return;
    }
    start();
    if (!S.size) markReady();            // nothing to draw (hidden / zero-size): do not keep the loading screen waiting
    if ('IntersectionObserver' in window) {
      new IntersectionObserver(function (en) { if (en[0].isIntersecting) start(); else stop(); }, { threshold: 0 }).observe(host);
    }
    document.addEventListener('visibilitychange', function () { if (document.hidden) stop(); else start(); });
  }
  if (loaderUp) window.addEventListener('nx:loader-done', function () { introT0 = t; });
  if ('ResizeObserver' in window) new ResizeObserver(function () { resize(); if (!running && !reduce && S.size) start(); }).observe(host);
  else window.addEventListener('resize', function () { resize(); });
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', boot); else boot();
})();
