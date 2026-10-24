/* Customer portal — request a connection, withdraw a request, send feedback, update profile */
(function () {
    'use strict';
    var $ = function (id) { return document.getElementById(id); };

    function bulkDiscount(q) { return q >= 41 ? 40 : q >= 31 ? 30 : q >= 20 ? 10 : 0; }
    function qtyVal() { return Math.min(10000, Math.max(1, Math.floor(Number($('p-qty').value) || 1))); }

    var form = $('portal-order-form');
    if (form) {
        function refresh() {
            var sel = $('p-plan'), opt = sel.value ? sel.options[sel.selectedIndex] : null;
            var q = qtyVal(), d = bulkDiscount(q);
            $('p-discount').hidden = d === 0;
            $('p-discount-text').textContent = q + ' lines unlock the ' + d + '% corporate rebate on advance and deposit.';
            $('p-deposit').hidden = !opt;
            if (opt) $('p-deposit').textContent = 'Refundable deposit ' + nx.money(Number(opt.dataset.deposit) * q) + ' (' + q + ' × ' + nx.money(opt.dataset.deposit) + ') · service tax 12.24% extra on bills.';
            $('p-submit').disabled = !opt;
        }
        $('p-plan').addEventListener('change', refresh);
        $('p-qty').addEventListener('input', refresh);

        form.addEventListener('submit', async function (e) {
            e.preventDefault();
            var btn = $('p-submit'), sel = $('p-plan'), opt = sel.options[sel.selectedIndex];
            $('p-error').textContent = ''; $('p-done').hidden = true;
            nx.busy(btn, true);
            var res = await nx.api('/api/orders', {
                method: 'POST',
                body: { planId: Number(sel.value), connectionType: opt.dataset.type, quantity: qtyVal() }
            });
            nx.busy(btn, false);
            if (!res.ok) { $('p-error').textContent = nx.errText(res, 'Request failed'); return; }
            try { sessionStorage.setItem('nx.portalOrder', res.data.order.orderId); } catch (e2) { /* ignore */ }
            nx.flash('Request lodged — your order id is ' + res.data.order.orderId, 'success');
            location.hash = '#orders';
            nx.reload();
        });

        var flash = null;
        try { flash = sessionStorage.getItem('nx.portalOrder'); sessionStorage.removeItem('nx.portalOrder'); } catch (e) { /* ignore */ }
        if (flash) { $('p-done-id').textContent = flash; $('p-done').hidden = false; }
    }

    /* ---- withdraw a pending request ---- */
    document.querySelectorAll('[data-cancel-order]').forEach(function (btn) {
        btn.addEventListener('click', async function () {
            var ok = await nx.confirm({
                title: 'Withdraw ' + btn.dataset.orderNo + '?', danger: true, confirmLabel: 'Withdraw request',
                message: 'The survey will not take place and the order id will be closed. You can always place a new request later.'
            });
            if (!ok) return;
            btn.disabled = true;
            var res = await nx.api('/api/orders', { method: 'PATCH', body: { id: Number(btn.dataset.cancelOrder), status: 'cancelled' } });
            if (!res.ok) { btn.disabled = false; nx.toast(nx.errText(res, 'Could not withdraw the request'), 'error'); return; }
            nx.flash('Request ' + btn.dataset.orderNo + ' withdrawn', 'success');
            location.hash = '#orders';
            nx.reload();
        });
    });

    /* ---- feedback ---- */
    var fb = $('fb-form');
    if (fb) fb.addEventListener('submit', async function (e) {
        e.preventDefault();
        var msg = $('fb-msg').value.trim();
        $('fb-error').textContent = '';
        if (msg.length < 5) { $('fb-error').textContent = 'Please write a few words so we can act on it.'; return; }
        nx.busy($('fb-submit'), true);
        var res = await nx.api('/api/crud/feedback', {
            method: 'POST',
            body: { name: fb.dataset.name, contact: fb.dataset.contact, rating: Number($('fb-rating').value) || 5, message: msg, connectionType: $('fb-type').value || undefined }
        });
        nx.busy($('fb-submit'), false);
        if (!res.ok) { $('fb-error').textContent = nx.errText(res, 'Could not send feedback'); return; }
        $('fb-msg').value = '';
        nx.toast('Thank you — your feedback reached the administration', 'success');
    });

    /* ---- profile ---- */
    var pf = $('profile-form');
    if (pf) pf.addEventListener('submit', async function (e) {
        e.preventDefault();
        var btn = $('pf-submit'), fd = new FormData(pf);
        $('pf-error').textContent = ''; $('pf-saved').hidden = true;
        nx.busy(btn, true);
        var res = await nx.api('/api/crud/customers', {
            method: 'PATCH',
            body: { id: Number(pf.dataset.customerId), phone: fd.get('phone'), email: fd.get('email'), altPhone: fd.get('altPhone'), address: fd.get('address') }
        });
        nx.busy(btn, false);
        if (!res.ok) { $('pf-error').textContent = nx.errText(res, 'Update failed'); return; }
        $('pf-saved').hidden = false;
        nx.toast('Contact details updated', 'success');
    });
})();
