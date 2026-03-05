/* ══════════════════════════════════════════════════════
   AUDIO — Web Audio engine (SFX + 8-bit BGM)
══════════════════════════════════════════════════════ */

export let ac         = null;
export let bgmRunning = false;
export let bgmMaster  = null;

export function ensureAudio() {
  if (ac) { if (ac.state === 'suspended') ac.resume(); return; }
  ac = new (window.AudioContext || window.webkitAudioContext)();
}

/* ── SFX ── */
export function playShot(bloody) {
  if (!ac) return;
  const t = ac.currentTime;
  const len = ac.sampleRate * 0.18;
  const buf = ac.createBuffer(1, len, ac.sampleRate);
  const d = buf.getChannelData(0);
  for (let i = 0; i < len; i++) d[i] = (Math.random() * 2 - 1) * Math.exp(-i / (len * 0.12));
  const src = ac.createBufferSource(); src.buffer = buf;
  const flt = ac.createBiquadFilter();
  flt.type = 'bandpass'; flt.frequency.value = bloody ? 280 : 1200; flt.Q.value = 0.8;
  const gain = ac.createGain();
  gain.gain.setValueAtTime(bloody ? 0.55 : 0.4, t);
  gain.gain.exponentialRampToValueAtTime(0.001, t + 0.22);
  src.connect(flt); flt.connect(gain); gain.connect(ac.destination);
  src.start(t);
  if (bloody) {
    const osc = ac.createOscillator(); osc.type = 'sine';
    osc.frequency.setValueAtTime(110, t);
    osc.frequency.exponentialRampToValueAtTime(35, t + 0.3);
    const og = ac.createGain();
    og.gain.setValueAtTime(0.6, t); og.gain.exponentialRampToValueAtTime(0.001, t + 0.35);
    osc.connect(og); og.connect(ac.destination);
    osc.start(t); osc.stop(t + 0.4);
  }
}

export function playReload() {
  if (!ac) return;
  [0.15, 0.65].forEach((delay, i) => {
    const t0 = ac.currentTime + delay;
    const len = ac.sampleRate * 0.1;
    const buf = ac.createBuffer(1, len, ac.sampleRate);
    const d = buf.getChannelData(0);
    for (let j = 0; j < len; j++) d[j] = (Math.random() * 2 - 1) * Math.exp(-j / (len * 0.12));
    const src = ac.createBufferSource(); src.buffer = buf;
    const f = ac.createBiquadFilter(); f.type = 'highpass';
    f.frequency.value = i === 0 ? 1400 : 900;
    const g = ac.createGain(); g.gain.value = 0.55;
    src.connect(f); f.connect(g); g.connect(ac.destination);
    src.start(t0);
  });
}

export function playEmpty() {
  if (!ac) return;
  const t = ac.currentTime;
  const osc = ac.createOscillator(); osc.type = 'square'; osc.frequency.value = 900;
  const g = ac.createGain();
  g.gain.setValueAtTime(0.12, t); g.gain.exponentialRampToValueAtTime(0.001, t + 0.045);
  osc.connect(g); g.connect(ac.destination);
  osc.start(t); osc.stop(t + 0.05);
}

/* ── 8-bit BGM sequencer (E Phrygian, BPM 168) ── */
export function startBGM() {
  if (!ac || bgmRunning) return;
  bgmRunning = true;

  const master = ac.createGain();
  master.gain.value = 0;
  master.gain.linearRampToValueAtTime(0.22, ac.currentTime + 1.8);
  master.connect(ac.destination);
  bgmMaster = master;

  /* 8-bit clipper */
  const clip = ac.createWaveShaper();
  const cv = new Float32Array(256);
  for (let i = 0; i < 256; i++) {
    const x = i / 128 - 1;
    cv[i] = Math.max(-0.8, Math.min(0.8, x * 2.8));
  }
  clip.curve = cv;

  const leadBus = ac.createGain(); leadBus.gain.value = 0.42; leadBus.connect(clip); clip.connect(master);
  const bassBus = ac.createGain(); bassBus.gain.value = 0.52; bassBus.connect(master);
  const percBus = ac.createGain(); percBus.gain.value = 0.28; percBus.connect(master);

  const f = (n, o) => 440 * Math.pow(2, (n + o * 12 - 69) / 12);
  const N = {
    E1:f(4,1), A1:f(9,1), B1:f(11,1),
    C2:f(0,2), D2:f(2,2), Ds2:f(3,2), E2:f(4,2), Fs2:f(6,2), G2:f(7,2), A2:f(9,2), B2:f(11,2),
    C3:f(0,3), D3:f(2,3), Ds3:f(3,3), E3:f(4,3), Fs3:f(6,3), G3:f(7,3), A3:f(9,3), B3:f(11,3),
    C4:f(0,4), D4:f(2,4), Ds4:f(3,4), E4:f(4,4), G4:f(7,4),
  };
  const BPM = 168, T = 60 / BPM / 2;

  /* prettier-ignore */
  const LEAD = [
    N.E4,0,    N.E4,0,    N.E4,N.D4, N.E4,N.D4,
    N.C4,0,    N.B3,N.A3, N.B3,N.C4, N.B3,0,
    N.A3,0,    N.G3,N.Fs3,N.G3,N.A3, N.G3,0,
    N.E3,0,    0,0,       N.E3,N.Ds3,N.E3,0,
    N.E4,0,    N.E4,0,    N.E4,N.G4, N.D4,N.E4,
    N.D4,N.C4, N.B3,0,    N.A3,0,    N.G3,0,
    N.A3,0,    N.G3,0,    N.Fs3,0,   N.E3,0,
    N.E3,0,    0,0,       N.Ds3,0,   N.E3,0,
  ];
  /* prettier-ignore */
  const BASS = [
    N.E2,0, N.E2,0, N.E2,0,    N.E2,N.D2,
    N.E2,0, N.E2,0, N.A1,0,    N.B1,0,
    N.A1,0, N.A1,0, N.A1,N.G2, N.A2,0,
    N.E2,0, 0,0,    N.E2,N.Ds2,N.E2,0,
    N.E2,0, N.E2,0, N.E2,N.G2, N.E2,0,
    N.D2,0, N.E2,0, N.A1,0,    N.G2,0,
    N.A1,0, N.G2,0, N.Fs2,0,   N.E2,0,
    N.E2,0, 0,0,    N.E2,N.Ds2,N.E2,0,
  ];

  function osc(hz, t, dur, type, bus, vol) {
    if (!hz || !bgmRunning) return;
    const o = ac.createOscillator(); o.type = type; o.frequency.value = hz;
    const g = ac.createGain();
    g.gain.setValueAtTime(0.001, t);
    g.gain.linearRampToValueAtTime(vol, t + 0.004);
    g.gain.setValueAtTime(vol, t + dur - 0.01);
    g.gain.linearRampToValueAtTime(0.001, t + dur);
    o.connect(g); g.connect(bus); o.start(t); o.stop(t + dur + 0.015);
  }
  function kick(t) {
    const o = ac.createOscillator(); o.type = 'sine';
    o.frequency.setValueAtTime(105, t); o.frequency.exponentialRampToValueAtTime(28, t + 0.13);
    const g = ac.createGain();
    g.gain.setValueAtTime(1, t); g.gain.exponentialRampToValueAtTime(0.001, t + 0.2);
    o.connect(g); g.connect(percBus); o.start(t); o.stop(t + 0.22);
  }
  function snare(t) {
    const len = ac.sampleRate * 0.14;
    const buf = ac.createBuffer(1, len, ac.sampleRate);
    const d = buf.getChannelData(0);
    for (let j = 0; j < len; j++) d[j] = (Math.random() * 2 - 1) * Math.exp(-j / (len * 0.35));
    const src = ac.createBufferSource(); src.buffer = buf;
    const bp = ac.createBiquadFilter(); bp.type = 'bandpass'; bp.frequency.value = 280; bp.Q.value = 0.6;
    const g = ac.createGain(); g.gain.value = 0.6;
    src.connect(bp); bp.connect(g); g.connect(percBus); src.start(t);
    const o = ac.createOscillator(); o.type = 'sine'; o.frequency.value = 195;
    const og = ac.createGain();
    og.gain.setValueAtTime(0.35, t); og.gain.exponentialRampToValueAtTime(0.001, t + 0.1);
    o.connect(og); og.connect(percBus); o.start(t); o.stop(t + 0.15);
  }
  function hat(t, accent) {
    const len = ac.sampleRate * (accent ? 0.09 : 0.035);
    const buf = ac.createBuffer(1, len, ac.sampleRate);
    const d = buf.getChannelData(0);
    for (let j = 0; j < len; j++) d[j] = Math.random() * 2 - 1;
    const src = ac.createBufferSource(); src.buffer = buf;
    const hp = ac.createBiquadFilter(); hp.type = 'highpass'; hp.frequency.value = 9500;
    const g = ac.createGain(); g.gain.value = accent ? 0.28 : 0.14;
    src.connect(hp); hp.connect(g); g.connect(percBus); src.start(t);
  }

  const LOOP = LEAD.length * T;
  let loopAt = ac.currentTime + 0.05;
  function schedule() {
    if (!bgmRunning) return;
    const t0 = loopAt;
    LEAD.forEach((hz, i) => osc(hz, t0 + i * T, T * 0.86, 'square',   leadBus, 0.9));
    BASS.forEach((hz, i) => osc(hz, t0 + i * T, T * 1.55, 'sawtooth', bassBus, 0.85));
    for (let bar = 0; bar < 8; bar++) {
      const b0 = t0 + bar * 8 * T;
      kick(b0); kick(b0 + T);
      snare(b0 + 2 * T);
      kick(b0 + 4 * T);
      snare(b0 + 6 * T);
      for (let h = 0; h < 8; h++) hat(b0 + h * T, h % 2 === 0);
    }
    loopAt += LOOP;
    setTimeout(schedule, (LOOP - 0.25) * 1000);
  }
  schedule();
}

export function stopBGM() {
  bgmRunning = false;
  if (bgmMaster) bgmMaster.gain.linearRampToValueAtTime(0, ac.currentTime + 0.9);
}
