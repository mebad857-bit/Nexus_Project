/* Nexus Assistant — floating rule-based chatbot (answers come from /api/chatbot, no external AI; the server limits answers to the signed-in role).
   Markup lives in Views/Shared/_Chatbot.cshtml. Everything is rendered with textContent, never innerHTML. */
(function () {
  'use strict';

  var root = document.getElementById('nxbot');
  if (!root) return;

  var fab = document.getElementById('nxbot-fab');
  var panel = document.getElementById('nxbot-panel');
  var log = document.getElementById('nxbot-log');
  var form = document.getElementById('nxbot-form');
  var input = document.getElementById('nxbot-input');
  var sendBtn = document.getElementById('nxbot-send');
  var closeBtn = document.getElementById('nxbot-close');

  var started = false;
  var busy = false;

  /* ------------------------------- tiny helpers ------------------------------- */
  function el(tag, cls, text) {
    var n = document.createElement(tag);
    if (cls) n.className = cls;
    if (text != null) n.textContent = text;
    return n;
  }

  function csrf() {
    var m = document.querySelector('meta[name="csrf-token"]');
    return m ? m.content : '';
  }

  async function call(url, body) {
    var headers = { RequestVerificationToken: csrf() };
    if (body !== undefined) headers['Content-Type'] = 'application/json';
    var res = await fetch(url, {
      method: body === undefined ? 'GET' : 'POST',
      headers: headers,
      credentials: 'same-origin',
      body: body === undefined ? undefined : JSON.stringify(body)
    });
    if (!res.ok) throw new Error('HTTP ' + res.status);
    return res.json();
  }

  function toBottom() { log.scrollTop = log.scrollHeight; }

  /** only same-site paths are ever opened from a bot reply */
  function safeHref(h) {
    return typeof h === 'string' && h.charAt(0) === '/' && h.charAt(1) !== '/' ? h : null;
  }

  /* -------------------------------- rendering --------------------------------- */
  function addUser(text) {
    var m = el('div', 'nxbot-msg me');
    m.appendChild(el('p', 'nxbot-bubble', text));
    log.appendChild(m);
    toBottom();
  }

  function renderSteps(steps) {
    var wrap = el('div', 'nxbot-steps');
    wrap.setAttribute('role', 'list');
    steps.forEach(function (s, i) {
      var st = el('div', 'nxbot-step ' + (s.state || 'todo'));
      st.setAttribute('role', 'listitem');
      var mark = s.state === 'done' ? '✓' : s.state === 'fail' ? '✕' : String(i + 1);
      st.appendChild(el('span', 'nxbot-dot', mark));
      st.appendChild(el('span', '', s.label));
      wrap.appendChild(st);
    });
    return wrap;
  }

  function renderItem(it) {
    var card = el('div', 'nxbot-card');
    var top = el('div', 'nxbot-card-top');
    top.appendChild(el('span', 'nxbot-card-title', it.title));
    if (it.badge) top.appendChild(el('span', 'nxbot-badge', it.badge));
    card.appendChild(top);
    if (it.lines && it.lines.length) {
      var ul = el('ul');
      it.lines.forEach(function (l) { ul.appendChild(el('li', '', l)); });
      card.appendChild(ul);
    }
    return card;
  }

  function renderButtons(buttons) {
    var row = el('div', 'nxbot-actions');
    buttons.forEach(function (b) {
      var href = safeHref(b.href);
      if (href) {
        var a = el('a', 'nxbot-chip link', b.label);
        a.href = href;
        a.addEventListener('click', function (e) { onLink(e, href); });
        row.appendChild(a);
      } else if (b.text) {
        var btn = el('button', 'nxbot-chip', b.label);
        btn.type = 'button';
        btn.addEventListener('click', function () { send(b.text, b.label); });
        row.appendChild(btn);
      }
    });
    return row;
  }

  function addBot(reply) {
    // only the newest reply keeps its quick buttons
    log.querySelectorAll('.nxbot-actions').forEach(function (n) { n.remove(); });

    var m = el('div', 'nxbot-msg bot');
    if (reply.text) m.appendChild(el('p', 'nxbot-bubble', reply.text));
    if (reply.steps && reply.steps.length) m.appendChild(renderSteps(reply.steps));
    (reply.items || []).forEach(function (it) { m.appendChild(renderItem(it)); });
    if (reply.buttons && reply.buttons.length) m.appendChild(renderButtons(reply.buttons));
    log.appendChild(m);
    toBottom();
  }

  function showTyping(on) {
    var t = document.getElementById('nxbot-typing');
    if (on && !t) {
      var m = el('div', 'nxbot-msg bot');
      m.id = 'nxbot-typing';
      var d = el('div', 'nxbot-typing');
      d.appendChild(el('i')); d.appendChild(el('i')); d.appendChild(el('i'));
      m.appendChild(d);
      log.appendChild(m);
      toBottom();
    } else if (!on && t) {
      t.remove();
    }
  }

  function setBusy(on) {
    busy = on;
    sendBtn.disabled = on;
    input.disabled = on;
    if (!on && panel.hidden === false && window.innerWidth > 640) input.focus();
  }

  /* --------------------------------- flow ------------------------------------- */
  var FALLBACK_BUTTONS = [
    { label: 'Plans', text: 'plans' },
    { label: 'Track order', text: 'track order' },
    { label: 'Complaint', text: 'complaint' }
  ];

  async function send(text, label) {
    text = (text || '').trim();
    if (!text || busy) return;
    addUser(label || text);
    setBusy(true);
    showTyping(true);
    try {
      var reply = await call(root.dataset.askUrl, { message: text });
      // short pause so the typing dots are seen and the answer does not "pop"
      await new Promise(function (r) { setTimeout(r, 250); });
      showTyping(false);
      addBot(reply);
    } catch (e) {
      showTyping(false);
      addBot({ text: 'Connection problem. Please try again in a moment.', buttons: FALLBACK_BUTTONS });
    }
    setBusy(false);
  }

  /** links that point to the page we are already on: scroll / switch tab instead of reloading */
  function onLink(e, href) {
    var u;
    try { u = new URL(href, location.origin); } catch (x) { return; }
    if (u.pathname !== location.pathname || !u.hash) return;
    var id = u.hash.slice(1);
    var nx = window.nx;
    if (nx && nx.showTab && nx.tabIds && nx.tabIds.indexOf(id) >= 0) {
      e.preventDefault();
      nx.showTab(id);
      closePanel();
      return;
    }
    var target = document.getElementById(id);
    if (target) {
      e.preventDefault();
      history.replaceState(null, '', u.hash);
      closePanel();
      target.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
  }

  async function start() {
    started = true;
    showTyping(true);
    try {
      addBotAfter(await call(root.dataset.initUrl));
    } catch (e) {
      showTyping(false);
      addBot({
        text: 'Hello! I\'m the Nexus Assistant. Pick an option below.',
        buttons: FALLBACK_BUTTONS
      });
    }
  }
  function addBotAfter(reply) { showTyping(false); addBot(reply); }

  /* ------------------------------- open / close ------------------------------- */
  function openPanel() {
    panel.hidden = false;
    fab.setAttribute('aria-expanded', 'true');
    fab.setAttribute('aria-label', 'Close chat assistant');
    if (!started) start();
    toBottom();
    if (window.innerWidth > 640) setTimeout(function () { input.focus(); }, 60);
  }

  function closePanel() {
    panel.hidden = true;
    fab.setAttribute('aria-expanded', 'false');
    fab.setAttribute('aria-label', 'Open chat assistant');
  }

  fab.addEventListener('click', function () { panel.hidden ? openPanel() : closePanel(); });
  closeBtn.addEventListener('click', function () { closePanel(); fab.focus(); });
  document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape' && !panel.hidden) { closePanel(); fab.focus(); }
  });

  form.addEventListener('submit', function (e) {
    e.preventDefault();
    var v = input.value;
    input.value = '';
    send(v);
  });
})();
