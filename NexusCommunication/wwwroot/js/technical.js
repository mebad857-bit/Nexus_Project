/* Technical desk — feasibility decisions, provisioning (incl. bulk), connection control, plan change, equipment */
(function () {
  'use strict';

  var FEASIBLE_REMARK = 'Distance & node capacity verified — cleared for installation.';
  var INFEASIBLE_REMARK = 'Feeder distance beyond serviceable limits for this address.';
  var data = nx.json('tech-data') || { plans: [], vendors: [] };

  /* ---- feasibility queue ---- */
  document.querySelectorAll('[data-order-card]').forEach(function (card) {
    card.querySelectorAll('[data-decide]').forEach(function (btn) {
      btn.addEventListener('click', async function () {
        var status = btn.dataset.decide;
        var typed = card.querySelector('[data-remark]').value.trim();
        if (status === 'infeasible') {
          var ok = await nx.confirm({
            title: 'Mark as not feasible?', danger: true, confirmLabel: 'Not feasible',
            message: 'The applicant will see this decision when tracking the order. A short survey remark helps them understand why.'
          });
          if (!ok) return;
        }
        var remark = typed || (status === 'feasible' ? FEASIBLE_REMARK : INFEASIBLE_REMARK);
        card.querySelectorAll('[data-decide]').forEach(function (b) { b.disabled = true; });
        nx.busy(btn, true);
        var res = await nx.api('/api/orders', { method: 'PATCH', body: { id: Number(card.dataset.orderCard), status: status, remark: remark } });
        if (!res.ok) {
          nx.toast(nx.errText(res, 'Update failed'), 'error');
          card.querySelectorAll('[data-decide]').forEach(function (b) { b.disabled = false; });
          nx.busy(btn, false);
          return;
        }
        nx.flash(card.dataset.orderNo + (status === 'feasible' ? ' cleared for installation' : ' marked not feasible'), 'success');
        nx.reload();
      });
    });
  });

  /* ---- provisioning ---- */
  var KEY = 'nx.issuedAccounts';
  function renderIssued() {
    var list = [];
    try { list = JSON.parse(sessionStorage.getItem(KEY) || '[]'); } catch (e) { list = []; }
    var box = document.getElementById('issued-box');
    if (!box) return;
    box.hidden = list.length === 0;
    var holder = document.getElementById('issued-list');
    holder.innerHTML = '';
    list.forEach(function (i) {
      var d = document.createElement('div');
      d.className = 'rounded-2xl border border-emerald-400/25 bg-emerald-400/5 px-4 py-3';
      var ids = document.createElement('div');
      (i.accounts || [i.accountId]).slice(0, 3).forEach(function (a) {
        var b = document.createElement('button');
        b.type = 'button'; b.dataset.copy = a; b.title = 'Copy account id';
        b.className = 'block font-mono text-base font-bold tracking-wider text-emerald-300 hover:underline';
        b.textContent = a; ids.appendChild(b);
      });
      if ((i.accounts || []).length > 3) {
        var more = document.createElement('p'); more.className = 'text-xs text-emerald-200/70';
        more.textContent = '+ ' + (i.accounts.length - 3) + ' more accounts'; ids.appendChild(more);
      }
      var p = document.createElement('p'); p.className = 'mt-1 text-xs text-slate-400';
      p.textContent = i.customer + ' is now live on the network';
      d.appendChild(ids); d.appendChild(p); holder.appendChild(d);
    });
  }
  renderIssued();

  document.querySelectorAll('[data-provision-card]').forEach(function (card) {
    card.querySelector('[data-activate]').addEventListener('click', async function () {
      var btn = this, eq = card.querySelector('[data-equipment]').value;
      var lines = Number(card.dataset.lines) || 1;
      if (!eq) {
        var go = await nx.confirm({
          title: 'Activate without equipment?',
          message: 'No modem, router or phone will be issued from stock for ' + (lines > 1 ? 'these ' + lines + ' lines' : 'this line') + '. You can still activate and hand over hardware later.',
          confirmLabel: 'Activate anyway'
        });
        if (!go) return;
      }
      nx.busy(btn, true);
      var res = await nx.api('/api/connections', {
        method: 'POST',
        body: { orderId: Number(card.dataset.provisionCard), equipmentId: eq ? Number(eq) : null }
      });
      if (!res.ok) { nx.busy(btn, false); nx.toast(nx.errText(res, 'Provisioning failed'), 'error'); return; }
      var list = [];
      try { list = JSON.parse(sessionStorage.getItem(KEY) || '[]'); } catch (e) { list = []; }
      list.unshift({ accountId: res.data.connection.accountId, accounts: res.data.accounts, customer: card.dataset.customer });
      try { sessionStorage.setItem(KEY, JSON.stringify(list.slice(0, 6))); } catch (e) { /* ignore */ }
      nx.flash((res.data.accounts || []).length > 1 ? res.data.accounts.length + ' accounts activated' : 'Account ' + res.data.connection.accountId + ' activated', 'success');
      nx.reload();
    });
  });

  /* ---- connection grid: status control ---- */
  document.querySelectorAll('[data-conn-status]').forEach(function (btn) {
    btn.addEventListener('click', async function () {
      var status = btn.dataset.connStatus;
      if (status === 'permanently_inactive') {
        var ok = await nx.confirm({
          title: 'Retire ' + btn.dataset.account + ' permanently?', danger: true, confirmLabel: 'Retire connection',
          message: 'This ends billing on the line and cannot be undone from this screen.'
        });
        if (!ok) return;
      }
      btn.disabled = true;
      var res = await nx.api('/api/connections', { method: 'PATCH', body: { id: Number(btn.dataset.id), status: status } });
      if (!res.ok) { btn.disabled = false; nx.toast(nx.errText(res, 'Update failed'), 'error'); return; }
      nx.flash('Connection updated', 'success');
      nx.reload();
    });
  });

  /* ---- connection grid: plan change (same connection type only) ---- */
  document.querySelectorAll('[data-change-plan]').forEach(function (btn) {
    btn.addEventListener('click', function () {
      var type = btn.dataset.type, current = Number(btn.dataset.current) || 0;
      var options = data.plans.filter(function (p) { return p.type === type; }).map(function (p) { return [String(p.id), p.label]; });
      nx.formModal({
        title: 'Change plan', subtitle: btn.dataset.account + ' — only plans of the same connection type are listed.', size: 'sm', submitLabel: 'Switch plan',
        fields: [{ name: 'planId', label: 'New plan', type: 'select', numeric: true, required: true, wide: true, options: options, value: current || '' }],
        onSubmit: async function (v) {
          if (v.planId === current) return { ok: false, error: 'That is already the current plan' };
          var res = await nx.api('/api/connections', { method: 'PATCH', body: { id: Number(btn.dataset.changePlan), planId: v.planId } });
          if (!res.ok) return { ok: false, error: nx.errText(res, 'Could not change the plan') };
          nx.flash('Plan changed — the next invoice will use the new rental', 'success');
          nx.reload();
          return { ok: true };
        }
      });
    });
  });

  /* ---- order tracker filter ---- */
  var of = document.getElementById('order-filter');
  if (of) of.addEventListener('change', function () {
    var f = this.value;
    document.querySelectorAll('[data-order-row]').forEach(function (r) { r.hidden = !(f === 'all' || r.dataset.status === f); });
    nx.refreshTables();
  });

  /* ---- equipment bay ---- */
  document.querySelectorAll('[data-stock-input]').forEach(function (input) {
    var save = document.querySelector('[data-stock-save="' + input.dataset.stockInput + '"]');
    input.addEventListener('input', function () { save.disabled = input.value === '' || Number(input.value) < 0 || Number(input.value) === Number(input.dataset.original); });
    save.addEventListener('click', async function () {
      nx.busy(save, true);
      var res = await nx.api('/api/crud/equipment', { method: 'PATCH', body: { id: Number(input.dataset.stockInput), stock: Math.floor(Number(input.value)) } });
      if (!res.ok) { nx.busy(save, false); nx.toast(nx.errText(res, 'Update failed'), 'error'); return; }
      nx.flash('Stock count saved', 'success');
      nx.reload();
    });
  });

  document.querySelectorAll('[data-receive]').forEach(function (btn) {
    btn.addEventListener('click', function () {
      nx.formModal({
        title: 'Receive stock', subtitle: btn.dataset.name, size: 'sm', submitLabel: 'Add to stock',
        fields: [{ name: 'qty', label: 'Units received', type: 'number', min: 1, numeric: true, required: true, wide: true, value: 10 }],
        onSubmit: async function (v) {
          if (!(v.qty > 0)) return { ok: false, error: 'Enter a quantity above zero' };
          var res = await nx.api('/api/crud/equipment', { method: 'PATCH', body: { id: Number(btn.dataset.receive), addStock: Math.floor(v.qty) } });
          if (!res.ok) return { ok: false, error: nx.errText(res, 'Update failed') };
          nx.flash(Math.floor(v.qty) + ' units added', 'success');
          nx.reload();
          return { ok: true };
        }
      });
    });
  });

  /* product photo: upload / change / remove */
  document.querySelectorAll('[data-equip-photo]').forEach(function (btn) {
    btn.addEventListener('click', function () {
      nx.imageDialog({ resource: 'equipment', id: Number(btn.dataset.equipPhoto), url: btn.dataset.url || '', name: btn.dataset.name, title: 'Product photo' });
    });
  });

  var add = document.getElementById('add-equipment');
  if (add) add.addEventListener('click', function () {
    nx.formModal({
      title: 'Add equipment', subtitle: 'A new product line that can be issued during provisioning.', submitLabel: 'Add product', imagePicker: true,
      fields: [
        { name: 'name', label: 'Product name', required: true },
        { name: 'type', label: 'Type', type: 'select', required: true, options: [['modem', 'Modem'], ['router', 'Router / ONT'], ['telephone', 'Telephone'], ['cabling', 'Cabling']] },
        { name: 'vendorId', label: 'Vendor', type: 'select', numeric: true, options: data.vendors.map(function (v) { return [v.value, v.label]; }) },
        { name: 'unitPrice', label: 'Unit price ($)', type: 'number', step: '0.01', min: 0, required: true },
        { name: 'stock', label: 'Opening stock', type: 'number', min: 0, required: true, value: 0 },
        { name: 'reorderLevel', label: 'Reorder level', type: 'number', min: 0, required: true, value: 10 }
      ],
      onSubmit: async function (v, photo) {
        var res = await nx.api('/api/crud/equipment', { method: 'POST', body: v });
        if (!res.ok) return { ok: false, error: nx.errText(res, 'Could not add the product') };
        var note = 'Equipment added', kind = 'success';
        if (photo && res.data && res.data.id) { var up = await nx.uploadImage('equipment', res.data.id, photo); if (!up.ok) { note = 'Equipment added, but the photo was not uploaded: ' + up.error; kind = 'error'; } }
        nx.flash(note, kind);
        nx.reload();
        return { ok: true };
      }
    });
  });
})();
