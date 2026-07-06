// AudioWorkletProcessor that detects beats on the audio rendering thread.
// The input signal is already lowpass-filtered (bass band) by a BiquadFilterNode
// in the main graph, so this processor only computes RMS energy per frame and
// compares it against a rolling average (energy-flux method).
//
// Running on the audio thread means detection keeps working when the tab is in
// the background (requestAnimationFrame is paused there) and has ~11ms
// resolution independent of display refresh rate.

const FRAME_SAMPLES = 512;        // ~10.7ms at 48kHz per energy sample
const HISTORY_SECONDS = 1.0;      // rolling average window
const MIN_BEAT_INTERVAL = 0.25;   // seconds, caps detection at 240 BPM
const ENERGY_FLOOR = 1e-5;        // ignore near-silence (mean-square of float samples)
const DISPLAY_GAIN = 3;           // rms -> 0..1 display scale
const MIN_MULTIPLIER = 1.1;       // must match the sensitivity slider range in .NET
const MAX_MULTIPLIER = 2.45;

class BeatDetectorProcessor extends AudioWorkletProcessor {
  constructor() {
    super();
    this.multiplier = 1.85;
    this.histSize = Math.max(8, Math.round(sampleRate * HISTORY_SECONDS / FRAME_SAMPLES));
    this.hist = new Float32Array(this.histSize);
    this.histIdx = 0;
    this.histCount = 0;
    this.histSum = 0;
    this.acc = 0;
    this.accCount = 0;
    this.prevEnergy = 0;
    this.lastBeatTime = -1;

    this.port.onmessage = (e) => {
      const msg = e.data;
      if (msg.type === 'mult') {
        this.multiplier = msg.value;
      } else if (msg.type === 'target') {
        this.setTargetLevel(msg.norm);
      }
    };
  }

  // The user dragged the threshold line to a normalized display level (0..1);
  // convert it back to an energy multiplier relative to the current average.
  setTargetLevel(norm) {
    const avg = this.histCount ? this.histSum / this.histCount : 0;
    if (avg < ENERGY_FLOOR) return;
    const rms = norm / DISPLAY_GAIN;
    const multiplier = Math.min(MAX_MULTIPLIER, Math.max(MIN_MULTIPLIER, (rms * rms) / avg));
    this.multiplier = multiplier;
    this.port.postMessage({ type: 'm', m: multiplier });
  }

  process(inputs) {
    const input = inputs[0];
    if (input && input.length > 0) {
      const c0 = input[0];
      const c1 = input.length > 1 ? input[1] : null;
      for (let i = 0; i < c0.length; i++) {
        const s = c1 ? (c0[i] + c1[i]) * 0.5 : c0[i];
        this.acc += s * s;
        this.accCount++;
        if (this.accCount >= FRAME_SAMPLES) this.endFrame();
      }
    }
    return true;
  }

  endFrame() {
    const energy = this.acc / this.accCount;
    this.acc = 0;
    this.accCount = 0;

    const avg = this.histCount ? this.histSum / this.histCount : 0;

    // O(1) rolling window update
    if (this.histCount === this.histSize) this.histSum -= this.hist[this.histIdx];
    else this.histCount++;
    this.hist[this.histIdx] = energy;
    this.histSum += energy;
    this.histIdx = (this.histIdx + 1) % this.histSize;

    const threshold = avg * this.multiplier;
    let beat = 0;
    if (this.histCount >= this.histSize / 2 &&
      avg > ENERGY_FLOOR &&
      energy > threshold &&
      energy > this.prevEnergy && // rising edge only, avoids re-triggering on sustained loudness
      currentTime - this.lastBeatTime > MIN_BEAT_INTERVAL) {
      this.lastBeatTime = currentTime;
      beat = 1;
    }
    this.prevEnergy = energy;

    this.port.postMessage({
      type: 'f',
      a: Math.min(1, Math.sqrt(energy) * DISPLAY_GAIN),
      th: Math.min(1, Math.sqrt(threshold) * DISPLAY_GAIN),
      b: beat,
      t: beat ? currentTime : 0
    });
  }
}

registerProcessor('beat-detector', BeatDetectorProcessor);
