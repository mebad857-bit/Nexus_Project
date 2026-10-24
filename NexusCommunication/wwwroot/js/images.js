/* Nexus — picture CRUD (create / replace / delete) shared by every page.
   Server side: POST|DELETE /api/images/{resource}/{id}  (resource = customers | employees | vendors | equipment | shops | plans) */
(function () {
  'use strict';
  var nx = (window.nx = window.nx || {});
  var MAX = 3 * 1024 * 1024;
  var TYPES = ['image/jpeg', 'image/png', 'image/webp', 'image/gif'];

  /** client-side check before sending; returns an error text or '' */
  nx.checkImage = function (file) {
    if (!file) return 'Choose a picture first';
    if (TYPES.indexOf(file.type) < 0) return 'Only JPG, PNG, WEBP or GIF pictures are allowed';
    if (file.size > MAX) return 'Picture is too large (max 3 MB)';
    return '';
  };

  /** nx.uploadImage('equipment', 12, file) -> { ok, imageUrl, error } */
  nx.uploadImage = async function (resource, id, file) {
    var bad = nx.checkImage(file);
    if (bad) return { ok: false, error: bad };
    var fd = new FormData(); fd.append('file', file);
    var res = await fetch('/api/images/' + resource + '/' + id, {
      method: 'POST', headers: { RequestVerificationToken: nx.csrf() }, credentials: 'same-origin', body: fd
    });
    var data = {}; try { data = await res.json(); } catch (e) { /* empty */ }
    return res.ok ? { ok: true, imageUrl: data.imageUrl } : { ok: false, error: (data && data.error) || 'Upload failed (' + res.status + ')' };
  };

  /** nx.removeImage('equipment', 12) -> { ok, error } */
  nx.removeImage = async function (resource, id) {
    var res = await fetch('/api/images/' + resource + '/' + id, {
      method: 'DELETE', headers: { RequestVerificationToken: nx.csrf() }, credentials: 'same-origin'
    });
    var data = {}; try { data = await res.json(); } catch (e) { /* empty */ }
    return res.ok ? { ok: true } : { ok: false, error: (data && data.error) || 'Could not remove the picture' };
  };

  /** same markup as Views/Shared/_ImageManager.cshtml, for dialogs built in JavaScript */
  nx.imageManagerHtml = function (o) {
    var url = o.url || '', e = nx.esc;
    var initials = String(o.name || '?').split(/\s+/).filter(Boolean).map(function (w, i, a) { return (i === 0 || i === a.length - 1) ? w.charAt(0) : ''; }).join('').toUpperCase() || '?';
    return '<div data-img-manager data-resource="' + e(o.resource) + '" data-id="' + Number(o.id) + '" class="flex items-center gap-5">' +
      '<span class="relative grid h-24 w-24 shrink-0 place-items-center overflow-hidden rounded-2xl bg-gradient-to-br from-cyan-500/30 to-violet-500/30 font-display text-2xl font-bold text-white ring-1 ring-cyan-400/30">' +
      '<span data-img-initials>' + e(initials) + '</span>' +
      '<img data-img-preview src="' + e(url) + '" alt="' + e(o.label || 'Photo') + '" onerror="this.style.display=\'none\'" class="absolute inset-0 h-full w-full object-cover' + (url ? '' : ' hidden') + '" /></span>' +
      '<div class="min-w-0"><p class="text-xs font-semibold uppercase tracking-[0.14em] text-slate-500">' + e(o.label || 'Photo') + '</p>' +
      '<p class="mt-1 text-xs text-slate-600">JPG, PNG, WEBP or GIF · max 3 MB</p>' +
      '<div class="mt-3 flex flex-wrap gap-2"><input type="file" data-img-file accept="image/jpeg,image/png,image/webp,image/gif" hidden />' +
      '<button type="button" data-img-pick class="rounded-lg border border-slate-700/70 px-3 py-1.5 text-xs font-semibold text-slate-300 transition hover:border-cyan-400/50 hover:text-cyan-300">' + (url ? 'Change photo' : 'Upload photo') + '</button>' +
      '<button type="button" data-img-remove class="rounded-lg border border-slate-700/70 px-3 py-1.5 text-xs font-semibold text-slate-400 transition hover:border-rose-400/50 hover:text-rose-300' + (url ? '' : ' hidden') + '">Remove</button></div>' +
      '<p data-img-msg class="mt-2 text-xs text-rose-300"></p></div></div>';
  };

  /** small dialog that only manages one record's picture: nx.imageDialog({ resource, id, url, name, title }) */
  nx.imageDialog = function (o) {
    return nx.formModal({
      title: o.title || 'Photo', subtitle: o.name || '', size: 'sm', submitLabel: 'Done', fields: [],
      image: { resource: o.resource, id: o.id, url: o.url, name: o.name, label: o.title || 'Photo' },
      onSubmit: async function () { return { ok: true }; }
    });
  };

  /** wire up one photo box (markup: Views/Shared/_ImageManager.cshtml). opts.reload = reload the page after a change */
  nx.initImageManager = function (box, opts) {
    opts = opts || {};
    if (box._imgReady) return; box._imgReady = true;
    var resource = box.dataset.resource, id = Number(box.dataset.id);
    var file = box.querySelector('[data-img-file]'), pick = box.querySelector('[data-img-pick]');
    var rm = box.querySelector('[data-img-remove]'), prev = box.querySelector('[data-img-preview]'), msg = box.querySelector('[data-img-msg]');

    function show(url) {
      if (url) { prev.src = url + (url.indexOf('?') < 0 ? '?v=' + Date.now() : ''); prev.style.display = ''; prev.classList.remove('hidden'); rm.classList.remove('hidden'); pick.textContent = 'Change photo'; }
      else { prev.removeAttribute('src'); prev.classList.add('hidden'); rm.classList.add('hidden'); pick.textContent = 'Upload photo'; }
    }
    pick.addEventListener('click', function () { file.click(); });
    file.addEventListener('change', async function () {
      var f = file.files[0]; file.value = ''; msg.textContent = '';
      if (!f) return;
      var bad = nx.checkImage(f); if (bad) { msg.textContent = bad; return; }
      pick.disabled = true; pick.textContent = 'Uploading…';
      var r = await nx.uploadImage(resource, id, f);
      pick.disabled = false;
      if (!r.ok) { msg.textContent = r.error; pick.textContent = rm.classList.contains('hidden') ? 'Upload photo' : 'Change photo'; return; }
      show(r.imageUrl); nx.toast('Photo saved', 'success');
      if (opts.reload !== false) { nx.flash('Photo saved', 'success'); nx.reload(); }
    });
    rm.addEventListener('click', async function () {
      msg.textContent = '';
      var ok = await nx.confirm({ title: 'Remove this photo?', danger: true, confirmLabel: 'Remove', message: 'The picture is deleted from the server. You can upload a new one any time.' });
      if (!ok) return;
      var r = await nx.removeImage(resource, id);
      if (!r.ok) { msg.textContent = r.error; return; }
      show(null); nx.toast('Photo removed', 'success');
      if (opts.reload !== false) { nx.flash('Photo removed', 'success'); nx.reload(); }
    });
  };

  /** live preview for a plain <input type=file data-img-input> (add forms / registration). Optional data-img-preview-target="#id" */
  function initPreviewInputs() {
    document.querySelectorAll('input[type=file][data-img-input]').forEach(function (inp) {
      var target = inp.dataset.imgPreviewTarget && document.querySelector(inp.dataset.imgPreviewTarget);
      var err = inp.parentElement && inp.parentElement.querySelector('[data-img-input-error]');
      inp.addEventListener('change', function () {
        var f = inp.files[0];
        if (err) err.textContent = '';
        if (!f) { if (target) { target.removeAttribute('src'); target.classList.add('hidden'); } return; }
        var bad = nx.checkImage(f);
        if (bad) { inp.value = ''; if (err) err.textContent = bad; if (target) target.classList.add('hidden'); return; }
        if (target) { target.src = URL.createObjectURL(f); target.classList.remove('hidden'); target.style.display = ''; }
      });
    });
  }

  function init() {
    document.querySelectorAll('[data-img-manager]').forEach(function (b) { nx.initImageManager(b); });
    initPreviewInputs();
  }
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init); else init();
})();
