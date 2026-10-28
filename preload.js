const { contextBridge, ipcRenderer } = require('electron');
contextBridge.exposeInMainWorld('api', {
  times: () => ipcRenderer.invoke('times'),
  onRefresh: f => ipcRenderer.on('refresh', f),
  shot: p => ipcRenderer.send('shot', p)
});
