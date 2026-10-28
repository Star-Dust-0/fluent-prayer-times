const { app, BrowserWindow, Tray, Menu, nativeImage, nativeTheme, ipcMain, net } = require('electron');
const path = require('path'), fs = require('fs');

if (!app.requestSingleInstanceLock()) { app.quit(); }
let win, tray;
const CITY = 'Alexandria', COUNTRY = 'Egypt', METHOD = 5;
const KEYS = ['Fajr', 'Sunrise', 'Dhuhr', 'Asr', 'Maghrib', 'Isha'];

function cacheFile() { return path.join(app.getPath('userData'), 'times-cache.json'); }
function readCache() { try { return JSON.parse(fs.readFileSync(cacheFile(), 'utf8')); } catch { return {}; } }
function ymd(d) { return `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}`; }
function dmy(d) { return `${String(d.getDate()).padStart(2,'0')}-${String(d.getMonth()+1).padStart(2,'0')}-${d.getFullYear()}`; }

function fetchJson(url) {
  return new Promise((resolve, reject) => {
    const req = net.request(url);
    let data = '';
    req.on('response', r => { r.on('data', c => data += c); r.on('end', () => { try { resolve(JSON.parse(data)); } catch (e) { reject(e); } }); });
    req.on('error', reject);
    setTimeout(() => { try { req.abort(); } catch {} reject(new Error('timeout')); }, 15000);
    req.end();
  });
}
async function dayTimes(d, cache) {
  const k = ymd(d);
  if (cache[k]) return cache[k];
  const j = await fetchJson(`https://api.aladhan.com/v1/timingsByCity/${dmy(d)}?city=${CITY}&country=${COUNTRY}&method=${METHOD}`);
  if (!j || j.code !== 200) throw new Error('bad response');
  const t = {}; KEYS.forEach(n => t[n] = String(j.data.timings[n]).slice(0, 5));
  cache[k] = t; return t;
}
async function getTimes() {
  const cache = readCache(); const now = new Date();
  const tom = new Date(now.getFullYear(), now.getMonth(), now.getDate() + 1);
  let today = null, tomorrow = null, offline = false;
  try { today = await dayTimes(now, cache); } catch { offline = true; }
  try { tomorrow = await dayTimes(tom, cache); } catch { tomorrow = cache[ymd(tom)] || null; }
  // prune old entries
  const keep = {}; [ymd(now), ymd(tom)].forEach(k => { if (cache[k]) keep[k] = cache[k]; });
  try { fs.mkdirSync(path.dirname(cacheFile()), { recursive: true }); fs.writeFileSync(cacheFile(), JSON.stringify(keep)); } catch {}
  return { date: ymd(now), today, tomorrow, offline };
}
ipcMain.handle('times', getTimes);

function makeIcon() {
  const S = 32, buf = Buffer.alloc(S * S * 4);
  for (let y = 0; y < S; y++) for (let x = 0; x < S; x++) {
    const dx = x - 15.5, dy = y - 15.5, ex = x - 21, ey = y - 12;
    const inMoon = dx*dx + dy*dy <= 13*13 && ex*ex + ey*ey > 11*11;
    const o = (y * S + x) * 4;
    if (inMoon) { buf[o] = 255; buf[o+1] = 255; buf[o+2] = 255; buf[o+3] = 255; }
  }
  return nativeImage.createFromBitmap(buf, { width: S, height: S });
}
function toggle() { if (win.isVisible() && win.isFocused()) win.hide(); else showWin(); }
function showWin() { win.show(); win.focus(); win.webContents.send('refresh'); }

function createWin() {
  const dark = nativeTheme.shouldUseDarkColors;
  win = new BrowserWindow({
    width: 380, height: 600, show: false, resizable: false, maximizable: false, autoHideMenuBar: true,
    title: 'مواقيت الصلاة', backgroundColor: dark ? '#202020' : '#f3f3f3',
    webPreferences: { preload: path.join(__dirname, 'preload.js') }
  });
  win.setMenuBarVisibility(false);
  win.loadFile('index.html');
  win.on('close', e => { if (!app.isQuitting) { e.preventDefault(); win.hide(); } });
  win.on('minimize', e => { e.preventDefault(); win.hide(); });
}
app.on('second-instance', () => { if (win) showWin(); });
app.whenReady().then(() => {
  createWin();
  tray = new Tray(makeIcon());
  tray.setToolTip('مواقيت الصلاة');
  tray.setContextMenu(Menu.buildFromTemplate([
    { label: 'فتح', click: showWin },
    { type: 'separator' },
    { label: 'خروج', click: () => { app.isQuitting = true; app.quit(); } }
  ]));
  tray.on('double-click', toggle);
  if (process.argv.includes('--show')) win.once('ready-to-show', showWin);
  ipcMain.on('shot', async (e, p) => { fs.writeFileSync(p, (await win.webContents.capturePage()).toPNG()); });
});
app.on('window-all-closed', () => {});
if (process.env.PRAYER_SHOT) app.whenReady().then(() => setTimeout(async () => {
  const img = await win.webContents.capturePage(); fs.writeFileSync(process.env.PRAYER_SHOT, img.toPNG());
  console.log('cache:', fs.existsSync(cacheFile()) ? fs.readFileSync(cacheFile(), 'utf8') : 'none'); app.isQuitting = true; app.quit();
}, 6000));
