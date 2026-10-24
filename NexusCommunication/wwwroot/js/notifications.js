/* Nexus — notification bell.
   Bell buttons: Views/Shared/_NotificationBell.cshtml ([data-nxn-btn]). Data: /api/notifications (always the signed-in person's own alerts).
   One dropdown is shared by every bell on the page and sits on <body>, so a scrolling sidebar can never clip it.
   Everything is rendered with textContent, never innerHTML. */
(function () {
  'use strict';

  var buttons = document.querySelectorAll('[data-nxn-btn]');
  if (!buttons.length) return;

  var POLL_MS = 45000;
  var panel = null, list = null, allBtn = null, sub = null, activeBtn = null;
  var unread = 0, items = [], opened = false, lastCount = null;

  var ICONS = { order: 'clipboard-list', bill: 'receipt', payment: 'hand-coins', complaint: 'message-square-warning', ticket: 'ticket', info: 'bell' };

  /* ------------------------------ helpers ------------------------------ */
  function el(tag, cls, text) {
    var n = document.createElement(tag);
    if (cls) n.className = cls;
    if (text != null) n.textContent = text;
    return n;
  }
  function icons() { if (window.lucide) window.lucide.createIcons(); }

  async function api(url, method) {
    var m = document.querySelector('meta[name="csrf-token"]');
    var res = await fetch(url, {
      method: method || 'GET',
      headers: { RequestVerificationToken: m ? m.content : '' },
      credentials: 'same-origin'
    });
    if (!res.ok) throw new Error('HTTP ' + res.status);
    return res.json();
  }

  function timeAgo(iso) {
    var t = new Date(iso).getTime();
    if (isNaN(t)) return '';
    var s = Math.max(0, Math.round((Date.now() - t) / 1000));
    if (s < 60) return 'Just now';
    var m = Math.round(s / 60); if (m < 60) return m + ' min ago';
    var h = Math.round(m / 60); if (h < 24) return h + ' h ago';
    var d = Math.round(h / 24); if (d < 7) return d + ' day' + (d === 1 ? '' : 's') + ' ago';
    return new Date(iso).toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric' });
  }

  /* ------------------------------- badge ------------------------------- */
  function setBadge(n, animate) {
    unread = n;
    buttons.forEach(function (b) {
      var badge = b.querySelector('[data-nxn-badge]');
      if (!badge) return;
      badge.hidden = n <= 0;
      badge.textContent = n > 99 ? '99+' : String(n);
      if (animate && n > 0) { badge.classList.remove('pop'); void badge.offsetWidth; badge.classList.add('pop'); }
      b.setAttribute('aria-label', n > 0 ? 'Notifications, ' + n + ' unread' : 'Notifications');
    });
    if (allBtn) allBtn.disabled = n <= 0;
    if (sub) sub.textContent = n > 0 ? n + ' unread' : 'You are all caught up';
  }

  /* ------------------------------ dropdown ------------------------------ */
  function build() {
    panel = el('section', 'nxn-panel glass-strong');
    panel.hidden = true;
    panel.setAttribute('role', 'dialog');
    panel.setAttribute('aria-label', 'Notifications');

    var head = el('header', 'nxn-head');
    var t = el('div');
    t.appendChild(el('h2', '', 'Notifications'));
    sub = el('small', '', '');
    t.appendChild(sub);
    allBtn = el('button', 'nxn-all', 'Mark all read');
    allBtn.type = 'button';
    allBtn.addEventListener('click', markAll);
    head.appendChild(t); head.appendChild(allBtn);

    list = el('ul', 'nxn-list');
    list.setAttribute('aria-live', 'polite');
    panel.appendChild(head); panel.appendChild(list);
    document.body.appendChild(panel);
  }

  function render() {
    list.textContent = '';
    if (!items.length) {
      var li = el('li', 'nxn-empty');
      li.appendChild(el('b', '', 'No notifications yet'));
      li.appendChild(document.createTextNode('Order updates, bills and payments will show up here.'));
      list.appendChild(li);
      return;
    }
    items.forEach(function (n) {
      var li = el('li');
      var b = el('button', 'nxn-item' + (n.isRead ? '' : ' unread'));
      b.type = 'button';
      var ico = el('span', 'nxn-ico t-' + (ICONS[n.type] ? n.type : 'info'));
      var i = document.createElement('i'); i.setAttribute('data-lucide', ICONS[n.type] || 'bell');
      ico.appendChild(i);
      var body = el('span', 'nxn-body');
      body.appendChild(el('span', 'nxn-title', n.title));
      body.appendChild(el('span', 'nxn-msg', n.message));
      body.appendChild(el('span', 'nxn-time', timeAgo(n.createdAt)));
      b.appendChild(ico); b.appendChild(body); b.appendChild(el('span', 'nxn-dot'));
      b.addEventListener('click', function () { openItem(n); });
      li.appendChild(b);
      list.appendChild(li);
    });
    icons();
  }

  function place() {
    if (!activeBtn) return;
    var r = activeBtn.getBoundingClientRect();
    var w = Math.min(376, window.innerWidth - 24);
    var left = r.left > window.innerWidth / 2 ? r.right - w : r.left;
    left = Math.max(12, Math.min(left, window.innerWidth - w - 12));
    var top = r.bottom + 8;
    panel.style.left = left + 'px';
    panel.style.top = top + 'px';
    panel.style.maxHeight = Math.max(200, Math.min(480, window.innerHeight - top - 12)) + 'px';
  }

  async function refresh() {
    try {
      var data = await api('/api/notifications');
      items = data.items || [];
      setBadge(data.unread || 0);
      lastCount = data.unread || 0;
      if (opened) render();
    } catch (e) {
      if (opened && !items.length) {
        list.textContent = '';
        list.appendChild(el('li', 'nxn-empty', 'Could not load notifications. Please try again.'));
      }
    }
  }

  function open(btn) {
    if (!panel) build();
    activeBtn = btn;
    opened = true;
    panel.hidden = false;
    buttons.forEach(function (b) { b.setAttribute('aria-expanded', b === btn ? 'true' : 'false'); });
    place();
    render();
    setBadge(unread);
    refresh();
  }

  function close(returnFocus) {
    if (!panel || panel.hidden) return;
    panel.hidden = true;
    opened = false;
    buttons.forEach(function (b) { b.setAttribute('aria-expanded', 'false'); });
    if (returnFocus && activeBtn) activeBtn.focus();
  }

  /* -------------------------------- actions ------------------------------- */
  async function markAll() {
    if (unread <= 0) return;
    var before = items.map(function (n) { return n.isRead; });
    items.forEach(function (n) { n.isRead = true; });
    setBadge(0); render();
    try { await api('/api/notifications/read-all', 'POST'); }
    catch (e) {                                   // roll back so the badge never lies
      items.forEach(function (n, k) { n.isRead = before[k]; });
      refresh();
    }
  }

  function goTo(link) {
    if (!link || link.charAt(0) !== '/' || link.charAt(1) === '/') return;
    var u;
    try { u = new URL(link, location.origin); } catch (e) { return; }
    var nx = window.nx;
    if (u.pathname === location.pathname && u.hash) {
      var id = u.hash.slice(1);
      if (nx && nx.showTab && nx.tabIds && nx.tabIds.indexOf(id) >= 0) { nx.showTab(id); return; }
    }
    location.href = link;
  }

  function openItem(n) {
    if (!n.isRead) {
      n.isRead = true;
      setBadge(Math.max(0, unread - 1));
      api('/api/notifications/' + n.id + '/read', 'POST').catch(function () { /* the next poll corrects the badge */ });
    }
    close(false);
    goTo(n.link);
  }

  /* --------------------------------- wiring ------------------------------- */
  buttons.forEach(function (b) {
    b.addEventListener('click', function (e) {
      e.stopPropagation();
      if (opened && activeBtn === b) close(false); else { if (opened) close(false); open(b); }
    });
  });
  document.addEventListener('mousedown', function (e) {
    if (!opened) return;
    if (panel.contains(e.target) || e.target.closest('[data-nxn-btn]')) return;
    close(false);
  });
  document.addEventListener('keydown', function (e) { if (e.key === 'Escape' && opened) close(true); });
  window.addEventListener('resize', function () { if (opened) place(); });

  /* live badge: poll while the tab is visible, and catch up when the person comes back to it */
  async function poll() {
    if (document.visibilityState !== 'visible') return;
    try {
      var data = await api('/api/notifications/count');
      var n = data.unread || 0;
      var grew = lastCount !== null && n > lastCount;
      setBadge(n, grew);
      if (grew && window.nx && nx.toast) nx.toast('You have a new notification', 'info', 3200);
      if (n !== lastCount && opened) refresh();
      lastCount = n;
    } catch (e) { /* offline or signed out - try again next time */ }
  }
  var first = buttons[0].querySelector('[data-nxn-badge]');
  lastCount = first && !first.hidden ? parseInt(first.textContent, 10) || 0 : 0;
  unread = lastCount;
  setInterval(poll, POLL_MS);
  document.addEventListener('visibilitychange', function () { if (document.visibilityState === 'visible') poll(); });
})();
