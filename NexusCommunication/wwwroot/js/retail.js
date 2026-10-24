/* Retail outlet desk — place order, order-book filter, customers, counter collections */
(function () {
    'use strict';
    var $ = function (id) { return document.getElementById(id); };
    var data = nx.json('retail-data') || { customers: [], cities: [] };

    // bulk slabs: 20-30 -> 10%, 31-40 -> 30%, 41+ -> 40% (same rule as Fmt.BulkDiscount)
    function bulkDiscount(q) { return q >= 41 ? 40 : q >= 31 ? 30 : q >= 20 ? 10 : 0; }

    /* ---- place order ---- */
    var mode = 'new';
    var modeBtns = document.querySelectorAll('[data-mode]');
    var newBox = $('new-box'), existingBox = $('existing-box');
    var requiredNew = [];
    newBox.querySelectorAll('[name]').forEach(function (el) { if (el.required) requiredNew.push(el); });

    function setMode(m) {
        mode = m;
        modeBtns.forEach(function (b) { b.classList.toggle('active', b.dataset.mode === m); });
        newBox.hidden = m !== 'new';
        existingBox.hidden = m !== 'existing';
        requiredNew.forEach(function (el) { el.required = m === 'new'; });   // only the visible block may be validated
        $('cust-id').required = m === 'existing';
        refresh();
    }
    modeBtns.forEach(function (b) { b.addEventListener('click', function () { setMode(b.dataset.mode); }); });

    function selectedPlan() { var sel = $('plan-id'); return sel.value ? sel.options[sel.selectedIndex] : null; }
    function qty() { return Math.min(10000, Math.max(1, Math.floor(Number($('qty').value) || 1))); }

    function refresh() {
        var opt = selectedPlan(), q = qty(), d = bulkDiscount(q);
        $('plan-summary').hidden = !opt;
        if (opt) {
            $('sum-price').textContent = nx.money(Number(opt.dataset.price) * q);
            $('sum-dep-label').textContent = 'Security deposit × ' + q;
            var dep = Number(opt.dataset.deposit) * q;
            $('sum-deposit').textContent = nx.money(dep);
            $('sum-discount-row').hidden = d === 0;
            $('sum-discount-label').textContent = 'Corporate rebate (' + q + ' lines)';
            $('sum-discount').textContent = d + '% off · saves ' + nx.money((Number(opt.dataset.price) * q + dep) * d / 100);
        }
        $('order-submit').disabled = !opt || (mode === 'existing' && !$('cust-id').value);
    }
    $('plan-id').addEventListener('change', refresh);
    $('qty').addEventListener('input', refresh);
    $('cust-id').addEventListener('change', refresh);
    setMode('new');

    /** "New order" shortcut from the customers table */
    function orderFor(id) {
        nx.showTab('order');
        $('order-success').hidden = true; $('order-form').hidden = false;
        setMode('existing');
        $('cust-id').value = String(id);
        refresh();
        $('plan-id').focus();
    }
    document.querySelectorAll('[data-order-for]').forEach(function (b) {
        b.addEventListener('click', function () { orderFor(b.dataset.orderFor); });
    });

    $('order-form').addEventListener('submit', async function (e) {
        e.preventDefault();
        var btn = $('order-submit'), err = $('order-error');
        err.textContent = '';
        var form = $('order-form');
        if (!form.checkValidity()) { form.reportValidity(); return; }

        var opt = selectedPlan();
        var body = { planId: Number($('plan-id').value), connectionType: opt.dataset.type, quantity: qty() };
        if (mode === 'existing') {
            body.customerId = Number($('cust-id').value);
        } else {
            var fd = new FormData(form);
            var cityOpt = form.querySelector('[name=city] option:checked');
            var code = (/\((\d{3})\)$/.exec(cityOpt ? cityOpt.textContent : '') || [])[1] || '101';
            body.customer = {
                name: fd.get('name'), phone: fd.get('phone'), email: fd.get('email'), address: fd.get('address'),
                city: fd.get('city'), cityCode: code, idProof: fd.get('idProof'), altPhone: fd.get('altPhone')
            };
        }
        nx.busy(btn, true);
        var res = await nx.api('/api/orders', { method: 'POST', body: body });
        nx.busy(btn, false);
        if (!res.ok) { err.textContent = nx.errText(res, 'Order failed'); return; }
        $('order-success-id').textContent = res.data.order.orderId;
        $('copy-order-id').dataset.copy = res.data.order.orderId;
        $('order-success').hidden = false;
        form.hidden = true;
        window.scrollTo({ top: 0, behavior: 'smooth' });
        nx.toast('Order ' + res.data.order.orderId + ' lodged', 'success');
    });

    $('book-another').addEventListener('click', function () {
        $('order-form').reset(); $('order-success').hidden = true; $('order-form').hidden = false;
        setMode('new'); refresh();
    });
    $('open-book').addEventListener('click', function () { location.hash = '#book'; nx.reload(); });

    /* ---- order book filter ---- */
    $('book-filter').addEventListener('change', function () {
        var f = this.value;
        document.querySelectorAll('[data-book-row]').forEach(function (row) { row.hidden = !(f === 'all' || row.dataset.status === f); });
        nx.refreshTables();
    });

    /* ---- customers ---- */
    function customerFields(c) {
        c = c || {};
        return [
            { name: 'name', label: 'Full name', required: true, value: c.name },
            { name: 'phone', label: 'Phone', required: true, value: c.phone },
            { name: 'email', label: 'E-mail', value: c.email, sendEmpty: !!c.id },
            { name: 'altPhone', label: 'Alternate phone', value: c.altPhone, sendEmpty: !!c.id },
            { name: 'city', label: 'City', type: 'select', required: true, value: c.city, options: data.cities.map(function (x) { return [x.value, x.label]; }) },
            { name: 'idProof', label: 'ID / address proof', value: c.idProof, sendEmpty: !!c.id },
            { name: 'address', label: 'Address', required: true, wide: true, value: c.address }
        ];
    }
    var addC = $('add-customer');
    if (addC) addC.addEventListener('click', function () {
        nx.formModal({
            title: 'Add customer', subtitle: 'The record can be used for any later order.', fields: customerFields(), submitLabel: 'Save customer', imagePicker: true,
            onSubmit: async function (v, photo) {
                var res = await nx.api('/api/crud/customers', { method: 'POST', body: v });
                if (!res.ok) return { ok: false, error: nx.errText(res, 'Could not save') };
                var note = 'Customer added', kind = 'success';
                if (photo && res.data && res.data.id) { var up = await nx.uploadImage('customers', res.data.id, photo); if (!up.ok) { note = 'Customer added, but the photo was not uploaded: ' + up.error; kind = 'error'; } }
                nx.flash(note, kind); location.hash = '#customers'; nx.reload();
                return { ok: true };
            }
        });
    });
    document.querySelectorAll('[data-edit-customer]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var id = Number(btn.dataset.editCustomer);
            var c = data.customers.filter(function (x) { return x.id === id; })[0];
            if (!c) { nx.toast('Could not load that customer', 'error'); return; }
            nx.formModal({
                title: 'Edit customer', subtitle: c.name, fields: customerFields(c), submitLabel: 'Save changes',
                image: { resource: 'customers', id: id, url: c.imageUrl || '', name: c.name, label: 'Customer photo' },
                onSubmit: async function (v) {
                    v.id = id;
                    var res = await nx.api('/api/crud/customers', { method: 'PATCH', body: v });
                    if (!res.ok) return { ok: false, error: nx.errText(res, 'Could not save') };
                    nx.flash('Customer updated', 'success'); location.hash = '#customers'; nx.reload();
                    return { ok: true };
                }
            });
        });
    });

    /* ---- collections ---- */
    document.querySelectorAll('[data-collect-toggle]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var f = document.querySelector('[data-collect-form="' + btn.dataset.collectToggle + '"]');
            f.hidden = !f.hidden;
            btn.textContent = f.hidden ? 'Collect' : 'Cancel';
            if (!f.hidden) f.querySelector('input').focus();
        });
    });
    document.querySelectorAll('[data-collect-form]').forEach(function (f) {
        f.addEventListener('submit', async function (e) {
            e.preventDefault();
            var btn = f.querySelector('button');
            var fd = new FormData(f);
            nx.busy(btn, true);
            var res = await nx.api('/api/payments', {
                method: 'POST',
                body: { billId: Number(f.dataset.collectForm), amount: Number(fd.get('amount')), method: fd.get('method') }
            });
            nx.busy(btn, false);
            if (!res.ok) { nx.toast(nx.errText(res, 'Payment failed'), 'error'); return; }
            nx.flash('Receipt of ' + nx.money(fd.get('amount')) + ' recorded', 'success');
            location.hash = '#collections';
            nx.reload();
        });
    });
})();
