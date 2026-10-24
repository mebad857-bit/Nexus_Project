/* Nexus — dashboard charts (Chart.js).
   Admin:    monthly revenue (line), orders by status (doughnut), connections by plan (bar)
   Accounts: collections vs dues (bar) + collected vs outstanding (doughnut)
   Every number comes from real database records: the views write them into <script type="application/json"> blocks.
   A chart lives in any element that has data-cjs="<kind>" and data-source="<json block id>".
   If Chart.js cannot be loaded (offline), the two charts that existed before fall back to the old SVG charts. */
(function () {
  'use strict';

  var hosts = document.querySelectorAll('[data-cjs]');
  if (!hosts.length) return;

  var nx = window.nx || {};
  var PALETTE = { pending: '#fbbf24', feasible: '#3b82f6', infeasible: '#fb7185', connected: '#34d399', cancelled: '#94a3b8',
    dialup: '#fbbf24', broadband: '#3b82f6', landline: '#8b5cf6' };
  var LABELS = { pending: 'Pending', feasible: 'Feasible', infeasible: 'Not feasible', connected: 'Connected', cancelled: 'Cancelled',
    dialup: 'Dial-Up', broadband: 'Broadband', landline: 'Landline' };
  var INK = '#334155', MUTED = '#64748b', GRID = 'rgba(99, 102, 241, 0.12)';
  var GREEN = '#10b981', ROSE = '#f43f5e', BLUE = '#3b82f6', INDIGO = '#4f46e5';

  function money(n) {
    return nx.money ? nx.money(n) : '$' + Number(n || 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }
  function axisMoney(n) {
    n = Number(n) || 0;
    return '$' + (n >= 1000 ? (n / 1000).toFixed(n >= 10000 ? 0 : 1).replace(/\.0$/, '') + 'k' : Math.round(n));
  }
  function read(host) {
    var id = host.getAttribute('data-source');
    var el = id && document.getElementById(id);
    if (!el) return null;
    try { return JSON.parse(el.textContent); } catch (e) { return null; }
  }
  function note(host, text) {
    host.textContent = '';
    var p = document.createElement('p');
    p.className = 'py-10 text-center text-sm text-slate-500';
    p.textContent = text;
    host.appendChild(p);
  }
  function makeCanvas(host, label, height) {
    host.textContent = '';
    var wrap = document.createElement('div');
    wrap.className = 'relative';
    wrap.style.height = height;
    var c = document.createElement('canvas');
    c.setAttribute('role', 'img');
    c.setAttribute('aria-label', label);
    wrap.appendChild(c);
    host.appendChild(wrap);
    return c;
  }
  var reduce = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

  /* ------------------------- fallback when Chart.js is missing ------------------------- */
  if (!window.Chart) {
    hosts.forEach(function (host) {
      var kind = host.getAttribute('data-cjs'), data = read(host);
      if (kind === 'revenue' && nx.chartBars && data) nx.chartBars(host, data);
      else if (kind === 'status' && nx.chartDonut && data) {
        nx.chartDonut(host, Object.keys(data).map(function (k) { return { label: LABELS[k] || k, value: data[k], color: PALETTE[k] || '#94a3b8' }; }), 'orders');
      } else note(host, 'The chart could not be loaded. Please check your internet connection and refresh.');
    });
    return;
  }

  /* ------------------------------- shared look ------------------------------- */
  var C = window.Chart;
  C.defaults.font.family = 'Inter, ui-sans-serif, system-ui, sans-serif';
  C.defaults.font.size = 11;
  C.defaults.color = MUTED;
  C.defaults.animation = reduce ? false : { duration: 900, easing: 'easeOutQuart' };

  var tooltip = {
    backgroundColor: '#1e293b', titleColor: '#fff', bodyColor: '#e2e8f0', padding: 10, cornerRadius: 10, boxPadding: 4,
    titleFont: { weight: '700' }
  };
  var legendBottom = {
    position: 'bottom',
    labels: { usePointStyle: true, pointStyle: 'circle', boxWidth: 8, boxHeight: 8, padding: 14, color: INK }
  };
  function moneyScale(extra) {
    return Object.assign({ beginAtZero: true, grid: { color: GRID, drawBorder: false }, border: { display: false },
      ticks: { color: MUTED, callback: axisMoney, maxTicksLimit: 6 } }, extra || {});
  }
  var catScale = { grid: { display: false }, border: { color: GRID }, ticks: { color: MUTED } };

  /** text in the hole of a doughnut: big number + small caption */
  function centerText(big, small) {
    return {
      id: 'nxCenter',
      afterDraw: function (chart) {
        var a = chart.chartArea, x = (a.left + a.right) / 2, y = (a.top + a.bottom) / 2, ctx = chart.ctx;
        ctx.save();
        ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
        ctx.fillStyle = '#0f172a'; ctx.font = '700 26px "Space Grotesk", Inter, sans-serif';
        ctx.fillText(big, x, y - 7);
        ctx.fillStyle = MUTED; ctx.font = '600 10px Inter, sans-serif';
        ctx.fillText(String(small).toUpperCase(), x, y + 15);
        ctx.restore();
      }
    };
  }

  /* --------------------------------- charts --------------------------------- */

  /** admin: monthly revenue - invoiced vs collected, two lines */
  function revenue(host) {
    var rows = read(host);
    if (!rows || !rows.length) return note(host, 'No billing data yet.');
    var cv = makeCanvas(host, 'Monthly revenue, invoiced and collected, last six months', '16rem');
    var g = cv.getContext('2d');
    function fill(color) {
      var gr = g.createLinearGradient(0, 0, 0, 260);
      gr.addColorStop(0, color + '33'); gr.addColorStop(1, color + '00');
      return gr;
    }
    new C(cv, {
      type: 'line',
      data: {
        labels: rows.map(function (r) { return r.label; }),
        datasets: [
          { label: 'Invoiced', data: rows.map(function (r) { return r.a; }), borderColor: BLUE, backgroundColor: fill(BLUE), fill: true,
            tension: 0.38, borderWidth: 2.5, pointRadius: 4, pointHoverRadius: 6, pointBackgroundColor: '#fff', pointBorderColor: BLUE, pointBorderWidth: 2 },
          { label: 'Collected', data: rows.map(function (r) { return r.b; }), borderColor: GREEN, backgroundColor: fill(GREEN), fill: true,
            tension: 0.38, borderWidth: 2.5, pointRadius: 4, pointHoverRadius: 6, pointBackgroundColor: '#fff', pointBorderColor: GREEN, pointBorderWidth: 2 }
        ]
      },
      options: {
        responsive: true, maintainAspectRatio: false, interaction: { mode: 'index', intersect: false },
        plugins: { legend: { display: false },
          tooltip: Object.assign({}, tooltip, { callbacks: { label: function (c) { return ' ' + c.dataset.label + ': ' + money(c.parsed.y); } } }) },
        scales: { x: catScale, y: moneyScale() }
      }
    });
  }

  /** admin: orders by status - doughnut */
  function status(host) {
    var data = read(host) || {};
    var keys = Object.keys(data).filter(function (k) { return data[k] > 0; });
    if (!keys.length) return note(host, 'No orders yet.');
    var total = keys.reduce(function (t, k) { return t + data[k]; }, 0);
    var cv = makeCanvas(host, 'Orders by status', '17rem');
    new C(cv, {
      type: 'doughnut',
      data: {
        labels: keys.map(function (k) { return LABELS[k] || k; }),
        datasets: [{ data: keys.map(function (k) { return data[k]; }), backgroundColor: keys.map(function (k) { return PALETTE[k] || '#94a3b8'; }),
          borderColor: '#fff', borderWidth: 3, hoverOffset: 6 }]
      },
      options: {
        responsive: true, maintainAspectRatio: false, cutout: '68%',
        plugins: { legend: legendBottom,
          tooltip: Object.assign({}, tooltip, { callbacks: { label: function (c) {
            return ' ' + c.label + ': ' + c.parsed + ' (' + Math.round(c.parsed / total * 100) + '%)'; } } }) }
      },
      plugins: [centerText(String(total), 'orders')]
    });
  }

  /** admin: connections by plan - horizontal bars coloured by connection type */
  function plans(host) {
    var rows = read(host);
    if (!rows || !rows.length) return note(host, 'No connections yet — plan-wise numbers appear once lines are provisioned.');
    var cv = makeCanvas(host, 'Connections by plan', Math.max(14, rows.length * 2.4 + 3) + 'rem');
    new C(cv, {
      type: 'bar',
      data: {
        labels: rows.map(function (r) { return r.name; }),
        datasets: [{ label: 'Connections', data: rows.map(function (r) { return r.count; }),
          backgroundColor: rows.map(function (r) { return PALETTE[r.type] || BLUE; }), borderRadius: 8, borderSkipped: false, maxBarThickness: 26 }]
      },
      options: {
        indexAxis: 'y', responsive: true, maintainAspectRatio: false,
        plugins: { legend: { display: false },
          tooltip: Object.assign({}, tooltip, { callbacks: {
            title: function (items) { var r = rows[items[0].dataIndex]; return r.name; },
            afterTitle: function (items) { var r = rows[items[0].dataIndex]; return (LABELS[r.type] || r.type) + ' · ' + r.category; },
            label: function (c) { return ' ' + c.parsed.x + ' connection' + (c.parsed.x === 1 ? '' : 's'); } } }) },
        scales: {
          x: { beginAtZero: true, grid: { color: GRID }, border: { display: false }, ticks: { precision: 0, color: MUTED } },
          y: { grid: { display: false }, border: { color: GRID }, ticks: { color: INK } }
        }
      }
    });
  }

  /** accounts: collections vs dues per month - grouped bars */
  function collections(host) {
    var rows = read(host);
    if (!rows || !rows.length) return note(host, 'No billing data yet.');
    var cv = makeCanvas(host, 'Collections versus dues, last six months', '16rem');
    new C(cv, {
      type: 'bar',
      data: {
        labels: rows.map(function (r) { return r.label; }),
        datasets: [
          { label: 'Collected', data: rows.map(function (r) { return r.collected; }), backgroundColor: GREEN, borderRadius: 7, borderSkipped: false, maxBarThickness: 28 },
          { label: 'Dues', data: rows.map(function (r) { return r.dues; }), backgroundColor: ROSE, borderRadius: 7, borderSkipped: false, maxBarThickness: 28 }
        ]
      },
      options: {
        responsive: true, maintainAspectRatio: false, interaction: { mode: 'index', intersect: false },
        plugins: { legend: { display: false },
          tooltip: Object.assign({}, tooltip, { callbacks: { label: function (c) { return ' ' + c.dataset.label + ': ' + money(c.parsed.y); } } }) },
        scales: { x: catScale, y: moneyScale() }
      }
    });
  }

  /** accounts: how much of everything billed has been collected - doughnut */
  function balance(host) {
    var d = read(host) || {};
    var collected = Number(d.collected) || 0, outstanding = Number(d.outstanding) || 0, total = collected + outstanding;
    if (total <= 0) return note(host, 'Nothing billed yet.');
    var cv = makeCanvas(host, 'Collected versus outstanding', '15rem');
    new C(cv, {
      type: 'doughnut',
      data: { labels: ['Collected', 'Outstanding'], datasets: [{ data: [collected, outstanding], backgroundColor: [GREEN, ROSE], borderColor: '#fff', borderWidth: 3, hoverOffset: 6 }] },
      options: {
        responsive: true, maintainAspectRatio: false, cutout: '70%',
        plugins: { legend: legendBottom,
          tooltip: Object.assign({}, tooltip, { callbacks: { label: function (c) { return ' ' + c.label + ': ' + money(c.parsed); } } }) }
      },
      plugins: [centerText(Math.round(collected / total * 100) + '%', 'collected')]
    });
  }

  var KINDS = { revenue: revenue, status: status, plans: plans, collections: collections, balance: balance };
  hosts.forEach(function (host) {
    var fn = KINDS[host.getAttribute('data-cjs')];
    if (!fn) return;
    try { fn(host); } catch (e) { note(host, 'This chart could not be drawn.'); if (window.console) console.error(e); }
  });
})();
