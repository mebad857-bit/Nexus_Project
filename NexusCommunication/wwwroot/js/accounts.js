/* Accounts desk — live invoice computation, ledger filter and payment posting */
(function () {
  'use strict';

  var form = document.getElementById('invoice-form');
  var TAX = Number(form.dataset.tax);
  var $ = function (id) { return document.getElementById(id); };
  var num = function (el) { return Number(el.value) || 0; };

  function pill(status, label) {
    var cls = { active: 'border-emerald-400/40 bg-emerald-400/10 text-emerald-300', temporarily_inactive: 'border-amber-400/40 bg-amber-400/10 text-amber-300', permanently_inactive: 'border-rose-400/40 bg-rose-400/10 text-rose-300' }[status] || '';
    var dot = { active: 'bg-emerald-400', temporarily_inactive: 'bg-amber-400', permanently_inactive: 'bg-rose-400' }[status] || 'bg-slate-400';
    return '<span class="inline-flex items-center gap-1.5 rounded-full border px-3 py-1 text-[11px] font-semibold uppercase tracking-[0.12em] ' + cls + '"><span class="h-1.5 w-1.5 rounded-full ' + dot + '"></span>' + label + '</span>';
  }
  function chip(type) {
    var m = { dialup: ['radio', 'border-amber-400/30 bg-amber-400/10 text-amber-300', 'Dial-Up'], broadband: ['wifi', 'border-cyan-400/30 bg-cyan-400/10 text-cyan-300', 'Broadband'], landline: ['phone', 'border-violet-400/30 bg-violet-400/10 text-violet-300', 'Landline'] }[type] || [];
    return '<span class="inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-[11px] font-semibold ' + m[1] + '"><i data-lucide="' + m[0] + '" class="h-3 w-3"></i>' + m[2] + '</span>';
  }

  function compute() {
    var rental = num($('inv-rental')), usage = num($('inv-usage')), discount = num($('inv-discount'));
    var subtotal = Math.max(0, rental + usage - discount);
    var tax = Math.round(subtotal * (TAX / 100) * 100) / 100;
    var total = Math.round((subtotal + tax) * 100) / 100;
    $('pv-rental').textContent = nx.money(rental);
    $('pv-usage').textContent = nx.money(usage);
    $('pv-discount').textContent = '− ' + nx.money(discount);
    $('pv-subtotal').textContent = nx.money(subtotal);
    $('pv-tax').textContent = nx.money(tax);
    $('pv-total').textContent = nx.money(total);
  }

  $('inv-conn').addEventListener('change', function () {
    var opt = this.options[this.selectedIndex];
    $('inv-rental').value = opt.dataset.price;
    $('inv-info').hidden = false;
    $('inv-type').innerHTML = chip(opt.dataset.type);
    var labels = { active: 'Active', temporarily_inactive: 'Temporarily Inactive', permanently_inactive: 'Permanently Inactive' };
    $('inv-status').innerHTML = pill(opt.dataset.status, labels[opt.dataset.status] || opt.dataset.status);
    $('inv-plan-price').textContent = nx.money(opt.dataset.price);
    $('inv-submit').disabled = false;
    var due = Number(opt.dataset.due) || 0, note = $('inv-due-note');
    note.hidden = due <= 0;
    if (due > 0) note.textContent = 'Already owes ' + nx.money(due);
    compute();
    nx.icons();
  });
  ['inv-rental', 'inv-usage', 'inv-discount'].forEach(function (id) { $(id).addEventListener('input', compute); });

  form.addEventListener('submit', async function (e) {
    e.preventDefault();
    var btn = $('inv-submit');
    $('inv-error').textContent = ''; $('inv-done').hidden = true;
    nx.busy(btn, true);
    var res = await nx.api('/api/bills', {
      method: 'POST',
      body: {
        connectionId: Number($('inv-conn').value), rental: num($('inv-rental')), usage: num($('inv-usage')),
        discount: num($('inv-discount')), period: $('inv-period').value
      }
    });
    nx.busy(btn, false);
    if (!res.ok) { $('inv-error').textContent = nx.errText(res, 'Could not generate invoice'); return; }
    nx.flash('Invoice ' + res.data.bill.billNo + ' published to the customer', 'success');
    location.hash = '#ledger';
    nx.reload();
  });

  var flash = sessionStorage.getItem('nx.invoiceFlash');
  if (flash) {
    sessionStorage.removeItem('nx.invoiceFlash');
    $('inv-done-no').textContent = flash;
    $('inv-done').hidden = false;
  }
  compute();

  /* ---- ledger ---- */
  $('ledger-filter').addEventListener('change', function () {
    var f = this.value, shown = 0;
    document.querySelectorAll('[data-bill-row]').forEach(function (row) {
      var on = f === 'all' || row.dataset.status === f;
      row.hidden = !on; if (on) shown++;
    });
    nx.refreshTables();
  });

  document.querySelectorAll('[data-pay-toggle]').forEach(function (btn) {
    btn.addEventListener('click', function () {
      var f = document.querySelector('[data-pay-form="' + btn.dataset.payToggle + '"]');
      f.hidden = !f.hidden;
      btn.textContent = f.hidden ? 'Collect at office' : 'Cancel';
    });
  });

  document.querySelectorAll('[data-pay-form]').forEach(function (f) {
    f.addEventListener('submit', async function (e) {
      e.preventDefault();
      var btn = f.querySelector('button');
      nx.busy(btn, true);
      var fd = new FormData(f);
      var res = await nx.api('/api/payments', {
        method: 'POST',
        body: { billId: Number(f.dataset.payForm), amount: Number(fd.get('amount')), method: fd.get('method') }
      });
      nx.busy(btn, false);
      if (!res.ok) { nx.toast(nx.errText(res, 'Payment failed'), 'error'); return; }
      nx.flash('Payment of ' + nx.money(fd.get('amount')) + ' recorded', 'success');
      nx.reload();
    });
  });
})();
