/* Password helpers for the Security page and the Reset page (the strength bar itself is in site.js):
   - [data-pw-rules-for="<input id>"]  : checklist whose rows ([data-rule]) turn green as the password meets them
   - [data-pw-match-for="<repeat id>"] [data-pw-with="<new id>"] : "Passwords match / do not match" line */
(function () {
  'use strict';
  var RULES = {
    len: function (v) { return v.length >= 6; },
    mix: function (v) { return /[a-z]/.test(v) && /[A-Z]/.test(v); },
    num: function (v) { return /\d/.test(v); },
    sym: function (v) { return /[^A-Za-z0-9]/.test(v); }
  };
  document.querySelectorAll('[data-pw-rules-for]').forEach(function (box) {
    var input = document.getElementById(box.getAttribute('data-pw-rules-for'));
    if (!input) return;
    function paint() {
      var v = input.value;
      box.querySelectorAll('[data-rule]').forEach(function (li) {
        var fn = RULES[li.getAttribute('data-rule')], ok = !!fn && fn(v);
        var dot = li.querySelector('[data-dot]');
        if (dot) dot.style.background = ok ? '#10b981' : '#cbd5e1';
        li.style.color = ok ? '#059669' : '';
      });
    }
    input.addEventListener('input', paint);
    paint();
  });
  document.querySelectorAll('[data-pw-match-for]').forEach(function (line) {
    var rep = document.getElementById(line.getAttribute('data-pw-match-for'));
    var neu = document.getElementById(line.getAttribute('data-pw-with'));
    if (!rep || !neu) return;
    function paint() {
      if (!rep.value) { line.textContent = ''; return; }
      var ok = rep.value === neu.value;
      line.textContent = ok ? 'Passwords match' : 'Passwords do not match yet';
      line.style.color = ok ? '#059669' : '#e11d48';
    }
    rep.addEventListener('input', paint);
    neu.addEventListener('input', paint);
  });
})();
