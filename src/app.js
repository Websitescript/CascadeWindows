'use strict';
const api = window.cascade;
const $ = (s, r = document) => r.querySelector(s);
const $$ = (s, r = document) => [...r.querySelectorAll(s)];
const esc = (s) => String(s).replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
const fmt = (s) => {
  s = Math.max(0, Math.floor(s || 0));
  const h = Math.floor(s / 3600), m = Math.floor((s % 3600) / 60), x = s % 60;
  const z = (n) => String(n).padStart(2, '0');
  return h ? `${h}:${z(m)}:${z(x)}` : `${z(m)}:${z(x)}`;
};
const human = (b) => (b >= 2 ** 30 ? (b / 2 ** 30).toFixed(1) + ' GB' : b >= 2 ** 20 ? Math.round(b / 2 ** 20) + ' MB' : Math.round(b / 1024) + ' KB');
const load = (k, d) => { try { const v = JSON.parse(localStorage.getItem(k)); return v == null ? d : v; } catch { return d; } };
const store = (k, v) => localStorage.setItem(k, JSON.stringify(v));
const isUrl = (p) => /^https?:/i.test(p);
const mediaSrc = (f) => (isUrl(f.path) ? f.path : 'cascade://media/' + encodeURIComponent(f.path));
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

/* ---------------- icons ---------------- */
const ICON = {
  search: 'M15.5 14h-.79l-.28-.27A6.47 6.47 0 0016 9.5 6.5 6.5 0 109.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z',
  add: 'M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z',
  sort: 'M3 18h6v-2H3v2zM3 6v2h18V6H3zm0 7h12v-2H3v2z',
  play: 'M8 5v14l11-7z',
  pause: 'M6 19h4V5H6v14zm8-14v14h4V5h-4z',
  next: 'M6 18l8.5-6L6 6v12zM16 6v12h2V6h-2z',
  prev: 'M6 6h2v12H6zm3.5 6l8.5 6V6z',
  rew: 'M12 5V1L7 6l5 5V7c3.31 0 6 2.69 6 6s-2.69 6-6 6-6-2.69-6-6H4c0 4.42 3.58 8 8 8s8-3.58 8-8-3.58-8-8-8z',
  fwd: 'M12 5V1l5 5-5 5V7c-3.31 0-6 2.69-6 6s2.69 6 6 6 6-2.69 6-6h2c0 4.42-3.58 8-8 8s-8-3.58-8-8 3.58-8 8-8z',
  back: 'M20 11H7.83l5.59-5.59L12 4l-8 8 8 8 1.41-1.41L7.83 13H20v-2z',
  camera: 'M9 2L7.17 4H4c-1.1 0-2 .9-2 2v12c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2h-3.17L15 2H9zm3 15c-2.76 0-5-2.24-5-5s2.24-5 5-5 5 2.24 5 5-2.24 5-5 5zm0-8a3 3 0 100 6 3 3 0 000-6z',
  headphones: 'M12 1c-4.97 0-9 4.03-9 9v7c0 1.66 1.34 3 3 3h3v-8H5v-2c0-3.87 3.13-7 7-7s7 3.13 7 7v2h-4v8h3c1.66 0 3-1.34 3-3v-7c0-4.97-4.03-9-9-9z',
  video: 'M17 10.5V7c0-.55-.45-1-1-1H4c-.55 0-1 .45-1 1v10c0 .55.45 1 1 1h12c.55 0 1-.45 1-1v-3.5l4 4v-11l-4 4z',
  pip: 'M19 7h-8v6h8V7zm2-4H3c-1.1 0-2 .9-2 1.98v14.04C1 20.1 1.9 21 3 21h18c1.1 0 2-.9 2-2.02V4.98C23 3.9 22.1 3 21 3zm0 16.02H3V4.97h18v14.05z',
  aspect: 'M19 12h-2v3h-3v2h5v-5zM7 9h3V7H5v5h2V9zm14-6H3c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h18c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm0 16.01H3V4.99h18v14.02z',
  subs: 'M20 4H4c-1.1 0-2 .9-2 2v12c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2zM4 12h4v2H4v-2zm10 6H4v-2h10v2zm6 0h-4v-2h4v2zm0-4H10v-2h10v2z',
  track: 'M12 3v10.55A4 4 0 1014 17V7h4V3h-6z',
  speed: 'M20.38 8.57l-1.23 1.85a8 8 0 01-.22 7.58H5.07A8 8 0 0115.58 6.85l1.85-1.23A10 10 0 003.35 19a2 2 0 001.72 1h13.85a2 2 0 001.74-1 10 10 0 00-.27-10.44zm-9.79 6.84a2 2 0 002.83 0l5.66-8.49-8.49 5.66a2 2 0 000 2.83z',
  more: 'M12 8c1.1 0 2-.9 2-2s-.9-2-2-2-2 .9-2 2 .9 2 2 2zm0 2c-1.1 0-2 .9-2 2s.9 2 2 2 2-.9 2-2-.9-2-2-2zm0 6c-1.1 0-2 .9-2 2s.9 2 2 2 2-.9 2-2-.9-2-2-2z',
  full: 'M7 14H5v5h5v-2H7v-3zm-2-4h2V7h3V5H5v5zm12 7h-3v2h5v-5h-2v3zM14 5v2h3v3h2V5h-5z',
  fullx: 'M5 16h3v3h2v-5H5v2zm3-8H5v2h5V5H8v3zm6 11h2v-3h3v-2h-5v5zm2-11V5h-2v5h5V8h-3z',
  vol: 'M3 9v6h4l5 5V4L7 9H3zm13.5 3c0-1.77-1.02-3.29-2.5-4.03v8.05c1.48-.73 2.5-2.25 2.5-4.02zM14 3.23v2.06c2.89.86 5 3.54 5 6.71s-2.11 5.85-5 6.71v2.06c4.01-.91 7-4.49 7-8.77s-2.99-7.86-7-8.77z',
  mute: 'M16.5 12c0-1.77-1.02-3.29-2.5-4.03v2.21l2.45 2.45c.03-.2.05-.41.05-.63zm2.5 0c0 .94-.2 1.82-.54 2.64l1.51 1.51A8.8 8.8 0 0021 12c0-4.28-2.99-7.86-7-8.77v2.06c2.89.86 5 3.54 5 6.71zM4.27 3L3 4.27 7.73 9H3v6h4l5 5v-6.73l4.25 4.25c-.67.52-1.42.93-2.25 1.18v2.06a8.99 8.99 0 003.69-1.81L19.73 21 21 19.73l-9-9L4.27 3zM12 4L9.91 6.09 12 8.18V4z',
  close: 'M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z',
  folder: 'M10 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2h-8l-2-2z',
  music: 'M12 3v10.55A4 4 0 1014 17V7h4V3h-6z',
  chev: 'M10 6L8.59 7.41 13.17 12l-4.58 4.59L10 18l6-6z',
  chevl: 'M15.41 7.41L14 6l-6 6 6 6 1.41-1.41L10.83 12z',
  sun: 'M12 7a5 5 0 100 10 5 5 0 000-10zM2 13h2v-2H2v2zm18 0h2v-2h-2v2zM11 2v2h2V2h-2zm0 18v2h2v-2h-2zM5.99 4.58L4.58 5.99l1.41 1.41 1.42-1.41-1.42-1.41zm12.02 12.02l-1.41 1.41 1.41 1.42 1.42-1.42-1.42-1.41zM18.01 7.4l1.41-1.41-1.41-1.41-1.42 1.41 1.42 1.41zM7.4 18.01l-1.41 1.41 1.41 1.42 1.42-1.42-1.42-1.41z',
  moon: 'M10 2c-1.82 0-3.53.5-5 1.35C7.99 5.08 10 8.3 10 12s-2.01 6.92-5 8.65C6.47 21.5 8.18 22 10 22c5.52 0 10-4.48 10-10S15.52 2 10 2z',
  auto: 'M12 2a10 10 0 100 20 10 10 0 000-20zm0 18V4a8 8 0 010 16z',
};
const setIcon = (el, name) => el.setAttribute('d', ICON[name]);
$$('path[data-i]').forEach((p) => setIcon(p, p.dataset.i));
setIcon($('#i-search'), 'search');
const svg = (n, extra = '') => `<svg viewBox="0 0 24 24" ${extra}><path d="${ICON[n]}"/></svg>`;

/* ---------------- state ---------------- */
let cfg = Object.assign({ theme: 'system', seek: 10, folders: [], sort: 'new', vol: 1, muted: false, repeat: 0 }, load('cascade.cfg', {}));
let prog = load('cascade.prog', {});
let meta = load('cascade.meta', {});
const S = { files: [], tab: 'videos', folder: null, query: '', view: [], hero: [], heroIdx: 0, folders: [], last: null };
const saveCfg = () => store('cascade.cfg', cfg);
const saveProg = () => store('cascade.prog', prog);
const saveMeta = () => store('cascade.meta', meta);
const dur = (f) => (meta[f.path] && meta[f.path].dur) || 0;

/* ---------------- theme ---------------- */
const mq = matchMedia('(prefers-color-scheme: dark)');
function isDark() { return cfg.theme === 'dark' || (cfg.theme === 'system' && mq.matches); }
function applyTheme() {
  document.documentElement.dataset.theme = isDark() ? 'dark' : 'light';
  setIcon($('#theme-icon'), cfg.theme === 'light' ? 'sun' : cfg.theme === 'dark' ? 'moon' : 'auto');
  if (!document.body.classList.contains('playing')) api.chrome(isDark() ? { color: '#0e0f13', symbol: '#ffffff' } : { color: '#f6f3f0', symbol: '#16171b' });
}
mq.addEventListener('change', applyTheme);
$('#btn-theme').onclick = () => { cfg.theme = { system: 'light', light: 'dark', dark: 'system' }[cfg.theme]; saveCfg(); applyTheme(); toast('Theme: ' + cfg.theme); };

/* ---------------- toast / modal ---------------- */
let toastT;
function toast(msg, label, fn) {
  const t = $('#toast');
  t.innerHTML = `<span>${esc(msg)}</span>${label ? `<button>${esc(label)}</button>` : ''}`;
  if (label) t.querySelector('button').onclick = () => { fn(); t.classList.remove('show'); };
  t.classList.add('show');
  clearTimeout(toastT);
  toastT = setTimeout(() => t.classList.remove('show'), label ? 4500 : 2200);
}
function modal(html, wire) {
  const m = $('#modal'), c = $('#modal-card');
  c.innerHTML = html;
  m.hidden = false;
  const close = () => { m.hidden = true; };
  m.onmousedown = (e) => { if (e.target === m) close(); };
  wire && wire(c, close);
  return close;
}

/* ---------------- thumbnails ---------------- */
const thumbs = new Map();
const failed = new Set();
const pending = [];
let workers = 0;

function showThumb(path, url) {
  $$('img[data-path]').forEach((img) => { if (img.dataset.path === path) { img.src = url; img.onload = () => img.classList.add('in'); } });
  const h = S.hero[S.heroIdx];
  if (h && h.path === path) setBackdrop(url);
}
function showDur(f) {
  $$('[data-dur]').forEach((el) => { if (el.dataset.dur === f.path) el.textContent = fmt(dur(f)); });
}
function wantThumb(f) {
  if (f.isAudio || failed.has(f.path)) return;
  if (thumbs.has(f.path)) { showThumb(f.path, thumbs.get(f.path)); return; }
  if (!pending.includes(f)) pending.push(f);
  pump();
}
async function pump() {
  while (workers < 2 && pending.length) {
    const f = pending.shift();
    workers++;
    makeThumb(f).then((url) => { if (url) { thumbs.set(f.path, url); showThumb(f.path, url); } }).catch(() => failed.add(f.path)).finally(() => { workers--; pump(); });
  }
}
function once(el, ev, ms) {
  return new Promise((res, rej) => {
    const t = setTimeout(() => { el.removeEventListener(ev, h); rej(new Error('timeout')); }, ms);
    const h = () => { clearTimeout(t); res(); };
    el.addEventListener(ev, h, { once: true });
  });
}
async function makeThumb(f) {
  const cached = await api.thumbGet(f);
  if (cached) { if (!meta[f.path]) await probeDuration(f); return cached; }
  const v = document.createElement('video');
  v.crossOrigin = 'anonymous'; v.muted = true; v.preload = 'metadata'; v.src = mediaSrc(f);
  try {
    await once(v, 'loadedmetadata', 10000);
    meta[f.path] = { dur: v.duration || 0 }; saveMeta(); showDur(f);
    v.currentTime = Math.min(6, (v.duration || 10) * 0.1);
    await once(v, 'seeked', 10000);
    const c = document.createElement('canvas');
    const w = 480, h = Math.round((w * v.videoHeight) / Math.max(1, v.videoWidth)) || 270;
    c.width = w; c.height = h;
    c.getContext('2d').drawImage(v, 0, 0, w, h);
    return await api.thumbSave(f, c.toDataURL('image/jpeg', 0.8));
  } finally { v.removeAttribute('src'); v.load(); }
}
async function probeDuration(f) {
  const v = document.createElement('video');
  v.preload = 'metadata'; v.src = mediaSrc(f);
  try { await once(v, 'loadedmetadata', 6000); meta[f.path] = { dur: v.duration || 0 }; saveMeta(); showDur(f); } catch { /* ignore */ } finally { v.removeAttribute('src'); v.load(); }
}

/* ---------------- backdrop ---------------- */
let bdFlip = 0, bdCur = '';
function setBackdrop(url) {
  if (!url || url === bdCur) return;
  bdCur = url;
  const el = $(bdFlip ? '#bd1' : '#bd2'), old = $(bdFlip ? '#bd2' : '#bd1');
  bdFlip ^= 1;
  el.style.backgroundImage = `url("${url}")`;
  el.classList.add('on'); old.classList.remove('on');
}

/* ---------------- library ---------------- */
function sorted(arr) {
  const a = arr.slice();
  if (cfg.sort === 'name') a.sort((x, y) => x.title.localeCompare(y.title, undefined, { numeric: true }));
  else if (cfg.sort === 'dur') a.sort((x, y) => dur(y) - dur(x));
  else if (cfg.sort === 'size') a.sort((x, y) => y.size - x.size);
  else a.sort((x, y) => y.mtime - x.mtime);
  return a;
}
const matches = (f) => !S.query || f.title.toLowerCase().includes(S.query.toLowerCase());

async function rescan() {
  $('#count').textContent = 'Scanning…';
  S.files = await api.scan(cfg.folders);
  renderHome();
}
function computeLast() {
  const byPath = new Map(S.files.map((f) => [f.path, f]));
  let best = null, t = 0;
  for (const [p, v] of Object.entries(prog)) { if (byPath.has(p) && v.t > t) { t = v.t; best = byPath.get(p); } }
  S.last = best;
  $('#fab').hidden = !best || document.body.classList.contains('playing');
}
function heroList(vids) {
  const byPath = new Map(vids.map((f) => [f.path, f]));
  const recent = Object.entries(prog)
    .filter(([p, v]) => byPath.has(p) && v.pos > 5 && (!v.dur || v.pos < v.dur * 0.97))
    .sort((a, b) => b[1].t - a[1].t).map(([p]) => byPath.get(p));
  const out = [];
  for (const f of [...recent, ...vids.slice().sort((a, b) => b.mtime - a.mtime)]) { if (!out.includes(f)) out.push(f); if (out.length >= 6) break; }
  return out;
}
const sect = (t, n, extra = '') => `<div class="sect">${esc(t)} <em>${n}</em>${extra}</div>`;

function cardHtml(f, i, group, pct) {
  return `<div class="card" data-play="${group}:${i}" style="--d:${Math.min(i, 14) * 35}ms">
    <div class="thumb"><img data-path="${esc(f.path)}" alt=""><span class="dur" data-dur="${esc(f.path)}">${dur(f) ? fmt(dur(f)) : '··'}</span>
    <div class="play">${svg('play')}</div>
    <div class="ct"><b>${esc(f.title)}</b><span>${human(f.size)}</span></div>${pct ? `<div class="prog"><i style="width:${pct}%"></i></div>` : ''}</div></div>`;
}
function rowHtml(f, i, group) {
  return `<div class="row" data-play="${group}:${i}" style="--d:${Math.min(i, 14) * 30}ms"><div class="ic aud">${svg('music')}</div>
    <div class="tx"><b>${esc(f.title)}</b><span>${esc(f.folderName)} · ${human(f.size)}</span></div><div class="rb" data-dur="${esc(f.path)}">${dur(f) ? fmt(dur(f)) : ''}</div></div>`;
}

function renderHome() {
  const c = $('#content');
  const nv = S.files.filter((f) => !f.isAudio).length, na = S.files.length - nv;
  $('#count').textContent = `${nv} videos · ${na} audio`;
  let html = '';
  S.hero = [];
  if (S.tab === 'videos') {
    const vids = sorted(S.files.filter((f) => !f.isAudio && matches(f)));
    S.view = vids;
    if (!S.query && vids.length) {
      S.hero = heroList(vids);
      html += `<div class="hero-wrap"><button class="hnav l" data-hnav="-1">${svg('chevl')}</button><button class="hnav r" data-hnav="1">${svg('chev')}</button><div class="hero" id="hero">` +
        S.hero.map((f, i) => {
          const p = prog[f.path], pct = p && p.dur ? Math.round((p.pos / p.dur) * 100) : 0;
          const cont = p && p.pos > 5 && pct < 97;
          return `<div class="hcard" data-play="hero:${i}" style="animation-delay:${i * 70}ms"><img data-path="${esc(f.path)}" alt=""><div class="tag">${cont ? 'Continue watching' : 'Latest'}</div>
            <div class="info"><b>${esc(f.title)}</b><span>${cont ? fmt(p.dur - p.pos) + ' left' : human(f.size)}</span></div><div class="go">${svg('play', 'style="width:30px;height:30px"')}</div>${cont ? `<div class="prog"><i style="width:${pct}%"></i></div>` : ''}</div>`;
        }).join('') + `</div></div><div class="dots" id="dots">${S.hero.map((_, i) => `<i data-dot="${i}"></i>`).join('')}</div>`;
    }
    html += sect('All videos', vids.length) + (vids.length ? `<div class="grid">${vids.map((f, i) => cardHtml(f, i, 'view')).join('')}</div>` : `<div class="empty">No videos yet. Use <b>+</b> to add a folder.</div>`);
  } else if (S.tab === 'folders') {
    if (S.folder == null) {
      const g = new Map();
      for (const f of S.files.filter((x) => !x.isAudio)) { if (!g.has(f.folder)) g.set(f.folder, []); g.get(f.folder).push(f); }
      S.folders = [...g.entries()].map(([p, l]) => ({ path: p, name: l[0].folderName, items: l }))
        .filter((x) => !S.query || x.name.toLowerCase().includes(S.query.toLowerCase())).sort((a, b) => a.name.localeCompare(b.name));
      const now = Date.now();
      html += sect('Folders', S.folders.length, `<button class="link" data-do="manage-folders">Library folders</button>`) +
        (S.folders.length ? `<div class="rows">${S.folders.map((x, i) => {
          const fresh = x.items.filter((f) => now - f.mtime < 3 * 864e5).length;
          return `<div class="row" data-folder="${i}" style="--d:${Math.min(i, 14) * 30}ms"><div class="ic">${svg('folder')}${fresh ? `<span class="badge">${fresh}</span>` : ''}</div>
            <div class="tx"><b>${esc(x.name)}</b><span>${x.items.length} videos · ${esc(x.path)}</span></div></div>`;
        }).join('')}</div>` : `<div class="empty">No folders found.</div>`);
    } else {
      const items = sorted(S.files.filter((f) => !f.isAudio && f.folder === S.folder && matches(f)));
      S.view = items;
      html += `<div class="back"><button class="gbtn" data-do="folder-back">${svg('back')}</button><b>${esc(items[0] ? items[0].folderName : S.folder)}</b></div>` +
        sect('Videos', items.length) + `<div class="grid">${items.map((f, i) => cardHtml(f, i, 'view')).join('')}</div>`;
    }
  } else {
    const aud = sorted(S.files.filter((f) => f.isAudio && matches(f)));
    S.view = aud;
    html += sect('All audio', aud.length) + (aud.length ? `<div class="rows">${aud.map((f, i) => rowHtml(f, i, 'view')).join('')}</div>` : `<div class="empty">No audio found.</div>`);
  }
  c.innerHTML = html;
  $$('img[data-path]', c).forEach((img) => { const f = S.files.find((x) => x.path === img.dataset.path); if (f) wantThumb(f); });
  $$('.row [data-dur]', c).forEach((el) => { const f = S.files.find((x) => x.path === el.dataset.dur); if (f && !dur(f)) probeDuration(f); });
  wireHero();
  computeLast();
}

function wireHero() {
  const hero = $('#hero');
  if (!hero) return;
  const cards = $$('.hcard', hero);
  const upd = () => {
    const mid = hero.scrollLeft + hero.clientWidth / 2;
    let best = 0, bd = 1e9;
    cards.forEach((c, i) => { const d = Math.abs(c.offsetLeft + c.offsetWidth / 2 - mid); if (d < bd) { bd = d; best = i; } });
    if (hero.scrollLeft < 8) best = 0;
    S.heroIdx = best;
    $$('#dots i').forEach((d, i) => d.classList.toggle('on', i === best));
    const f = S.hero[best];
    if (f && thumbs.has(f.path)) setBackdrop(thumbs.get(f.path));
  };
  let t;
  hero.addEventListener('scroll', () => { clearTimeout(t); t = setTimeout(upd, 40); });
  S.heroIdx = 0; upd();
  $$('#dots i').forEach((d) => d.onclick = () => cards[+d.dataset.dot].scrollIntoView({ behavior: 'smooth', inline: 'start', block: 'nearest' }));
  $$('.hnav').forEach((b) => b.onclick = (e) => { e.stopPropagation(); hero.scrollBy({ left: +b.dataset.hnav * (cards[0].offsetWidth + 18), behavior: 'smooth' }); });
}

/* ---------------- tabs (elastic stretch indicator) ---------------- */
let tabIdx = 0;
function moveInd(idx, animate = true) {
  const btns = $$('#tabs button'), b = btns[idx], ind = $('#ind'), box = $('#tabs');
  const L = b.offsetLeft, R = box.clientWidth - (b.offsetLeft + b.offsetWidth);
  const right = idx > tabIdx;
  const fast = '.28s cubic-bezier(.3,1.5,.5,1)', slow = '.62s cubic-bezier(.3,1.35,.5,1)';
  ind.style.transition = animate ? `left ${right ? slow : fast}, right ${right ? fast : slow}` : 'none';
  ind.style.left = L + 'px'; ind.style.right = R + 'px';
  tabIdx = idx;
}
$$('#tabs button').forEach((b, i) => b.onclick = () => {
  S.tab = b.dataset.tab; S.folder = null;
  $$('#tabs button').forEach((x) => x.classList.toggle('on', x === b));
  moveInd(i);
  renderHome();
  $('#home').scrollTo({ top: 0 });
});

/* ---------------- menus ---------------- */
function closeMenus() { $$('.menu').forEach((m) => m.classList.remove('open')); }
[['#btn-add', '#menu-add'], ['#btn-sort', '#menu-sort']].forEach(([b, m]) => $(b).onclick = (e) => { e.stopPropagation(); const o = $(m).classList.contains('open'); closeMenus(); if (!o) $(m).classList.add('open'); });
document.addEventListener('click', closeMenus);
function markSort() { $$('#menu-sort button').forEach((b) => b.classList.toggle('sel', b.dataset.sort === cfg.sort)); }
$$('#menu-sort button').forEach((b) => b.onclick = () => { cfg.sort = b.dataset.sort; saveCfg(); markSort(); renderHome(); });
$('#menu-add').addEventListener('click', (e) => { const d = e.target.dataset.do; if (d) actions[d](); });

let qT;
$('#q').addEventListener('input', (e) => { clearTimeout(qT); qT = setTimeout(() => { S.query = e.target.value.trim(); renderHome(); }, 120); });

const actions = {
  async 'add-folder'() { const p = await api.pickFolder(); if (p && !cfg.folders.includes(p)) { cfg.folders.push(p); saveCfg(); await rescan(); toast('Folder added'); } },
  async 'open-files'() { const f = await api.pickFiles(); if (f.length) openPlayer(f, 0); },
  'open-url'() {
    modal(`<h3>Play network URL</h3><input id="url" placeholder="https://example.com/video.mp4"><div class="mact"><button class="btn" data-x>Cancel</button><button class="btn pri" data-ok>Play</button></div>`, (c, close) => {
      const inp = $('#url', c); inp.focus();
      const go = () => { const u = inp.value.trim(); if (u) { close(); openPlayer([{ path: u, title: u.split('/').pop() || u, folderName: 'Network', size: 0, mtime: 0, isAudio: /\.(mp3|m4a|aac|flac|wav|ogg|opus)(\?|$)/i.test(u) }], 0); } };
      $('[data-x]', c).onclick = close; $('[data-ok]', c).onclick = go; inp.onkeydown = (e) => { if (e.key === 'Enter') go(); };
    });
  },
  'manage-folders'() {
    const draw = (c, close) => {
      c.innerHTML = `<h3>Library folders</h3>${cfg.folders.length ? cfg.folders.map((f, i) => `<div class="mrow"><span>${esc(f)}</span><button class="btn" data-rm="${i}">Remove</button></div>`).join('') : '<div class="empty" style="padding:24px">No folders yet.</div>'}
        <div class="mact"><button class="btn" data-add>Add folder…</button><button class="btn pri" data-x>Done</button></div>`;
      $$('[data-rm]', c).forEach((b) => b.onclick = async () => { cfg.folders.splice(+b.dataset.rm, 1); saveCfg(); draw(c, close); await rescan(); });
      $('[data-add]', c).onclick = async () => { const p = await api.pickFolder(); if (p && !cfg.folders.includes(p)) { cfg.folders.push(p); saveCfg(); draw(c, close); await rescan(); } };
      $('[data-x]', c).onclick = close;
    };
    modal('', (c, close) => draw(c, close));
  },
  'folder-back'() { S.folder = null; renderHome(); },
};

/* click handling on home */
$('#content').addEventListener('click', (e) => {
  const d = e.target.closest('[data-do]'); if (d) { actions[d.dataset.do](); return; }
  const fo = e.target.closest('[data-folder]');
  if (fo) { S.folder = S.folders[+fo.dataset.folder].path; renderHome(); $('#home').scrollTo({ top: 0 }); return; }
  const p = e.target.closest('[data-play]');
  if (p) { const [g, i] = p.dataset.play.split(':'); openPlayer(g === 'hero' ? S.hero : S.view, +i); }
});
$('#fab').onclick = () => {
  const f = S.last; if (!f) return;
  const list = sorted(S.files.filter((x) => x.isAudio === f.isAudio));
  openPlayer(list, Math.max(0, list.indexOf(f)));
};

/* drag & drop */
addEventListener('dragover', (e) => e.preventDefault());
addEventListener('drop', async (e) => {
  e.preventDefault();
  const paths = [...e.dataTransfer.files].map((f) => api.pathForFile(f)).filter(Boolean);
  if (!paths.length) return;
  const { media, dirs } = await api.classify(paths);
  if (dirs.length) { dirs.forEach((d) => { if (!cfg.folders.includes(d)) cfg.folders.push(d); }); saveCfg(); await rescan(); toast('Folder added to library'); }
  if (media.length) openPlayer(media, 0);
});

/* =====================================================================
   PLAYER
   ===================================================================== */
const V = $('#video'), PL = $('#player');
const P = { q: [], i: 0, f: null, userAudio: false, resume: 0, rate: 1, fit: 0, sleepT: null, boost: false, panel: '' };
let uiT, lastMove = 0, lastSave = 0;

function openPlayer(list, i) {
  if (!list || !list[i]) return;
  P.q = list; P.i = i; P.userAudio = false;
  document.body.classList.add('playing');
  PL.hidden = false;
  api.chrome({ color: '#000000', symbol: '#ffffff' });
  loadItem();
  showUI();
}
function closePlayer() {
  saveNow();
  V.pause(); V.removeAttribute('src'); V.load();
  clearTrackEls();
  document.body.classList.remove('playing', 'hidecursor');
  PL.hidden = true; PL.classList.remove('panel-open');
  if (document.fullscreenElement) document.exitFullscreen();
  api.progress(-1);
  applyTheme();
  renderHome();
}
function loadItem() {
  const f = P.f = P.q[P.i];
  if (isUrl(f.path)) V.removeAttribute('crossorigin'); else V.crossOrigin = 'anonymous';
  V.src = mediaSrc(f);
  V.defaultPlaybackRate = P.rate; V.playbackRate = P.rate;
  const pr = prog[f.path];
  P.resume = pr && pr.pos > 5 && (!pr.dur || pr.pos < pr.dur * 0.97) ? pr.pos : 0;
  setAudioMode(f.isAudio || P.userAudio);
  $('#ptitle').textContent = $('#atitle').textContent = f.title;
  $('#psub').textContent = '';
  clearTrackEls();
  V.play().catch(() => {});
  loadSiblingSubs(f);
  updateSub();
  setupSession(f);
  $('#p-prev').style.display = $('#p-next').style.display = P.q.length > 1 ? '' : 'none';
}
function setAudioMode(on) {
  PL.classList.toggle('audio-mode', on);
  document.body.classList.toggle('playing-audio', on && !V.paused);
  $('#p-audio').classList.toggle('on', on);
  if (on) showUI();
}
function updateSub() {
  const parts = [];
  if (P.q.length > 1) parts.push(`${P.i + 1} of ${P.q.length}`);
  if (V.videoHeight && !PL.classList.contains('audio-mode')) parts.push(V.videoHeight + 'p');
  const s = sysText(); if (s) parts.push(s);
  $('#psub').textContent = parts.join('  ·  ');
}
function saveNow() {
  const f = P.f; if (!f || !isFinite(V.duration) || !V.duration) return;
  prog[f.path] = { pos: V.ended ? 0 : V.currentTime, dur: V.duration, t: Date.now() };
  saveProg();
}
function go(d) {
  const n = P.i + d;
  if (n < 0 || n >= P.q.length) { if (d > 0 && cfg.repeat === 2) { saveNow(); P.i = 0; loadItem(); } return; }
  saveNow(); P.i = n; loadItem();
}

/* ---- video events ---- */
V.addEventListener('loadedmetadata', () => {
  const f = P.f;
  meta[f.path] = { dur: V.duration || 0 }; saveMeta();
  if (P.resume) { V.currentTime = P.resume; toast('Resumed from ' + fmt(P.resume)); P.resume = 0; }
  updateSub(); fillTracks();
});
V.addEventListener('play', () => { setPlayIcon(); document.body.classList.toggle('playing-audio', PL.classList.contains('audio-mode')); showUI(); });
V.addEventListener('pause', () => { setPlayIcon(); document.body.classList.remove('playing-audio'); showUI(); saveNow(); });
V.addEventListener('waiting', () => $('#p-play').classList.add('buf'));
V.addEventListener('playing', () => $('#p-play').classList.remove('buf'));
V.addEventListener('ended', () => { saveNow(); if (cfg.repeat === 1) { V.currentTime = 0; V.play(); } else if (P.i < P.q.length - 1 || cfg.repeat === 2) go(1); else showUI(); });
V.addEventListener('ratechange', () => { $('#speed-l').textContent = rateLabel(V.playbackRate); $('#p-speed').classList.toggle('fast', Math.abs(V.playbackRate - 1) > 0.01); });
V.addEventListener('volumechange', () => { setVolIcon(); $('#vol').value = V.muted ? 0 : V.volume; cfg.vol = V.volume; cfg.muted = V.muted; saveCfg(); });
V.addEventListener('error', () => {
  const f = P.f; if (!f) return;
  modal(`<h3>Can't play this file</h3><p style="color:var(--mut);line-height:1.5">The built-in player doesn't support this file's video or audio codec (for example some AVI/WMV files or AC3/DTS audio).</p>
    <div class="mact"><button class="btn" data-x>Close</button>${isUrl(f.path) ? '' : '<button class="btn pri" data-open>Open with default app</button>'}</div>`, (c, close) => {
    $('[data-x]', c).onclick = close;
    const o = $('[data-open]', c); if (o) o.onclick = () => { api.openDefault(f.path); close(); };
  });
});
V.addEventListener('timeupdate', () => {
  const d = V.duration || 0, t = V.currentTime || 0;
  $('#seek').style.setProperty('--p', (d ? (t / d) * 100 : 0) + '%');
  $('#tcur').textContent = fmt(t); $('#trem').textContent = '-' + fmt(d - t);
  if (V.buffered.length) { let e = 0; for (let i = 0; i < V.buffered.length; i++) if (V.buffered.start(i) <= t + 0.5) e = V.buffered.end(i); $('#seek').style.setProperty('--b', (d ? (e / d) * 100 : 0) + '%'); }
  const now = Date.now();
  if (now - lastSave > 3000 && !V.paused) { lastSave = now; saveNow(); api.progress(d ? t / d : -1); }
  if ('mediaSession' in navigator && d && isFinite(d) && now % 4 === 0) { try { navigator.mediaSession.setPositionState({ duration: d, position: Math.min(t, d), playbackRate: V.playbackRate }); } catch { /* ignore */ } }
});
function setPlayIcon() { setIcon($('#play-icon'), V.paused ? 'play' : 'pause'); }
function setVolIcon() { setIcon($('#vol-icon'), V.muted || V.volume === 0 ? 'mute' : 'vol'); }
const rateLabel = (r) => (+r.toFixed(2)) + 'x';
setPlayIcon(); setVolIcon(); setIcon($('#full-icon'), 'full');
V.volume = cfg.vol; V.muted = cfg.muted; $('#vol').value = cfg.muted ? 0 : cfg.vol;
$('#vol').oninput = (e) => { V.muted = false; V.volume = +e.target.value; };

/* ---- seeking ---- */
const toggle = () => { if (V.paused) { if (V.ended) V.currentTime = 0; V.play(); } else V.pause(); };
function seekBy(s) { V.currentTime = Math.max(0, Math.min((V.duration || 1e9), V.currentTime + s)); }
function ripple(fwd, sec) {
  const r = $(fwd ? '#rip-r' : '#rip-l');
  r.querySelector('b').textContent = (fwd ? '+' : '-') + sec + ' s';
  r.classList.remove('show'); void r.offsetWidth; r.classList.add('show');
}
function skip(fwd) {
  seekBy(fwd ? cfg.seek : -cfg.seek);
  ripple(fwd, cfg.seek);
}
const seek = $('#seek');
const fracAt = (e) => { const r = seek.getBoundingClientRect(); return Math.max(0, Math.min(1, (e.clientX - r.left) / r.width)); };
seek.addEventListener('pointermove', (e) => { const f = fracAt(e); $('#chip').style.left = f * 100 + '%'; $('#chip').textContent = fmt(f * (V.duration || 0)); if (seek.classList.contains('drag')) { seek.style.setProperty('--p', f * 100 + '%'); } });
seek.addEventListener('pointerdown', (e) => { seek.setPointerCapture(e.pointerId); seek.classList.add('drag'); seek.dataset.f = fracAt(e); seek.style.setProperty('--p', fracAt(e) * 100 + '%'); });
seek.addEventListener('pointermove', (e) => { if (seek.classList.contains('drag')) seek.dataset.f = fracAt(e); });
seek.addEventListener('pointerup', (e) => { if (!seek.classList.contains('drag')) return; seek.classList.remove('drag'); V.currentTime = fracAt(e) * (V.duration || 0); });

/* ---- buttons ---- */
$('#p-back').onclick = closePlayer;
$('#p-play').onclick = toggle;
$('#p-rew').onclick = () => { const b = $('#p-rew'); b.classList.remove('spin'); void b.offsetWidth; b.classList.add('spin'); skip(false); };
$('#p-fwd').onclick = () => { const b = $('#p-fwd'); b.classList.remove('spin'); void b.offsetWidth; b.classList.add('spin'); skip(true); };
$('#p-prev').onclick = () => go(-1);
$('#p-next').onclick = () => go(1);
$('#p-vol').onclick = () => { V.muted = !V.muted; };
$('#p-full').onclick = toggleFull;
$('#p-pip').onclick = () => { if (document.pictureInPictureElement) document.exitPictureInPicture(); else V.requestPictureInPicture().catch(() => toast('Picture-in-picture not available')); };
$('#p-audio').onclick = () => { P.userAudio = !PL.classList.contains('audio-mode'); setAudioMode(P.f.isAudio || P.userAudio); toast(P.userAudio ? 'Playing as audio' : 'Video on'); };
$('#p-aspect').onclick = cycleFit;
$('#p-shot').onclick = screenshot;
function toggleFull() { if (document.fullscreenElement) document.exitFullscreen(); else PL.requestFullscreen(); }
document.addEventListener('fullscreenchange', () => setIcon($('#full-icon'), document.fullscreenElement ? 'fullx' : 'full'));
function cycleFit() { P.fit = (P.fit + 1) % 3; V.style.objectFit = ['contain', 'cover', 'fill'][P.fit]; toast(['Fit', 'Zoom (fill screen)', 'Stretch'][P.fit]); }
function gauge(label, v) { const g = $('#gauge'); g.innerHTML = `<span>${label}</span><i style="--g:${Math.round(v * 100)}%"></i><span>${Math.round(v * 100)}%</span>`; g.classList.add('show'); clearTimeout(gauge.t); gauge.t = setTimeout(() => g.classList.remove('show'), 900); }

async function screenshot() {
  let p;
  try {
    const c = document.createElement('canvas'); c.width = V.videoWidth; c.height = V.videoHeight;
    c.getContext('2d').drawImage(V, 0, 0);
    p = await api.saveShot(c.toDataURL('image/png'));
  } catch {
    const r = V.getBoundingClientRect();
    PL.classList.remove('ui'); await sleep(200);
    p = await api.shotRect({ x: r.x, y: r.y, width: r.width, height: r.height });
  }
  toast('Screenshot saved to Pictures\\Cascade', 'Show', () => api.reveal(p));
}

/* ---- stage gestures ---- */
let clickT = 0, pressT = 0, suppress = false;
const stage = $('#stage');
stage.addEventListener('mousedown', () => {
  suppress = false;
  pressT = setTimeout(() => { if (!V.paused) { P.boost = true; suppress = true; P.prevRate = V.playbackRate; V.playbackRate = 2; $('#boost').classList.add('show'); } }, 450);
});
addEventListener('mouseup', () => { clearTimeout(pressT); if (P.boost) { P.boost = false; V.playbackRate = P.prevRate; $('#boost').classList.remove('show'); } });
stage.addEventListener('click', () => { if (suppress) { suppress = false; return; } clearTimeout(clickT); clickT = setTimeout(toggle, 230); });
stage.addEventListener('dblclick', (e) => {
  clearTimeout(clickT);
  const x = e.clientX / innerWidth;
  if (x < 0.34) skip(false); else if (x > 0.66) skip(true); else toggleFull();
});
stage.addEventListener('wheel', (e) => { e.preventDefault(); V.muted = false; V.volume = Math.max(0, Math.min(1, V.volume + (e.deltaY < 0 ? 0.05 : -0.05))); gauge('Volume', V.volume); }, { passive: false });
PL.addEventListener('mousemove', () => { const n = Date.now(); if (n - lastMove > 60) { lastMove = n; showUI(); } });

/* ---- controls visibility ---- */
function showUI() {
  PL.classList.add('ui'); document.body.classList.remove('hidecursor');
  clearTimeout(uiT);
  if (!V.paused && !P.panel && !PL.classList.contains('audio-mode')) uiT = setTimeout(() => { PL.classList.remove('ui'); document.body.classList.add('hidecursor'); }, 2800);
}
$$('.ctl').forEach((el) => el.addEventListener('mouseenter', () => clearTimeout(uiT)));

/* ---- side panel ---- */
function openPanel(kind) {
  P.panel = kind;
  const body = $('#panel-body'); body.className = 'pbody';
  const chips = (arr, cur, fn) => { const d = document.createElement('div'); d.className = 'chips'; arr.forEach(([l, v]) => { const b = document.createElement('button'); b.className = 'ochip' + (v === cur ? ' on' : ''); b.textContent = l; b.onclick = () => { fn(v); $$('.ochip', d).forEach((x) => x.classList.remove('on')); b.classList.add('on'); }; d.appendChild(b); }); return d; };
  const lbl = (t) => { const d = document.createElement('div'); d.className = 'lbl'; d.textContent = t; return d; };
  body.innerHTML = '';
  if (kind === 'speed') {
    $('#panel-title').textContent = 'Playback speed';
    const big = document.createElement('div'); big.className = 'bigv'; big.textContent = rateLabel(V.playbackRate);
    const r = document.createElement('input'); r.type = 'range'; r.min = 0.25; r.max = 4; r.step = 0.05; r.value = V.playbackRate;
    r.oninput = () => { P.rate = +r.value; V.playbackRate = P.rate; big.textContent = rateLabel(P.rate); };
    body.append(big, r, chips([0.5, 0.75, 1, 1.25, 1.5, 2, 3, 4].map((x) => [x + 'x', x]), V.playbackRate, (v) => { P.rate = v; V.playbackRate = v; big.textContent = rateLabel(v); r.value = v; }));
  } else if (kind === 'subs') {
    $('#panel-title').textContent = 'Subtitles';
    const tt = [...V.textTracks];
    const row = (label, on, fn) => { const b = document.createElement('button'); b.className = 'trow' + (on ? ' on' : ''); b.innerHTML = `<span>${esc(label)}</span>${on ? '<em>✓</em>' : ''}`; b.onclick = fn; return b; };
    body.append(row('Off', tt.every((t) => t.mode !== 'showing'), () => { tt.forEach((t) => t.mode = 'disabled'); closePanel(); }));
    tt.forEach((t, i) => body.append(row(t.label || t.language || 'Track ' + (i + 1), t.mode === 'showing', () => { tt.forEach((x) => x.mode = 'disabled'); t.mode = 'showing'; closePanel(); })));
    body.append(row('Load subtitle file…  (.srt / .vtt)', false, async () => { closePanel(); const p = await api.pickSubtitle(); if (p) { await addSubtitle(p, true); toast('Subtitle loaded'); } }));
  } else if (kind === 'tracks') {
    $('#panel-title').textContent = 'Audio track';
    const at = V.audioTracks ? [...V.audioTracks] : [];
    if (at.length < 2) { const d = document.createElement('div'); d.style.color = 'rgba(255,255,255,.6)'; d.textContent = at.length ? 'This file has a single audio track.' : 'Track switching is not available for this file.'; body.append(d); }
    at.forEach((t, i) => { const b = document.createElement('button'); b.className = 'trow' + (t.enabled ? ' on' : ''); b.innerHTML = `<span>${esc(t.label || t.language || 'Track ' + (i + 1))}</span>${t.enabled ? '<em>✓</em>' : ''}`; b.onclick = () => { at.forEach((x) => x.enabled = false); t.enabled = true; closePanel(); }; body.append(b); });
  } else {
    $('#panel-title').textContent = 'More';
    body.append(lbl('Repeat'), chips([['Off', 0], ['One', 1], ['All', 2]], cfg.repeat, (v) => { cfg.repeat = v; saveCfg(); }));
    body.append(lbl('Sleep timer'), chips([['Off', 0], ['15 min', 15], ['30 min', 30], ['45 min', 45], ['60 min', 60]], P.sleep || 0, (v) => { clearTimeout(P.sleepT); P.sleep = v; if (v) { P.sleepT = setTimeout(() => { V.pause(); P.sleep = 0; toast('Sleep timer: paused'); }, v * 60000); toast('Pausing in ' + v + ' min'); } }));
    body.append(lbl('Double-click / skip step'), chips([['5 s', 5], ['10 s', 10], ['15 s', 15], ['30 s', 30]], cfg.seek, (v) => { cfg.seek = v; saveCfg(); setSkipLabels(); }));
    if (!isUrl(P.f.path)) { const b = document.createElement('button'); b.className = 'trow'; b.innerHTML = '<span>Show in Explorer</span>'; b.onclick = () => api.reveal(P.f.path); body.append(lbl('File'), b); }
    const k = document.createElement('div'); k.className = 'kbd';
    k.innerHTML = [['Space', 'Play / pause'], ['← →', 'Skip (Shift = 30 s)'], ['↑ ↓', 'Volume'], ['M', 'Mute'], ['F', 'Fullscreen'], ['N / P', 'Next / previous'], ['S', 'Screenshot'], ['A', 'Aspect ratio'], ['C', 'Subtitles'], ['[ ]', 'Speed − / +'], ['Esc', 'Close panel / player']].map(([a, b]) => `<kbd>${a}</kbd><span>${b}</span>`).join('');
    body.append(lbl('Keyboard shortcuts'), k);
  }
  PL.classList.add('panel-open'); showUI(); clearTimeout(uiT);
}
function closePanel() { P.panel = ''; PL.classList.remove('panel-open'); showUI(); }
$('#panel-x').onclick = closePanel; $('#panel-scrim').onclick = closePanel;
$('#p-speed').onclick = () => openPanel('speed');
$('#p-subs').onclick = () => openPanel('subs');
$('#p-tracks').onclick = () => openPanel('tracks');
$('#p-more').onclick = () => openPanel('more');
function setSkipLabels() { $('#p-rew b').textContent = $('#p-fwd b').textContent = cfg.seek; }
setSkipLabels();
function fillTracks() { /* audio track list is read on demand when the panel opens */ }

/* ---- subtitles ---- */
function clearTrackEls() { $$('track', V).forEach((t) => { URL.revokeObjectURL(t.src); t.remove(); }); }
function srtToVtt(s) {
  return 'WEBVTT\n\n' + s.replace(/\r+/g, '').replace(/(\d+:\d+:\d+),(\d+)/g, '$1.$2').trim();
}
async function addSubtitle(p, show) {
  let txt = await api.readText(p); if (!txt) return;
  if (!/^WEBVTT/.test(txt)) txt = srtToVtt(txt);
  const url = URL.createObjectURL(new Blob([txt], { type: 'text/vtt' }));
  const t = document.createElement('track');
  t.kind = 'subtitles'; t.label = p.split(/[\\/]/).pop(); t.src = url; t.srclang = 'und';
  V.appendChild(t);
  if (show) { await sleep(60); [...V.textTracks].forEach((x) => x.mode = 'disabled'); t.track.mode = 'showing'; }
}
async function loadSiblingSubs(f) {
  if (isUrl(f.path)) return;
  const subs = await api.siblingSubs(f.path);
  for (let i = 0; i < subs.length; i++) await addSubtitle(subs[i], i === 0);
}

/* ---- clock + battery ---- */
let batt = null;
if (navigator.getBattery) navigator.getBattery().then((b) => { batt = b; ['levelchange', 'chargingchange'].forEach((e) => b.addEventListener(e, tickSys)); tickSys(); });
function sysText() {
  const t = new Date().toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' }).toLowerCase();
  const b = batt && !(batt.level >= 1 && batt.charging) ? (batt.charging ? '⚡' : '') + Math.round(batt.level * 100) + '%  ' : '';
  return b + t;
}
function tickSys() { $('#sysmini').textContent = sysText(); if (P.f) updateSub(); }
setInterval(tickSys, 10000); tickSys();

/* ---- Windows media keys / overlay ---- */
function setupSession(f) {
  if (!('mediaSession' in navigator)) return;
  const th = thumbs.get(f.path);
  navigator.mediaSession.metadata = new MediaMetadata({ title: f.title, artist: 'Cascade', album: f.folderName || '' });
  if (th) fetch(th).then((r) => r.blob()).then((b) => {
    if (P.f !== f) return;
    navigator.mediaSession.metadata = new MediaMetadata({ title: f.title, artist: 'Cascade', album: f.folderName || '', artwork: [{ src: URL.createObjectURL(b), sizes: '480x270', type: 'image/jpeg' }] });
  }).catch(() => {});
  const set = (a, fn) => { try { navigator.mediaSession.setActionHandler(a, fn); } catch { /* unsupported */ } };
  set('play', () => V.play()); set('pause', () => V.pause());
  set('previoustrack', () => go(-1)); set('nexttrack', () => go(1));
  set('seekbackward', () => skip(false)); set('seekforward', () => skip(true));
  set('seekto', (d) => { V.currentTime = d.seekTime; });
}

/* ---- keyboard ---- */
addEventListener('keydown', (e) => {
  if (!document.body.classList.contains('playing')) {
    if (e.key === '/' && document.activeElement !== $('#q')) { e.preventDefault(); $('#q').focus(); }
    return;
  }
  if (e.target.tagName === 'INPUT' && e.target.type === 'text') return;
  const k = e.key;
  const used = () => e.preventDefault();
  if (k === ' ' || k === 'k') { used(); toggle(); }
  else if (k === 'ArrowLeft') { used(); seekBy(e.shiftKey ? -30 : -cfg.seek); ripple(false, e.shiftKey ? 30 : cfg.seek); }
  else if (k === 'ArrowRight') { used(); seekBy(e.shiftKey ? 30 : cfg.seek); ripple(true, e.shiftKey ? 30 : cfg.seek); }
  else if (k === 'ArrowUp') { used(); V.muted = false; V.volume = Math.min(1, V.volume + 0.05); gauge('Volume', V.volume); }
  else if (k === 'ArrowDown') { used(); V.volume = Math.max(0, V.volume - 0.05); gauge('Volume', V.volume); }
  else if (k === 'm') V.muted = !V.muted;
  else if (k === 'f') toggleFull();
  else if (k === 'n') go(1);
  else if (k === 'p') go(-1);
  else if (k === 's') screenshot();
  else if (k === 'a') cycleFit();
  else if (k === 'c') { const tt = [...V.textTracks]; if (tt.length) { const i = tt.findIndex((t) => t.mode === 'showing'); tt.forEach((t) => t.mode = 'disabled'); if (i + 1 < tt.length) tt[i + 1].mode = 'showing'; toast(i + 1 < tt.length ? 'Subtitles: ' + (tt[i + 1].label || 'on') : 'Subtitles off'); } }
  else if (k === '[' || k === ']') { P.rate = Math.max(0.25, Math.min(4, V.playbackRate + (k === ']' ? 0.1 : -0.1))); V.playbackRate = P.rate; toast('Speed ' + rateLabel(P.rate)); }
  else if (k === 'Escape') { if (P.panel) closePanel(); else if (document.fullscreenElement) document.exitFullscreen(); else closePlayer(); }
});

/* ---------------- boot ---------------- */
function openExternal(entries) { if (entries && entries.length) openPlayer(entries, 0); }
(async function boot() {
  applyTheme(); markSort();
  requestAnimationFrame(() => moveInd(0, false));
  if (!cfg.folders.length) { cfg.folders = await api.defaultFolders(); saveCfg(); }
  api.onOpenFiles(openExternal);
  const initial = await api.initialFiles();
  await rescan();
  openExternal(initial);
})();
