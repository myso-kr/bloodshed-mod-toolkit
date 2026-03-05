/* ══════════════════════════════════════════════════════
   GAME — HP, damage, projectiles, game-over, leaderboard
══════════════════════════════════════════════════════ */

import { ctx } from './canvas.js';
import { state, MAX_HP, MAX_AMMO } from './state.js';
import { shake } from './shake.js';

/* ── Supabase credentials ── */
const SB_URL    = 'https://qucelkfkincvhotygsci.supabase.co';
const SB_KEY    = 'sb_publishable_1GJtKjYBYIyvHVcfPe-5jw_vgTC8dzv';
const SB_DOMAIN = 'bloodshed';

/* ── HP HUD ── */
export function updateHpHud() {
  const pips = document.getElementById('hp-pips');
  pips.innerHTML = '';
  for (let i = 0; i < MAX_HP; i++) {
    const pip = document.createElement('div');
    pip.className = 'hp-pip ' + (i < state.hp ? 'alive' : 'dead');
    pips.appendChild(pip);
  }
}

/* ── damage ── */
export function takeDamage() {
  if (state.invincible > 0 || state.gameOver) return;
  state.hp--;
  state.invincible  = 1.8;
  state.damageFlash = 1;
  shake(6);
  updateHpHud();
  if (state.hp <= 0) triggerGameOver();
}

export function triggerGameOver() {
  state.gameOver = true;
  document.getElementById('go-score-val').textContent = state.kills;
  document.getElementById('gameover-modal').classList.add('active');
  document.body.classList.remove('game-active');
  /* auto-focus first slot for keyboard users; delay for modal animation */
  setTimeout(() => document.getElementById('ns-0')?.focus({ preventScroll: true }), 200);
}

/* ── reset ── */
export function resetGame(killValueEl) {
  state.hp           = MAX_HP;
  state.kills        = 0;
  state.ammo         = MAX_AMMO;
  state.invincible   = 0;
  state.damageFlash  = 0;
  state.fireCooldown = 0;
  state.gameOver     = false;
  state.monsters.length    = 0;
  state.projectiles.length = 0;
  state.parts.length       = 0;
  updateHpHud();
  if (killValueEl) killValueEl.textContent = '0';
  [0, 1, 2].forEach(i => { document.getElementById(`ns-${i}`).textContent = 'A'; });
  document.getElementById('go-submit').style.display = '';
  document.getElementById('go-submit').disabled = false;
  document.getElementById('go-submit').textContent = 'REGISTER SCORE';
  document.getElementById('go-lb').style.display = 'none';
  document.querySelector('.name-row').style.display = '';
  document.querySelector('.go-hint').style.display = '';
  document.getElementById('gameover-modal').classList.remove('active');
  document.body.classList.add('game-active');
}

/* ── projectiles update + draw ── */
export function tickProjectiles() {
  for (let i = state.projectiles.length - 1; i >= 0; i--) {
    const p = state.projectiles[i];
    p.x += p.vx; p.y += p.vy;
    p.life -= 0.004;

    /* cursor hit detection */
    if (!state.gameOver) {
      const dx = p.x - state.mx, dy = p.y - state.my;
      if (dx * dx + dy * dy < 18 * 18) {
        state.projectiles.splice(i, 1);
        takeDamage();
        continue;
      }
    }

    if (p.life <= 0 || p.x < -30 || p.x > innerWidth + 30 || p.y < -30 || p.y > innerHeight + 30) {
      state.projectiles.splice(i, 1); continue;
    }

    ctx.save();
    ctx.globalAlpha = p.life;
    const gr = ctx.createRadialGradient(p.x, p.y, 0, p.x, p.y, p.r * 2.2);
    gr.addColorStop(0,   '#fff');
    gr.addColorStop(0.3, p.color);
    gr.addColorStop(1,   'rgba(0,0,0,0)');
    ctx.fillStyle = gr;
    ctx.beginPath(); ctx.arc(p.x, p.y, p.r * 2.2, 0, Math.PI * 2); ctx.fill();
    ctx.globalAlpha = p.life * 0.9;
    ctx.fillStyle = '#fff';
    ctx.beginPath(); ctx.arc(p.x, p.y, p.r * 0.45, 0, Math.PI * 2); ctx.fill();
    ctx.restore();
  }
}

/* ── damage vignette tick ── */
const dmgVignette = document.getElementById('dmg-vignette');
export function tickDamageVignette(dt) {
  if (state.invincible > 0) state.invincible -= dt;
  if (state.damageFlash > 0) {
    state.damageFlash = Math.max(0, state.damageFlash - dt * 3);
    dmgVignette.style.opacity = state.damageFlash;
  } else if (dmgVignette.style.opacity !== '0') {
    dmgVignette.style.opacity = '0';
  }
}

/* ── Supabase API ── */
async function insertScore(name, score) {
  try {
    await fetch(`${SB_URL}/rest/v1/leaderboard`, {
      method: 'POST',
      headers: {
        'apikey': SB_KEY, 'Authorization': `Bearer ${SB_KEY}`,
        'Content-Type': 'application/json', 'Prefer': 'return=minimal',
      },
      body: JSON.stringify({ domain: SB_DOMAIN, name, score }),
    });
  } catch (e) { console.warn('[BMT] leaderboard insert failed:', e); }
}

async function fetchLeaderboard() {
  try {
    const r = await fetch(
      `${SB_URL}/rest/v1/leaderboard?select=name,score&domain=eq.${SB_DOMAIN}&order=score.desc&limit=10`,
      { headers: { 'apikey': SB_KEY, 'Authorization': `Bearer ${SB_KEY}` } }
    );
    return await r.json();
  } catch (e) { return []; }
}

/* ── arcade name input (OTP style) + leaderboard UI ──────────────────────
 *
 *  Desktop : arrow keys ↑↓ cycle letter, ←→ move slot, Enter submit
 *            typing a letter auto-advances to next slot
 *  Mobile  : tap slot → virtual keyboard opens → type letter → auto-advance
 *            ▲/▼ buttons cycle letter without keyboard
 * ────────────────────────────────────────────────────────────────────── */
export function setupGameOverUI(killValueEl) {
  const slots = [0, 1, 2].map(i => document.getElementById(`ns-${i}`));

  /* A-Z then 0-9 cycle order: A…Z→0…9→A */
  const CHARS = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';

  /* read validated char from slot i (textContent, not value) */
  function charAt(i) {
    const v = (slots[i].textContent || '').toUpperCase().replace(/[^A-Z0-9]/g, '');
    return v ? v[0] : 'A';
  }

  /* set slot i textContent */
  function setChar(i, ch) { slots[i].textContent = ch; }

  /* cycle slot i by dir (+1 next, -1 prev) through CHARS */
  function cycleLetter(i, dir) {
    const idx = CHARS.indexOf(charAt(i));
    const next = (idx + dir + CHARS.length) % CHARS.length;
    setChar(i, CHARS[next]);
  }

  /* focus slot i (clamps to 0–2) — no .select() on div */
  function focusSlot(i) {
    slots[Math.max(0, Math.min(2, i))].focus();
  }

  /* ── ▲ / ▼ arrow buttons ── */
  document.querySelectorAll('.ns-arrow').forEach(btn => {
    /* pointerup: reliable user-activation on both iOS and Android */
    btn.addEventListener('pointerup', e => {
      const i   = parseInt(btn.dataset.slot, 10);
      const dir = parseInt(btn.dataset.dir,  10);
      cycleLetter(i, dir);
      focusSlot(i);
    });
    /* prevent synthetic click from firing after pointerup */
    btn.addEventListener('click', e => e.preventDefault());
  });

  /* ── key input per slot
   *  Non-contenteditable div → IME 미활성 → e.key가 정상 단일 문자로 옴 ── */
  slots.forEach((slot, i) => {
    slot.addEventListener('keydown', e => {
      if (!state.gameOver) return;
      e.preventDefault();

      /* single A-Z / 0-9 character */
      const ch = e.key.toUpperCase();
      if (ch.length === 1 && /^[A-Z0-9]$/.test(ch)) {
        setChar(i, ch);
        if (i < 2) focusSlot(i + 1);
        return;
      }

      switch (e.key) {
        case 'ArrowUp':    cycleLetter(i,  1); break;
        case 'ArrowDown':  cycleLetter(i, -1); break;
        case 'ArrowLeft':  focusSlot(i - 1);   break;
        case 'ArrowRight': focusSlot(i + 1);   break;
        case 'Backspace':  setChar(i, 'A'); if (i > 0) focusSlot(i - 1); break;
        case 'Enter':      document.getElementById('go-submit').click(); break;
      }
    });
  });

  /* ── submit score ── */
  document.getElementById('go-submit').addEventListener('click', async () => {
    const name  = [0, 1, 2].map(charAt).join('');
    const score = state.kills;
    const btn   = document.getElementById('go-submit');
    btn.textContent = '...'; btn.disabled = true;
    await insertScore(name, score);
    const rows   = await fetchLeaderboard();
    const lbBody = document.getElementById('lb-body');
    lbBody.innerHTML = '';
    let meFound = false;
    rows.forEach((row, idx) => {
      const tr = document.createElement('tr');
      if (!meFound && row.name === name && row.score === score) { tr.className = 'me'; meFound = true; }
      tr.innerHTML = `<td>${idx + 1}</td><td>${row.name}</td><td>${row.score}</td>`;
      lbBody.appendChild(tr);
    });
    if (!rows.length) {
      lbBody.innerHTML = '<tr><td colspan="3" style="color:#4b5563;text-align:center">—</td></tr>';
    }
    document.getElementById('go-lb').style.display = 'block';
    btn.style.display = 'none';
    document.querySelector('.name-row').style.display = 'none';
    document.querySelector('.go-hint').style.display = 'none';
  });

  /* ── play again ── */
  document.getElementById('go-restart').addEventListener('click', () => {
    resetGame(killValueEl);
  });
}
