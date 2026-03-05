/* ══════════════════════════════════════════════════════
   MAIN — orchestrates all FX modules, input, game loop
══════════════════════════════════════════════════════ */

import { cvs, ctx }                                            from './canvas.js';
import { state, MAX_AMMO, RELOAD_DUR, FIRE_COOLDOWN, PX }     from './state.js';
import { ensureAudio, startBGM, stopBGM, playShot, playReload, playMonsterDeath } from './audio.js';
import { blast, tickParticles }                                from './particles.js';
import { shake, collectShakeEls }                              from './shake.js';
import { spawnMonster, scheduleSpawn, tickMonsters }           from './monsters.js';
import { drawCursor }                                          from './cursor.js';
import { updateHpHud, tickProjectiles, tickDamageVignette, setupGameOverUI } from './game.js';

/* ── drag / text-select guard ── */
document.addEventListener('dragstart', e => e.preventDefault());
document.addEventListener('mousedown', e => {
  if (!['INPUT', 'TEXTAREA', 'SELECT'].includes(e.target.tagName)) e.preventDefault();
});

/* ── cursor tracking ── */
window.addEventListener('mousemove', e => { state.mx = e.clientX; state.my = e.clientY; });
window.addEventListener('touchmove', e => {
  state.mx = e.touches[0].clientX;
  state.my = e.touches[0].clientY;
}, { passive: true });

/* ── kill counter HUD ── */
const killValue = document.getElementById('kill-value');
const killHud   = document.getElementById('kill-hud');

function addKill() {
  state.kills++;
  killValue.textContent = state.kills;
  killValue.classList.remove('bump');
  void killValue.offsetWidth; /* force reflow to restart animation */
  killValue.classList.add('bump');
  setTimeout(() => killValue.classList.remove('bump'), 120);
  killHud.classList.add('flash');
  setTimeout(() => killHud.classList.remove('flash'), 180);
}

/* ── ammo / fire ── */
function doReload() {
  if (state.reloading || state.ammo >= MAX_AMMO) return;
  ensureAudio();
  state.reloading = true;
  state.reloadT   = RELOAD_DUR;
  playReload();
}

function fire(cx, cy) {
  if (state.reloading || state.gameOver || state.fireCooldown > 0) return;
  ensureAudio();
  if (state.ammo <= 0) { doReload(); return; }
  state.ammo--;
  state.fireCooldown = FIRE_COOLDOWN;

  let hit = false;
  const hw = 12 * PX * 0.5;
  for (const m of state.monsters) {
    if (m.dyingT >= 0) continue;
    if (Math.abs(cx - m.x) < hw && Math.abs(cy - m.y) < hw) {
      m.dyingT = 0;
      blast(m.x, m.y, true);
      shake(9);
      playShot(true);
      playMonsterDeath();
      addKill();
      hit = true;
      break;
    }
  }
  if (!hit) { blast(cx, cy, false); shake(5); playShot(false); }
}

/* mouse: left=fire, right=reload */
window.addEventListener('mousedown',   e => { if (e.button === 0) fire(e.clientX, e.clientY); });
window.addEventListener('contextmenu', e => { e.preventDefault(); doReload(); });

/* touch: touchstart fires + syncs cursor position so crosshair follows tap */
window.addEventListener('touchstart', e => {
  const t = e.touches[0];
  state.mx = t.clientX;
  state.my = t.clientY;
  fire(t.clientX, t.clientY);
}, { passive: true });

/* ── BGM toggle ── */
const bgmBtn = document.getElementById('bgm-btn');
let bgmOn = false;

function setBgmOn(on) {
  bgmOn = on;
  bgmBtn.textContent             = bgmOn ? '♫' : '♪';
  bgmBtn.style.borderColor       = bgmOn ? '#dc2626' : '';
  bgmBtn.style.boxShadow         = bgmOn ? '0 0 10px rgba(220,38,38,0.5)' : '';
  if (bgmOn) startBGM(); else stopBGM();
}
bgmBtn.addEventListener('click', () => { ensureAudio(); setBgmOn(!bgmOn); });

/* Auto-start BGM on first user interaction (browser autoplay policy) */
let bgmAutoStarted = false;
function tryAutoStartBGM() {
  if (bgmAutoStarted) return;
  bgmAutoStarted = true;
  ensureAudio();
  setBgmOn(true);
}
window.addEventListener('mousedown', tryAutoStartBGM, { once: true });
window.addEventListener('touchstart', tryAutoStartBGM, { once: true, passive: true });

/* ── initial monster spawns ── */
setTimeout(spawnMonster, 900);
setTimeout(spawnMonster, 3200);
scheduleSpawn();

/* ── init ── */
updateHpHud();
setupGameOverUI(killValue);
collectShakeEls();

/* ── main loop ── */
let lastT = 0;
function loop(ts) {
  const dt = Math.min((ts - lastT) * 0.001, 0.05);
  lastT = ts;
  ctx.clearRect(0, 0, cvs.width, cvs.height);

  /* timers */
  if (state.reloading) {
    state.reloadT -= dt;
    if (state.reloadT <= 0) { state.ammo = MAX_AMMO; state.reloading = false; }
  }
  if (state.fireCooldown > 0) state.fireCooldown = Math.max(0, state.fireCooldown - dt);

  tickDamageVignette(dt);
  tickParticles();
  tickMonsters(dt);
  tickProjectiles();
  drawCursor();

  requestAnimationFrame(loop);
}
requestAnimationFrame(loop);
