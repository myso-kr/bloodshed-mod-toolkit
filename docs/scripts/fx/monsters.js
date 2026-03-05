/* ══════════════════════════════════════════════════════
   MONSTERS — pixel-art sprites, pool, projectile firing
══════════════════════════════════════════════════════ */

import { ctx } from './canvas.js';
import { state, PX } from './state.js';
import { playMonsterShot } from './audio.js';

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

/* ── dynamic difficulty (sqrt scale: fast early ramp, gentle late plateau) ──
 *
 *  kills │ max  │ spawn  │ spd range       │ shot cd range
 *  ──────┼──────┼────────┼─────────────────┼──────────────
 *      0 │   4  │ 4.00s  │ 0.014 – 0.022   │ 5.5 – 8.5s
 *     10 │  13  │ 2.27s  │ 0.017 – 0.025   │ 4.8 – 7.4s
 *     30 │  20  │ 1.58s  │ 0.020 – 0.028   │ 4.4 – 6.9s
 *    100 │  34  │ 0.95s  │ 0.024 – 0.032   │ 3.5 – 5.5s
 *    200 │  46  │ 0.68s  │ 0.028 – 0.036   │ 2.7 – 4.3s
 *    400 │  64  │ 0.50s  │ 0.032 – 0.040   │ 1.5 – 3.0s
 */
function sq() { return Math.sqrt(state.kills); }
function maxMonsters()   { return Math.min(64, 4 + Math.floor(sq() * 3)); }
function spawnInterval() { return Math.max(500, 4000 / (1 + sq() * 0.4)); }
function monsterSpd()    { const b = Math.min(0.018, sq() * 0.001); return rand(0.014 + b, 0.022 + b); }
function monsterShotCd() { const s = sq(); return rand(Math.max(1.5, 5.5 - s * 0.2), Math.max(3.0, 8.5 - s * 0.3)); }

/* ── projectile toward cursor ── */
export function fireProjectile(m) {
  const dx = state.mx - m.x, dy = state.my - m.y;
  const dist = Math.sqrt(dx * dx + dy * dy) || 1;
  const spd = Math.min(6.0, rand(2.4 + sq() * 0.08, 3.4 + sq() * 0.08));
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
    spd: monsterSpd(),
    pal,
    fA: bake(pal, 0),
    fB: bake(pal, 1),
    timer: rand(0, 0.5),
    frame: 0,
    dyingT: -1,
    alpha: 1,
    shotCooldown: monsterShotCd(),
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
          playMonsterShot();
          m.shotCooldown = monsterShotCd();
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

    /* pre-attack warning — drawn ON TOP of sprite, no shadowBlur (unreliable cross-browser) */
    if (m.dyingT < 0 && !state.gameOver && m.shotCooldown < 2) {
      const progress = 1 - m.shotCooldown / 2;                        /* 0→1 as shot nears */
      const hz       = 3 + progress * 12;                              /* blink Hz: 3→15    */
      const pulse    = 0.3 + 0.7 * Math.abs(Math.sin(performance.now() * 0.001 * Math.PI * hz));
      const ringR    = sz * 0.7 + progress * sz * 0.15;

      ctx.save();

      /* 1. sprite color flash overlay — always visible regardless of GPU */
      ctx.globalAlpha = pulse * (0.15 + progress * 0.3);
      ctx.fillStyle   = m.pal.glow;
      ctx.fillRect(m.x - sz * 0.5, m.y - sz * 0.5, sz, sz);

      /* 2. outer glow ring */
      ctx.globalAlpha = pulse * (0.6 + progress * 0.35);
      ctx.strokeStyle = m.pal.glow;
      ctx.lineWidth   = 2.5 + progress * 2.5;
      ctx.beginPath(); ctx.arc(m.x, m.y, ringR, 0, Math.PI * 2); ctx.stroke();

      /* 3. thin white highlight ring inside glow ring */
      ctx.globalAlpha = pulse * progress * 0.5;
      ctx.strokeStyle = '#ffffff';
      ctx.lineWidth   = 1;
      ctx.beginPath(); ctx.arc(m.x, m.y, ringR - 3, 0, Math.PI * 2); ctx.stroke();

      ctx.restore();
    }
    if (m.dyingT >= 0 && m.dyingT < 0.1) {
      ctx.globalAlpha = (1 - m.dyingT / 0.1) * 0.85;
      ctx.fillStyle = '#ffffff';
      ctx.fillRect(m.x - sz / 2, m.y - sz / 2, sz, sz);
    }
    ctx.restore();
  }
}
