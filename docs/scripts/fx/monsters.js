/* ══════════════════════════════════════════════════════
   MONSTERS — pixel-art sprites, pool, projectile firing
══════════════════════════════════════════════════════ */

import { ctx } from './canvas.js';
import { state, PX } from './state.js';
import { playMonsterShot } from './audio.js';
import { IS_MOBILE } from './platform.js';
import { player, PLAYER_RADIUS } from './player.js';

const MOBILE_MIN_DIST = 80;  /* minimum px gap between monster and player on mobile */
const MONSTER_RADIUS = 18;          /* ~12*PX/2 — collision circle for separation */
const SEPARATION_STRENGTH = 0.5;

function getTarget() {
  return IS_MOBILE ? { x: player.x, y: player.y } : { x: state.mx, y: state.my };
}

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

/* ── projectile toward target ── */
export function fireProjectile(m) {
  const t = getTarget();
  const dx = t.x - m.x, dy = t.y - m.y;
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
  const W = innerWidth, H = innerHeight, EDGE = 70;
  /* 위쪽 70%, 좌우 각 15%, 아래쪽 없음 */
  const r = Math.random();
  const [sx, sy] = r < 0.70 ? [rand(0, W), -EDGE]
                 : r < 0.85  ? [-EDGE,      rand(0, H * 0.6)]
                              : [W + EDGE,  rand(0, H * 0.6)];
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

/* ── separation physics ── */
function separateMonsters(dt) {
  const ms = state.monsters;
  const len = ms.length;
  const minDistMM = MONSTER_RADIUS * 2;
  const minDistMP = MONSTER_RADIUS + PLAYER_RADIUS;

  for (let i = 0; i < len; i++) {
    const a = ms[i];
    if (a.dyingT >= 0) continue;

    /* monster–monster */
    for (let j = i + 1; j < len; j++) {
      const b = ms[j];
      if (b.dyingT >= 0) continue;
      const dx = a.x - b.x, dy = a.y - b.y;
      const distSq = dx * dx + dy * dy;
      if (distSq > minDistMM * minDistMM || distSq < 0.01) continue;
      const dist = Math.sqrt(distSq);
      const overlap = minDistMM - dist;
      const nx = dx / dist, ny = dy / dist;
      const push = overlap * SEPARATION_STRENGTH * 0.5;
      a.x += nx * push; a.y += ny * push;
      b.x -= nx * push; b.y -= ny * push;
    }

    /* monster–player (mobile only) */
    if (IS_MOBILE) {
      const dx = a.x - player.x, dy = a.y - player.y;
      const distSq = dx * dx + dy * dy;
      if (distSq < minDistMP * minDistMP && distSq > 0.01) {
        const dist = Math.sqrt(distSq);
        const overlap = minDistMP - dist;
        const nx = dx / dist, ny = dy / dist;
        a.x += nx * overlap * SEPARATION_STRENGTH;
        a.y += ny * overlap * SEPARATION_STRENGTH;
      }
    }
  }
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
      const tgt = getTarget();
      m.x += (tgt.x - m.x) * m.spd * 60 * dt;
      m.y += (tgt.y - m.y) * m.spd * 60 * dt;
      /* mobile: keep minimum distance from player */
      if (IS_MOBILE) {
        const edx = m.x - tgt.x, edy = m.y - tgt.y;
        const ed  = Math.sqrt(edx * edx + edy * edy) || 1;
        if (ed < MOBILE_MIN_DIST) {
          m.x = tgt.x + (edx / ed) * MOBILE_MIN_DIST;
          m.y = tgt.y + (edy / ed) * MOBILE_MIN_DIST;
        }
      }
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

  separateMonsters(dt);
}
