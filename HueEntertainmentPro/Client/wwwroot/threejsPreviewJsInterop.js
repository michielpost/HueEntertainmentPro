// This is a JavaScript module that is loaded on demand. It can export any number of
// functions, and may import other JavaScript modules if required.

import * as preview from './js/threejs/threejspreview.js';

export function initPreview() {
  preview.renderPreviewGrid();
}

export function showLights(list) {
  preview.scheduleLightUpdate(list);
}

// Ensure the renderer resizes when entering/leaving fullscreen
document.addEventListener('fullscreenchange', () => window.dispatchEvent(new Event('resize')));

export function toggleFullscreen(elementId) {
  if (document.fullscreenElement) {
    document.exitFullscreen();
    return;
  }
  const element = document.getElementById(elementId);
  if (element && element.requestFullscreen) {
    element.requestFullscreen();
  }
}
