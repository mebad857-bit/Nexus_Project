/* Admin console — registry CRUD: add (inline form), edit (dialog), toggle, delete (confirm dialog), receive stock, billing filter */
(function () {
  'use strict';

  var reg = nx.json('registry-data') || {};

  /* ---------- open / close the "add record" form ---------- */
  document.querySelectorAll('[data-crud-toggle]').forEach(function (btn) {
    btn.addEventListener('click', function () {
      var form = document.querySelector('[data-crud-form="' + btn.dataset.crudToggle + '"]');
      form.hidden = !form.hidden;
      var open = !form.hidden;
      btn.querySelector('[data-toggle-text]').textContent = open ? 'Close' : btn.dataset.addLabel;
      var fresh = document.createElement('i');
      fresh.setAttribute('data-lucide', open ? 'x' : 'plus');
      fresh.setAttribute('data-toggle-icon', '');
      fresh.className = 'h-4 w-4';
      var old = btn.querySelector('[data-toggle-icon]');
      if (old && old.parentNode) old.parentNode.replaceChild(fresh, old);
      nx.icons();
      if (open) { var first = form.querySelector('input,select,textarea'); if (first) first.focus(); }
    });
  });

  /* ---------- save new record ---------- */
  document.querySelectorAll('[data-crud-form]').forEach(function (form) {
    form.addEventListener('submit', async function (e) {
      e.preventDefault();
      var resource = form.dataset.crudForm;
      var err = form.querySelector('[data-crud-error]');
      var submit = form.querySelector('button:not([type=button])');
      err.textContent = '';
      nx.busy(submit, true);

      var body = {};
      form.querySelectorAll('input[name],select[name],textarea[name]').forEach(function (el) {
        if (el.value === '') return;
        body[el.name] = el.dataset.numeric ? Number(el.value) : el.value;
      });

      // optional photo: check it first, upload it right after the record exists
      var fileInput = form.querySelector('input[type=file][data-crud-image]');
      var photo = fileInput && fileInput.files[0];
      if (photo) { var bad = nx.checkImage(photo); if (bad) { err.textContent = bad; nx.busy(submit, false); return; } }

      var res = await nx.api('/api/crud/' + resource, { method: 'POST', body: body });
      if (!res.ok) { nx.busy(submit, false); err.textContent = nx.errText(res, 'Save failed'); return; }
      var note = 'Record saved', kind = 'success';
      if (photo && res.data && res.data.id) {
        var up = await nx.uploadImage(resource, res.data.id, photo);
        if (!up.ok) { note = 'Record saved, but the photo was not uploaded: ' + up.error; kind = 'error'; }
      }
      nx.busy(submit, false);
      nx.flash(note, kind);
      nx.reload();
    });
  });

  /* ---------- quick field toggles (suspend / reactivate, publish / unpublish) ---------- */
  document.querySelectorAll('[data-crud-patch]').forEach(function (btn) {
    btn.addEventListener('click', async function () {
      var body = JSON.parse(btn.dataset.body);
      body.id = Number(btn.dataset.id);
      btn.disabled = true;
      var res = await nx.api('/api/crud/' + btn.dataset.crudPatch, { method: 'PATCH', body: body });
      if (!res.ok) { btn.disabled = false; nx.toast(nx.errText(res, 'Update failed'), 'error'); return; }
      nx.flash('Updated', 'success');
      nx.reload();
    });
  });

  /* ---------- delete ---------- */
  document.querySelectorAll('[data-crud-delete]').forEach(function (btn) {
    btn.addEventListener('click', async function () {
      var ok = await nx.confirm({
        title: 'Delete this record?', danger: true, confirmLabel: 'Delete',
        message: 'This permanently removes the record. Records that are used elsewhere (orders, bills) cannot be deleted.'
      });
      if (!ok) return;
      var res = await nx.api('/api/crud/' + btn.dataset.crudDelete + '?id=' + btn.dataset.id, { method: 'DELETE' });
      if (!res.ok) { nx.toast(nx.errText(res, 'Delete failed'), 'error'); return; }
      nx.flash('Record deleted', 'success');
      nx.reload();
    });
  });

  /* ---------- edit dialogs ---------- */
  function opts(list) { return (list || []).map(function (o) { return [String(o.value), o.label]; }); }

  var FORMS = {
    employees: { title: 'Edit employee', fields: function () { return [
      { name: 'name', label: 'Full name', required: true },
      { name: 'designation', label: 'Designation', required: true },
      { name: 'department', label: 'Department', type: 'select', required: true, options: [['admin', 'Administration'], ['accounts', 'Accounts'], ['technical', 'Technical'], ['retail', 'Retail outlet']] },
      { name: 'shopId', label: 'Outlet (retail staff)', type: 'select', numeric: true, sendEmpty: true, options: opts(reg.shopOptions) },
      { name: 'email', label: 'E-mail', required: true }, { name: 'phone', label: 'Phone', required: true }
    ]; } },
    vendors: { title: 'Edit vendor', fields: function () { return [
      { name: 'name', label: 'Vendor name', required: true }, { name: 'contactPerson', label: 'Contact person', required: true },
      { name: 'email', label: 'E-mail', required: true }, { name: 'phone', label: 'Phone', required: true },
      { name: 'address', label: 'Address', required: true, wide: true }, { name: 'products', label: 'Products supplied', required: true, wide: true }
    ]; } },
    equipment: { title: 'Edit equipment', fields: function () { return [
      { name: 'name', label: 'Product name', required: true },
      { name: 'type', label: 'Type', type: 'select', required: true, options: [['modem', 'Modem'], ['router', 'Router / ONT'], ['telephone', 'Telephone'], ['cabling', 'Cabling']] },
      { name: 'vendorId', label: 'Vendor', type: 'select', numeric: true, options: opts(reg.vendorOptions) },
      { name: 'unitPrice', label: 'Unit price ($)', type: 'number', step: '0.01', min: 0, required: true },
      { name: 'stock', label: 'Stock on hand', type: 'number', min: 0, required: true },
      { name: 'reorderLevel', label: 'Reorder level', type: 'number', min: 0, required: true }
    ]; } },
    shops: { title: 'Edit outlet', fields: function () { return [
      { name: 'code', label: 'Outlet code', required: true }, { name: 'name', label: 'Outlet name', required: true },
      { name: 'city', label: 'City', required: true }, { name: 'phone', label: 'Phone', required: true },
      { name: 'address', label: 'Address', required: true, wide: true }, { name: 'manager', label: 'Manager', sendEmpty: true }
    ]; } },
    plans: { title: 'Edit plan', fields: function () { return [
      { name: 'connectionType', label: 'Connection type', type: 'select', required: true, options: [['dialup', 'Dial-Up'], ['broadband', 'Broadband'], ['landline', 'Landline']] },
      { name: 'category', label: 'Category', required: true }, { name: 'name', label: 'Plan name', required: true },
      { name: 'speed', label: 'Speed', sendEmpty: true },
      { name: 'price', label: 'Charge ($)', type: 'number', step: '0.01', min: 0, required: true },
      { name: 'deposit', label: 'Security deposit ($)', type: 'number', step: '0.01', min: 0, required: true },
      { name: 'validityLabel', label: 'Validity label', required: true },
      { name: 'validityMonths', label: 'Validity (months)', type: 'number', min: 1, required: true },
      { name: 'callRates', label: 'Call rate card', wide: true, sendEmpty: true },
      { name: 'description', label: 'Description', type: 'textarea', required: true, wide: true }
    ]; } },
    faqs: { title: 'Edit question', noImage: true, fields: function () { return [
      { name: 'question', label: 'Question', required: true, wide: true },
      { name: 'answer', label: 'Answer', type: 'textarea', required: true, wide: true },
      { name: 'sortOrder', label: 'Display order (lower shows first)', type: 'number', min: 0, required: true }
    ]; } },
    customers: { title: 'Edit customer', fields: function () { return [
      { name: 'name', label: 'Full name', required: true }, { name: 'phone', label: 'Phone', required: true },
      { name: 'email', label: 'E-mail', sendEmpty: true }, { name: 'altPhone', label: 'Alternate phone', sendEmpty: true },
      { name: 'city', label: 'City', type: 'select', required: true, options: opts(reg.cityOptions) },
      { name: 'idProof', label: 'ID / address proof', sendEmpty: true },
      { name: 'address', label: 'Address', required: true, wide: true }
    ]; } }
  };

  document.querySelectorAll('[data-edit]').forEach(function (btn) {
    btn.addEventListener('click', function () {
      var resource = btn.dataset.edit, id = Number(btn.dataset.id);
      var row = (reg[resource] || []).filter(function (r) { return r.id === id || r.Id === id; })[0];
      var def = FORMS[resource];
      if (!row || !def) { nx.toast('Could not load that record', 'error'); return; }
      var fields = def.fields().map(function (f) {
        // JSON is camelCase already (registry-data is serialised by System.Text.Json defaults -> PascalCase), so look up both
        var key = f.name.charAt(0).toUpperCase() + f.name.slice(1);
        var v = row[f.name] !== undefined ? row[f.name] : row[key];
        f.value = v == null ? '' : v;
        return f;
      });
      nx.formModal({
        title: def.title, subtitle: 'Changes apply immediately everywhere in the system.', fields: fields, submitLabel: 'Save changes',
        image: def.noImage ? null : { resource: resource, id: id, url: row.imageUrl || row.ImageUrl || '', name: row.name || row.Name || '', label: 'Photo' },
        onSubmit: async function (vals) {
          vals.id = id;
          var res = await nx.api('/api/crud/' + resource, { method: 'PATCH', body: vals });
          if (!res.ok) return { ok: false, error: nx.errText(res, 'Update failed') };
          nx.flash('Changes saved', 'success');
          nx.reload();
          return { ok: true };
        }
      });
    });
  });

  /* ---------- receive stock (relative +N, race-safe on the server) ---------- */
  document.querySelectorAll('[data-add-stock]').forEach(function (btn) {
    btn.addEventListener('click', function () {
      nx.formModal({
        title: 'Receive stock', subtitle: btn.dataset.name, size: 'sm', submitLabel: 'Add to stock',
        fields: [{ name: 'qty', label: 'Units received', type: 'number', min: 1, numeric: true, required: true, wide: true, value: 10 }],
        onSubmit: async function (vals) {
          if (!(vals.qty > 0)) return { ok: false, error: 'Enter a quantity above zero' };
          var res = await nx.api('/api/crud/equipment', { method: 'PATCH', body: { id: Number(btn.dataset.addStock), addStock: Math.floor(vals.qty) } });
          if (!res.ok) return { ok: false, error: nx.errText(res, 'Update failed') };
          nx.flash(vals.qty + ' units added to stock', 'success');
          nx.reload();
          return { ok: true };
        }
      });
    });
  });

  /* ---------- contact inbox: unread badge, filter, mark all read ---------- */
  var unreadEl = document.getElementById('messages-unread');
  var unread = unreadEl ? Number(unreadEl.dataset.count) || 0 : 0;
  if (unread > 0) {
    document.querySelectorAll('.side-tab[data-tab="messages"]').forEach(function (b) {
      var badge = document.createElement('span'); badge.className = 'badge'; badge.textContent = unread > 99 ? '99+' : String(unread); b.appendChild(badge);
    });
  }
  var msgFilter = document.getElementById('msg-filter');
  if (msgFilter) {
    msgFilter.addEventListener('change', function () {
      var f = this.value;
      document.querySelectorAll('[data-msg]').forEach(function (card) { card.hidden = !(f === 'all' || card.dataset.read === f); });
    });
  }
  document.querySelectorAll('[data-read-all]').forEach(function (btn) {
    btn.addEventListener('click', async function () {
      btn.disabled = true;
      var res = await nx.api('/api/contact/read-all', { method: 'POST' });
      if (!res.ok) { btn.disabled = false; nx.toast(nx.errText(res, 'Update failed'), 'error'); return; }
      nx.flash('All messages marked read', 'success');
      nx.reload();
    });
  });

  /* ---------- billing audit filter ---------- */
  var filter = document.getElementById('admin-bill-filter');
  if (filter) {
    filter.addEventListener('change', function () {
      var f = this.value;
      document.querySelectorAll('[data-admin-bill]').forEach(function (row) { row.hidden = !(f === 'all' || row.dataset.status === f); });
      nx.refreshTables();
    });
  }
})();
