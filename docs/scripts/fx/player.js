/* ══════════════════════════════════════════════════════
   PLAYER — mobile-only auto-walking pixel-art character
   8×12 sprite, blue armor, walks at screen bottom,
   bounces horizontally off viewport edges.
══════════════════════════════════════════════════════ */

import { ctx } from './canvas.js';

export const PLAYER_RADIUS = 16;   /* projectile hit radius (px) */

const SPD = 55;                    /* walk speed px/s */
const PPX = 3;                     /* sprite pixel size (matches monster PX) */
const SW  = 8  * PPX;             /* sprite pixel width  = 24px */
const SH  = 12 * PPX;             /* sprite pixel height = 36px */

/* ── palette ── */
/* 1=skin  2=blue-armor  3=dark-blue  4=gray-pants  5=dark-boots  6=red-eye */
const PAL = {
  1: '#f4c28a',
  2: '#3b82f6',
  3: '#1d4ed8',
  4: '#4b5563',
  5: '#1f2937',
  6: '#ef4444',
};

/* ── sprite frames (8-wide × 12-tall) ── */
/* prettier-ignore */
const FRAME_A = [
  [0,0,1,1,1,1,0,0],
  [0,1,1,1,1,1,1,0],
  [0,1,6,1,1,6,1,0],
  [0,1,1,1,1,1,1,0],
  [0,3,3,3,3,3,3,0],
  [3,2,2,2,2,2,2,3],
  [3,2,2,2,2,2,2,3],
  [3,2,2,2,2,2,2,3],
  [0,4,4,0,0,4,4,0],
  [0,4,4,0,0,4,4,0],
  [0,4,4,0,0,4,4,0],
  [0,5,5,0,0,5,5,0],
];

/* prettier-ignore */
const FRAME_B = [
  [0,0,1,1,1,1,0,0],
  [0,1,1,1,1,1,1,0],
  [0,1,6,1,1,6,1,0],
  [0,1,1,1,1,1,1,0],
  [0,3,3,3,3,3,3,0],
  [3,2,2,2,2,2,2,3],
  [3,2,2,2,2,2,2,3],
  [3,2,2,2,2,2,2,3],
  [0,0,4,4,4,4,0,0],
  [0,0,4,0,0,4,0,0],
  [0,0,4,0,0,4,0,0],
  [0,0,5,0,0,5,0,0],
];

function bake(frame) {
  const oc  = document.createElement('canvas');
  oc.width  = SW;
  oc.height = SH;
  const ox  = oc.getContext('2d');
  frame.forEach((row, r) =>
    row.forEach((v, c) => {
      if (!v) return;
      ox.fillStyle = PAL[v];
      ox.fillRect(c * PPX, r * PPX, PPX, PPX);
    })
  );
  return oc;
}

const FRAMES = [bake(FRAME_A), bake(FRAME_B)];

/* ── shared player state ── */
export const player = {
  x: 0, y: 0,
  vx: SPD,
  frame: 0,
  timer: 0,
};

export function initPlayer() {
  player.x     = window.innerWidth  / 2;
  player.y     = window.innerHeight * 0.83;
  player.vx    = SPD;
  player.frame = 0;
  player.timer = 0;
}

export function tickPlayer(dt) {
  const margin = SW / 2 + 6;
  player.x += player.vx * dt;

  /* bounce off left / right edges */
  if (player.x > window.innerWidth - margin) {
    player.x  = window.innerWidth - margin;
    player.vx = -SPD;
  } else if (player.x < margin) {
    player.x  = margin;
    player.vx =  SPD;
  }

  /* walk animation */
  player.timer += dt;
  if (player.timer > 0.22) { player.timer = 0; player.frame ^= 1; }
}

export function drawPlayer() {
  const sprite = FRAMES[player.frame];
  ctx.save();
  ctx.imageSmoothingEnabled = false;

  const drawX = player.x - SW / 2;
  const drawY = player.y - SH / 2;

  if (player.vx < 0) {
    /* flip horizontally when walking left */
    ctx.translate(player.x, player.y);
    ctx.scale(-1, 1);
    ctx.drawImage(sprite, -SW / 2, -SH / 2, SW, SH);
  } else {
    ctx.drawImage(sprite, drawX, drawY, SW, SH);
  }

  ctx.restore();
}
