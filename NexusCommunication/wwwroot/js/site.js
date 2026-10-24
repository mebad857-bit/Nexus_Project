/* Nexus Communication System — shared client behaviour (replaces the React client components) */
(function () {
  'use strict';

  var nx = (window.nx = window.nx || {});

  /* ------------------------------ fetch helper ------------------------------ */
  nx.csrf = function () {
    var m = document.querySelector('meta[name="csrf-token"]');
    return m ? m.content : '';
  };

  /** api(url, {method, body}) -> { ok, data } — same contract as the old api() helper */
  nx.api = async function (url, options) {
    options = options || {};
    var headers = { RequestVerificationToken: nx.csrf() };
    if (options.body !== undefined) headers['Content-Type'] = 'application/json';
    var res = await fetch(url, {
      method: options.method || 'GET',
      headers: headers,
      credentials: 'same-origin',
      body: options.body !== undefined ? JSON.stringify(options.body) : undefined
    });
    var data = {};
    try { data = await res.json(); } catch (e) { /* empty body */ }
    return { ok: res.ok, data: data };
  };

  nx.icons = function () {
    if (window.lucide) window.lucide.createIcons();
  };

  nx.spinner = '<i data-lucide="loader-circle" class="h-4 w-4 animate-spin"></i>';

  /** replace / restore the icon inside a button while a request is running */
  nx.busy = function (btn, on, icon) {
    if (!btn) return;
    btn.disabled = on;
    var holder = btn.querySelector('[data-icon-slot]');
    if (!holder) return;
    if (on) {
      holder.dataset.prev = holder.innerHTML;
      holder.innerHTML = nx.spinner;
    } else if (holder.dataset.prev !== undefined) {
      holder.innerHTML = holder.dataset.prev;
    }
    nx.icons();
  };

  nx.reload = function () { window.location.reload(); };

  nx.money = function (n) {
    return '$' + Number(n || 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  };

  /* --------------------------------- tabs ---------------------------------- */
  // any number of [data-tab] buttons (top bar, sidebar, mobile drawer) drive the <section data-panel="id" hidden> blocks
  function initTabs() {
    var buttons = document.querySelectorAll('[data-tab]');
    if (!buttons.length) return;
    var panels = document.querySelectorAll('[data-panel]');
    var ids = [];
    buttons.forEach(function (b) { if (ids.indexOf(b.dataset.tab) < 0) ids.push(b.dataset.tab); });

    function show(id, silent) {
      if (ids.indexOf(id) < 0) id = ids[0];
      buttons.forEach(function (b) { b.classList.toggle('active', b.dataset.tab === id); });
      panels.forEach(function (p) { p.hidden = p.dataset.panel !== id; });
      if (!silent) {
        history.replaceState(null, '', '#' + id);
        window.scrollTo({ top: 0, behavior: 'smooth' });
      }
      var drawer = document.getElementById('drawer');
      if (drawer) drawer.hidden = true;
      document.dispatchEvent(new CustomEvent('nx:tab', { detail: id }));
    }

    buttons.forEach(function (b) {
      b.addEventListener('click', function () { show(b.dataset.tab); });
    });
    nx.showTab = show;
    nx.tabIds = ids;
    show((location.hash || '').replace('#', '') || ids[0], true);
  }

  /* --------------------------- public site behaviour ------------------------ */
  function initHeader() {
    var header = document.getElementById('site-header');
    if (!header) return;
    var onScroll = function () { header.classList.toggle('scrolled', window.scrollY > 24); };
    onScroll();
    window.addEventListener('scroll', onScroll, { passive: true });

    var toggle = document.getElementById('nav-toggle');
    var menu = document.getElementById('nav-mobile');
    if (toggle && menu) {
      toggle.addEventListener('click', function () {
        menu.hidden = !menu.hidden;
        document.getElementById('nav-icon-open').classList.toggle('hidden', !menu.hidden);
        document.getElementById('nav-icon-close').classList.toggle('hidden', menu.hidden);
      });
      menu.querySelectorAll('a').forEach(function (a) {
        a.addEventListener('click', function () { menu.hidden = true; });
      });
    }
  }

  function initReveal() {
    var els = document.querySelectorAll('.reveal');
    if (!('IntersectionObserver' in window)) { els.forEach(function (e) { e.classList.add('in'); }); return; }
    var io = new IntersectionObserver(function (entries) {
      entries.forEach(function (en) {
        if (en.isIntersecting) { en.target.classList.add('in'); io.unobserve(en.target); }
      });
    }, { rootMargin: '0px 0px -70px 0px' });
    els.forEach(function (e) { io.observe(e); });

    var bars = document.querySelectorAll('[data-bar]');
    var bio = new IntersectionObserver(function (entries) {
      entries.forEach(function (en) {
        if (en.isIntersecting) { en.target.style.width = en.target.dataset.bar + '%'; bio.unobserve(en.target); }
      });
    });
    bars.forEach(function (b) { bio.observe(b); });
  }

  function initCounters() {
    var els = document.querySelectorAll('[data-count]');
    if (!els.length) return;
    var io = new IntersectionObserver(function (entries) {
      entries.forEach(function (en) {
        if (!en.isIntersecting) return;
        io.unobserve(en.target);
        var el = en.target, value = Number(el.dataset.count) || 0, suffix = el.dataset.suffix || '';
        var start = performance.now(), dur = 1700;
        (function step(t) {
          var p = Math.min(1, (t - start) / dur);
          el.textContent = Math.round(value * (1 - Math.pow(1 - p, 3))).toLocaleString() + suffix;
          if (p < 1) requestAnimationFrame(step);
        })(start);
      });
    }, { rootMargin: '0px 0px -40px 0px' });
    els.forEach(function (e) { io.observe(e); });
  }

  /** Ambient interactive constellation — drifting nodes linked by luminescent threads */
  function initNetwork() {
    var canvas = document.querySelector('canvas[data-network]');
    if (!canvas) return;
    // fx.js provides the richer engine (cursor attraction, click shock-waves, shooting stars)
    if (nx.constellation && nx.constellation(canvas, { density: 15000, min: 36, max: 96, link: 130, mlink: 170, linkAlpha: 0.38, mAlpha: 0.6, size: 1.4 })) return;
    var ctx = canvas.getContext('2d');
    if (!ctx) return;
    var w = 0, h = 0, nodes = [], mouse = { x: -9999, y: -9999 }, LINK = 130;

    function resize() {
      var dpr = Math.min(window.devicePixelRatio || 1, 2);
      w = canvas.clientWidth; h = canvas.clientHeight;
      canvas.width = w * dpr; canvas.height = h * dpr;
      ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
      var count = Math.max(36, Math.min(96, Math.floor((w * h) / 15000)));
      nodes = Array.from({ length: count }, function () {
        return { x: Math.random() * w, y: Math.random() * h, vx: (Math.random() - 0.5) * 0.32, vy: (Math.random() - 0.5) * 0.32 };
      });
    }

    function tick() {
      ctx.clearRect(0, 0, w, h);
      nodes.forEach(function (n) {
        n.x += n.vx; n.y += n.vy;
        if (n.x < -20) n.x = w + 20; if (n.x > w + 20) n.x = -20;
        if (n.y < -20) n.y = h + 20; if (n.y > h + 20) n.y = -20;
      });
      for (var i = 0; i < nodes.length; i++) {
        var a = nodes[i];
        for (var j = i + 1; j < nodes.length; j++) {
          var b = nodes[j], dist = Math.hypot(a.x - b.x, a.y - b.y);
          if (dist < LINK) {
            ctx.strokeStyle = 'rgba(79, 70, 229, ' + (1 - dist / LINK) * 0.38 + ')';
            ctx.lineWidth = 1; ctx.beginPath(); ctx.moveTo(a.x, a.y); ctx.lineTo(b.x, b.y); ctx.stroke();
          }
        }
        var md = Math.hypot(a.x - mouse.x, a.y - mouse.y);
        if (md < 170) {
          ctx.strokeStyle = 'rgba(124, 58, 237, ' + (1 - md / 170) * 0.6 + ')';
          ctx.lineWidth = 1; ctx.beginPath(); ctx.moveTo(a.x, a.y); ctx.lineTo(mouse.x, mouse.y); ctx.stroke();
        }
      }
      ctx.fillStyle = 'rgba(59, 91, 240, 0.85)';
      nodes.forEach(function (n) { ctx.beginPath(); ctx.arc(n.x, n.y, 1.4, 0, Math.PI * 2); ctx.fill(); });
      requestAnimationFrame(tick);
    }

    resize(); tick();
    window.addEventListener('resize', resize);
    window.addEventListener('mousemove', function (e) {
      var r = canvas.getBoundingClientRect(); mouse.x = e.clientX - r.left; mouse.y = e.clientY - r.top;
    });
  }

  /* ------------------------ charges calculator (landing page) ------------------------ */
  // rates come from the plan list rendered by the server; the bulk slabs from Fmt.BulkDiscount
  function initCalculator() {
    var root = document.querySelector('[data-calc]'); if (!root) return;
    var plan = root.querySelector('[data-calc-plan]'), qtyEl = root.querySelector('[data-calc-qty]');
    var tax = parseFloat(root.getAttribute('data-tax')) || 0, slabs = [];
    try { slabs = JSON.parse(root.getAttribute('data-slabs') || '[]'); } catch (e) { slabs = []; }
    function q(sel) { return root.querySelector(sel); }
    // start on the first broadband plan (our flagship), like the plans page
    for (var oi = 0; oi < plan.options.length; oi++) { if (plan.options[oi].getAttribute('data-type') === 'broadband') { plan.selectedIndex = oi; break; } }
    function r2(n) { return Math.round(n * 100) / 100; }
    function discountFor(n) { var d = 0; slabs.forEach(function (s) { if (n >= s[0]) d = s[1]; }); return d; }

    function calc() {
      var opt = plan.options[plan.selectedIndex]; if (!opt) return;
      var n = Math.floor(Number(qtyEl.value));
      if (!(n >= 1)) n = 1; if (n > 10000) n = 10000;
      var price = parseFloat(opt.getAttribute('data-price')) || 0, dep = parseFloat(opt.getAttribute('data-deposit')) || 0;
      var pct = discountFor(n);
      var rental = r2(price * n), deposit = r2(dep * n);
      var offRental = r2(rental * pct / 100), offDeposit = r2(deposit * pct / 100);
      var netRental = r2(rental - offRental), netDeposit = r2(deposit - offDeposit);
      var taxAmt = r2(netRental * tax / 100);
      var total = r2(netRental + netDeposit + taxAmt);

      q('[data-calc-rental]').textContent = nx.money(rental);
      q('[data-calc-rental-note]').textContent = n + ' × ' + nx.money(price);
      q('[data-calc-deposit]').textContent = nx.money(deposit);
      q('[data-calc-tax]').textContent = nx.money(taxAmt);
      q('[data-calc-total]').textContent = nx.money(total);
      var row = q('[data-calc-disc-row]'); row.hidden = pct === 0;
      q('[data-calc-pct]').textContent = pct + '% off';
      q('[data-calc-disc]').textContent = '−' + nx.money(offRental + offDeposit);

      var hint = q('[data-calc-hint]'), next = null;
      slabs.forEach(function (s) { if (next === null && s[0] > n) next = s; });
      if (pct > 0) hint.textContent = 'You save ' + nx.money(offRental + offDeposit) + ' with the ' + pct + '% bulk discount.' + (next ? ' Order ' + next[0] + '+ lines for ' + next[1] + '% off.' : '');
      else hint.textContent = next ? 'Order ' + next[0] + '+ lines to unlock ' + next[1] + '% off rental and deposit.' : '';
    }

    function setQty(v) { v = Math.floor(Number(v)); if (!(v >= 1)) v = 1; if (v > 10000) v = 10000; qtyEl.value = v; calc(); }
    plan.addEventListener('change', calc);
    qtyEl.addEventListener('input', calc);
    qtyEl.addEventListener('blur', function () { setQty(qtyEl.value); });
    q('[data-calc-dec]').addEventListener('click', function () { setQty((Number(qtyEl.value) || 1) - 1); });
    q('[data-calc-inc]').addEventListener('click', function () { setQty((Number(qtyEl.value) || 0) + 1); });
    calc();
  }

  /* ---------------------- plans: compare 2-3 side by side ---------------------- */
  function initCompare(root) {
    var wrap = root.querySelector('[data-compare-wrap]'), table = root.querySelector('[data-compare-table]');
    if (!wrap || !table) return;
    var MAX = 3, TAX = 12.24, sel = [], data = {};
    var orderHref = root.getAttribute('data-order-href') || '/register', orderText = root.getAttribute('data-order-text') || 'Get this plan';

    var pill = document.createElement('div'); pill.className = 'cmp-pill'; pill.hidden = true;
    pill.setAttribute('role', 'status'); document.body.appendChild(pill);

    var boxes = {};
    root.querySelectorAll('[data-plan-card]').forEach(function (card) {
      var d = card.dataset, id = d.id; if (!id) return;
      data[id] = {
        id: id, type: d.type, category: d.category, name: d.name, speed: d.speed || '',
        price: parseFloat(d.price) || 0, deposit: parseFloat(d.deposit) || 0,
        validity: d.validity || '', months: Math.max(1, parseInt(d.months, 10) || 1), calls: d.calls || '', card: card
      };
      var cb = card.querySelector('input[data-compare]'); if (!cb) return;
      boxes[id] = cb;
      cb.addEventListener('change', function () {
        if (cb.checked) {
          if (sel.length >= MAX) { cb.checked = false; nx.toast('You can compare up to ' + MAX + ' plans at a time — remove one first.', 'warn'); return; }
          sel.push(id);
        } else sel = sel.filter(function (x) { return x !== id; });
        update();
      });
    });

    function clearAll() { sel.forEach(function (id) { if (boxes[id]) boxes[id].checked = false; }); sel = []; update(); }
    function remove(id) { if (boxes[id]) boxes[id].checked = false; sel = sel.filter(function (x) { return x !== id; }); update(); }

    wrap.addEventListener('click', function (e) {
      var r = e.target.closest('[data-cmp-remove]'); if (r) { remove(r.getAttribute('data-cmp-remove')); return; }
      if (e.target.closest('[data-cmp-clear]')) clearAll();
    });
    pill.addEventListener('click', function (e) {
      if (e.target.closest('[data-cmp-clear]')) clearAll();
      else if (e.target.closest('[data-cmp-view]')) wrap.scrollIntoView({ behavior: 'smooth', block: 'start' });
    });

    if ('IntersectionObserver' in window) {
      new IntersectionObserver(function (en) { pill.classList.toggle('seen', en[0].isIntersecting); }, { threshold: 0.25 }).observe(wrap);
    }

    function row(label, cells) {
      return '<tr><th scope="row">' + label + '</th>' + cells.map(function (c) { return '<td>' + c + '</td>'; }).join('') + '</tr>';
    }

    function render() {
      var plans = sel.map(function (id) { return data[id]; });
      var per = plans.map(function (p) { return p.price / p.months; });
      var minPer = Math.min.apply(null, per), allSame = per.every(function (v) { return Math.abs(v - per[0]) < 0.005; });
      var dash = '<span class="cmp-dash">—</span>';

      var head = '<thead><tr><th></th>' + plans.map(function (p) {
        return '<th scope="col" class="cmp-head"><button type="button" class="cmp-x" data-cmp-remove="' + p.id + '" aria-label="Remove ' + nx.esc(p.name) + ' from comparison"><i data-lucide="x"></i></button>' +
          '<span class="cmp-type">' + nx.esc(p.type) + '</span><span class="cmp-cat">' + nx.esc(p.category) + '</span><span class="cmp-name">' + nx.esc(p.name) + '</span></th>';
      }).join('') + '</tr></thead>';

      var body = '<tbody>' +
        row('Rental', plans.map(function (p) { return '<strong class="cmp-big">' + nx.money(p.price) + '</strong>'; })) +
        row('Validity', plans.map(function (p) { return nx.esc(p.validity) || dash; })) +
        row('Per month', plans.map(function (p, i) {
          var best = !allSame && Math.abs(per[i] - minPer) < 0.005;
          return nx.money(per[i]) + (best ? ' <span class="cmp-tag">Lowest</span>' : '');
        })) +
        row('Speed', plans.map(function (p) { return p.speed ? nx.esc(p.speed) : dash; })) +
        row('Service tax <small>' + TAX + '%</small>', plans.map(function (p) { return nx.money(p.price * TAX / 100); })) +
        row('Rental incl. tax', plans.map(function (p) { return nx.money(p.price * (1 + TAX / 100)); })) +
        row('Security deposit', plans.map(function (p) { return nx.money(p.deposit) + '<small class="cmp-sub">refundable</small>'; })) +
        row('Call &amp; usage charges', plans.map(function (p) { return p.calls ? nx.esc(p.calls) : dash; })) +
        row('To start', plans.map(function (p) { return '<strong>' + nx.money(p.price * (1 + TAX / 100) + p.deposit) + '</strong><small class="cmp-sub">rental + tax + deposit</small>'; })) +
        '<tr class="cmp-cta"><th></th>' + plans.map(function () {
          return '<td><a href="' + nx.esc(orderHref) + '" class="btn-glow inline-flex items-center gap-1.5 rounded-xl px-4 py-2 text-xs font-bold text-white">' + nx.esc(orderText) + '<i data-lucide="arrow-up-right" class="h-3.5 w-3.5"></i></a></td>';
        }).join('') + '</tr></tbody>';

      table.innerHTML = head + body;
      nx.icons();
    }

    function update() {
      Object.keys(data).forEach(function (id) { data[id].card.classList.toggle('is-compared', sel.indexOf(id) > -1); });
      root.classList.toggle('cmp-full', sel.length >= MAX);
      var open = sel.length >= 2;
      if (open) render();
      var wasHidden = wrap.hidden;
      wrap.hidden = !open;
      if (open && wasHidden) { wrap.classList.remove('cmp-in'); void wrap.offsetWidth; wrap.classList.add('cmp-in'); }

      pill.hidden = sel.length === 0;
      pill.innerHTML = '<span class="cmp-count">' + sel.length + '<small>/' + MAX + '</small></span>' +
        '<span class="cmp-msg">' + (open ? 'Comparison is ready below' : 'Pick one more plan to compare') + '</span>' +
        (open ? '<button type="button" data-cmp-view class="cmp-view">View<i data-lucide="arrow-down" class="h-3.5 w-3.5"></i></button>' : '') +
        '<button type="button" data-cmp-clear class="cmp-clear">Clear</button>';
      nx.icons();
    }
  }

  function initPlans() {
    document.querySelectorAll('[data-plans-browser]').forEach(function (root) {
      initCompare(root);
      var tabs = root.querySelectorAll('[data-plan-type]');
      var panels = root.querySelectorAll('[data-plan-panel]');
      tabs.forEach(function (t) {
        t.addEventListener('click', function () {
          tabs.forEach(function (x) { x.classList.toggle('active', x === t); });
          panels.forEach(function (p) {
            var on = p.dataset.planPanel === t.dataset.planType;
            p.hidden = !on;
            if (on) {
              var inner = p.querySelector('.plan-panel');
              if (inner) { inner.style.animation = 'none'; void inner.offsetWidth; inner.style.animation = ''; }
              p.querySelectorAll('.reveal').forEach(function (r) { r.classList.add('in'); });
            }
          });
        });
      });
    });
  }

  /* ---------------------------- dashboard shell ---------------------------- */
  function initShell() {
    var drawer = document.getElementById('drawer');
    if (!drawer) return;
    document.querySelectorAll('[data-drawer-open]').forEach(function (b) {
      b.addEventListener('click', function () { drawer.hidden = false; });
    });
    document.querySelectorAll('[data-drawer-close]').forEach(function (b) {
      b.addEventListener('click', function () { drawer.hidden = true; });
    });
  }


  /* ============================================================================
     v2 interaction layer — toasts, dialogs, form modal, data tables, charts,
     command palette, small widgets
     ============================================================================ */

  nx.esc = function (t) {
    var d = document.createElement('div'); d.textContent = t == null ? '' : String(t); return d.innerHTML;
  };

  /** readable error text from an nx.api() result */
  nx.errText = function (res, fallback) {
    return (res && res.data && (res.data.error || res.data.title)) || fallback || 'Something went wrong';
  };

  /* ------------------------------- toasts ------------------------------- */
  nx.toast = function (msg, type, ms) {
    type = type || 'info';
    var wrap = document.getElementById('toast-wrap');
    if (!wrap) {
      wrap = document.createElement('div'); wrap.id = 'toast-wrap'; wrap.className = 'toast-wrap';
      wrap.setAttribute('aria-live', 'polite'); document.body.appendChild(wrap);
    }
    var icon = { success: 'circle-check', error: 'circle-alert', warn: 'triangle-alert', info: 'info' }[type] || 'info';
    var el = document.createElement('div');
    el.className = 'toast'; el.setAttribute('data-type', type); el.setAttribute('role', type === 'error' ? 'alert' : 'status');
    el.innerHTML = '<i data-lucide="' + icon + '" class="t-icon h-[18px] w-[18px]"></i><p class="flex-1 leading-snug"></p>' +
      '<button type="button" class="text-slate-500 transition hover:text-slate-200" aria-label="Dismiss"><i data-lucide="x" class="h-4 w-4"></i></button>';
    el.querySelector('p').textContent = msg;
    wrap.appendChild(el);
    nx.icons();
    function gone() { el.classList.add('out'); setTimeout(function () { el.remove(); }, 300); }
    el.querySelector('button').addEventListener('click', gone);
    setTimeout(gone, ms || (type === 'error' ? 6500 : 4200));
  };

  /** remember a toast across the page reload that follows most actions */
  nx.flash = function (msg, type) { try { sessionStorage.setItem('nx.flash', JSON.stringify({ m: msg, t: type || 'success' })); } catch (e) { /* storage blocked */ } };
  function showFlash() {
    try {
      var raw = sessionStorage.getItem('nx.flash'); if (!raw) return;
      sessionStorage.removeItem('nx.flash');
      var f = JSON.parse(raw); nx.toast(f.m, f.t);
    } catch (e) { /* ignore */ }
  }

  /* ------------------------------- dialogs ------------------------------- */
  nx.dialog = function (innerHtml, cls, onClose) {
    var back = document.createElement('div');
    back.className = 'modal-back'; back.setAttribute('role', 'dialog'); back.setAttribute('aria-modal', 'true');
    var card = document.createElement('div');
    card.className = 'glass-strong modal-card ' + (cls || '');
    card.innerHTML = innerHtml;
    back.appendChild(card); document.body.appendChild(back);
    var prev = document.activeElement, closed = false;
    function close(v) {
      if (closed) return; closed = true;
      document.removeEventListener('keydown', onKey); back.remove();
      if (prev && prev.focus) prev.focus();
      if (onClose) onClose(v);
    }
    function onKey(e) { if (e.key === 'Escape') close(false); }
    back.addEventListener('mousedown', function (e) { if (e.target === back) close(false); });
    document.addEventListener('keydown', onKey);
    nx.icons();
    return { el: card, close: close };
  };

  /** nicer replacement for window.confirm -> Promise<boolean> */
  nx.confirm = function (o) {
    o = o || {};
    return new Promise(function (resolve) {
      var tone = o.danger ? 'bg-rose-500/15 text-rose-300' : 'bg-cyan-500/15 text-cyan-300';
      var btn = o.danger ? 'bg-gradient-to-r from-rose-500 to-pink-600' : 'btn-glow';
      var d = nx.dialog(
        '<div class="flex items-start gap-4"><span class="grid h-11 w-11 shrink-0 place-items-center rounded-xl ' + tone + '">' +
        '<i data-lucide="' + (o.danger ? 'triangle-alert' : 'circle-help') + '" class="h-5 w-5"></i></span>' +
        '<div><h3 class="font-display text-lg font-bold text-slate-100" data-t></h3><p class="mt-1.5 text-sm leading-relaxed text-slate-400" data-m></p></div></div>' +
        '<div class="mt-7 flex justify-end gap-3"><button type="button" data-no class="rounded-xl border border-slate-700 px-5 py-2.5 text-sm font-semibold text-slate-300 transition hover:bg-white/5">Cancel</button>' +
        '<button type="button" data-yes class="' + btn + ' rounded-xl px-5 py-2.5 text-sm font-semibold text-white transition hover:brightness-110"></button></div>',
        'sm', function (v) { resolve(v === true); });
      d.el.querySelector('[data-t]').textContent = o.title || 'Are you sure?';
      d.el.querySelector('[data-m]').textContent = o.message || '';
      var yes = d.el.querySelector('[data-yes]');
      yes.textContent = o.confirmLabel || 'Confirm';
      yes.addEventListener('click', function () { d.close(true); });
      d.el.querySelector('[data-no]').addEventListener('click', function () { d.close(false); });
      yes.focus();
    });
  };

  /**
   * Generic form dialog.
   * nx.formModal({ title, subtitle, submitLabel, fields:[{name,label,type,value,options:[[v,l]],required,wide,step,min,numeric,placeholder,hint}],
   *                onSubmit: async (values) => ({ ok, error }) })
   */
  nx.formModal = function (o) {
    var d = nx.dialog(
      '<div class="flex items-start justify-between gap-4"><div><h3 class="font-display text-xl font-bold text-slate-100" data-t></h3>' +
      '<p class="mt-1 text-sm text-slate-500" data-s></p></div>' +
      '<button type="button" data-x class="text-slate-500 transition hover:text-slate-200" aria-label="Close"><i data-lucide="x" class="h-5 w-5"></i></button></div>' +
      '<form class="mt-6 grid gap-4 sm:grid-cols-2" novalidate></form>', o.size || '');
    d.el.querySelector('[data-t]').textContent = o.title || '';
    d.el.querySelector('[data-s]').textContent = o.subtitle || '';
    d.el.querySelector('[data-x]').addEventListener('click', function () { d.close(false); });
    var form = d.el.querySelector('form');
    var controls = {};

    /* picture support: o.image = { resource, id, url, name, label } -> change / remove right away (edit dialogs)
                       o.imagePicker = true                          -> optional file chooser; the chosen file is passed to onSubmit(values, file) (add dialogs) */
    var pickedFile = null;
    if (o.image && nx.imageManagerHtml) {
      var holder = document.createElement('div'); holder.className = 'sm:col-span-2';
      holder.innerHTML = nx.imageManagerHtml(o.image);
      form.appendChild(holder); nx.initImageManager(holder.firstElementChild);
    }
    if (o.imagePicker) {
      var pw = document.createElement('div'); pw.className = 'flex items-center gap-4 sm:col-span-2';
      pw.innerHTML = '<span class="relative grid h-16 w-16 shrink-0 place-items-center overflow-hidden rounded-2xl bg-gradient-to-br from-cyan-500/25 to-violet-500/25 ring-1 ring-cyan-400/30"><i data-lucide="image-plus" class="h-5 w-5 text-cyan-300"></i><img data-pick-prev alt="Photo preview" class="absolute inset-0 hidden h-full w-full object-cover" /></span>' +
        '<label class="block min-w-0 flex-1"><span class="mb-1.5 block text-xs font-semibold uppercase tracking-[0.14em] text-slate-500">Photo (optional)</span>' +
        '<input type="file" accept="image/jpeg,image/png,image/webp,image/gif" class="field !py-2 text-xs file:mr-3 file:rounded-lg file:border-0 file:bg-cyan-400/15 file:px-3 file:py-1.5 file:text-xs file:font-semibold file:text-cyan-300" />' +
        '<span data-pick-err class="mt-1 block text-xs text-rose-300"></span></label>';
      form.appendChild(pw);
      var pin = pw.querySelector('input'), ppv = pw.querySelector('[data-pick-prev]'), perr = pw.querySelector('[data-pick-err]');
      pin.addEventListener('change', function () {
        pickedFile = null; perr.textContent = ''; ppv.classList.add('hidden');
        var fl = pin.files[0]; if (!fl) return;
        var bad = nx.checkImage ? nx.checkImage(fl) : '';
        if (bad) { pin.value = ''; perr.textContent = bad; return; }
        pickedFile = fl; ppv.src = URL.createObjectURL(fl); ppv.classList.remove('hidden');
      });
    }

    (o.fields || []).forEach(function (f) {
      var label = document.createElement('label');
      label.className = 'block' + (f.wide ? ' sm:col-span-2' : '');
      var cap = document.createElement('span');
      cap.className = 'mb-1.5 block text-xs font-semibold uppercase tracking-[0.14em] text-slate-500';
      cap.textContent = f.label + (f.required ? ' *' : '');
      label.appendChild(cap);
      var el;
      if (f.type === 'select') {
        el = document.createElement('select');
        var blank = document.createElement('option'); blank.value = ''; blank.textContent = 'Select…'; el.appendChild(blank);
        (f.options || []).forEach(function (op) {
          var opt = document.createElement('option'); opt.value = op[0]; opt.textContent = op[1]; el.appendChild(opt);
        });
      } else if (f.type === 'textarea') {
        el = document.createElement('textarea'); el.rows = 3; el.className = 'resize-none';
      } else if (f.type === 'checkbox') {
        el = document.createElement('input'); el.type = 'checkbox';
      } else {
        el = document.createElement('input'); el.type = f.type || 'text';
        if (f.step) el.step = f.step;
        if (f.min !== undefined) el.min = f.min;
      }
      el.name = f.name;
      if (f.type !== 'checkbox') el.className = ((el.className || '') + ' field').trim();
      if (f.placeholder) el.placeholder = f.placeholder;
      if (f.value !== undefined && f.value !== null) {
        if (f.type === 'checkbox') el.checked = !!f.value; else el.value = f.value;
      }
      label.appendChild(el);
      if (f.hint) {
        var h = document.createElement('span'); h.className = 'mt-1 block text-xs text-slate-600'; h.textContent = f.hint; label.appendChild(h);
      }
      form.appendChild(label);
      controls[f.name] = { el: el, def: f };
    });

    var foot = document.createElement('div');
    foot.className = 'flex flex-wrap items-center gap-3 sm:col-span-2';
    foot.innerHTML = '<button type="submit" class="btn-glow flex items-center gap-2 rounded-xl px-6 py-3 text-sm font-semibold text-white disabled:opacity-60"><span data-icon-slot></span><span data-lbl></span></button>' +
      '<button type="button" data-cancel class="rounded-xl border border-slate-700 px-5 py-3 text-sm font-semibold text-slate-300 transition hover:bg-white/5">Cancel</button>' +
      '<p data-err class="w-full text-sm text-rose-300"></p>';
    foot.querySelector('[data-lbl]').textContent = o.submitLabel || 'Save changes';
    form.appendChild(foot);
    foot.querySelector('[data-cancel]').addEventListener('click', function () { d.close(false); });

    form.addEventListener('submit', async function (e) {
      e.preventDefault();
      var err = foot.querySelector('[data-err]'); err.textContent = '';
      var vals = {};
      for (var name in controls) {
        var c = controls[name], f = c.def, el = c.el, v;
        if (f.type === 'checkbox') v = el.checked;
        else {
          v = el.value.trim();
          if (v === '') {
            if (f.required) { err.textContent = f.label + ' is required'; el.focus(); return; }
            if (f.sendEmpty) vals[name] = null;
            continue;
          }
          if (f.numeric || f.type === 'number') v = Number(v);
        }
        vals[name] = v;
      }
      var submit = foot.querySelector('button[type=submit]');
      nx.busy(submit, true);
      var r;
      try { r = await o.onSubmit(vals, pickedFile); } catch (ex) { r = { ok: false, error: 'Unexpected error — please retry' }; }
      nx.busy(submit, false);
      if (r && r.ok) { d.close(true); } else { err.textContent = (r && r.error) || 'Save failed'; }
    });

    nx.icons();
    var first = form.querySelector('input:not([type=file]),select,textarea'); if (first) first.focus();
    return d;
  };

  /* ------------------------ export (Excel / CSV / PDF) ------------------------ */
  // <div data-table data-export="orders" data-export-filter="#status-select"> gets the chips in its toolbar,
  // <div data-export-slot="bills"> gets them wherever there is no data table
  function exportGroup(resource, filterSel, label) {
    var g = document.createElement('div');
    g.className = 'tt-export'; g.setAttribute('role', 'group'); g.setAttribute('aria-label', 'Export ' + resource);
    g.innerHTML = '<span class="tt-export-label"><i data-lucide="download"></i>' + nx.esc(label || 'Export') + '</span>' +
      '<button type="button" data-fmt="xlsx" title="Download as Excel workbook">Excel</button>' +
      '<button type="button" data-fmt="csv" title="Download as CSV">CSV</button>' +
      '<button type="button" data-fmt="pdf" title="Open a print-friendly report and save it as PDF">PDF</button>';
    g.addEventListener('click', function (e) {
      var b = e.target.closest('[data-fmt]'); if (!b) return;
      var fmt = b.getAttribute('data-fmt'), qs = [];
      var sel = filterSel ? document.querySelector(filterSel) : null;
      if (sel && sel.value && sel.value !== 'all') qs.push('status=' + encodeURIComponent(sel.value));
      if (fmt === 'pdf') {
        qs.push('auto=1');
        window.open('/export/' + resource + '/print?' + qs.join('&'), '_blank');
        return;
      }
      qs.push('format=' + fmt);
      nx.toast('Preparing your ' + (fmt === 'csv' ? 'CSV' : 'Excel') + ' file…', 'info', 2600);
      window.location.href = '/export/' + resource + '?' + qs.join('&');
    });
    return g;
  }
  function initExportSlots() {
    document.querySelectorAll('[data-export-slot]').forEach(function (slot) {
      slot.appendChild(exportGroup(slot.getAttribute('data-export-slot'), slot.getAttribute('data-export-filter'), slot.getAttribute('data-export-label')));
    });
    nx.icons();
  }

  /* ---------------------------- data tables ---------------------------- */
  // <div data-table data-page-size="10"> <table> … <th data-sort> … </table></div>
  // adds search box, sortable headers, paging and a live "x of y" counter
  var tables = [];
  function cellValue(row, idx) {
    var c = row.cells[idx]; if (!c) return '';
    var v = c.getAttribute('data-v'); return v !== null ? v : c.textContent.trim();
  }
  function compareVals(x, y) {
    var dre = /^[A-Z][a-z]{2} \d{1,2}, \d{4}/;
    if (dre.test(x) && dre.test(y)) return Date.parse(x) - Date.parse(y);
    var nre = /^[-+$\s\d.,%]+$/;
    if (nre.test(x) && nre.test(y)) {
      var a = parseFloat(x.replace(/[^0-9.\-]/g, '')), b = parseFloat(y.replace(/[^0-9.\-]/g, ''));
      if (!isNaN(a) && !isNaN(b)) return a - b;
    }
    return x.localeCompare(y, undefined, { numeric: true, sensitivity: 'base' });
  }

  function initTables() {
    document.querySelectorAll('[data-table]').forEach(function (wrap) {
      var table = wrap.querySelector('table'); if (!table || !table.tBodies[0]) return;
      var tbody = table.tBodies[0];
      var rows = Array.prototype.filter.call(tbody.rows, function (r) { return !r.querySelector('td[colspan]'); });
      if (!rows.length) return;

      var pageSize = Number(wrap.dataset.pageSize) || 10;
      var state = { q: '', page: 1, col: -1, dir: 1 };
      var text = rows.map(function (r) { return r.textContent.toLowerCase(); });

      var bar = document.createElement('div'); bar.className = 'tt-bar';
      bar.innerHTML = '<div class="tt-search"><i data-lucide="search"></i><input type="search" class="field" aria-label="Search this table" /></div>' +
        '<span class="tt-count" aria-live="polite"></span>';
      bar.querySelector('input').placeholder = wrap.dataset.searchPlaceholder || 'Search this table…';
      var extra = wrap.querySelector('[data-tt-extra]');
      if (extra) bar.insertBefore(extra, bar.querySelector('.tt-count'));
      if (wrap.dataset.export) bar.appendChild(exportGroup(wrap.dataset.export, wrap.dataset.exportFilter));
      wrap.insertBefore(bar, wrap.firstChild);

      var pager = document.createElement('div'); pager.className = 'tt-pager';
      pager.innerHTML = '<span data-info></span><div class="flex gap-2"><button type="button" data-prev>Previous</button><button type="button" data-next>Next</button></div>';
      wrap.appendChild(pager);

      var none = document.createElement('tr');
      none.innerHTML = '<td colspan="' + (table.tHead ? table.tHead.rows[0].cells.length : 8) + '" class="px-5 py-10 text-center text-sm text-slate-500">No rows match your search.</td>';
      none.hidden = true; tbody.appendChild(none);

      function render() {
        var q = state.q;
        var visible = [];
        rows.forEach(function (r, i) {
          var ok = !r.hidden && (q === '' || text[i].indexOf(q) > -1);
          r._ok = ok; if (ok) visible.push(r);
        });
        var pages = Math.max(1, Math.ceil(visible.length / pageSize));
        if (state.page > pages) state.page = pages;
        var from = (state.page - 1) * pageSize, to = from + pageSize;
        rows.forEach(function (r) { r.removeAttribute('data-tt-hidden'); });
        var shown = 0;
        rows.forEach(function (r) {
          if (!r._ok) { r.setAttribute('data-tt-hidden', ''); return; }
          var idx = visible.indexOf(r);
          if (idx < from || idx >= to) r.setAttribute('data-tt-hidden', ''); else shown++;
        });
        none.hidden = visible.length > 0;
        bar.querySelector('.tt-count').textContent = visible.length + (visible.length === 1 ? ' record' : ' records');
        pager.hidden = visible.length <= pageSize;
        pager.querySelector('[data-info]').textContent = visible.length ? ('Showing ' + (from + 1) + '–' + Math.min(to, visible.length) + ' of ' + visible.length) : '';
        pager.querySelector('[data-prev]').disabled = state.page <= 1;
        pager.querySelector('[data-next]').disabled = state.page >= pages;
      }

      var timer;
      bar.querySelector('input').addEventListener('input', function () {
        var v = this.value.toLowerCase().trim();
        clearTimeout(timer); timer = setTimeout(function () { state.q = v; state.page = 1; render(); }, 120);
      });
      pager.querySelector('[data-prev]').addEventListener('click', function () { state.page--; render(); });
      pager.querySelector('[data-next]').addEventListener('click', function () { state.page++; render(); });

      var heads = table.tHead ? table.tHead.rows[0].cells : [];
      Array.prototype.forEach.call(heads, function (th, idx) {
        if (th.getAttribute('data-sort') === null) return;
        th.setAttribute('tabindex', '0'); th.setAttribute('role', 'button');
        function sortBy() {
          state.dir = state.col === idx ? -state.dir : 1; state.col = idx;
          Array.prototype.forEach.call(heads, function (h) { h.removeAttribute('data-dir'); });
          th.setAttribute('data-dir', state.dir === 1 ? 'asc' : 'desc');
          rows.sort(function (a, b) { return state.dir * compareVals(cellValue(a, idx), cellValue(b, idx)); });
          // text[] must follow the new order
          text = rows.map(function (r) { return r.textContent.toLowerCase(); });
          rows.forEach(function (r) { tbody.insertBefore(r, none); });
          state.page = 1; render();
        }
        th.addEventListener('click', sortBy);
        th.addEventListener('keydown', function (e) { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); sortBy(); } });
      });

      wrap._ttRender = render;
      tables.push(wrap);
      render();
    });
    nx.icons();
  }
  /** call after code that toggles row.hidden (status filters) so paging and counters stay correct */
  nx.refreshTables = function () { tables.forEach(function (w) { if (w._ttRender) w._ttRender(); }); };

  /* -------------------------------- charts -------------------------------- */
  nx.json = function (id) {
    var el = document.getElementById(id); if (!el) return null;
    try { return JSON.parse(el.textContent); } catch (e) { return null; }
  };

  function money0(n) { return '$' + Math.round(n).toLocaleString('en-US'); }

  /** grouped bars: series = [{label, a, b}] (a = invoiced, b = collected) */
  nx.chartBars = function (host, series, names) {
    if (!host || !series || !series.length) return;
    names = names || ['Invoiced', 'Collected'];
    var W = 640, H = 240, padL = 48, padB = 30, padT = 14, padR = 10;
    var max = Math.max.apply(null, series.map(function (s) { return Math.max(s.a, s.b); }).concat([1]));
    var nice = Math.pow(10, Math.floor(Math.log10(max))), top = Math.ceil(max / nice) * nice;
    if (top / nice < 2) top = nice * 2;
    var innerW = W - padL - padR, innerH = H - padT - padB, slot = innerW / series.length, bw = Math.min(26, slot / 3);
    var svg = '<svg viewBox="0 0 ' + W + ' ' + H + '" class="h-auto w-full" role="img" aria-label="Invoiced versus collected, last six months">' +
      '<defs><linearGradient id="gA" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#60a5fa"/><stop offset="1" stop-color="#4f46e5"/></linearGradient>' +
      '<linearGradient id="gB" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#34d399"/><stop offset="1" stop-color="#0d9488"/></linearGradient></defs>';
    for (var g = 0; g <= 4; g++) {
      var y = padT + innerH - (innerH * g / 4);
      svg += '<line x1="' + padL + '" x2="' + (W - padR) + '" y1="' + y + '" y2="' + y + '" stroke="rgba(99,102,241,0.14)"/>' +
        '<text x="' + (padL - 8) + '" y="' + (y + 4) + '" text-anchor="end" font-size="10" fill="#64748b">' + money0(top * g / 4) + '</text>';
    }
    series.forEach(function (s, i) {
      var cx = padL + slot * i + slot / 2;
      [['a', -bw - 2, 'gA', names[0]], ['b', 2, 'gB', names[1]]].forEach(function (k) {
        var h = innerH * (s[k[0]] / top);
        svg += '<rect class="chart-bar" x="' + (cx + k[1]) + '" width="' + bw + '" rx="5" fill="url(#' + k[2] + ')" data-h="' + h + '" data-y="' + (padT + innerH) +
          '" y="' + (padT + innerH) + '" height="0"><title>' + nx.esc(s.label) + ' · ' + k[3] + ': ' + nx.money(s[k[0]]) + '</title></rect>';
      });
      svg += '<text x="' + cx + '" y="' + (H - 8) + '" text-anchor="middle" font-size="11" fill="#94a3b8">' + nx.esc(s.label) + '</text>';
    });
    host.innerHTML = svg + '</svg>';
    requestAnimationFrame(function () {
      setTimeout(function () {
        host.querySelectorAll('.chart-bar').forEach(function (r) {
          var h = Number(r.getAttribute('data-h')), y0 = Number(r.getAttribute('data-y'));
          r.setAttribute('height', h); r.setAttribute('y', y0 - h);
        });
      }, 60);
    });
  };

  /** donut: items = [{label, value, color}] */
  nx.chartDonut = function (host, items, centerLabel) {
    if (!host) return;
    var total = items.reduce(function (t, i) { return t + i.value; }, 0);
    var R = 54, C = 2 * Math.PI * R, off = 0;
    var svg = '<svg viewBox="0 0 140 140" class="mx-auto h-44 w-44 -rotate-90" role="img" aria-label="' + nx.esc(centerLabel || 'Breakdown') + '">' +
      '<circle cx="70" cy="70" r="' + R + '" fill="none" stroke="rgba(99,102,241,0.12)" stroke-width="16"/>';
    if (total > 0) {
      items.forEach(function (i) {
        if (!i.value) return;
        var len = C * i.value / total;
        svg += '<circle class="donut-seg" cx="70" cy="70" r="' + R + '" fill="none" stroke="' + i.color + '" stroke-width="16" stroke-dasharray="0 ' + C +
          '" data-dash="' + len + ' ' + (C - len) + '" stroke-dashoffset="' + (-off) + '"><title>' + nx.esc(i.label) + ': ' + i.value + '</title></circle>';
        off += len;
      });
    }
    svg += '</svg>';
    var legend = '<ul class="mt-4 grid grid-cols-2 gap-x-4 gap-y-2 text-xs">' + items.map(function (i) {
      return '<li class="flex items-center gap-2 text-slate-400"><span class="h-2.5 w-2.5 rounded-full" style="background:' + i.color + '"></span>' +
        nx.esc(i.label) + '<span class="ml-auto font-semibold text-slate-200">' + i.value + '</span></li>';
    }).join('') + '</ul>';
    host.innerHTML = '<div class="relative">' + svg + '<div class="pointer-events-none absolute inset-0 grid place-items-center text-center"><div>' +
      '<p class="font-display text-3xl font-bold text-slate-100">' + total + '</p><p class="text-[10px] uppercase tracking-[0.2em] text-slate-500">' + nx.esc(centerLabel || 'total') + '</p></div></div></div>' + legend;
    setTimeout(function () {
      host.querySelectorAll('.donut-seg').forEach(function (c) { c.setAttribute('stroke-dasharray', c.getAttribute('data-dash')); });
    }, 80);
  };

  function initCharts() {
    var bars = document.querySelector('[data-chart="bars"]');
    if (bars) nx.chartBars(bars, nx.json(bars.dataset.source));
    var palette = { pending: '#fbbf24', feasible: '#3b82f6', infeasible: '#fb7185', connected: '#34d399', cancelled: '#64748b',
      dialup: '#fbbf24', broadband: '#3b82f6', landline: '#8b5cf6' };
    var labels = { pending: 'Pending', feasible: 'Feasible', infeasible: 'Not feasible', connected: 'Connected', cancelled: 'Cancelled',
      dialup: 'Dial-Up', broadband: 'Broadband', landline: 'Landline' };
    document.querySelectorAll('[data-chart="donut"]').forEach(function (host) {
      var data = nx.json(host.dataset.source) || {};
      var items = Object.keys(data).map(function (k) { return { label: labels[k] || k, value: data[k], color: palette[k] || '#94a3b8' }; });
      nx.chartDonut(host, items, host.dataset.center);
    });
  }

  /* ------------------------ animated numbers on stat cards ------------------------ */
  function initStatCounters() {
    var els = document.querySelectorAll('[data-stat]');
    if (!els.length || !('IntersectionObserver' in window)) return;
    var io = new IntersectionObserver(function (entries) {
      entries.forEach(function (en) {
        if (!en.isIntersecting) return; io.unobserve(en.target);
        var el = en.target, raw = el.textContent.trim();
        var m = /^(\D*)([\d,]+(?:\.\d+)?)(.*)$/.exec(raw); if (!m) return;
        var num = parseFloat(m[2].replace(/,/g, '')), dec = (m[2].split('.')[1] || '').length, start = performance.now(), dur = 1100;
        (function step(t) {
          var p = Math.min(1, (t - start) / dur), v = num * (1 - Math.pow(1 - p, 3));
          el.textContent = m[1] + v.toLocaleString('en-US', { minimumFractionDigits: dec, maximumFractionDigits: dec }) + m[3];
          if (p < 1) requestAnimationFrame(step); else el.textContent = raw;
        })(start);
      });
    });
    els.forEach(function (e) { io.observe(e); });
  }

  /* ---------------------------- command palette ---------------------------- */
  function openPalette() {
    if (document.querySelector('.palette')) return;
    var items = [];
    var seen = {};
    document.querySelectorAll('[data-tab]').forEach(function (b) {
      if (seen['t' + b.dataset.tab]) return; seen['t' + b.dataset.tab] = 1;
      items.push({ label: b.textContent.trim().replace(/\s+/g, ' ').replace(/\d+$/, '').trim(), hint: 'Panel', icon: 'layout-dashboard', run: function () { nx.showTab(b.dataset.tab); } });
    });
    [['/', 'Public site', 'house'], ['/account', 'My account', 'user-round'], ['/login', 'Sign in', 'log-in']].forEach(function (l) {
      items.push({ label: l[1], hint: 'Page', icon: l[2], run: function () { location.href = l[0]; } });
    });
    var d = nx.dialog('<div class="palette glass-strong"><input type="text" placeholder="Jump to a panel or page…  (Esc to close)" aria-label="Command palette" />' +
      '<ul role="listbox"></ul></div>', '!p-0 !bg-transparent !border-0 !shadow-none !overflow-visible');
    var input = d.el.querySelector('input'), list = d.el.querySelector('ul'), sel = 0, view = [];
    function draw() {
      var q = input.value.toLowerCase().trim();
      view = items.filter(function (i) { return !q || i.label.toLowerCase().indexOf(q) > -1; });
      if (sel >= view.length) sel = 0;
      list.innerHTML = view.map(function (i, n) {
        return '<li><button type="button" data-n="' + n + '" aria-selected="' + (n === sel) + '"><i data-lucide="' + i.icon + '" class="h-4 w-4 text-cyan-300"></i>' +
          '<span>' + nx.esc(i.label) + '</span><span class="kbd ml-auto">' + i.hint + '</span></button></li>';
      }).join('') || '<li class="px-4 py-6 text-center text-sm text-slate-500">Nothing matches.</li>';
      nx.icons();
    }
    function go(n) { var it = view[n]; if (!it) return; d.close(); it.run(); }
    input.addEventListener('input', function () { sel = 0; draw(); });
    input.addEventListener('keydown', function (e) {
      if (e.key === 'ArrowDown') { e.preventDefault(); sel = Math.min(view.length - 1, sel + 1); draw(); }
      else if (e.key === 'ArrowUp') { e.preventDefault(); sel = Math.max(0, sel - 1); draw(); }
      else if (e.key === 'Enter') { e.preventDefault(); go(sel); }
    });
    list.addEventListener('click', function (e) { var b = e.target.closest('button[data-n]'); if (b) go(Number(b.dataset.n)); });
    draw(); input.focus();
  }

  function initPalette() {
    document.addEventListener('keydown', function (e) {
      var typing = /^(INPUT|TEXTAREA|SELECT)$/.test((document.activeElement || {}).tagName || '');
      if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') { e.preventDefault(); openPalette(); }
      else if (e.key === '/' && !typing && !e.ctrlKey && !e.metaKey) { e.preventDefault(); openPalette(); }
    });
    document.querySelectorAll('[data-open-palette]').forEach(function (b) { b.addEventListener('click', openPalette); });
  }

  /* ------------------------------ small widgets ------------------------------ */
  function initWidgets() {
    // copy to clipboard
    document.addEventListener('click', function (e) {
      var b = e.target.closest('[data-copy]'); if (!b) return;
      var val = b.dataset.copy;
      (navigator.clipboard ? navigator.clipboard.writeText(val) : Promise.reject()).then(function () {
        nx.toast('Copied ' + val, 'success', 2200);
      }, function () { nx.toast('Copy is not available in this browser', 'warn'); });
    });

    // show / hide password
    document.querySelectorAll('[data-toggle-pass]').forEach(function (b) {
      b.addEventListener('click', function () {
        var inp = document.getElementById(b.dataset.togglePass); if (!inp) return;
        var show = inp.type === 'password'; inp.type = show ? 'text' : 'password';
        b.setAttribute('aria-pressed', show); b.innerHTML = '<i data-lucide="' + (show ? 'eye-off' : 'eye') + '" class="h-4 w-4"></i>'; nx.icons();
      });
    });

    // password strength meter
    document.querySelectorAll('[data-strength-for]').forEach(function (m) {
      var inp = document.getElementById(m.dataset.strengthFor); if (!inp) return;
      var segs = m.querySelectorAll('span'), label = m.parentNode.querySelector('[data-strength-label]');
      inp.addEventListener('input', function () {
        var v = inp.value, score = 0;
        if (v.length >= 6) score++; if (v.length >= 10) score++; if (/[A-Z]/.test(v) && /[a-z]/.test(v)) score++; if (/\d/.test(v) && /[^A-Za-z0-9]/.test(v)) score++;
        var colors = ['#fb7185', '#fbbf24', '#38bdf8', '#34d399'], words = ['Too short', 'Weak', 'Okay', 'Strong'];
        segs.forEach(function (s, i) { s.style.background = v && i < Math.max(score, 1) ? colors[Math.max(score, 1) - 1] : ''; });
        if (label) label.textContent = v ? words[Math.max(score, 1) - 1] : '';
      });
    });

    // star rating
    document.querySelectorAll('[data-stars]').forEach(function (box) {
      var input = box.querySelector('input[type=hidden]'), btns = box.querySelectorAll('button');
      function paint(n) { btns.forEach(function (b) { b.classList.toggle('on', Number(b.dataset.v) <= n); }); }
      btns.forEach(function (b) {
        b.addEventListener('click', function () { input.value = b.dataset.v; paint(Number(b.dataset.v)); });
        b.addEventListener('mouseenter', function () { paint(Number(b.dataset.v)); });
      });
      box.addEventListener('mouseleave', function () { paint(Number(input.value)); });
      paint(Number(input.value));
    });

    // "go to tab" shortcuts
    document.querySelectorAll('[data-go-tab]').forEach(function (b) {
      b.addEventListener('click', function () { if (nx.showTab) nx.showTab(b.dataset.goTab); });
    });

    // print
    document.querySelectorAll('[data-print]').forEach(function (b) { b.addEventListener('click', function () { window.print(); }); });
  }

  document.addEventListener('DOMContentLoaded', function () {
    initHeader();
    initReveal();
    initCounters();
    initNetwork();
    initPlans();
    initShell();
    initTabs();
    initWidgets();
    initTables();
    initExportSlots();
    initCalculator();
    initCharts();
    initStatCounters();
    initPalette();
    showFlash();
    nx.icons();
  });
})();
