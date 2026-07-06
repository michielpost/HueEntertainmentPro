// Client-side audio beat detection using the Web Audio API.
// Captures audio from the microphone or from the PC (tab/screen share with audio).
// Detection itself runs on the audio rendering thread (see beatDetectorWorklet.js):
// source -> lowpass biquad (bass band) -> AudioWorklet. The main thread only
// renders the scrolling waveform and forwards beats to .NET, so detection keeps
// working when the tab is in the background.

let audioContext = null;
let sourceNode = null;
let filterNode = null;
let workletNode = null;
let silentGain = null;
let mediaStream = null;
let rafId = null;
let dotNetRef = null;
let containerId = null;

// Scrolling display ring buffer: normalized amplitude, threshold and beat flag
// per ~11ms energy frame (~4s of history).
const DISPLAY_SIZE = 360;
const dispAmp = new Float32Array(DISPLAY_SIZE);
const dispThr = new Float32Array(DISPLAY_SIZE);
const dispBeat = new Uint8Array(DISPLAY_SIZE);
let dispIdx = 0;
let dispCount = 0;

let beatTimes = [];
let canvas = null;
let ctx = null;
let beatDot = null;
let colors = null;
let dragging = false;

export async function start(dotNet, sourceType, elementContainerId) {
  stop();

  let stream;
  if (sourceType === 'microphone') {
    stream = await navigator.mediaDevices.getUserMedia({
      audio: { echoCancellation: false, noiseSuppression: false, autoGainControl: false }
    });
  } else {
    // Audio-only getDisplayMedia is not allowed; request video too and drop it.
    stream = await navigator.mediaDevices.getDisplayMedia({
      video: true,
      audio: { echoCancellation: false, noiseSuppression: false, autoGainControl: false }
    });
    stream.getVideoTracks().forEach(t => t.stop());
    if (stream.getAudioTracks().length === 0) {
      stream.getTracks().forEach(t => t.stop());
      throw new Error('No audio was shared. Select a tab or screen and enable the "Share audio" option.');
    }
  }

  dotNetRef = dotNet;
  containerId = elementContainerId;
  mediaStream = stream;

  // Notify .NET when the user stops sharing from the browser UI.
  stream.getAudioTracks().forEach(track => {
    track.addEventListener('ended', () => {
      if (mediaStream !== stream) return; // a newer session replaced this one
      const ref = dotNetRef;
      stop();
      ref?.invokeMethodAsync('HandleSourceEnded');
    });
  });

  audioContext = new AudioContext();
  await audioContext.resume();
  await audioContext.audioWorklet.addModule('./js/beatDetectorWorklet.js');

  sourceNode = audioContext.createMediaStreamSource(stream);
  filterNode = new BiquadFilterNode(audioContext, { type: 'lowpass', frequency: 160, Q: 0.9 });
  workletNode = new AudioWorkletNode(audioContext, 'beat-detector', {
    numberOfInputs: 1,
    numberOfOutputs: 1,
    outputChannelCount: [1]
  });
  // A worklet only processes when its output reaches the destination; keep it silent.
  silentGain = new GainNode(audioContext, { gain: 0 });
  sourceNode.connect(filterNode);
  filterNode.connect(workletNode);
  workletNode.connect(silentGain);
  silentGain.connect(audioContext.destination);

  workletNode.port.onmessage = onWorkletMessage;

  dispIdx = 0;
  dispCount = 0;
  beatTimes = [];

  const loop = () => {
    draw();
    rafId = requestAnimationFrame(loop);
  };
  rafId = requestAnimationFrame(loop);
}

export function stop() {
  if (rafId) {
    cancelAnimationFrame(rafId);
    rafId = null;
  }
  if (workletNode) {
    workletNode.port.onmessage = null;
    try { workletNode.disconnect(); } catch { }
    workletNode = null;
  }
  for (const node of [sourceNode, filterNode, silentGain]) {
    try { node?.disconnect(); } catch { }
  }
  sourceNode = filterNode = silentGain = null;
  if (audioContext) {
    audioContext.close().catch(() => { });
    audioContext = null;
  }
  if (mediaStream) {
    mediaStream.getTracks().forEach(t => t.stop());
    mediaStream = null;
  }
  detachCanvas();
  beatDot = null;
  dotNetRef = null;
  containerId = null;
  beatTimes = [];
  dispIdx = 0;
  dispCount = 0;
  dragging = false;
}

// Higher multiplier = the bass spike must stand out more before it counts as a beat.
export function setThreshold(multiplier) {
  workletNode?.port.postMessage({ type: 'mult', value: multiplier });
}

function onWorkletMessage(e) {
  const msg = e.data;
  if (msg.type === 'f') {
    dispAmp[dispIdx] = msg.a;
    dispThr[dispIdx] = msg.th;
    dispBeat[dispIdx] = msg.b;
    dispIdx = (dispIdx + 1) % DISPLAY_SIZE;
    if (dispCount < DISPLAY_SIZE) dispCount++;
    if (msg.b) onBeat(msg.t);
  } else if (msg.type === 'm') {
    // Threshold changed by dragging on the canvas; sync the .NET slider.
    dotNetRef?.invokeMethodAsync('HandleThresholdChanged', msg.m);
  }
}

function onBeat(audioTime) {
  beatTimes.push(audioTime);
  if (beatTimes.length > 12) beatTimes.shift();

  flashBeatDot();
  dotNetRef?.invokeMethodAsync('HandleBeat', estimateBpm());
}

function estimateBpm() {
  const intervals = [];
  for (let i = 1; i < beatTimes.length; i++) {
    const interval = beatTimes[i] - beatTimes[i - 1];
    if (interval >= 0.25 && interval <= 2) intervals.push(interval);
  }
  if (intervals.length < 3) return 0;
  intervals.sort((a, b) => a - b);
  const median = intervals[Math.floor(intervals.length / 2)];
  return Math.round(60 / median);
}

function flashBeatDot() {
  if (!beatDot || !beatDot.isConnected) {
    beatDot = document.querySelector(`#${containerId} .beat-dot`);
  }
  if (!beatDot) return;
  beatDot.classList.remove('beat-flash');
  void beatDot.offsetWidth; // restart the CSS animation
  beatDot.classList.add('beat-flash');
}

// ---- Waveform rendering ----

function ensureCanvas() {
  if (!canvas || !canvas.isConnected) {
    detachCanvas();
    canvas = document.querySelector(`#${containerId} .beat-canvas`);
    if (!canvas) return null;
    ctx = canvas.getContext('2d');
    colors = resolveColors(canvas);
    canvas.addEventListener('pointerdown', onPointerDown);
    canvas.addEventListener('pointermove', onPointerMove);
    canvas.addEventListener('pointerup', onPointerUp);
    canvas.addEventListener('pointercancel', onPointerUp);
  }
  const dpr = window.devicePixelRatio || 1;
  const w = Math.round(canvas.clientWidth * dpr);
  const h = Math.round(canvas.clientHeight * dpr);
  if (w > 0 && h > 0 && (canvas.width !== w || canvas.height !== h)) {
    canvas.width = w;
    canvas.height = h;
  }
  return canvas;
}

function detachCanvas() {
  if (canvas) {
    canvas.removeEventListener('pointerdown', onPointerDown);
    canvas.removeEventListener('pointermove', onPointerMove);
    canvas.removeEventListener('pointerup', onPointerUp);
    canvas.removeEventListener('pointercancel', onPointerUp);
  }
  canvas = null;
  ctx = null;
  colors = null;
}

function resolveColors(el) {
  const style = getComputedStyle(el);
  const accent = style.getPropertyValue('--accent-fill-rest').trim() || '#2b88d8';
  return {
    bar: 'rgba(128, 128, 128, 0.55)',
    beat: accent,
    threshold: '#e8a33d'
  };
}

function draw() {
  const c = ensureCanvas();
  if (!c || dispCount === 0) return;

  const w = c.width;
  const h = c.height;
  const dpr = window.devicePixelRatio || 1;
  const barW = w / DISPLAY_SIZE;

  ctx.clearRect(0, 0, w, h);

  // Amplitude bars, newest sample at the right edge; beats in accent color.
  for (let i = 0; i < dispCount; i++) {
    const idx = (dispIdx - dispCount + i + DISPLAY_SIZE) % DISPLAY_SIZE;
    const x = w - (dispCount - i) * barW;
    const barH = Math.max(1, dispAmp[idx] * h);
    ctx.fillStyle = dispBeat[idx] ? colors.beat : colors.bar;
    ctx.fillRect(x, h - barH, Math.max(1, barW), barH);
  }

  // Detection threshold as a dashed line; draggable.
  ctx.strokeStyle = colors.threshold;
  ctx.lineWidth = 1.5 * dpr;
  ctx.setLineDash([4 * dpr, 3 * dpr]);
  ctx.beginPath();
  for (let i = 0; i < dispCount; i++) {
    const idx = (dispIdx - dispCount + i + DISPLAY_SIZE) % DISPLAY_SIZE;
    const x = w - (dispCount - i) * barW;
    const y = h - dispThr[idx] * h;
    if (i === 0) ctx.moveTo(x, y);
    else ctx.lineTo(x, y);
  }
  ctx.stroke();
  ctx.setLineDash([]);
}

// ---- Drag on the canvas to set the detection threshold ----

function onPointerDown(e) {
  if (!canvas) return;
  dragging = true;
  try { canvas.setPointerCapture(e.pointerId); } catch { }
  sendTargetLevel(e);
  e.preventDefault();
}

function onPointerMove(e) {
  if (dragging) sendTargetLevel(e);
}

function onPointerUp() {
  dragging = false;
}

function sendTargetLevel(e) {
  const rect = canvas.getBoundingClientRect();
  if (rect.height <= 0) return;
  const norm = Math.min(0.98, Math.max(0.02, 1 - (e.clientY - rect.top) / rect.height));
  workletNode?.port.postMessage({ type: 'target', norm });
}
