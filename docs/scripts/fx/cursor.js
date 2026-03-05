/* ══════════════════════════════════════════════════════
   CURSOR — custom crosshair with ammo + cooldown ring
══════════════════════════════════════════════════════ */

import { ctx } from './canvas.js';
import { state, MAX_AMMO, RELOAD_DUR, FIRE_COOLDOWN } from './state.js';
import { SceneManager } from './scene.js';
import { IS_MOBILE } from './platform.js';

let cursorVisible = false;
document.addEventListener('mousemove',  () => { cursorVisible = true;  });
document.addEventListener('mouseleave', () => { cursorVisible = false; });

export function drawCursor() {
  if (IS_MOBILE || !cursorVisible || SceneManager.is('gameover')) return;
  const cx = state.mx, cy = state.my;
  const R = 10, gap = 3, armLen = 6;
  const outOfAmmo = state.ammo <= 0 && !state.reloading;
  const onCooldown = state.fireCooldown > 0;

  /* crosshair colour: grey=reloading, dark-red=empty, orange=cooldown, red=ready */
  const col = state.reloading ? '#6b7280'
            : outOfAmmo       ? '#7f1d1d'
            : onCooldown      ? '#f97316'
            : '#dc2626';

  ctx.save();
  ctx.strokeStyle = col;
  ctx.lineWidth   = 1.5;
  ctx.shadowColor = col;
  ctx.shadowBlur  = 6;

  /* outer ring */
  ctx.beginPath(); ctx.arc(cx, cy, R, 0, Math.PI * 2); ctx.stroke();

  /* 4 arms */
  [[0,-1],[0,1],[-1,0],[1,0]].forEach(([dx, dy]) => {
    ctx.beginPath();
    ctx.moveTo(cx + dx * (R + gap),         cy + dy * (R + gap));
    ctx.lineTo(cx + dx * (R + gap + armLen), cy + dy * (R + gap + armLen));
    ctx.stroke();
  });

  /* centre dot */
  ctx.shadowBlur = 0;
  ctx.fillStyle = col;
  ctx.beginPath(); ctx.arc(cx, cy, 1.5, 0, Math.PI * 2); ctx.fill();

  /* reload progress arc (yellow, behind outer ring) */
  if (state.reloading) {
    const prog = 1 - state.reloadT / RELOAD_DUR;
    ctx.strokeStyle = '#fbbf24';
    ctx.lineWidth   = 2;
    ctx.shadowColor = '#fbbf24'; ctx.shadowBlur = 4;
    ctx.beginPath();
    ctx.arc(cx, cy, R + 5, -Math.PI / 2, -Math.PI / 2 + prog * Math.PI * 2);
    ctx.stroke();
  }

  /* fire-cooldown progress arc (orange, tighter ring) */
  if (onCooldown && !state.reloading) {
    const prog = 1 - state.fireCooldown / FIRE_COOLDOWN;
    ctx.strokeStyle = 'rgba(249,115,22,0.7)';
    ctx.lineWidth   = 1.5;
    ctx.shadowColor = '#f97316'; ctx.shadowBlur = 3;
    ctx.beginPath();
    ctx.arc(cx, cy, R + 3, -Math.PI / 2, -Math.PI / 2 + prog * Math.PI * 2);
    ctx.stroke();
  }

  ctx.restore();
}
