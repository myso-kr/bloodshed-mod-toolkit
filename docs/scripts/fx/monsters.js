/* ══════════════════════════════════════════════════════
   MONSTERS — pixel-art sprites, pool, projectile firing
══════════════════════════════════════════════════════ */

import { ctx } from './canvas.js';
import { state, PX } from './state.js';

/* ── sprite data ── */
/* prettier-ignore */
const FRAME_A = [
  [0,0,1,1,0,0,0,0,1,1,0,0],
  [0,1,1,1,1,1,1,1,1,1,1,0],
  [1,1,2,3,1,1,1,1,3,2,1,1],
  [1,1,1,1,1,1,1,1,1,1,1,1],
  [1,2,1,4,2,1,1,2,4,1,2,1],
  [0,1,1,1,1,1,1,1,1,1,1,0],
  [0,1,1,1,1,1,1,1,1,1,1,0],
  [1,1,2,1,1,1,1,1,1,2,1,1],
  [0,1,1,1,1,1,1,1,1,1,1,0],
  [0,0,1,1,0,0,0,0,1,1,0,0],
  [0,1,1,0,0,0,0,0,0,1,1,0],
  [1,1,0,0,0,0,0,0,0,0,1,1],
];
/* prettier-ignore */
const FRAME_B = [
  [0,0,1,1,0,0,0,0,1,1,0,0],
  [0,1,1,1,1,1,1,1,1,1,1,0],
  [1,1,2,3,1,1,1,1,3,2,1,1],
  [1,1,1,1,1,1,1,1,1,1,1,1],
  [1,2,1,4,2,1,1,2,4,1,2,1],
  [0,1,1,1,1,1,1,1,1,1,1,0],
  [0,1,1,1,1,1,1,1,1,1,1,0],
  [1,1,2,1,1,1,1,1,1,2,1,1],
  [0,1,1,1,1,1,1,1,1,1,1,0],
  [0,1,1,0,0,0,0,0,0,1,1,0],
  [0,0,1,1,0,0,0,0,1,1,0,0],
  [0,0,1,1,0,0,0,0,1,1,0,0],
];
const FRAMES = [FRAME_A, FRAME_B];

const PALETTES = [
  { body: '#8b3a0f', shad: '#5c2007', eye: '#ff2222', teeth: '#d1d5db', glow: '#ff2222' },
  { body: '#1a5c2a', shad: '#0d3016', eye: '#00ff66', teeth: '#d1d5db', glow: '#00ff66' },
  { body: '#4a1060', shad: '#280a38', eye: '#e040fb', teeth: '#d1d5db', glow: '#dd00ff' },
];

function bake(pal, fi) {
  const oc = document.createElement('canvas');
  oc.width = oc.height = 12 * PX;
  const ox = oc.getContext('2d');
  FRAMES[fi].forEach((row, r) =>
    row.forEach((v, c) => {
      if (!v) return;
      ox.fillStyle = v === 1 ? pal.body : v === 2 ? pal.shad : v === 3 ? pal.eye : pal.teeth;
      ox.fillRect(c * PX, r * PX, PX, PX);
    })
  );
  return oc;
}

function rand(a, b) { return a + Math.random() * (b - a); }
function hexRgba(hex, a) {
  const n = parseInt(hex.replace('#', ''), 16);
  return `rgba(${n >> 16 & 255},${n >> 8 & 255},${n & 255},${a})`;
}

/* ── dynamic scaling ── */
function maxMonsters() { return Math.min(12, Math.floor(4 + Math.log(state.kills + 1) * 1.8)); }
function spawnInterval() { return Math.max(1000, 5000 / (1 + state.kills * 0.1)); }

/* ── projectile toward cursor ── */
export function fireProjectile(m) {
  const dx = state.mx - m.x, dy = state.my - m.y;
  const dist = Math.sqrt(dx * dx + dy * dy) || 1;
  const spd = rand(2.4, 3.4);
  state.projectiles.push({
    x: m.x, y: m.y,
    vx: (dx / dist) * spd,
    vy: (dy / dist) * spd,
    color: m.pal.glow,
    r: 5, life: 1,
  });
}

/* ── pool management ── */
export function spawnMonster() {
  if (state.monsters.length >= maxMonsters()) return;
  const pal  = PALETTES[Math.random() * PALETTES.length | 0];
  const side = Math.random() * 4 | 0;
  const W = innerWidth, H = innerHeight, EDGE = 70;
  const [sx, sy] = side === 0 ? [rand(0, W), -EDGE]
                 : side === 1 ? [W + EDGE,   rand(0, H)]
                 : side === 2 ? [rand(0, W), H + EDGE]
                              : [-EDGE,      rand(0, H)];
  state.monsters.push({
    x: sx, y: sy,
    spd: rand(0.016, 0.026),
    pal,
    fA: bake(pal, 0),
    fB: bake(pal, 1),
    timer: rand(0, 0.5),
    frame: 0,
    dyingT: -1,
    alpha: 1,
    shotCooldown: rand(3, 7),
  });
}

export function scheduleSpawn() {
  setTimeout(() => { spawnMonster(); scheduleSpawn(); }, spawnInterval());
}

/* ── update + draw ── */
export function tickMonsters(dt) {
  const sz = 12 * PX;
  for (let i = state.monsters.length - 1; i >= 0; i--) {
    const m = state.monsters[i];

    if (m.dyingT >= 0) {
      m.dyingT += dt;
      m.alpha   = Math.max(0, 1 - m.dyingT / 0.35);
      if (m.dyingT > 0.38) { state.monsters.splice(i, 1); continue; }
    } else {
      m.x += (state.mx - m.x) * m.spd * 60 * dt;
      m.y += (state.my - m.y) * m.spd * 60 * dt;
      m.timer += dt;
      if (m.timer > 0.28) { m.timer = 0; m.frame ^= 1; }
      if (!state.gameOver) {
        m.shotCooldown -= dt;
        if (m.shotCooldown <= 0) {
          fireProjectile(m);
          m.shotCooldown = rand(3, 7);
        }
      }
    }

    const sprite = m.frame === 0 ? m.fA : m.fB;
    ctx.save();
    ctx.globalAlpha = m.alpha;
    if (m.dyingT < 0) {
      const gr = ctx.createRadialGradient(m.x, m.y, 0, m.x, m.y, sz * 0.7);
      gr.addColorStop(0, hexRgba(m.pal.glow, 0.2));
      gr.addColorStop(1, hexRgba(m.pal.glow, 0));
      ctx.fillStyle = gr;
      ctx.beginPath(); ctx.arc(m.x, m.y, sz * 0.7, 0, Math.PI * 2); ctx.fill();
    }
    ctx.imageSmoothingEnabled = false;
    ctx.drawImage(sprite, m.x - sz / 2, m.y - sz / 2, sz, sz);
    if (m.dyingT >= 0 && m.dyingT < 0.1) {
      ctx.globalAlpha = (1 - m.dyingT / 0.1) * 0.85;
      ctx.fillStyle = '#ffffff';
      ctx.fillRect(m.x - sz / 2, m.y - sz / 2, sz, sz);
    }
    ctx.restore();
  }
}
