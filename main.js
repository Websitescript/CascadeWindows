const { app, BrowserWindow, ipcMain, dialog, protocol, net, shell, Menu } = require('electron');
const path = require('path');
const fs = require('fs');
const fsp = fs.promises;
const crypto = require('crypto');
const { pathToFileURL } = require('url');

protocol.registerSchemesAsPrivileged([
  { scheme: 'cascade', privileges: { standard: true, secure: true, stream: true, supportFetchAPI: true, corsEnabled: true, bypassCSP: true } },
]);

app.commandLine.appendSwitch('autoplay-policy', 'no-user-gesture-required');
app.commandLine.appendSwitch('enable-features', 'PlatformHEVCDecoderSupport');
app.commandLine.appendSwitch('enable-blink-features', 'AudioVideoTracks');

const VIDEO = new Set(['mp4', 'mkv', 'avi', 'mov', 'wmv', 'flv', 'webm', 'm4v', 'mpg', 'mpeg', 'ts', 'm2ts', '3gp', 'ogv', 'vob']);
const AUDIO = new Set(['mp3', 'm4a', 'aac', 'flac', 'wav', 'ogg', 'opus', 'wma', 'amr', 'mka']);
const extOf = (p) => path.extname(p).slice(1).toLowerCase();

let win = null;
let thumbDir = '';

if (!app.requestSingleInstanceLock()) {
  app.quit();
}

function entryFor(full, st) {
  const ext = extOf(full);
  const name = path.basename(full);
  const dir = path.dirname(full);
  return {
    path: full,
    name,
    title: name.slice(0, name.length - (ext ? ext.length + 1 : 0)),
    folder: dir,
    folderName: path.basename(dir) || dir,
    size: st.size,
    mtime: st.mtimeMs,
    isAudio: AUDIO.has(ext),
  };
}

async function walk(dir, out, depth) {
  if (depth > 8 || out.size > 20000) return;
  let ents;
  try { ents = await fsp.readdir(dir, { withFileTypes: true }); } catch { return; }
  for (const e of ents) {
    if (e.name.startsWith('.') || e.name.startsWith('$') || e.name === 'System Volume Information' || e.name === 'node_modules') continue;
    const full = path.join(dir, e.name);
    if (e.isDirectory()) { await walk(full, out, depth + 1); continue; }
    const ext = extOf(e.name);
    if (!VIDEO.has(ext) && !AUDIO.has(ext)) continue;
    try { out.set(full, entryFor(full, await fsp.stat(full))); } catch { /* ignore */ }
  }
}

async function toEntries(paths) {
  const res = [];
  for (const p of paths) {
    try {
      const st = await fsp.stat(p);
      if (st.isFile()) res.push(entryFor(p, st));
    } catch { /* ignore */ }
  }
  return res;
}

function mediaFromArgv(argv) {
  return argv.slice(1).filter((a) => !a.startsWith('-') && (VIDEO.has(extOf(a)) || AUDIO.has(extOf(a))) && fs.existsSync(a));
}

function createWindow() {
  win = new BrowserWindow({
    width: 1280,
    height: 800,
    minWidth: 900,
    minHeight: 600,
    show: false,
    backgroundColor: '#0e0f13',
    icon: path.join(__dirname, 'build', 'icon.png'),
    title: 'Cascade',
    titleBarStyle: 'hidden',
    titleBarOverlay: { color: '#0e0f13', symbolColor: '#ffffff', height: 60 },
    webPreferences: { preload: path.join(__dirname, 'preload.js'), contextIsolation: true },
  });
  win.once('ready-to-show', () => win.show());
  win.loadFile(path.join(__dirname, 'src', 'index.html'));
}

app.whenReady().then(() => {
  Menu.setApplicationMenu(null);
  thumbDir = path.join(app.getPath('userData'), 'thumbs');
  fs.mkdirSync(thumbDir, { recursive: true });

  protocol.handle('cascade', async (req) => {
    const u = new URL(req.url);
    let file;
    if (u.hostname === 'media') file = decodeURIComponent(u.pathname.slice(1));
    else if (u.hostname === 'thumb') file = path.join(thumbDir, path.basename(decodeURIComponent(u.pathname.slice(1))));
    else return new Response('Not found', { status: 404 });
    try {
      const r = await net.fetch(pathToFileURL(file).toString(), { headers: req.headers });
      const h = new Headers(r.headers);
      h.set('Access-Control-Allow-Origin', '*');
      return new Response(r.body, { status: r.status, statusText: r.statusText, headers: h });
    } catch {
      return new Response('Not found', { status: 404 });
    }
  });

  createWindow();

  ipcMain.handle('default-folders', () => [app.getPath('videos'), app.getPath('music')].filter((p) => fs.existsSync(p)));
  ipcMain.handle('scan', async (_e, folders) => {
    const out = new Map();
    for (const f of folders) await walk(f, out, 0);
    return [...out.values()];
  });
  ipcMain.handle('pick-folder', async () => {
    const r = await dialog.showOpenDialog(win, { properties: ['openDirectory'] });
    return r.canceled ? null : r.filePaths[0];
  });
  ipcMain.handle('pick-files', async () => {
    const r = await dialog.showOpenDialog(win, {
      properties: ['openFile', 'multiSelections'],
      filters: [{ name: 'Media', extensions: [...VIDEO, ...AUDIO] }, { name: 'All files', extensions: ['*'] }],
    });
    return r.canceled ? [] : toEntries(r.filePaths);
  });
  ipcMain.handle('pick-subtitle', async () => {
    const r = await dialog.showOpenDialog(win, { properties: ['openFile'], filters: [{ name: 'Subtitles', extensions: ['srt', 'vtt'] }] });
    return r.canceled ? null : r.filePaths[0];
  });
  ipcMain.handle('read-text', async (_e, p) => {
    try { return (await fsp.readFile(p, 'utf8')).replace(/^\uFEFF/, ''); } catch { return ''; }
  });
  ipcMain.handle('sibling-subs', async (_e, p) => {
    const dir = path.dirname(p);
    const base = path.basename(p, path.extname(p)).toLowerCase();
    try {
      return (await fsp.readdir(dir))
        .filter((n) => ['.srt', '.vtt'].includes(path.extname(n).toLowerCase()) && n.toLowerCase().startsWith(base))
        .map((n) => path.join(dir, n));
    } catch { return []; }
  });
  ipcMain.handle('classify', async (_e, paths) => {
    const media = [];
    const dirs = [];
    for (const p of paths) {
      try {
        const st = await fsp.stat(p);
        if (st.isDirectory()) dirs.push(p);
        else if (VIDEO.has(extOf(p)) || AUDIO.has(extOf(p))) media.push(entryFor(p, st));
      } catch { /* ignore */ }
    }
    return { media, dirs };
  });
  ipcMain.handle('initial-files', () => toEntries(mediaFromArgv(process.argv)));

  const thumbKey = (f) => crypto.createHash('sha1').update(`${f.path}|${f.size}|${f.mtime}`).digest('hex').slice(0, 24) + '.jpg';
  ipcMain.handle('thumb-get', (_e, f) => {
    const k = thumbKey(f);
    return fs.existsSync(path.join(thumbDir, k)) ? `cascade://thumb/${k}` : null;
  });
  ipcMain.handle('thumb-save', async (_e, f, dataUrl) => {
    const k = thumbKey(f);
    await fsp.writeFile(path.join(thumbDir, k), Buffer.from(dataUrl.split(',')[1], 'base64'));
    return `cascade://thumb/${k}`;
  });

  const shotPath = () => {
    const d = path.join(app.getPath('pictures'), 'Cascade');
    fs.mkdirSync(d, { recursive: true });
    const n = new Date();
    const z = (x) => String(x).padStart(2, '0');
    return path.join(d, `Cascade_${n.getFullYear()}${z(n.getMonth() + 1)}${z(n.getDate())}_${z(n.getHours())}${z(n.getMinutes())}${z(n.getSeconds())}.png`);
  };
  ipcMain.handle('save-shot', async (_e, dataUrl) => {
    const p = shotPath();
    await fsp.writeFile(p, Buffer.from(dataUrl.split(',')[1], 'base64'));
    return p;
  });
  ipcMain.handle('shot-rect', async (_e, r) => {
    const img = await win.webContents.capturePage({ x: Math.round(r.x), y: Math.round(r.y), width: Math.round(r.width), height: Math.round(r.height) });
    const p = shotPath();
    await fsp.writeFile(p, img.toPNG());
    return p;
  });
  ipcMain.handle('reveal', (_e, p) => shell.showItemInFolder(p));
  ipcMain.handle('open-default', (_e, p) => shell.openPath(p));
  ipcMain.handle('chrome', (_e, c) => {
    try { win.setTitleBarOverlay({ color: c.color, symbolColor: c.symbol, height: 60 }); win.setBackgroundColor(c.color); } catch { /* not supported */ }
  });
  ipcMain.handle('progress', (_e, v) => { if (win) win.setProgressBar(v); });

  app.on('second-instance', async (_e, argv) => {
    if (win) { if (win.isMinimized()) win.restore(); win.focus(); }
    const entries = await toEntries(mediaFromArgv(argv));
    if (entries.length && win) win.webContents.send('open-files', entries);
  });
});

app.on('window-all-closed', () => app.quit());
