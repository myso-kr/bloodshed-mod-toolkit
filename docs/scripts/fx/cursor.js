/* ══════════════════════════════════════════════════════
   CURSOR — custom crosshair
══════════════════════════════════════════════════════ */

import { ctx } from './canvas.js';
import { state } from './state.js';
import { SceneManager } from './scene.js';
import { IS_MOBILE } from './platform.js';

let cursorVisible = false;
document.addEventListener('mousemove',  () => { cursorVisible = true;  });
document.addEventListener('mouseleave', () => { cursorVisible = false; });

export function drawCursor() {
  if (IS_MOBILE || !cursorVisible || SceneManager.is('gameover')) return;
  const cx = state.mx, cy = state.my;
  const R = 10, gap = 3, armLen = 6;

  /* crosshair colour: grey=reloading, orange=cooldown, red=ready */
  const col = state.reloading     ? '#6b7280'
            : state.fireCooldown > 0 ? '#f97316'
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

  ctx.restore();
}
