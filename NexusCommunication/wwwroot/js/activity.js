/* Admin "Activity log" tab - reads /api/audit (administrators only). Filters run on the server, so the log can grow without slowing the page.
   Rendered with textContent only. */
(function () {
  'use strict';

  var root = document.getElementById('audit-root');
  if (!root) return;

  var $ = function (id) { return document.getElementById(id); };
  var body = $('audit-body'), pager = $('audit-pager'), count = $('audit-count');
  var fUser = $('audit-user'), fAction = $('audit-action'), fFrom = $('audit-from'), fTo = $('audit-to'), fQ = $('audit-q');
  var page = 1, pages = 1, ticket = 0, metaDone = false, timer;

  var ACTIONS = {
    create: ['Created', 'border-emerald-400/40 bg-emerald-400/10 text-emerald-300'],
    update: ['Edited', 'border-sky-400/40 bg-sky-400/10 text-sky-300'],
    delete: ['Deleted', 'border-rose-400/40 bg-rose-400/10 text-rose-300'],
    status: ['Status change', 'border-amber-400/40 bg-amber-400/10 text-amber-300'],
    payment: ['Payment', 'border-emerald-400/40 bg-emerald-400/10 text-emerald-300'],
    image: ['Picture', 'border-violet-400/40 bg-violet-400/10 text-violet-300'],
    login: ['Sign in', 'border-slate-500/40 bg-slate-500/10 text-slate-400'],
    logout: ['Sign out', 'border-slate-500/40 bg-slate-500/10 text-slate-400']
  };

  function el(tag, cls, text) {
    var n = document.createElement(tag);
    if (cls) n.className = cls;
    if (text != null) n.textContent = text;
    return n;
  }

  function messageRow(text) {
    body.textContent = '';
    var tr = el('tr'), td = el('td', 'px-5 py-10 text-center text-sm text-slate-500', text);
    td.colSpan = 5; tr.appendChild(td); body.appendChild(tr);
  }

  function row(r) {
    var tr = el('tr', 'table-row');
    var d = new Date(r.at);

    var when = el('td', 'px-5 py-3.5 whitespace-nowrap');
    when.appendChild(el('p', 'text-xs font-semibold text-slate-300', d.toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric' })));
    when.appendChild(el('p', 'text-xs text-slate-500', d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })));

    var who = el('td', 'px-5 py-3.5');
    var wrap = el('div', 'flex items-center gap-3');
    var av = el('span', 'relative grid h-9 w-9 shrink-0 place-items-center overflow-hidden rounded-full bg-cyan-400/10 text-xs font-bold text-cyan-300', r.initials || '?');
    if (r.image) {
      var img = document.createElement('img');
      img.src = r.image; img.alt = r.userName || ''; img.loading = 'lazy';
      img.className = 'absolute inset-0 h-full w-full object-cover';
      img.onerror = function () { img.remove(); };
      av.appendChild(img);
    }
    var txt = el('div', 'min-w-0');
    txt.appendChild(el('p', 'font-semibold text-slate-200', r.userName || '—'));
    if (r.role) txt.appendChild(el('p', 'text-xs capitalize text-slate-500', r.role));
    wrap.appendChild(av); wrap.appendChild(txt); who.appendChild(wrap);

    var a = ACTIONS[r.action] || [r.action, 'border-slate-500/40 bg-slate-500/10 text-slate-400'];
    var act = el('td', 'px-5 py-3.5');
    act.appendChild(el('span', 'inline-flex items-center rounded-full border px-3 py-1 text-[11px] font-semibold uppercase tracking-[0.12em] ' + a[1], a[0]));

    var rec = el('td', 'px-5 py-3.5');
    rec.appendChild(el('p', 'text-slate-300', r.entity));
    if (r.entityId) rec.appendChild(el('p', 'font-mono text-xs text-slate-500', r.entityId));

    var det = el('td', 'px-5 py-3.5');
    det.appendChild(el('p', 'max-w-xl break-words text-xs leading-relaxed text-slate-400', r.summary));

    [when, who, act, rec, det].forEach(function (c) { tr.appendChild(c); });
    return tr;
  }

  function query(p) {
    var q = new URLSearchParams();
    if (fUser.value) q.set('user', fUser.value);
    if (fAction.value) q.set('action', fAction.value);
    if (fFrom.value) q.set('from', fFrom.value);
    if (fTo.value) q.set('to', fTo.value);
    if (fQ.value.trim()) q.set('q', fQ.value.trim());
    q.set('tz', String(new Date().getTimezoneOffset()));   // so "from / to" mean the admin's own calendar days
    q.set('page', String(p));
    return q.toString();
  }

  async function load(p) {
    page = p || 1;
    var mine = ++ticket;
    var res = await nx.api('/api/audit?' + query(page));
    if (mine !== ticket) return;                           // a newer filter change already took over
    if (!res.ok) { messageRow(nx.errText(res, 'Could not load the activity log')); count.textContent = ''; pager.hidden = true; return; }

    var data = res.data, items = data.items || [];
    pages = Math.max(1, Math.ceil(data.total / data.pageSize));
    count.textContent = data.total + (data.total === 1 ? ' entry' : ' entries');
    if (!items.length) { messageRow('No activity matches these filters.'); pager.hidden = true; return; }

    body.textContent = '';
    items.forEach(function (r) { body.appendChild(row(r)); });
    pager.hidden = data.total <= data.pageSize;
    var from = (data.page - 1) * data.pageSize + 1;
    pager.querySelector('[data-info]').textContent = 'Showing ' + from + '–' + Math.min(data.page * data.pageSize, data.total) + ' of ' + data.total;
    pager.querySelector('[data-prev]').disabled = data.page <= 1;
    pager.querySelector('[data-next]').disabled = data.page >= pages;
  }

  async function loadMeta() {
    var res = await nx.api('/api/audit/meta');
    if (!res.ok) return;
    metaDone = true;
    (res.data.users || []).forEach(function (u) {
      var o = el('option', '', u); o.value = u; fUser.appendChild(o);
    });
  }

  function activate() {
    if (!metaDone) loadMeta();
    load(page);                                            // fresh entries every time the tab is opened
  }

  [fUser, fAction, fFrom, fTo].forEach(function (f) { f.addEventListener('change', function () { load(1); }); });
  fQ.addEventListener('input', function () { clearTimeout(timer); timer = setTimeout(function () { load(1); }, 300); });
  $('audit-reset').addEventListener('click', function () {
    fUser.value = ''; fAction.value = ''; fFrom.value = ''; fTo.value = ''; fQ.value = ''; load(1);
  });
  pager.querySelector('[data-prev]').addEventListener('click', function () { if (page > 1) load(page - 1); });
  pager.querySelector('[data-next]').addEventListener('click', function () { if (page < pages) load(page + 1); });

  document.addEventListener('nx:tab', function (e) { if (e.detail === 'activity') activate(); });
  var panel = document.querySelector('[data-panel="activity"]');
  if (panel && !panel.hidden) activate();                  // page opened straight on #activity
})();
