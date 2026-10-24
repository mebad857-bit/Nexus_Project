/* Nexus FX layer — interaction & motion for every page (landing, auth, dashboards).
   Presentation only: it never touches data, forms or API calls (it only *observes* nx.api to show a progress shimmer).
   Everything is event-delegated, so content that is rendered later (tables, modals, tabs) is covered automatically. */
(function () {
    'use strict';

    var nx = (window.nx = window.nx || {});
    var doc = document, root = doc.documentElement;
    var mq = function (q) { return window.matchMedia ? window.matchMedia(q).matches : false; };
    var reduce = mq('(prefers-reduced-motion: reduce)');
    var fine = mq('(hover: hover) and (pointer: fine)');
    var raf = window.requestAnimationFrame.bind(window);
    var mouse = { x: -9999, y: -9999, seen: false };
    nx.fx = { reduce: reduce, fine: fine, mouse: mouse };

    function clamp(v, a, b) { return Math.max(a, Math.min(b, v)); }
    function lerp(a, b, t) { return a + (b - a) * t; }
    function byX(a, b) { return a.x - b.x; }
    function make(tag, cls) { var e = doc.createElement(tag); if (cls) e.className = cls; return e; }

    /* ========================================================================
       1. CONSTELLATION ENGINE (reusable)
          drifting nodes, threads to the cursor, gentle attraction, click shock-waves, shooting stars
       ======================================================================== */
    nx.constellation = function (canvas, opts) {
        var o = Object.assign({
            density: 15000, min: 36, max: 96, link: 130, mlink: 170,
            linkAlpha: 0.38, mAlpha: 0.6, size: 1.4, shooting: true, burst: true
        }, opts || {});
        var ctx = canvas.getContext('2d');
        if (!ctx) return null;

        var w = 0, h = 0, nodes = [], shocks = [], stars = [];
        var running = false, visible = true, nextStar = performance.now() + 2600;

        function build() {
            var dpr = Math.min(window.devicePixelRatio || 1, 2);
            w = canvas.clientWidth; h = canvas.clientHeight;
            if (!w || !h) return;
            canvas.width = Math.round(w * dpr); canvas.height = Math.round(h * dpr);
            ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
            var count = clamp(Math.floor((w * h) / o.density), o.min, o.max);
            nodes = [];
            for (var i = 0; i < count; i++) {
                nodes.push({
                    x: Math.random() * w, y: Math.random() * h,
                    vx: (Math.random() - 0.5) * 0.32, vy: (Math.random() - 0.5) * 0.32,
                    px: 0, py: 0, r: (0.7 + Math.random() * 1.2) * o.size, ph: Math.random() * 6.28
                });
            }
            if (reduce || !running) draw(performance.now(), true);
        }

        function draw(now, still) {
            if (!w || !h) return;
            var rect = canvas.getBoundingClientRect();
            var mx = mouse.x - rect.left, my = mouse.y - rect.top;
            var has = mouse.seen && mx > -60 && mx < w + 60 && my > -60 && my < h + 60;
            var i, j, n, d, f, dx, dy;

            ctx.clearRect(0, 0, w, h);

            /* physics */
            if (!still) {
                for (i = 0; i < nodes.length; i++) {
                    n = nodes[i];
                    if (has) {
                        dx = mx - n.x; dy = my - n.y; d = Math.hypot(dx, dy);
                        if (d < o.mlink && d > 1) {
                            f = d < 60 ? -(1 - d / 60) * 0.08 : (1 - d / o.mlink) * 0.045;
                            n.px += (dx / d) * f; n.py += (dy / d) * f;
                        }
                    }
                    for (j = 0; j < shocks.length; j++) {
                        var s = shocks[j];
                        dx = n.x - s.x; dy = n.y - s.y; d = Math.hypot(dx, dy) || 1;
                        var gap = Math.abs(d - s.r);
                        if (gap < 70) { f = (1 - gap / 70) * s.p * 1.6; n.px += (dx / d) * f; n.py += (dy / d) * f; }
                    }
                    n.px *= 0.94; n.py *= 0.94;
                    n.x += n.vx + n.px; n.y += n.vy + n.py;
                    if (n.x < -20) n.x = w + 20; else if (n.x > w + 20) n.x = -20;
                    if (n.y < -20) n.y = h + 20; else if (n.y > h + 20) n.y = -20;
                }
                for (j = shocks.length - 1; j >= 0; j--) {
                    shocks[j].r += 7; shocks[j].p *= 0.962;
                    if (shocks[j].p < 0.05) shocks.splice(j, 1);
                }
            }

            /* shock rings */
            ctx.lineWidth = 1.5;
            for (j = 0; j < shocks.length; j++) {
                ctx.strokeStyle = 'rgba(99, 102, 241, ' + (shocks[j].p * 0.32) + ')';
                ctx.beginPath(); ctx.arc(shocks[j].x, shocks[j].y, shocks[j].r, 0, 6.2832); ctx.stroke();
            }

            /* threads — x-sorted candidates avoid checking distant node pairs */
            nodes.sort(byX);
            ctx.lineWidth = 1;
            for (i = 0; i < nodes.length; i++) {
                var a = nodes[i];
                for (j = i + 1; j < nodes.length; j++) {
                    var b = nodes[j];
                    dx = a.x - b.x; if (dx < -o.link) break;
                    dy = a.y - b.y; if (dy > o.link || dy < -o.link) continue;
                    d = Math.sqrt(dx * dx + dy * dy);
                    if (d < o.link) {
                        ctx.strokeStyle = 'rgba(79, 70, 229, ' + ((1 - d / o.link) * o.linkAlpha) + ')';
                        ctx.beginPath(); ctx.moveTo(a.x, a.y); ctx.lineTo(b.x, b.y); ctx.stroke();
                    }
                }
                if (has) {
                    a.mouseD = Math.hypot(a.x - mx, a.y - my);
                    if (a.mouseD < o.mlink) {
                        ctx.strokeStyle = 'rgba(124, 58, 237, ' + ((1 - a.mouseD / o.mlink) * o.mAlpha) + ')';
                        ctx.beginPath(); ctx.moveTo(a.x, a.y); ctx.lineTo(mx, my); ctx.stroke();
                    }
                }
            }

            /* stars (nodes) — twinkle, and swell + glow near the cursor */
            for (i = 0; i < nodes.length; i++) {
                n = nodes[i];
                var tw = 0.62 + 0.3 * Math.sin(now * 0.0021 + n.ph), near = 0;
                if (has && n.mouseD < o.mlink) near = 1 - n.mouseD / o.mlink;
                if (near > 0.02) {
                    ctx.fillStyle = 'rgba(124, 58, 237, ' + (near * 0.16) + ')';
                    ctx.beginPath(); ctx.arc(n.x, n.y, n.r * (3 + near * 5), 0, 6.2832); ctx.fill();
                    ctx.fillStyle = 'rgba(99, 70, 240, ' + Math.min(1, tw + near * 0.35) + ')';
                } else {
                    ctx.fillStyle = 'rgba(59, 91, 240, ' + tw + ')';
                }
                ctx.beginPath(); ctx.arc(n.x, n.y, n.r * (1 + near * 1.1), 0, 6.2832); ctx.fill();
            }

            /* shooting stars */
            if (!still && o.shooting) {
                if (now > nextStar && stars.length < 1) {
                    var ang = 0.5 + Math.random() * 0.35;
                    stars.push({ x: Math.random() * w * 0.75, y: -20, vx: Math.cos(ang) * (9 + Math.random() * 4), vy: Math.sin(ang) * (9 + Math.random() * 4), len: 110 + Math.random() * 90 });
                    nextStar = now + 4500 + Math.random() * 6500;
                }
                for (j = stars.length - 1; j >= 0; j--) {
                    var st = stars[j];
                    st.x += st.vx; st.y += st.vy;
                    var sp = Math.hypot(st.vx, st.vy), tx = st.x - (st.vx / sp) * st.len, ty = st.y - (st.vy / sp) * st.len;
                    var g = ctx.createLinearGradient(tx, ty, st.x, st.y);
                    g.addColorStop(0, 'rgba(99, 102, 241, 0)'); g.addColorStop(1, 'rgba(99, 102, 241, 0.75)');
                    ctx.strokeStyle = g; ctx.lineWidth = 1.6;
                    ctx.beginPath(); ctx.moveTo(tx, ty); ctx.lineTo(st.x, st.y); ctx.stroke();
                    ctx.fillStyle = 'rgba(124, 58, 237, 0.9)'; ctx.beginPath(); ctx.arc(st.x, st.y, 1.8, 0, 6.2832); ctx.fill();
                    if (st.x > w + 60 || st.y > h + 60) stars.splice(j, 1);
                }
            }
        }

        var skip = 0;
        function loop(now) { if (!running) return; if (root.classList.contains('nx-loading')) { raf(loop); return; } if (nx.fx.lite && (skip ^= 1)) { raf(loop); return; } draw(now, false); raf(loop); }   // idle behind the loading screen
        function start() { if (running || reduce || !visible || doc.hidden) return; running = true; raf(loop); }
        function stop() { running = false; }

        build();
        if (!reduce) start();

        if ('ResizeObserver' in window) {
            var t0; new ResizeObserver(function () { clearTimeout(t0); t0 = setTimeout(build, 120); }).observe(canvas);
        } else {
            window.addEventListener('resize', build);
        }
        if ('IntersectionObserver' in window) {
            new IntersectionObserver(function (es) { visible = es[0].isIntersecting; visible ? start() : stop(); }).observe(canvas);
        }
        doc.addEventListener('visibilitychange', function () { doc.hidden ? stop() : start(); });

        if (o.burst) {
            window.addEventListener('click', function (e) {
                if (reduce) return;
                var r = canvas.getBoundingClientRect();
                var x = e.clientX - r.left, y = e.clientY - r.top;
                if (x >= 0 && y >= 0 && x <= r.width && y <= r.height) shocks.push({ x: x, y: y, r: 0, p: 1 });
            }, { passive: true });
        }
        return { start: start, stop: stop, rebuild: build };
    };

    /** same engine on every inner page, as a quiet fixed backdrop */
    function initAmbient() {
        if (reduce) return;
        var dash = !!doc.querySelector('[data-accent]');
        var c = make('canvas', 'fx-ambient'); c.setAttribute('aria-hidden', 'true');
        c.style.setProperty('--fx-ambient', dash ? '0.42' : '0.6');
        doc.body.insertBefore(c, doc.body.firstChild);
        nx.constellation(c, {
            density: dash ? 26000 : 20000, min: 22, max: fine ? 68 : 40, link: 120, mlink: 150,
            size: 1.15, linkAlpha: 0.3, mAlpha: 0.5, shooting: !dash
        });
    }

    /* ========================================================================
       2. POINTER LAYER — aura, ring, spotlight, tilt, magnet, parallax, grid glow
       ======================================================================== */
    var aura, ring, ax = 0, ay = 0, rx = 0, ry = 0;
    var par = [], looping = false, hoverPending = false, lastTarget = null;
    var activeTilt = null, activeMagnet = null;
    var LINK_SEL = 'a[href], button:not(:disabled), [role="button"], summary, select, label[for], [data-tab], .side-tab, .tab-btn, .cmp-toggle';
    var TEXT_SEL = 'input:not([type="checkbox"]):not([type="radio"]):not([type="range"]):not([type="button"]):not([type="submit"]), textarea, [contenteditable="true"]';
    var RIPPLE_SEL = '.btn-glow, .side-tab, .tab-btn, .plan-tab, .seg, .tt-pager button, .tt-export button, .cmp-view, .calc-qty button';

    function registerParallax(el, mx, my, sd) { par.push({ el: el, mx: mx, my: my, sd: sd, cx: 0, cy: 0 }); }

    function updateParallax() {
        var moving = false, vw = window.innerWidth, vh = window.innerHeight, sy = window.scrollY;
        for (var i = 0; i < par.length; i++) {
            var p = par[i];
            var tx = mouse.seen ? (mouse.x - vw / 2) * p.mx : 0;
            var ty = mouse.seen ? (mouse.y - vh / 2) * p.my : 0;
            var nx_ = lerp(p.cx, tx, 0.08), ny_ = lerp(p.cy, ty, 0.08);
            if (Math.abs(nx_ - p.cx) > 0.04 || Math.abs(ny_ - p.cy) > 0.04) moving = true;
            p.cx = nx_; p.cy = ny_;
            p.el.style.translate = nx_.toFixed(2) + 'px ' + (ny_ + sy * p.sd).toFixed(2) + 'px';
        }
        return moving;
    }

    function frame() {
        var moving = false;
        if (aura) {
            ax = lerp(ax, mouse.x, 0.085); ay = lerp(ay, mouse.y, 0.085);
            rx = lerp(rx, mouse.x, 0.24); ry = lerp(ry, mouse.y, 0.24);
            aura.style.transform = 'translate3d(' + ax.toFixed(1) + 'px,' + ay.toFixed(1) + 'px,0)';
            ring.style.transform = 'translate3d(' + rx.toFixed(1) + 'px,' + ry.toFixed(1) + 'px,0)';
            if (Math.abs(ax - mouse.x) > 0.4 || Math.abs(ay - mouse.y) > 0.4 || Math.abs(rx - mouse.x) > 0.4) moving = true;
        }
        if (par.length && updateParallax()) moving = true;
        if (moving) raf(frame); else looping = false;
    }
    function kick() { if (!looping) { looping = true; raf(frame); } }

    function resetTilt(el) { if (el) { el.style.setProperty('--rx', '0deg'); el.style.setProperty('--ry', '0deg'); } }
    function resetMagnet(el) { if (el) el.style.translate = ''; }

    function autoTilt(card) {
        // small, display-only glass cards (stat tiles, plan/shop cards) get a tilt; forms and tables never do
        if (card.dataset.fxTilt !== undefined) return;
        var ok = card.offsetWidth < 460 && card.offsetHeight < 340 && !card.querySelector('input, select, textarea, table, canvas') && !card.closest('.modal-card, .palette, .nxbot-panel, .nxn-panel, aside, header, .toast');
        card.dataset.fxTilt = ok ? '1' : '0';
        if (ok) card.classList.add('fx-tilt');
    }

    var gridList = null, gridAt = 0;
    function hover() {
        hoverPending = false;
        var t = lastTarget; if (!t || !t.closest) return;

        /* ---- read phase ---- */
        var card = t.closest('.glass, .glass-strong');
        var cr = card ? card.getBoundingClientRect() : null;
        var tilt = t.closest('.card-hover, .fx-tilt');
        var mag = t.closest('a.btn-glow, .fx-magnet');
        var tr = null, mr = null;
        if (card && !card.classList.contains('card-hover')) autoTilt(card);          // only writes a data attribute/class on first sight of a card
        tilt = t.closest('.card-hover, .fx-tilt');
        if (tilt && fine) tr = tilt.getBoundingClientRect();
        if (mag) mr = mag.getBoundingClientRect();
        var now = performance.now();
        if (!gridList || now - gridAt > 1500) { gridList = doc.querySelectorAll('.grid-bg'); gridAt = now; }
        var vh = window.innerHeight, grects = [];
        for (var g = 0; g < gridList.length; g++) grects.push(gridList[g].getBoundingClientRect());

        /* ---- write phase ---- */
        if (ring) {
            var isText = !!t.closest(TEXT_SEL);
            ring.classList.toggle('is-text', isText);
            ring.classList.toggle('is-link', !isText && !!t.closest(LINK_SEL));
        }
        if (card) {
            card.style.setProperty('--sx', (mouse.x - cr.left).toFixed(0) + 'px');
            card.style.setProperty('--sy', (mouse.y - cr.top).toFixed(0) + 'px');
        }
        if (tilt !== activeTilt) { resetTilt(activeTilt); activeTilt = tilt; }
        if (tilt && fine && tr) {
            var px = clamp((mouse.x - tr.left) / tr.width, 0, 1), py = clamp((mouse.y - tr.top) / tr.height, 0, 1);
            tilt.style.setProperty('--ry', ((px - 0.5) * 9).toFixed(2) + 'deg');
            tilt.style.setProperty('--rx', ((0.5 - py) * 7).toFixed(2) + 'deg');
        }
        if (mag !== activeMagnet) { resetMagnet(activeMagnet); activeMagnet = mag; }
        if (mag && mr) {
            mag.classList.add('fx-magnet');
            mag.style.translate = clamp((mouse.x - (mr.left + mr.width / 2)) * 0.2, -9, 9).toFixed(1) + 'px ' +
                clamp((mouse.y - (mr.top + mr.height / 2)) * 0.28, -7, 7).toFixed(1) + 'px';
        }
        /* the background grid lights up under the cursor */
        for (var k = 0; k < gridList.length; k++) {
            var gr = grects[k];
            if (gr.bottom < 0 || gr.top > vh) continue;
            gridList[k].style.setProperty('--gx', (mouse.x - gr.left).toFixed(0) + 'px');
            gridList[k].style.setProperty('--gy', (mouse.y - gr.top).toFixed(0) + 'px');
        }
    }

    function initPointer() {
        // parallax elements: explicit [data-depth], the hero artwork, and the soft background blobs
        doc.querySelectorAll('[data-depth]').forEach(function (el) { var d = parseFloat(el.getAttribute('data-depth')) || 0; registerParallax(el, d, d, 0); });
        doc.querySelectorAll('div.pointer-events-none.absolute[class*="blur-["]').forEach(function (el, i) { registerParallax(el, 0, 0, i % 2 ? -0.07 : 0.06); });
        if (!reduce && par.length) { window.addEventListener('scroll', kick, { passive: true }); kick(); }

        if (fine && !reduce) {
            aura = make('div', 'fx-aura'); ring = make('div', 'fx-ring');
            doc.body.appendChild(aura); doc.body.appendChild(ring);

            doc.addEventListener('mouseleave', function () { aura.classList.remove('on'); ring.classList.remove('on'); resetTilt(activeTilt); resetMagnet(activeMagnet); activeTilt = activeMagnet = null; });
            doc.addEventListener('mouseenter', function () { if (mouse.seen) { aura.classList.add('on'); ring.classList.add('on'); } });
            doc.addEventListener('mousedown', function () { ring.classList.add('is-down'); });
            doc.addEventListener('mouseup', function () { ring.classList.remove('is-down'); });
        }

        if (reduce) return;
        window.addEventListener('mousemove', function (e) {
            if (!mouse.seen) {
                mouse.seen = true; ax = rx = e.clientX; ay = ry = e.clientY;
                if (aura) { aura.classList.add('on'); ring.classList.add('on'); }
            }
            mouse.x = e.clientX; mouse.y = e.clientY; lastTarget = e.target;
            kick();
            if (!hoverPending) { hoverPending = true; raf(hover); }
        }, { passive: true });

        /* click pulse on empty space + ripple on controls */
        doc.addEventListener('pointerdown', function (e) {
            if (e.button !== 0) return;
            var t = e.target; if (!t.closest) return;
            var host = t.closest(RIPPLE_SEL);
            if (host && !host.disabled) {
                host.classList.add('fx-ripple-host');
                var r = host.getBoundingClientRect(), size = Math.max(r.width, r.height) * 2;
                var dot = make('span', 'fx-ripple' + (host.classList.contains('btn-glow') || host.classList.contains('active') ? '' : ' dark'));
                dot.style.width = dot.style.height = size + 'px';
                dot.style.left = (e.clientX - r.left - size / 2) + 'px'; dot.style.top = (e.clientY - r.top - size / 2) + 'px';
                host.appendChild(dot); setTimeout(function () { dot.remove(); }, 700);
            }
            if (e.pointerType === 'mouse' && !t.closest(TEXT_SEL + ', select, textarea, .modal-card')) {
                var p = make('span', 'fx-pulse'); p.style.left = e.clientX + 'px'; p.style.top = e.clientY + 'px';
                doc.body.appendChild(p); setTimeout(function () { p.remove(); }, 750);
            }
        }, { passive: true });
    }

    /* ========================================================================
       3. PROGRESS BAR, API SHIMMER, BACK-TO-TOP, HEADER BEHAVIOUR
       ======================================================================== */
    function initScrollUi() {
        var bar = make('div'); bar.id = 'fx-progress'; doc.body.appendChild(bar);

        var top = make('button'); top.id = 'fx-top'; top.type = 'button'; top.setAttribute('aria-label', 'Back to top');
        top.innerHTML = '<svg class="ring" viewBox="0 0 46 46" aria-hidden="true"><defs><linearGradient id="fx-top-grad" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#2563eb"/><stop offset="1" stop-color="#9333ea"/></linearGradient></defs><circle class="bg" cx="23" cy="23" r="21"/><circle class="fg" cx="23" cy="23" r="21" stroke-dasharray="131.95" stroke-dashoffset="131.95"/></svg>' +
            '<svg class="arrow" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M12 19V5M5 12l7-7 7 7"/></svg>';
        doc.body.appendChild(top);
        var fg = top.querySelector('.fg');
        top.addEventListener('click', function () { window.scrollTo({ top: 0, behavior: reduce ? 'auto' : 'smooth' }); });

        var header = doc.getElementById('site-header'), mobileMenu = doc.getElementById('nav-mobile');
        var lastY = window.scrollY, ticking = false, busy = 0;

        function update() {
            ticking = false;
            var y = window.scrollY, max = Math.max(1, root.scrollHeight - window.innerHeight), p = clamp(y / max, 0, 1);
            bar.style.transform = 'scaleX(' + p.toFixed(4) + ')';
            bar.classList.toggle('on', y > 8 || busy > 0);
            fg.style.strokeDashoffset = (131.95 * (1 - p)).toFixed(2);
            top.classList.toggle('show', y > 640);
            if (header && !reduce) {
                var open = mobileMenu && !mobileMenu.hidden;
                if (y > 520 && y > lastY + 6 && !open && !header.contains(doc.activeElement)) header.classList.add('fx-hide');
                else if (y < lastY - 4 || y < 240 || open) header.classList.remove('fx-hide');
            }
            lastY = y;
        }
        window.addEventListener('scroll', function () { if (!ticking) { ticking = true; raf(update); } }, { passive: true });
        window.addEventListener('resize', update);
        if (header) doc.addEventListener('mousemove', function (e) { if (e.clientY < 70) header.classList.remove('fx-hide'); }, { passive: true });
        update();

        /* shimmer on the bar while any nx.api call is in flight */
        if (typeof nx.api === 'function' && !nx.api.__fx) {
            var orig = nx.api;
            nx.api = function () {
                busy++; bar.classList.add('busy', 'on');
                function done() { busy = Math.max(0, busy - 1); if (!busy) { bar.classList.remove('busy'); update(); } }
                var pr = orig.apply(this, arguments);
                pr.then(done, done);
                return pr;
            };
            nx.api.__fx = true;
        }
        nx.fx.setBusy = function (on) { busy = on ? busy + 1 : Math.max(0, busy - 1); bar.classList.toggle('busy', busy > 0); update(); };
    }

    /* sliding pill behind the desktop nav links */
    function initNavPill() {
        var nav = doc.querySelector('#site-header nav'); if (!nav || reduce) return;
        var pill = make('span', 'fx-nav-pill'); nav.insertBefore(pill, nav.firstChild);
        nav.querySelectorAll(':scope > a').forEach(function (a) {
            a.addEventListener('mouseenter', function () {
                pill.style.width = a.offsetWidth + 'px'; pill.style.height = a.offsetHeight + 'px';
                pill.style.transform = 'translate(' + a.offsetLeft + 'px,' + a.offsetTop + 'px)'; pill.classList.add('on');
            });
        });
        nav.addEventListener('mouseleave', function () { pill.classList.remove('on'); });
    }

    /* ========================================================================
       4. REVEALS, NUMBER POP, IMAGE PARALLAX
       ======================================================================== */
    function initAutoReveal() {
        if (reduce || !('IntersectionObserver' in window)) return;
        var skip = '.reveal, .fade-up, header, nav, aside, [data-panel], .modal-card, .palette, .nxbot-root, .toast, .card-hover, .fx-tilt, [hidden], #site-header';
        var picks = [];
        doc.querySelectorAll('main .glass, main .glass-strong, main .faq-item').forEach(function (el) {
            if (el.closest(skip) || el.parentElement.closest('.reveal')) return;
            picks.push(el);
        });
        if (!picks.length) return;
        var io = new IntersectionObserver(function (es) {
            es.forEach(function (en) { if (en.isIntersecting) { en.target.classList.add('in'); io.unobserve(en.target); } });
        }, { rootMargin: '0px 0px -50px 0px' });
        picks.forEach(function (el, i) {
            var idx = Array.prototype.indexOf.call(el.parentElement.children, el);
            el.style.setProperty('--d', (Math.min(idx, 6) * 0.07).toFixed(2) + 's');
            el.classList.add('reveal'); io.observe(el);
        });
    }

    function initNumberPop() {
        doc.querySelectorAll('[data-calc-total], [data-calc-rental], [data-calc-deposit], [data-calc-tax]').forEach(function (el) {
            new MutationObserver(function () {
                el.classList.remove('fx-pop'); void el.offsetWidth; el.classList.add('fx-pop');
            }).observe(el, { childList: true, characterData: true, subtree: true });
        });
    }

    function initImageDepth() {
        // hero / auth artwork drifts opposite to the cursor; scaled a touch so edges never show
        doc.querySelectorAll('img.absolute.inset-0.object-cover[src*="/images/hero/"]').forEach(function (img) {
            img.style.scale = '1.06'; registerParallax(img, -0.012, -0.012, 0);
        });
    }

    /* ========================================================================
       5. PAGE TRANSITIONS (curtain wipe between internal pages)
       ======================================================================== */
    function initTransitions() {
        var key = 'fx.nav';
        if (root.classList.contains('fx-entering')) {
            raf(function () {
                raf(function () {
                    root.classList.remove('fx-entering'); root.classList.add('fx-entered');
                    try { sessionStorage.removeItem(key); } catch (e) { /* ignore */ }
                    setTimeout(function () { root.classList.remove('fx-entered'); }, 1500);
                });
            });
        }
        if (reduce) return;

        window.addEventListener('pageshow', function (e) {
            if (e.persisted) { root.classList.remove('fx-leaving', 'fx-entering', 'fx-entered'); if (nx.fx.setBusy) nx.fx.setBusy(false); }
        });

        doc.addEventListener('click', function (e) {
            if (e.defaultPrevented || e.button !== 0 || e.metaKey || e.ctrlKey || e.shiftKey || e.altKey) return;
            var a = e.target.closest && e.target.closest('a[href]'); if (!a) return;
            if ((a.target && a.target !== '_self') || a.hasAttribute('download') || a.hasAttribute('data-no-fx')) return;
            var u; try { u = new URL(a.href, location.href); } catch (err) { return; }
            if (u.origin !== location.origin || !/^https?:$/.test(u.protocol)) return;
            if (u.pathname === location.pathname && u.search === location.search) return;           // same page / hash jump
            if (/^\/(api|export)\b|\.(pdf|xlsx?|csv|zip|png|jpe?g|gif|webp|svg)$/i.test(u.pathname)) return; // files & downloads
            if (/\/(print|download|receipt|invoice)\b/i.test(u.pathname)) return;

            e.preventDefault();
            try { sessionStorage.setItem(key, '1'); } catch (err) { /* ignore */ }
            root.classList.add('fx-leaving');
            setTimeout(function () { window.location.href = u.href; }, 400);
            // if the response turns out to be a download the page stays alive — lift the curtain again
            setTimeout(function () { root.classList.remove('fx-leaving'); try { sessionStorage.removeItem(key); } catch (err) { /* ignore */ } }, 4500);
        });
    }

    /* ------------------------------------------------------------------ boot */
    function boot() {
        initTransitions();
        initAmbient();
        initImageDepth();
        initPointer();
        initScrollUi();
        initNavPill();
        initAutoReveal();
        initNumberPop();
    }
    if (doc.readyState === 'loading') doc.addEventListener('DOMContentLoaded', boot); else boot();
})();
