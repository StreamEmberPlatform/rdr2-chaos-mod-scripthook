/* StreamEmber Chaos Mod — page script (MHud kit + StreamEmber overlay).
 *
 *   game -> page: window.streamember messages { action, data } are re-dispatched as window 'message' events so
 *                 MH.on('chaos:...') handlers receive them (same adapter as the trainers);
 *   page -> game: MH.post(name, data) -> window.streamember.post({ cb: name, data }).
 *
 * Messages from the game: chaos:init (effects, settings, theme), chaos:menu { open }, chaos:active { items, auto },
 * chaos:toast, chaos:settings, chaos:hello. To the game: ready, run { id }, stop { id }, stopAll, random, cleanup,
 * setting { key, value }, close.
 * Without the overlay (a normal browser) ?demo=1 fills the page with sample data.
 */
(function () {
  'use strict';
  var bridge = window.streamember;
  var MH = window.MH;
  var $ = function (s) { return document.querySelector(s); };

  /* ---------------- Overlay bridge ---------------- */
  MH.isNui = !!bridge;
  MH.post = function (name, data) {
    if (bridge) bridge.post({ cb: name, data: data || {} });
    else if (demo) demoPost(name, data || {});
    return Promise.resolve({ ok: true });
  };
  if (bridge) {
    bridge.on(function (msg) {
      if (msg && typeof msg === 'object') window.dispatchEvent(new MessageEvent('message', { data: msg }));
    });
  }

  var CATS = [
    { id: 'player', label: 'Oyuncu', icon: 'user' },
    { id: 'peds', label: 'NPC', icon: 'users' },
    { id: 'world', label: 'Dünya', icon: 'world' },
    { id: 'vehicles', label: 'Araç', icon: 'car' },
    { id: 'meta', label: 'Meta', icon: 'fire-circle' }
  ];
  var CAT_LABEL = {};
  CATS.forEach(function (c) { CAT_LABEL[c.id] = c.label; });

  var state = {
    effects: [], byId: {},
    view: 'player',          // category id, 'active' or 'settings'
    filter: 'all', query: '', sel: null,
    open: false, menuKey: 'F6', menuKeyCode: 117,
    settings: { auto: false, interval: 45, experimental: false },
    active: [], activeAt: 0, auto: null,
    activeKey: ''
  };
  var demo = !bridge && /[?&]demo=1/.test(location.search);

  MH.autoScale();

  /* ---------------- Helpers ---------------- */
  function esc(s) { return MH.esc(s == null ? '' : String(s)); }
  function icon(name) { return MH.icon(name || 'sparkles'); }
  function seconds(ms) { return Math.max(0, Math.ceil(ms / 1000)); }
  function durText(e) { return e.dur > 0 ? e.dur + ' sn' : 'Anlık'; }
  function activeOf(id) {
    for (var i = 0; i < state.active.length; i++) if (state.active[i].id === id && !state.active[i].instant) return state.active[i];
    return null;
  }
  function leftNow(a) { return Math.max(0, a.left - (performance.now() - state.activeAt)); }

  function visibleEffects() {
    var q = state.query.trim().toLocaleLowerCase('tr');
    return state.effects.filter(function (e) {
      if (q) {
        var hay = (e.name + ' ' + e.desc + ' ' + e.id).toLocaleLowerCase('tr');
        if (hay.indexOf(q) < 0) return false;
      } else if (e.cat !== state.view) {
        return false;
      }
      if (state.filter === 'instant' && e.dur > 0) return false;
      if (state.filter === 'timed' && e.dur === 0) return false;
      return true;
    });
  }

  /* ---------------- Navigation ---------------- */
  function renderNav() {
    var counts = {};
    state.effects.forEach(function (e) { counts[e.cat] = (counts[e.cat] || 0) + 1; });
    var running = state.active.filter(function (a) { return !a.instant; }).length;
    var html = '<div class="mh-nav__group"><span class="mh-kicker">Efektler</span></div>';
    CATS.forEach(function (c) {
      html += '<button class="mh-nav__item' + (state.view === c.id && !state.query ? ' is-active' : '') + '" data-view="' + c.id + '">' +
        icon(c.icon) + esc(c.label) + '<span class="x-nav-count">' + (counts[c.id] || 0) + '</span></button>';
    });
    html += '<div class="mh-nav__group"><span class="mh-kicker">Durum</span></div>' +
      '<button class="mh-nav__item' + (state.view === 'active' ? ' is-active' : '') + '" data-view="active">' + icon('timer') + 'Çalışanlar' +
      (running ? '<span class="mh-count">' + running + '</span>' : '<span class="x-nav-count">0</span>') + '</button>' +
      '<div class="mh-nav__group"><span class="mh-kicker">Sistem</span></div>' +
      '<button class="mh-nav__item' + (state.view === 'settings' ? ' is-active' : '') + '" data-view="settings">' + icon('settings') + 'Ayarlar</button>';
    $('#nav').innerHTML = html;
  }

  function setView(view) {
    state.view = view;
    if (view !== 'active' && view !== 'settings' && state.query) {
      state.query = '';
      $('#search').value = '';
    }
    $('#panel-list').classList.toggle('mh-hidden', view === 'active' || view === 'settings');
    $('#panel-active').classList.toggle('mh-hidden', view !== 'active');
    $('#panel-settings').classList.toggle('mh-hidden', view !== 'settings');
    renderNav();
    if (view === 'active') renderActivePanel();
    else if (view !== 'settings') renderList(true);
  }

  $('#nav').addEventListener('click', function (e) {
    var b = e.target.closest('[data-view]');
    if (b) setView(b.getAttribute('data-view'));
  });

  /* ---------------- Effect list ---------------- */
  function rowHtml(e) {
    var a = activeOf(e.id);
    var meta = '';
    if (e.exp) meta += '<span class="mh-badge mh-t-danger">Deneysel</span>';
    if (a) meta += '<span class="mh-badge mh-badge--dot mh-t-success">Aktif</span>';
    meta += '<span class="mh-badge' + (e.dur > 0 ? ' mh-t-info' : '') + '">' + durText(e) + '</span>';
    var sub = state.query ? CAT_LABEL[e.cat] + ' · ' + e.desc : e.desc;
    return '<div class="mh-row' + (state.sel === e.id ? ' is-active' : '') + (e.exp && !state.settings.experimental ? ' x-locked' : '') + '" data-id="' + esc(e.id) + '">' +
      '<span class="mh-row__icon">' + icon(e.icon) + '</span>' +
      '<div class="mh-row__main"><span class="mh-row__title">' + esc(e.name) + '</span><span class="mh-row__sub">' + esc(sub) + '</span></div>' +
      '<div class="mh-row__meta">' + meta + '</div></div>';
  }

  function renderList(keepSelection) {
    var items = visibleEffects();
    if (!keepSelection || !items.some(function (e) { return e.id === state.sel; })) {
      state.sel = items.length ? items[0].id : null;
    }
    $('#list').innerHTML = items.length ? items.map(rowHtml).join('') : '<div class="x-empty">Bu filtrede efekt yok.</div>';
    scrollToSelection();
    renderAside();
  }

  function scrollToSelection() {
    var row = $('#list .mh-row.is-active');
    if (row && row.scrollIntoView) row.scrollIntoView({ block: 'nearest' });
  }

  function select(id) {
    state.sel = id;
    Array.prototype.forEach.call(document.querySelectorAll('#list .mh-row'), function (r) {
      r.classList.toggle('is-active', r.getAttribute('data-id') === id);
    });
    scrollToSelection();
    renderAside();
  }

  $('#list').addEventListener('click', function (e) {
    var r = e.target.closest('.mh-row');
    if (r) select(r.getAttribute('data-id'));
  });
  $('#list').addEventListener('dblclick', function (e) {
    var r = e.target.closest('.mh-row');
    if (r) run(r.getAttribute('data-id'));
  });

  $('#search').addEventListener('input', function () {
    state.query = this.value;
    renderNav();
    renderList(false);
  });
  $('#filter').addEventListener('click', function (e) {
    var b = e.target.closest('button[data-f]');
    if (!b) return;
    state.filter = b.getAttribute('data-f');
    Array.prototype.forEach.call(this.querySelectorAll('button'), function (x) { x.classList.toggle('is-active', x === b); });
    renderList(true);
  });
  $('#random').addEventListener('click', function () { MH.post('random'); });

  /* ---------------- Preview ---------------- */
  function renderAside() {
    var e = state.byId[state.sel];
    var runBtn = $('#pv-run');
    if (!e) {
      $('#pv-art').innerHTML = icon('sparkles');
      $('#pv-name').textContent = 'Efekt seç';
      $('#pv-cat').textContent = '';
      $('#pv-badges').innerHTML = '';
      $('#pv-desc').textContent = 'Soldan bir kategori, ortadan bir efekt seç.';
      runBtn.disabled = true;
      $('#pv-stop').classList.add('mh-hidden');
      $('#pv-running').classList.add('mh-hidden');
      return;
    }
    $('#pv-art').innerHTML = icon(e.icon);
    $('#pv-name').textContent = e.name;
    $('#pv-cat').textContent = CAT_LABEL[e.cat] + ' · ' + e.id;
    var badges = '<span class="mh-badge' + (e.dur > 0 ? ' mh-t-info' : '') + '">' + (e.dur > 0 ? icon('timer') : '') + durText(e) + '</span>';
    if (e.fake) badges += '<span class="mh-badge mh-t-legendary">Sahte efekt</span>';
    if (e.exp) badges += '<span class="mh-badge mh-t-danger">Deneysel</span>';
    if (e.cat === 'meta') badges += '<span class="mh-badge mh-t-gold">Meta</span>';
    $('#pv-badges').innerHTML = badges;
    $('#pv-desc').textContent = e.desc;
    var locked = e.exp && !state.settings.experimental;
    runBtn.disabled = locked;
    runBtn.innerHTML = locked ? icon('lock') + 'Ayarlar\'dan aç' : '<span class="mh-key mh-key--sm">ENTER</span>' + (activeOf(e.id) ? 'Süreyi yenile' : 'Çalıştır');
    updateRunning();
  }

  function updateRunning() {
    var e = state.byId[state.sel];
    var a = e ? activeOf(e.id) : null;
    $('#pv-stop').classList.toggle('mh-hidden', !a);
    $('#pv-running').classList.toggle('mh-hidden', !a);
    if (!a) return;
    var left = leftNow(a);
    $('#pv-left').textContent = seconds(left) + ' sn';
    MH.bar('#pv-bar', left / a.total * 100);
  }

  function run(id) {
    var e = state.byId[id];
    if (!e) return;
    if (e.exp && !state.settings.experimental) {
      MH.toast({ tone: 'warn', title: 'Deneysel efekt kapalı', text: 'Ayarlar > Deneysel efektler', duration: 3000 });
      return;
    }
    MH.post('run', { id: id });
  }

  $('#pv-run').addEventListener('click', function () { run(state.sel); });
  $('#pv-stop').addEventListener('click', function () { if (state.sel) MH.post('stop', { id: state.sel }); });

  /* ---------------- Running effects (HUD + panel) ---------------- */
  function renderHud() {
    var host = $('#hud-active');
    var html = '';
    state.active.forEach(function (a) {
      var left = leftNow(a);
      html += '<div class="x-chip' + (a.instant ? ' is-instant' : '') + (a.meta ? ' is-meta' : '') + '" data-id="' + esc(a.id) + '">' +
        '<span class="x-chip__icon">' + icon(a.icon) + '</span>' +
        '<span class="x-chip__name">' + esc(a.name) + '</span>' +
        '<span class="x-chip__time">' + (a.instant ? '' : seconds(left) + ' sn') + '</span>' +
        (a.instant ? '' : '<div class="mh-bar mh-bar--xs ' + (a.meta ? 'mh-t-gold' : 'mh-t-accent') + '"><i class="mh-bar__fill"></i></div>') +
        '</div>';
    });
    host.innerHTML = html;
    tickBars();
  }

  function renderActivePanel() {
    var running = state.active.filter(function (a) { return !a.instant; });
    var html = running.map(function (a) {
      return '<div class="mh-row x-active-row" data-id="' + esc(a.id) + '">' +
        '<span class="mh-row__icon">' + icon(a.icon) + '</span>' +
        '<div class="mh-row__main"><span class="mh-row__title">' + esc(a.name) + '</span>' +
        '<div class="mh-bar mh-bar--xs ' + (a.meta ? 'mh-t-gold' : 'mh-t-accent') + '"><i class="mh-bar__fill"></i></div></div>' +
        '<div class="mh-row__meta"><span class="x-chip__time" data-left></span>' +
        '<button class="mh-btn mh-btn--sm mh-btn--tone mh-t-danger" data-stop="' + esc(a.id) + '">' + icon('stop') + 'Durdur</button></div></div>';
    }).join('');
    $('#active-list').innerHTML = html || '<div class="x-empty">Şu an çalışan süreli efekt yok.</div>';
    tickBars();
  }

  $('#active-list').addEventListener('click', function (e) {
    var b = e.target.closest('[data-stop]');
    if (b) MH.post('stop', { id: b.getAttribute('data-stop') });
  });
  $('#stop-all').addEventListener('click', function () { MH.post('stopAll'); });
  $('#stop-all-2').addEventListener('click', function () { MH.post('stopAll'); });
  $('#cleanup').addEventListener('click', function () { MH.post('cleanup'); });

  /* Bars and seconds move every frame between the game's 5 Hz updates */
  function tickBars() {
    var byId = {};
    state.active.forEach(function (a) { if (!a.instant) byId[a.id] = a; });
    ['#hud-active .x-chip', '#active-list .x-active-row'].forEach(function (sel) {
      Array.prototype.forEach.call(document.querySelectorAll(sel), function (el) {
        var a = byId[el.getAttribute('data-id')];
        if (!a) return;
        var left = leftNow(a);
        var bar = el.querySelector('.mh-bar');
        if (bar) MH.bar(bar, left / a.total * 100);
        var t = el.querySelector('.x-chip__time');
        if (t) t.textContent = seconds(left) + ' sn';
      });
    });
    if (state.auto && state.auto.next > 0) {
      var next = Math.max(0, state.auto.next - (performance.now() - state.activeAt));
      $('#hud-auto').textContent = (state.auto.forced ? 'Total Chaos' : 'Otomatik') + ': sonraki efekt ' + seconds(next) + ' sn';
    }
    if (state.open) updateRunning();
  }
  (function loop() {
    if (state.active.length || (state.auto && state.auto.next > 0)) tickBars();
    requestAnimationFrame(loop);
  })();

  /* ---------------- Settings ---------------- */
  function renderSettings() {
    var s = state.settings;
    $('#set-auto').checked = !!s.auto;
    $('#set-exp').checked = !!s.experimental;
    $('#set-interval').value = s.interval;
    $('#set-interval-out').textContent = s.interval + ' sn';
    $('#stat-auto').textContent = s.auto ? s.interval + ' sn' : 'Kapalı';
  }
  $('#set-auto').addEventListener('change', function () { MH.post('setting', { key: 'auto', value: this.checked }); });
  $('#set-exp').addEventListener('change', function () { MH.post('setting', { key: 'experimental', value: this.checked }); });
  $('#set-interval').addEventListener('input', function () { $('#set-interval-out').textContent = this.value + ' sn'; });
  $('#set-interval').addEventListener('change', function () { MH.post('setting', { key: 'interval', value: +this.value }); });

  /* ---------------- Menu open / close & keyboard ---------------- */
  function setOpen(open) {
    state.open = !!open;
    $('#menu-layer').classList.toggle('mh-hidden', !state.open);
    document.body.classList.toggle('x-menu-open', state.open);
    if (state.open) {
      renderNav();
      if (state.view === 'active') renderActivePanel();
      else if (state.view !== 'settings') renderList(true);
    } else if (document.activeElement && document.activeElement.blur) {
      document.activeElement.blur();
    }
  }
  $('#menu-close').addEventListener('click', function () { MH.post('close'); if (!bridge) setOpen(false); });

  document.addEventListener('keydown', function (e) {
    if (!state.open) return;
    if (e.key === 'Escape' || e.key === state.menuKey || e.keyCode === state.menuKeyCode) {
      e.preventDefault();
      if (document.activeElement === $('#search') && state.query && e.key === 'Escape') {
        $('#search').value = '';
        state.query = '';
        renderNav();
        renderList(false);
        return;
      }
      MH.post('close');
      if (!bridge) setOpen(false);
      return;
    }
    var inList = state.view !== 'active' && state.view !== 'settings';
    if (e.key === 'ArrowLeft' || e.key === 'ArrowRight') {
      if (document.activeElement === $('#search')) return;
      var order = CATS.map(function (c) { return c.id; }).concat(['active', 'settings']);
      var i = order.indexOf(state.query ? 'player' : state.view);
      i = (i + (e.key === 'ArrowRight' ? 1 : order.length - 1)) % order.length;
      e.preventDefault();
      setView(order[i]);
      return;
    }
    if (!inList) return;
    if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
      var items = visibleEffects();
      if (!items.length) return;
      var idx = Math.max(0, items.findIndex(function (x) { return x.id === state.sel; }));
      idx = (idx + (e.key === 'ArrowDown' ? 1 : items.length - 1)) % items.length;
      e.preventDefault();
      select(items[idx].id);
    } else if (e.key === 'Enter') {
      e.preventDefault();
      run(state.sel);
    }
  });

  /* ---------------- Game messages ---------------- */
  MH.on('chaos:init', function (d) {
    state.effects = d.effects || [];
    state.byId = {};
    state.effects.forEach(function (e) { state.byId[e.id] = e; });
    state.settings = d.settings || state.settings;
    state.menuKey = d.menuKey || 'F6';
    state.menuKeyCode = d.menuKeyCode || 0;
    if (d.theme) MH.theme(d.theme);
    $('#stat-total').textContent = state.effects.length;
    $('#version').textContent = 'Kaos Modu ' + (d.version || '') + ' · ' + state.menuKey + ' menüyü açar/kapatır';
    $('#close-key').textContent = 'ESC';
    renderSettings();
    renderNav();
    renderList(false);
  });

  MH.on('chaos:menu', function (d) { setOpen(d.open); });

  MH.on('chaos:settings', function (d) {
    state.settings = d;
    renderSettings();
    if (state.open && state.view !== 'settings' && state.view !== 'active') renderList(true);
  });

  MH.on('chaos:active', function (d) {
    state.active = d.items || [];
    state.auto = d.auto || null;
    state.activeAt = performance.now();
    $('#hud-auto').classList.toggle('mh-hidden', !(state.auto && state.auto.next > 0));
    $('#stat-active').textContent = state.active.filter(function (a) { return !a.instant; }).length;
    // Rebuild the HUD only when the set of effects (or a disguised name) changed; times move in tickBars
    var key = state.active.map(function (a) { return a.id + ':' + a.name + ':' + a.instant; }).join('|');
    if (key !== state.activeKey) {
      state.activeKey = key;
      renderHud();
      if (state.open) {
        renderNav();
        if (state.view === 'active') renderActivePanel();
        else if (state.view !== 'settings') renderList(true);
      }
    }
  });

  MH.on('chaos:toast', function (d) {
    MH.toast({ tone: d.tone || 'info', title: d.title, text: d.text, icon: d.icon, duration: 3500, max: 4 });
  });

  MH.on('chaos:hello', function () { MH.post('ready'); });

  setView('player');
  MH.post('ready');

  /* ---------------- Demo (browser without the overlay) ---------------- */
  function demoPost(name, data) {
    if (name === 'run') demoRun(data.id);
    else if (name === 'stop') demoStop(data.id);
    else if (name === 'stopAll') { demoActive = []; demoSend(); }
    else if (name === 'random') demoRun(state.effects[Math.floor(Math.random() * state.effects.length)].id);
    else if (name === 'setting') { state.settings[data.key] = data.value; MH.route({ action: 'chaos:settings', data: state.settings }); }
    else if (name === 'close') setOpen(false);
  }
  var demoActive = [];
  function demoRun(id) {
    var e = state.byId[id];
    if (!e) return;
    demoActive = demoActive.filter(function (a) { return a.id !== id; });
    demoActive.push({ id: e.id, name: e.name, icon: e.icon, instant: !e.dur, meta: e.cat === 'meta', end: Date.now() + (e.dur ? e.dur * 1000 : 6000), total: e.dur ? e.dur * 1000 : 6000 });
    MH.route({ action: 'chaos:toast', data: { tone: 'accent', title: e.name, text: e.dur ? e.dur + ' sn' : null } });
    demoSend();
  }
  function demoStop(id) { demoActive = demoActive.filter(function (a) { return a.id !== id || a.instant; }); demoSend(); }
  function demoSend() {
    var now = Date.now();
    demoActive = demoActive.filter(function (a) { return a.end > now; });
    MH.route({ action: 'chaos:active', data: { items: demoActive.map(function (a) {
      return { id: a.id, name: a.name, icon: a.icon, instant: a.instant, meta: a.meta, left: a.end - now, total: a.total };
    }), auto: { on: state.settings.auto, forced: false, interval: state.settings.interval, next: 0 } } });
  }
  if (demo) {
    var sample = [
      ['launch_player_up', 'Havaya fırlat', 'player', 'arrow-up', 0, 'Oyuncuyu (ya da bindiği atı / arabayı) havaya fırlatır.'],
      ['set_drunk', 'Sarhoş', 'player', 'whiskey', 30, '30 saniye sarhoşluk: kamera sallanır, yürüyüş bozulur, ara ara düşersin.'],
      ['cow_skin', 'İnek oldun', 'player', 'user', 20, '20 saniye boyunca oyuncu bir inektir.'],
      ['honor_good', 'Onur: iyi', 'player', 'sun', 0, 'Onuru en iyi seviyeye çeker. (Deneysel)', true],
      ['spawn_vampire', 'Vampir', 'peds', 'moon', 0, 'Saint Denis\'in vampiri (1000 can) sana saldırır.'],
      ['party_time', 'Parti zamanı', 'peds', 'music', 25, '25 saniye: herkes (sen de) cancan dansı yapar.'],
      ['snowstorm', 'Kar fırtınası', 'world', 'snow', 45, '45 saniye boyunca tipi: kar, sert rüzgâr, beyaz örtü.'],
      ['spawn_ufo', 'UFO', 'world', 'alien', 25, '25 saniye: gece, sis ve insanları çeken bir UFO; sonunda patlar.'],
      ['horses_rain', 'At yağmuru', 'vehicles', 'horse', 25, '25 saniye boyunca gökten atlar yağar.'],
      ['total_chaos', 'Total Chaos', 'meta', 'fire-circle', 180, '3 dakika boyunca her 15 saniyede bir rastgele efekt çalışır.']
    ];
    MH.route({ action: 'chaos:init', data: {
      version: 'demo', theme: (location.search.match(/theme=(\w+)/) || [])[1] || 'frontier', menuKey: 'F6',
      settings: { auto: false, interval: 45, experimental: false },
      effects: sample.map(function (s) { return { id: s[0], name: s[1], cat: s[2], icon: s[3], dur: s[4], desc: s[5], exp: !!s[6] }; })
    } });
    setOpen(!/[?&]menu=0/.test(location.search));
    setInterval(demoSend, 200);
    demoRun('set_drunk');
    demoRun('total_chaos');
  }
})();
