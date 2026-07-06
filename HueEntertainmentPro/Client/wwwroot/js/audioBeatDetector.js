// Client-side audio beat detection using the Web Audio API.
// Captures audio from the microphone or from the PC (tab/screen share with audio)
// and detects beats by comparing the instantaneous bass energy against a rolling
// average (energy-flux method). Runs entirely in the browser.

let audioContext = null;
let analyser = null;
let sourceNode = null;
let mediaStream = null;
let rafId = null;
let dotNetRef = null;
let containerId = null;

let freqData = null;
let energyHistory = [];
let lastBeatTime = 0;
let beatTimes = [];
let thresholdMultiplier = 1.6;

const HISTORY_SIZE = 45;        // ~0.75s of frames at 60fps
const MIN_BEAT_INTERVAL = 250;  // ms, caps detection at 240 BPM
const ENERGY_FLOOR = 800;       // ignore near-silence (byte spectrum, squared scale)
const MAX_BASS_HZ = 160;        // beats are detected on the bass band only

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
  sourceNode = audioContext.createMediaStreamSource(stream);
  analyser = audioContext.createAnalyser();
  analyser.fftSize = 2048;
  analyser.smoothingTimeConstant = 0;
  sourceNode.connect(analyser);

  freqData = new Uint8Array(analyser.frequencyBinCount);
  energyHistory = [];
  beatTimes = [];
  lastBeatTime = 0;

  const loop = () => {
    detectFrame();
    rafId = requestAnimationFrame(loop);
  };
  rafId = requestAnimationFrame(loop);
}

export function stop() {
  if (rafId) {
    cancelAnimationFrame(rafId);
    rafId = null;
  }
  if (sourceNode) {
    try { sourceNode.disconnect(); } catch { }
    sourceNode = null;
  }
  if (audioContext) {
    audioContext.close().catch(() => { });
    audioContext = null;
  }
  if (mediaStream) {
    mediaStream.getTracks().forEach(t => t.stop());
    mediaStream = null;
  }
  analyser = null;
  dotNetRef = null;
  containerId = null;
  energyHistory = [];
  beatTimes = [];
}

// Higher multiplier = the bass spike must stand out more before it counts as a beat.
export function setThreshold(multiplier) {
  thresholdMultiplier = multiplier;
}

function detectFrame() {
  if (!analyser) return;
  analyser.getByteFrequencyData(freqData);

  const binHz = audioContext.sampleRate / analyser.fftSize;
  const maxBin = Math.max(2, Math.min(freqData.length - 1, Math.round(MAX_BASS_HZ / binHz)));
  let sum = 0;
  for (let i = 1; i <= maxBin; i++) {
    sum += freqData[i] * freqData[i];
  }
  const energy = sum / maxBin;

  updateLevelBar(energy);

  const avg = energyHistory.length
    ? energyHistory.reduce((a, b) => a + b, 0) / energyHistory.length
    : 0;

  energyHistory.push(energy);
  if (energyHistory.length > HISTORY_SIZE) energyHistory.shift();

  const now = performance.now();
  if (energyHistory.length >= HISTORY_SIZE / 2 &&
    avg > ENERGY_FLOOR &&
    energy > avg * thresholdMultiplier &&
    now - lastBeatTime > MIN_BEAT_INTERVAL) {
    lastBeatTime = now;
    onBeat(now);
  }
}

function onBeat(now) {
  beatTimes.push(now);
  if (beatTimes.length > 12) beatTimes.shift();

  flashBeatDot();
  dotNetRef?.invokeMethodAsync('HandleBeat', estimateBpm());
}

function estimateBpm() {
  const intervals = [];
  for (let i = 1; i < beatTimes.length; i++) {
    const interval = beatTimes[i] - beatTimes[i - 1];
    if (interval >= MIN_BEAT_INTERVAL && interval <= 2000) intervals.push(interval);
  }
  if (intervals.length < 3) return 0;
  intervals.sort((a, b) => a - b);
  const median = intervals[Math.floor(intervals.length / 2)];
  return Math.round(60000 / median);
}

function flashBeatDot() {
  const dot = document.querySelector(`#${containerId} .beat-dot`);
  if (!dot) return;
  dot.classList.remove('beat-flash');
  void dot.offsetWidth; // restart the CSS animation
  dot.classList.add('beat-flash');
}

function updateLevelBar(energy) {
  const fill = document.querySelector(`#${containerId} .level-fill`);
  if (!fill) return;
  const percent = Math.min(100, (Math.sqrt(energy) / 255) * 130);
  fill.style.width = percent.toFixed(1) + '%';
}
