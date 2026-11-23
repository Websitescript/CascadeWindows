const { contextBridge, ipcRenderer, webUtils } = require('electron');
const call = (name) => (...a) => ipcRenderer.invoke(name, ...a);

contextBridge.exposeInMainWorld('cascade', {
  defaultFolders: call('default-folders'),
  scan: call('scan'),
  pickFolder: call('pick-folder'),
  pickFiles: call('pick-files'),
  pickSubtitle: call('pick-subtitle'),
  readText: call('read-text'),
  siblingSubs: call('sibling-subs'),
  classify: call('classify'),
  initialFiles: call('initial-files'),
  thumbGet: call('thumb-get'),
  thumbSave: call('thumb-save'),
  saveShot: call('save-shot'),
  shotRect: call('shot-rect'),
  reveal: call('reveal'),
  openDefault: call('open-default'),
  chrome: call('chrome'),
  progress: call('progress'),
  pathForFile: (f) => webUtils.getPathForFile(f),
  onOpenFiles: (cb) => ipcRenderer.on('open-files', (_e, entries) => cb(entries)),
});
