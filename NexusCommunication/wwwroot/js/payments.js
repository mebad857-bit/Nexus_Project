/* Demo online payment — "Pay now" on the customer's bills.
   NOTHING typed into the card / wallet fields is ever sent to the server: only { billId, method, outcome }.
   Server: POST /api/payments/online (Controllers/Api/PaymentsApiController.cs) · receipt page: /receipt/{transactionId}
   Test data:  card ending 0002  or  mobile number ending 0000  -> payment is declined; anything else succeeds. */
(function () {
    'use strict';
    var cfg = document.getElementById('pay-config');
    var esc = nx.esc;

    var METHODS = [
        { id: 'card', label: 'Card', icon: 'credit-card' },
        { id: 'jazzcash', label: 'JazzCash', icon: 'smartphone' },
        { id: 'easypaisa', label: 'Easypaisa', icon: 'wallet' }
    ];
    var FAIL_STEPS = ['Contacting the demo bank…', 'Verifying details…', 'Confirming payment…'];

    function digits(v) { return String(v || '').replace(/\D/g, ''); }

    document.addEventListener('click', function (e) {
        var b = e.target.closest('[data-pay-bill]');
        if (!b) return;
        open({ id: Number(b.dataset.payBill), no: b.dataset.billNo, period: b.dataset.period, due: Number(b.dataset.due) });
    });

    function open(bill) {
        var state = { method: 'card', paid: false, busy: false, closed: false };
        var dlg = nx.dialog('', 'pay-modal', function () {
            state.closed = true;
            if (state.paid) { nx.flash('Payment successful — your bill is updated', 'success'); location.hash = '#bills'; nx.reload(); }
        });
        form();

        function head(title, sub) {
            var banner = cfg && cfg.dataset.banner ? '<div class="pay-banner"><img src="' + esc(cfg.dataset.banner) + '" alt="" onerror="this.parentElement.style.display=\'none\'" /></div>' : '';
            return banner + '<div class="pay-body"><div class="flex items-start justify-between gap-4"><div>' +
                '<h3 class="font-display text-xl font-bold text-slate-100">' + esc(title) + '</h3><p class="mt-1 text-sm text-slate-500">' + esc(sub) + '</p></div>' +
                '<button type="button" data-x class="text-slate-500 transition hover:text-slate-200" aria-label="Close"><i data-lucide="x" class="h-5 w-5"></i></button></div>';
        }
        function bindClose() { var x = dlg.el.querySelector('[data-x]'); if (x) x.addEventListener('click', function () { dlg.close(false); }); }

        /* ------------------------------ step 1: the demo form ------------------------------ */
        function form(msg) {
            dlg.el.innerHTML = head('Pay bill ' + bill.no, bill.period) +
                '<div class="pay-demo mt-4"><i data-lucide="flask-conical" class="mt-0.5 h-4 w-4 shrink-0"></i><span><b>DEMO PAYMENT</b>This is a simulation. No real money is charged and no card or wallet details leave this window or are stored. Please do not enter your real card details.</span></div>' +
                '<div class="mt-5 flex items-end justify-between rounded-2xl border border-slate-800 bg-ink-850 px-4 py-3"><span class="text-xs font-semibold uppercase tracking-[0.14em] text-slate-500">Amount to pay</span>' +
                '<span class="font-display text-2xl font-bold text-slate-100">' + esc(nx.money(bill.due)) + '</span></div>' +
                '<div class="pay-methods mt-5" role="radiogroup" aria-label="Payment method">' + METHODS.map(function (m) {
                    return '<button type="button" role="radio" aria-checked="' + (m.id === state.method) + '" data-m="' + m.id + '" class="pay-method' + (m.id === state.method ? ' on' : '') + '"><i data-lucide="' + m.icon + '"></i>' + m.label + '</button>';
                }).join('') + '</div>' +

                '<form data-form class="mt-5 space-y-4" novalidate autocomplete="off">' +
                '<div data-set="card" class="space-y-4"' + (state.method === 'card' ? '' : ' hidden') + '>' +
                field('Name on card', '<input name="cname" class="field" placeholder="e.g. Test Customer" autocomplete="off" maxlength="60" />') +
                field('Card number', '<input name="cnum" class="field font-mono tracking-wider" inputmode="numeric" placeholder="4242 4242 4242 4242" autocomplete="off" maxlength="19" />') +
                '<div class="grid grid-cols-2 gap-4">' +
                field('Expiry', '<input name="cexp" class="field font-mono" inputmode="numeric" placeholder="MM/YY" autocomplete="off" maxlength="5" />') +
                field('CVV', '<input name="ccvv" type="password" class="field font-mono" inputmode="numeric" placeholder="123" autocomplete="off" maxlength="3" />') + '</div></div>' +
                '<div data-set="wallet" class="space-y-4"' + (state.method === 'card' ? ' hidden' : '') + '>' +
                field('<span data-wlabel></span> mobile number', '<input name="wnum" class="field font-mono tracking-wider" inputmode="numeric" placeholder="03XX XXXXXXX" autocomplete="off" maxlength="12" />') +
                field('Demo PIN', '<input name="wpin" type="password" class="field font-mono" inputmode="numeric" placeholder="4 digits (any)" autocomplete="off" maxlength="4" />') + '</div>' +

                '<p data-err class="text-sm text-rose-300">' + esc(msg || '') + '</p>' +
                '<button type="submit" class="btn-glow flex w-full items-center justify-center gap-2 rounded-xl px-6 py-3.5 font-display text-sm font-bold uppercase tracking-wider text-white"><i data-lucide="shield-check" class="h-4 w-4"></i>Pay ' + esc(nx.money(bill.due)) + ' (demo)</button>' +
                '</form></div>';
            nx.icons(); bindClose(); wire();
        }
        function field(label, input) {
            return '<label class="block"><span class="mb-1.5 block text-xs font-semibold uppercase tracking-[0.14em] text-slate-500">' + label + '</span>' + input + '</label>';
        }

        function wire() {
            var el = dlg.el, f = el.querySelector('[data-form]'), err = el.querySelector('[data-err]');
            var mName = function () { return METHODS.filter(function (m) { return m.id === state.method; })[0].label; };

            function syncMethod() {
                el.querySelectorAll('[data-m]').forEach(function (b) {
                    var on = b.dataset.m === state.method; b.classList.toggle('on', on); b.setAttribute('aria-checked', on);
                });
                el.querySelector('[data-set="card"]').hidden = state.method !== 'card';
                el.querySelector('[data-set="wallet"]').hidden = state.method === 'card';
                el.querySelector('[data-wlabel]').textContent = mName();
                err.textContent = '';
            }
            el.querySelectorAll('[data-m]').forEach(function (b) { b.addEventListener('click', function () { state.method = b.dataset.m; syncMethod(); }); });
            syncMethod();

            /* light input formatting (display only) */
            f.cnum.addEventListener('input', function () { f.cnum.value = digits(f.cnum.value).slice(0, 16).replace(/(.{4})/g, '$1 ').trim(); });
            f.cexp.addEventListener('input', function () { var d = digits(f.cexp.value).slice(0, 4); f.cexp.value = d.length > 2 ? d.slice(0, 2) + '/' + d.slice(2) : d; });
            f.ccvv.addEventListener('input', function () { f.ccvv.value = digits(f.ccvv.value).slice(0, 3); });
            f.wnum.addEventListener('input', function () { var d = digits(f.wnum.value).slice(0, 11); f.wnum.value = d.length > 4 ? d.slice(0, 4) + ' ' + d.slice(4) : d; });
            f.wpin.addEventListener('input', function () { f.wpin.value = digits(f.wpin.value).slice(0, 4); });

            f.addEventListener('submit', function (ev) {
                ev.preventDefault(); err.textContent = '';
                var decline = false;
                if (state.method === 'card') {
                    var n = digits(f.cnum.value), ex = f.cexp.value, mm = Number(ex.slice(0, 2)), yy = Number(ex.slice(3, 5));
                    if (f.cname.value.trim().length < 2) { err.textContent = 'Enter the name on the card'; f.cname.focus(); return; }
                    if (n.length !== 16) { err.textContent = 'Card number must be 16 digits'; f.cnum.focus(); return; }
                    var now = new Date(), curY = now.getFullYear() % 100, curM = now.getMonth() + 1;
                    if (!/^\d\d\/\d\d$/.test(ex) || mm < 1 || mm > 12 || yy < curY || (yy === curY && mm < curM)) { err.textContent = 'Enter a valid expiry date (MM/YY)'; f.cexp.focus(); return; }
                    if (digits(f.ccvv.value).length !== 3) { err.textContent = 'CVV must be 3 digits'; f.ccvv.focus(); return; }
                    decline = n.slice(-4) === '0002';
                } else {
                    var w = digits(f.wnum.value);
                    if (w.length !== 11 || w.indexOf('03') !== 0) { err.textContent = 'Enter an 11-digit mobile number starting with 03'; f.wnum.focus(); return; }
                    if (digits(f.wpin.value).length !== 4) { err.textContent = 'Enter a 4-digit demo PIN'; f.wpin.focus(); return; }
                    decline = w.slice(-4) === '0000';
                }
                process(decline ? 'decline' : 'success');   // the typed values are dropped with the form; only the outcome travels on
            });
        }

        /* ------------------------------ step 2: processing ------------------------------ */
        function process(outcome) {
            state.busy = true;
            var ms = 2000 + Math.round(Math.random() * 1000);
            dlg.el.innerHTML = '<div class="pay-center" role="status" aria-live="polite"><div class="pay-ring"></div>' +
                '<h3 class="mt-6 font-display text-lg font-bold text-slate-100">Processing…</h3>' +
                '<p data-step class="mt-1 text-sm text-slate-500">' + FAIL_STEPS[0] + '</p>' +
                '<div class="pay-bar"><i data-fill></i></div>' +
                '<p class="mt-4 text-xs font-semibold uppercase tracking-[0.16em] text-amber-300">Demo payment · do not close</p></div>';
            var bar = dlg.el.querySelector('[data-fill]'), stepEl = dlg.el.querySelector('[data-step]');
            bar.style.transition = 'width ' + ms + 'ms linear';
            requestAnimationFrame(function () { requestAnimationFrame(function () { bar.style.width = '100%'; }); });
            FAIL_STEPS.forEach(function (t, i) { setTimeout(function () { if (!state.closed) stepEl.textContent = t; }, (ms / FAIL_STEPS.length) * i); });

            setTimeout(async function () {
                if (state.closed) return;                        // closed during the animation: nothing was charged, nothing was sent
                var res = await nx.api('/api/payments/online', { method: 'POST', body: { billId: bill.id, method: state.method, outcome: outcome } });
                state.busy = false;
                if (state.closed) { return; }
                if (!res.ok) { form(nx.errText(res, 'The payment could not be processed')); return; }
                if (res.data.status === 'success') { state.paid = true; success(res.data); } else failure(res.data);
            }, ms);
        }

        /* ------------------------------ step 3: result ------------------------------ */
        function row(k, v) { return '<div><span>' + esc(k) + '</span><span>' + esc(v) + '</span></div>'; }

        function success(d) {
            var img = cfg && cfg.dataset.success ? '<div class="pay-banner"><img src="' + esc(cfg.dataset.success) + '" alt="" onerror="this.parentElement.style.display=\'none\'" /></div>' : '';
            dlg.el.innerHTML = img + '<div class="pay-center" style="padding-top:' + (img ? '0.5' : '2.5') + 'rem">' +
                '<div class="pay-icon ok"><i data-lucide="check"></i></div>' +
                '<h3 class="mt-5 font-display text-xl font-bold text-slate-100">Payment successful</h3>' +
                '<p class="mt-1 text-sm text-slate-500">Your bill has been updated and a receipt is ready.</p>' +
                '<p class="mt-3 font-display text-3xl font-bold text-emerald-300">' + esc(nx.money(d.amount)) + '</p>' +
                '<div class="pay-rows">' +
                '<div><span>Transaction ID</span><span class="font-mono">' + esc(d.transactionId) + ' <button type="button" data-copy title="Copy" class="ml-1 text-slate-500 hover:text-cyan-300"><i data-lucide="copy" class="inline h-3.5 w-3.5"></i></button></span></div>' +
                row('Invoice', d.billNo) + row('Method', d.methodLabel + ' (demo)') +
                row('Invoice status', d.billStatus === 'paid' ? 'Paid in full' : 'Balance ' + nx.money(d.balanceDue)) + '</div>' +
                '<p class="mt-4 text-xs font-semibold uppercase tracking-[0.14em] text-amber-300">Demo payment — no real money moved</p>' +
                '<div class="mt-6 flex w-full flex-wrap gap-3"><a href="' + esc(d.receiptUrl) + '" target="_blank" rel="noopener" class="flex flex-1 items-center justify-center gap-2 rounded-xl border border-slate-700 px-5 py-3 text-sm font-semibold text-slate-300 transition hover:border-cyan-400/50 hover:text-cyan-300"><i data-lucide="receipt" class="h-4 w-4"></i>View receipt</a>' +
                '<button type="button" data-done class="btn-glow flex-1 rounded-xl px-5 py-3 text-sm font-semibold text-white">Done</button></div></div>';
            nx.icons();
            dlg.el.querySelector('[data-done]').addEventListener('click', function () { dlg.close(true); });
            var cp = dlg.el.querySelector('[data-copy]');
            if (cp) cp.addEventListener('click', function () {
                try { navigator.clipboard.writeText(d.transactionId); nx.toast('Transaction ID copied', 'success', 1800); } catch (e) { /* clipboard blocked */ }
            });
        }

        function failure(d) {
            dlg.el.innerHTML = '<div class="pay-center"><div class="pay-icon bad"><i data-lucide="x"></i></div>' +
                '<h3 class="mt-5 font-display text-xl font-bold text-slate-100">Payment failed</h3>' +
                '<p class="mt-1 text-sm text-slate-500">' + esc(d.reason || 'The payment could not be completed.') + '</p>' +
                '<div class="pay-rows"><div><span>Reference</span><span class="font-mono">' + esc(d.transactionId) + '</span></div>' +
                row('Method', d.methodLabel + ' (demo)') + row('Charged', nx.money(0)) + '</div>' +
                '<p class="mt-4 text-xs font-semibold uppercase tracking-[0.14em] text-amber-300">Demo payment — nothing was charged</p>' +
                '<div class="mt-6 flex w-full flex-wrap gap-3"><button type="button" data-close class="flex-1 rounded-xl border border-slate-700 px-5 py-3 text-sm font-semibold text-slate-300 transition hover:bg-white/5">Close</button>' +
                '<button type="button" data-retry class="btn-glow flex-1 rounded-xl px-5 py-3 text-sm font-semibold text-white">Try again</button></div></div>';
            nx.icons();
            dlg.el.querySelector('[data-close]').addEventListener('click', function () { dlg.close(false); });
            dlg.el.querySelector('[data-retry]').addEventListener('click', function () { form(); });
        }
    }
})();
