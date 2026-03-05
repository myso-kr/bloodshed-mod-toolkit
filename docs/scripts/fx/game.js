/* ══════════════════════════════════════════════════════
   GAME — HP, damage, projectiles, game-over, leaderboard
══════════════════════════════════════════════════════ */

import { ctx } from './canvas.js';
import { state, MAX_HP, MAX_AMMO } from './state.js';
import { shake } from './shake.js';

/* ── Supabase credentials — replace with your project values ── */
const SB_URL = 'https://qucelkfkincvhotygsci.supabase.co';
const SB_KEY = 'sb_publishable_1GJtKjYBYIyvHVcfPe-5jw_vgTC8dzv';

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
  document.body.style.cursor = 'auto';
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
  nameSlots[0] = nameSlots[1] = nameSlots[2] = 'A';
  activeSlot = 0;
  updateNameUI();
  document.getElementById('go-submit').style.display = '';
  document.getElementById('go-submit').disabled = false;
  document.getElementById('go-submit').textContent = 'REGISTER SCORE';
  document.getElementById('go-lb').style.display = 'none';
  document.getElementById('gameover-modal').classList.remove('active');
  document.body.style.cursor = '';
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

/* ── arcade name input + leaderboard UI ── */
const nameSlots = ['A', 'A', 'A'];
let activeSlot  = 0;

function updateNameUI() {
  for (let i = 0; i < 3; i++) {
    const el = document.getElementById(`ns-${i}`);
    el.textContent = nameSlots[i];
    el.className   = 'name-slot' + (i === activeSlot ? ' ns-active' : '');
  }
}

async function insertScore(name, score) {
  try {
    await fetch(`${SB_URL}/rest/v1/leaderboard`, {
      method: 'POST',
      headers: {
        'apikey': SB_KEY, 'Authorization': `Bearer ${SB_KEY}`,
        'Content-Type': 'application/json', 'Prefer': 'return=minimal',
      },
      body: JSON.stringify({ name, score }),
    });
  } catch (e) { console.warn('[BMT] leaderboard insert failed:', e); }
}

async function fetchLeaderboard() {
  try {
    const r = await fetch(
      `${SB_URL}/rest/v1/leaderboard?select=name,score&order=score.desc&limit=10`,
      { headers: { 'apikey': SB_KEY, 'Authorization': `Bearer ${SB_KEY}` } }
    );
    return await r.json();
  } catch (e) { return []; }
}

export function setupGameOverUI(killValueEl) {
  /* slot click to select */
  for (let i = 0; i < 3; i++) {
    document.getElementById(`ns-${i}`).addEventListener('click', () => {
      activeSlot = i; updateNameUI();
    });
  }

  /* keyboard navigation */
  document.addEventListener('keydown', e => {
    if (!state.gameOver) return;
    if (!document.getElementById('gameover-modal').classList.contains('active')) return;
    const charCode = nameSlots[activeSlot].charCodeAt(0);
    if (e.key === 'ArrowUp') {
      nameSlots[activeSlot] = String.fromCharCode(charCode === 65 ? 90 : charCode - 1);
      updateNameUI(); e.preventDefault();
    } else if (e.key === 'ArrowDown') {
      nameSlots[activeSlot] = String.fromCharCode(charCode === 90 ? 65 : charCode + 1);
      updateNameUI(); e.preventDefault();
    } else if (e.key === 'ArrowLeft') {
      activeSlot = Math.max(0, activeSlot - 1); updateNameUI(); e.preventDefault();
    } else if (e.key === 'ArrowRight') {
      activeSlot = Math.min(2, activeSlot + 1); updateNameUI(); e.preventDefault();
    } else if (e.key === 'Enter') {
      document.getElementById('go-submit').click(); e.preventDefault();
    }
  });

  /* submit score */
  document.getElementById('go-submit').addEventListener('click', async () => {
    const name  = nameSlots.join('');
    const score = state.kills;
    const btn   = document.getElementById('go-submit');
    btn.textContent = '...'; btn.disabled = true;
    await insertScore(name, score);
    const rows = await fetchLeaderboard();
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
  });

  /* play again */
  document.getElementById('go-restart').addEventListener('click', () => {
    resetGame(killValueEl);
  });
}
