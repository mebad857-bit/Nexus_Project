/* Complaint tickets — one script for the customer portal, the admin desk and the technical desk.
   Markup: Views/Shared/_TicketsPanel.cshtml · server: /api/tickets (Controllers/Api/TicketsApiController.cs)
   Flow: open → assigned → in progress → resolved → closed */
(function () {
  'use strict';
  var root = document.querySelector('[data-tickets]');
  if (!root) return;

  var mode = root.dataset.mode;                       // customer | admin | technical
  var $ = function (id) { return document.getElementById(id); };
  var esc = nx.esc;
  var PAGE = 10;
  var items = [], counts = {}, state = { status: '', q: '', page: 1 };

  var STEPS = [['open', 'Open', 'inbox'], ['assigned', 'Assigned', 'user-check'], ['in_progress', 'In progress', 'wrench'], ['resolved', 'Resolved', 'circle-check'], ['closed', 'Closed', 'archive']];
  var LABEL = { open: 'Open', assigned: 'Assigned', in_progress: 'In progress', resolved: 'Resolved', closed: 'Closed' };
  var PILL = {
    open: 'border-amber-400/40 bg-amber-400/10 text-amber-300',
    assigned: 'border-sky-400/40 bg-sky-400/10 text-sky-300',
    in_progress: 'border-violet-400/40 bg-violet-400/10 text-violet-300',
    resolved: 'border-emerald-400/40 bg-emerald-400/10 text-emerald-300',
    closed: 'border-slate-500/40 bg-slate-500/10 text-slate-400'
  };
  var DOT = { open: 'bg-amber-400', assigned: 'bg-sky-400', in_progress: 'bg-violet-400', resolved: 'bg-emerald-400', closed: 'bg-slate-500' };
  var ROLE = { customer: 'Customer', technical: 'Technical desk', admin: 'Admin' };
  var ACT_BTN = 'rounded-xl border border-slate-700/80 px-4 py-2.5 text-sm font-semibold text-slate-300 transition hover:border-cyan-400/50 hover:text-cyan-300';

  function pill(s) {
    return '<span class="inline-flex items-center gap-1.5 rounded-full border px-3 py-1 text-[11px] font-semibold uppercase tracking-[0.12em] ' + (PILL[s] || PILL.open) + '">' +
      '<span class="h-1.5 w-1.5 rounded-full ' + (DOT[s] || 'bg-slate-400') + '"></span>' + esc(LABEL[s] || s) + '</span>';
  }
  function stamp(d) {
    if (!d) return '—';
    var x = new Date(d); if (isNaN(x)) return '—';
    return x.toLocaleString('en-US', { month: 'short', day: '2-digit', hour: 'numeric', minute: '2-digit' });
  }
  function day(d) {
    if (!d) return '—';
    var x = new Date(d); if (isNaN(x)) return '—';
    return x.toLocaleDateString('en-US', { month: 'short', day: '2-digit', year: 'numeric' });
  }
  function ago(d) {
    var x = new Date(d); if (isNaN(x)) return '';
    var m = Math.max(0, Math.round((Date.now() - x.getTime()) / 60000));
    if (m < 1) return 'just now';
    if (m < 60) return m + ' min ago';
    var h = Math.round(m / 60); if (h < 24) return h + ' h ago';
    var dd = Math.round(h / 24); return dd + (dd === 1 ? ' day ago' : ' days ago');
  }
  async function postForm(url, fd) {
    var res = await fetch(url, { method: 'POST', headers: { RequestVerificationToken: nx.csrf() }, credentials: 'same-origin', body: fd });
    var data = {}; try { data = await res.json(); } catch (e) { /* empty body */ }
    return { ok: res.ok, data: data };
  }

  /* ------------------------------ loading + list ------------------------------ */
  async function load() {
    var res = await nx.api('/api/tickets');
    if (!res.ok) {
      var msg = nx.errText(res, 'Could not load tickets');
      if (mode === 'customer') $('tk-list').innerHTML = '<p class="rounded-2xl border border-rose-400/30 bg-rose-400/5 px-4 py-3 text-sm text-rose-300">' + esc(msg) + '</p>';
      else $('tk-body').innerHTML = '<tr><td colspan="7" class="px-5 py-10 text-center text-sm text-rose-300">' + esc(msg) + '</td></tr>';
      return;
    }
    items = res.data.items || []; counts = res.data.counts || {};
    render();
  }

  function filtered() {
    var q = state.q.toLowerCase();
    return items.filter(function (t) {
      if (state.status && t.status !== state.status) return false;
      if (!q) return true;
      return (t.ticketNo + ' ' + t.subject + ' ' + (t.customerName || '') + ' ' + (t.category || '') + ' ' + (t.assigneeName || '')).toLowerCase().indexOf(q) > -1;
    });
  }

  function render() { if (mode === 'customer') renderCustomer(); else renderStaff(); nx.icons(); }

  function renderCustomer() {
    var list = filtered(), host = $('tk-list');
    var open = (counts.open || 0) + (counts.assigned || 0) + (counts.in_progress || 0);
    $('tk-summary').textContent = items.length === 0 ? 'Nothing raised yet.' :
      items.length + ' in total · ' + open + ' being handled · ' + (counts.resolved || 0) + ' resolved';
    if (list.length === 0) {
      host.innerHTML = '<div class="flex flex-col items-center justify-center gap-3 rounded-2xl border border-dashed border-slate-700/70 py-14 text-center">' +
        '<i data-lucide="life-buoy" class="h-8 w-8 text-slate-600"></i>' +
        '<p class="font-display text-sm font-semibold text-slate-300">' + (items.length ? 'No complaints with this status' : 'No complaints yet') + '</p>' +
        '<p class="max-w-sm text-xs text-slate-500">' + (items.length ? 'Change the filter to see the rest.' : 'If something is not working, raise a complaint on the left and we will take it from there.') + '</p></div>';
      return;
    }
    host.innerHTML = list.map(function (t) {
      var who = t.assigneeName ? 'Handled by ' + esc(t.assigneeName) : (t.status === 'open' ? 'Waiting to be assigned' : '');
      return '<button type="button" data-tk-open="' + t.id + '" class="glass tk-card rounded-2xl p-5">' +
        '<div class="flex flex-wrap items-center justify-between gap-3">' +
        '<span class="font-mono text-sm font-bold tracking-wider text-cyan-300">' + esc(t.ticketNo) + '</span>' + pill(t.status) + '</div>' +
        '<p class="mt-3 font-display text-base font-bold text-slate-100">' + esc(t.subject) + '</p>' +
        '<p class="mt-1 text-xs text-slate-500">' + esc(t.category) + ' · raised ' + esc(day(t.createdAt)) + '</p>' +
        '<div class="mt-4 flex flex-wrap items-center justify-between gap-2 border-t border-slate-800 pt-3 text-xs text-slate-500">' +
        '<span>' + who + '</span>' +
        '<span class="flex items-center gap-3">' +
        (t.imageUrl ? '<span class="flex items-center gap-1"><i data-lucide="image" class="h-3.5 w-3.5"></i>Photo</span>' : '') +
        '<span class="flex items-center gap-1"><i data-lucide="message-square" class="h-3.5 w-3.5"></i>' + t.replies + '</span>' +
        '<span>Updated ' + esc(ago(t.updatedAt)) + '</span></span></div></button>';
    }).join('');
  }

  function renderStaff() {
    var list = filtered();
    Object.keys(LABEL).forEach(function (k) {
      var el = root.querySelector('[data-tk-count="' + k + '"]'); if (el) el.textContent = counts[k] || 0;
    });
    root.querySelectorAll('[data-tk-chip]').forEach(function (c) { c.classList.toggle('on', c.dataset.tkChip === state.status); });

    var pages = Math.max(1, Math.ceil(list.length / PAGE));
    if (state.page > pages) state.page = pages;
    var slice = list.slice((state.page - 1) * PAGE, state.page * PAGE);
    $('tk-count').textContent = list.length + (list.length === 1 ? ' ticket' : ' tickets');

    if (slice.length === 0) {
      $('tk-body').innerHTML = '<tr><td colspan="7" class="px-5 py-12 text-center text-sm text-slate-500">' +
        (items.length ? 'No tickets match this filter.' : (mode === 'admin' ? 'No complaints have been raised yet.' : 'No tickets are assigned to you right now.')) + '</td></tr>';
    } else {
      $('tk-body').innerHTML = slice.map(function (t) {
        var needs = mode === 'admin' && t.status === 'open';
        var todo = mode === 'technical' && (t.status === 'assigned' || t.status === 'in_progress');
        return '<tr class="table-row">' +
          '<td class="px-5 py-3.5"><span class="font-mono text-xs font-bold tracking-wider text-cyan-300">' + esc(t.ticketNo) + '</span>' +
          '<p class="mt-0.5 max-w-[16rem] truncate font-semibold text-slate-200" title="' + esc(t.subject) + '">' + esc(t.subject) + '</p></td>' +
          '<td class="px-4 py-3.5"><p class="font-semibold text-slate-200">' + esc(t.customerName) + '</p><p class="text-xs text-slate-500">' + esc(t.customerPhone) + '</p></td>' +
          '<td class="px-4 py-3.5 text-xs text-slate-400">' + esc(t.category) + '</td>' +
          '<td class="px-4 py-3.5">' + pill(t.status) + '</td>' +
          '<td class="px-4 py-3.5 text-slate-300">' + (t.assigneeName ? esc(t.assigneeName) : '<span class="text-xs text-slate-500">Not assigned</span>') + '</td>' +
          '<td class="px-4 py-3.5 text-xs text-slate-500">' + esc(stamp(t.updatedAt)) + '</td>' +
          '<td class="px-5 py-3.5 text-right"><button type="button" data-tk-open="' + t.id + '" class="' +
          ((needs || todo) ? 'btn-glow rounded-lg px-3 py-1.5 text-[11px] font-bold text-white' : 'rounded-lg border border-slate-700/70 px-3 py-1.5 text-[11px] font-bold text-slate-300 transition hover:border-cyan-400/50 hover:text-cyan-300') + '">' +
          (needs ? 'Assign' : todo ? 'Work on it' : 'Open') + '</button></td></tr>';
      }).join('');
    }
    var pager = $('tk-pager');
    pager.hidden = list.length <= PAGE;
    pager.querySelector('[data-info]').textContent = 'Page ' + state.page + ' of ' + pages;
    pager.querySelector('[data-prev]').disabled = state.page <= 1;
    pager.querySelector('[data-next]').disabled = state.page >= pages;
  }

  /* ------------------------------ filters + clicks ------------------------------ */
  var filterSel = $('tk-filter');
  if (filterSel) filterSel.addEventListener('change', function () { state.status = filterSel.value; state.page = 1; render(); });
  var qBox = $('tk-q'), qTimer;
  if (qBox) qBox.addEventListener('input', function () { clearTimeout(qTimer); qTimer = setTimeout(function () { state.q = qBox.value.trim(); state.page = 1; render(); }, 180); });
  root.querySelectorAll('[data-tk-chip]').forEach(function (c) {
    c.addEventListener('click', function () {
      state.status = state.status === c.dataset.tkChip ? '' : c.dataset.tkChip; state.page = 1;
      if (filterSel) filterSel.value = state.status; render();
    });
  });
  var pager = $('tk-pager');
  if (pager) {
    pager.querySelector('[data-prev]').addEventListener('click', function () { state.page--; render(); });
    pager.querySelector('[data-next]').addEventListener('click', function () { state.page++; render(); });
  }
  root.addEventListener('click', function (e) {
    var b = e.target.closest('[data-tk-open]'); if (b) openDetail(Number(b.dataset.tkOpen));
  });

  /* ------------------------------ raise a complaint (customer) ------------------------------ */
  var form = $('tk-form');
  if (form) {
    var fileIn = $('tk-file'), prev = $('tk-preview');
    fileIn.addEventListener('change', function () {
      prev.classList.add('hidden'); $('tk-error').textContent = '';
      var f = fileIn.files[0]; if (!f) return;
      var bad = nx.checkImage(f);
      if (bad) { fileIn.value = ''; $('tk-error').textContent = bad; return; }
      prev.src = URL.createObjectURL(f); prev.classList.remove('hidden');
    });
    form.addEventListener('submit', async function (e) {
      e.preventDefault();
      var err = $('tk-error'); err.textContent = '';
      var category = $('tk-category').value, subject = $('tk-subject').value.trim(), desc = $('tk-desc').value.trim();
      if (!category) { err.textContent = 'Choose a category'; $('tk-category').focus(); return; }
      if (subject.length < 5) { err.textContent = 'Write a short subject (at least 5 characters)'; $('tk-subject').focus(); return; }
      if (desc.length < 10) { err.textContent = 'Please describe the problem (at least 10 characters)'; $('tk-desc').focus(); return; }
      var fd = new FormData();
      fd.append('category', category); fd.append('subject', subject); fd.append('description', desc);
      if (fileIn.files[0]) fd.append('file', fileIn.files[0]);
      var btn = $('tk-submit'); nx.busy(btn, true);
      var res = await postForm('/api/tickets', fd);
      nx.busy(btn, false);
      if (!res.ok) { err.textContent = nx.errText(res, 'Could not send your complaint'); return; }
      form.reset(); prev.classList.add('hidden');
      nx.toast('Complaint ' + res.data.ticketNo + ' raised — we will update you here', 'success');
      state.status = ''; if (filterSel) filterSel.value = '';
      await load();
    });
  }

  /* ------------------------------ ticket detail dialog ------------------------------ */
  var techCache = null;
  async function technicians() {
    if (techCache) return techCache;
    var r = await nx.api('/api/tickets/technicians');
    techCache = r.ok ? r.data : [];
    return techCache;
  }

  async function openDetail(id) {
    var res = await nx.api('/api/tickets/' + id);
    if (!res.ok) { nx.toast(nx.errText(res, 'Could not open the ticket'), 'error'); return; }
    var dlg = nx.dialog('', 'tk-modal');
    await paint(dlg, res.data, id);
  }

  async function refreshDetail(dlg, id) {
    var res = await nx.api('/api/tickets/' + id);
    if (!res.ok) { nx.toast(nx.errText(res, 'Could not refresh the ticket'), 'error'); return; }
    await paint(dlg, res.data, id);
    load();                                           // keep the list behind the dialog current
  }

  function stepper(status) {
    var cur = 0; STEPS.forEach(function (s, i) { if (s[0] === status) cur = i; });
    return '<div class="tk-steps" role="list" aria-label="Ticket progress">' + STEPS.map(function (s, i) {
      return '<div role="listitem" class="tk-step' + (i <= cur ? ' done' : '') + (i === cur ? ' now' : '') + '"><span class="tk-dot"><i data-lucide="' + (i < cur ? 'check' : s[2]) + '"></i></span>' + esc(s[1]) + '</div>';
    }).join('') + '</div>';
  }

  function photo(url) {
    if (!url) return '';
    return '<a href="' + esc(url) + '" target="_blank" rel="noopener" class="tk-photo" title="Open full size"><img src="' + esc(url) + '" alt="Attached photo" loading="lazy" onerror="this.parentElement.remove()" /></a>';
  }

  function thread(replies) {
    if (!replies.length) return '<p class="py-6 text-center text-sm text-slate-500">No replies yet.</p>';
    return replies.map(function (r) {
      if (r.isSystem) return '<span class="tk-sys"><i data-lucide="dot" class="h-3 w-3"></i>' + esc(r.message) + ' <b>· ' + esc(stamp(r.createdAt)) + '</b></span>';
      var me = r.authorRole === mode;                  // my own side of the conversation sits on the right
      return '<div class="tk-msg' + (me ? ' me' : '') + '"><div class="who">' + esc(r.authorName) + '<span class="role">' + esc(ROLE[r.authorRole] || r.authorRole) + '</span>' +
        '<span class="when">' + esc(stamp(r.createdAt)) + '</span></div><p>' + esc(r.message) + '</p>' + photo(r.imageUrl) + '</div>';
    }).join('');
  }

  async function paint(dlg, d, id) {
    var t = d.ticket, actions = d.actions || [];
    var staff = mode !== 'customer';
    var html =
      '<div class="flex items-start justify-between gap-4"><div class="min-w-0">' +
      '<div class="flex flex-wrap items-center gap-3"><span class="font-mono text-sm font-bold tracking-wider text-cyan-300">' + esc(t.ticketNo) + '</span>' + pill(t.status) + '</div>' +
      '<h3 class="mt-2 font-display text-xl font-bold text-slate-100 break-words">' + esc(t.subject) + '</h3></div>' +
      '<button type="button" data-x class="text-slate-500 transition hover:text-slate-200" aria-label="Close"><i data-lucide="x" class="h-5 w-5"></i></button></div>' +

      '<div class="mt-6">' + stepper(t.status) + '</div>' +

      '<div class="mt-6 grid gap-3 text-sm sm:grid-cols-2">' +
      info('Category', t.category) +
      (staff ? info('Customer', t.customerName + (t.customerPhone ? ' · ' + t.customerPhone : '')) : info('Raised', day(t.createdAt))) +
      (staff ? info('Raised', stamp(t.createdAt)) : '') +
      info('Assigned to', t.assigneeName || 'Not assigned yet') +
      (t.resolvedAt ? info('Resolved', stamp(t.resolvedAt)) : '') +
      (t.closedAt ? info('Closed', stamp(t.closedAt)) : '') +
      '</div>' +

      '<div class="mt-5 rounded-2xl border border-slate-800 bg-ink-850 p-4"><p class="text-[11px] font-bold uppercase tracking-[0.16em] text-slate-500">Complaint</p>' +
      '<p class="mt-2 whitespace-pre-line break-words text-sm leading-relaxed text-slate-300">' + esc(d.description) + '</p>' + photo(t.imageUrl) + '</div>';

    if (actions.indexOf('assign') > -1) {
      html += '<div class="mt-5 rounded-2xl border border-cyan-400/25 bg-cyan-400/5 p-4"><p class="text-[11px] font-bold uppercase tracking-[0.16em] text-slate-500">' + (t.assigneeId ? 'Re-assign to technical desk' : 'Assign to technical desk') + '</p>' +
        '<div class="mt-3 flex flex-wrap items-center gap-3"><select data-assign-sel class="field !w-auto min-w-[14rem] flex-1"><option value="">Loading technicians…</option></select>' +
        '<button type="button" data-assign class="btn-glow flex items-center gap-2 rounded-xl px-5 py-2.5 text-sm font-semibold text-white disabled:opacity-60"><span data-icon-slot><i data-lucide="user-check" class="h-4 w-4"></i></span>Assign</button></div>' +
        '<p data-assign-err class="mt-2 text-sm text-rose-300"></p></div>';
    }

    html += '<div class="mt-6"><p class="mb-3 text-[11px] font-bold uppercase tracking-[0.16em] text-slate-500">Conversation</p><div class="tk-thread" data-thread>' + thread(d.replies || []) + '</div></div>';

    if (d.canReply) {
      html += '<form data-reply class="mt-4 space-y-3" novalidate>' +
        '<textarea name="message" rows="3" maxlength="2000" required placeholder="' + (mode === 'customer' ? 'Write a reply or add more details…' : 'Write a reply to the customer…') + '" class="field resize-none"></textarea>' +
        '<div class="flex flex-wrap items-center gap-3">' +
        '<input type="file" data-reply-file accept="image/jpeg,image/png,image/webp,image/gif" hidden />' +
        '<button type="button" data-reply-pick class="flex items-center gap-2 rounded-xl border border-slate-700/80 px-4 py-2.5 text-sm font-semibold text-slate-300 transition hover:border-cyan-400/50 hover:text-cyan-300"><i data-lucide="image-plus" class="h-4 w-4"></i><span data-reply-fname>Attach photo</span></button>' +
        '<button type="submit" class="btn-glow ml-auto flex items-center gap-2 rounded-xl px-6 py-2.5 text-sm font-semibold text-white disabled:opacity-60"><span data-icon-slot><i data-lucide="send" class="h-4 w-4"></i></span>Send reply</button></div>' +
        '<p data-reply-err class="text-sm text-rose-300"></p></form>';
    } else {
      html += '<p class="mt-4 rounded-xl border border-slate-800 bg-ink-850 px-4 py-3 text-sm text-slate-500">This ticket is closed, so no more replies can be added.</p>';
    }

    var btns = '';
    if (actions.indexOf('start') > -1) btns += '<button type="button" data-act="start" class="' + ACT_BTN + '"><i data-lucide="play" class="mr-2 inline h-4 w-4"></i>Start working</button>';
    if (actions.indexOf('resolve') > -1) btns += '<button type="button" data-act="resolve" class="rounded-xl bg-gradient-to-r from-emerald-500 to-teal-600 px-5 py-2.5 text-sm font-semibold text-white transition hover:brightness-110"><i data-lucide="circle-check" class="mr-2 inline h-4 w-4"></i>Mark resolved</button>';
    if (actions.indexOf('reopen') > -1) btns += '<button type="button" data-act="reopen" class="' + ACT_BTN + '"><i data-lucide="rotate-ccw" class="mr-2 inline h-4 w-4"></i>' + (mode === 'customer' ? 'Not fixed — reopen' : 'Reopen') + '</button>';
    if (actions.indexOf('close') > -1) btns += '<button type="button" data-act="close" class="' + ACT_BTN + '"><i data-lucide="archive" class="mr-2 inline h-4 w-4"></i>' + (mode === 'customer' ? (t.status === 'resolved' ? 'Problem solved — close' : 'Withdraw complaint') : 'Close ticket') + '</button>';
    if (btns) html += '<div class="mt-5 flex flex-wrap items-center gap-3 border-t border-slate-800 pt-5">' + btns + '</div>';

    dlg.el.innerHTML = html;
    nx.icons();
    wire(dlg, d, id);
    var th = dlg.el.querySelector('[data-thread]'); if (th) th.scrollTop = th.scrollHeight;

    if (actions.indexOf('assign') > -1) {
      var sel = dlg.el.querySelector('[data-assign-sel]');
      var techs = await technicians();
      sel.innerHTML = '<option value="">' + (techs.length ? 'Choose technical-desk employee…' : 'No technical employees with a login') + '</option>' +
        techs.map(function (x) {
          return '<option value="' + x.id + '"' + (x.id === t.assigneeId ? ' selected' : '') + '>' + esc(x.name) + ' — ' + esc(x.designation) + ' (' + x.openCount + ' open)</option>';
        }).join('');
    }
  }

  function info(label, value) {
    return '<div class="flex items-center justify-between gap-3 rounded-xl border border-slate-800 px-4 py-2.5"><span class="text-xs text-slate-500">' + esc(label) + '</span><span class="text-right font-semibold text-slate-200 break-words">' + esc(value) + '</span></div>';
  }

  function wire(dlg, d, id) {
    var el = dlg.el;
    el.querySelector('[data-x]').addEventListener('click', function () { dlg.close(false); });

    /* assign */
    var assign = el.querySelector('[data-assign]');
    if (assign) assign.addEventListener('click', async function () {
      var sel = el.querySelector('[data-assign-sel]'), err = el.querySelector('[data-assign-err]');
      err.textContent = '';
      if (!sel.value) { err.textContent = 'Choose who should handle this ticket'; return; }
      nx.busy(assign, true);
      var r = await nx.api('/api/tickets/' + id + '/assign', { method: 'POST', body: { employeeId: Number(sel.value) } });
      nx.busy(assign, false);
      if (!r.ok) { err.textContent = nx.errText(r, 'Could not assign'); return; }
      techCache = null;
      nx.toast('Ticket assigned', 'success');
      refreshDetail(dlg, id);
    });

    /* reply */
    var rf = el.querySelector('[data-reply]');
    if (rf) {
      var file = rf.querySelector('[data-reply-file]'), fname = rf.querySelector('[data-reply-fname]'), rerr = rf.querySelector('[data-reply-err]');
      rf.querySelector('[data-reply-pick]').addEventListener('click', function () { file.click(); });
      file.addEventListener('change', function () {
        rerr.textContent = '';
        var f = file.files[0]; if (!f) { fname.textContent = 'Attach photo'; return; }
        var bad = nx.checkImage(f);
        if (bad) { file.value = ''; fname.textContent = 'Attach photo'; rerr.textContent = bad; return; }
        fname.textContent = f.name.length > 24 ? f.name.slice(0, 21) + '…' : f.name;
      });
      rf.addEventListener('submit', async function (e) {
        e.preventDefault(); rerr.textContent = '';
        var msg = rf.elements.message.value.trim();
        if (msg.length < 2) { rerr.textContent = 'Write your message first'; rf.elements.message.focus(); return; }
        var fd = new FormData(); fd.append('message', msg);
        if (file.files[0]) fd.append('file', file.files[0]);
        var btn = rf.querySelector('button[type=submit]'); nx.busy(btn, true);
        var r = await postForm('/api/tickets/' + id + '/reply', fd);
        nx.busy(btn, false);
        if (!r.ok) { rerr.textContent = nx.errText(r, 'Could not send your reply'); return; }
        nx.toast('Reply sent', 'success');
        refreshDetail(dlg, id);
      });
    }

    /* status actions */
    el.querySelectorAll('[data-act]').forEach(function (b) {
      b.addEventListener('click', function () { act(dlg, id, b.dataset.act, d.ticket); });
    });
  }

  async function sendAction(dlg, id, action, note, done) {
    var r = await nx.api('/api/tickets/' + id + '/status', { method: 'POST', body: { action: action, note: note || '' } });
    if (!r.ok) return { ok: false, error: nx.errText(r, 'Could not update the ticket') };
    nx.toast(done, 'success');
    refreshDetail(dlg, id);
    return { ok: true };
  }

  async function act(dlg, id, action, t) {
    if (action === 'start') { await sendAction(dlg, id, 'start', '', 'Ticket is now in progress'); return; }

    if (action === 'resolve') {
      nx.formModal({
        title: 'Mark ' + t.ticketNo + ' resolved', subtitle: 'The customer is notified and sees your note.', size: 'sm', submitLabel: 'Mark resolved',
        fields: [{ name: 'note', label: 'What was done to fix it?', type: 'textarea', required: true, wide: true, placeholder: 'e.g. Replaced the faulty ONT and re-tested the line — speed is back to normal.' }],
        onSubmit: function (v) { return sendAction(dlg, id, 'resolve', v.note, 'Ticket marked resolved'); }
      });
      return;
    }

    var customer = mode === 'customer';
    var cfg = {
      close: {
        title: customer ? (t.status === 'resolved' ? 'Close this complaint?' : 'Withdraw this complaint?') : 'Close ' + t.ticketNo + '?',
        message: customer ? 'Closed complaints cannot be replied to. If the problem comes back you can raise a new one.' : 'The customer is told the ticket is closed. No more replies can be added.',
        label: customer && t.status !== 'resolved' ? 'Withdraw' : 'Close ticket', done: 'Ticket closed', danger: !customer || t.status !== 'resolved'
      },
      reopen: {
        title: 'Reopen ' + t.ticketNo + '?',
        message: customer ? 'It goes back to the technical desk so they can look at it again.' : 'The ticket goes back to the technician (or to the queue if nobody is assigned).',
        label: 'Reopen', done: 'Ticket reopened', danger: false
      }
    }[action];
    if (!cfg) return;
    var ok = await nx.confirm({ title: cfg.title, message: cfg.message, confirmLabel: cfg.label, danger: cfg.danger });
    if (!ok) return;
    var r = await sendAction(dlg, id, action, '', cfg.done);
    if (!r.ok) nx.toast(r.error, 'error');
  }

  load();
  // a bell alert can change the list while the page is open: refresh when the tab is shown again
  document.addEventListener('nx:tab', function (e) { if (e.detail === 'tickets') load(); });
})();
