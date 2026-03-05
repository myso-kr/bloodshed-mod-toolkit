/* ══════════════════════════════════════════════════════
   PARTICLES — shotgun blast + muzzle flash
══════════════════════════════════════════════════════ */

import { ctx } from './canvas.js';
import { state } from './state.js';

export function blast(x, y, bloody) {
  const n = 12 + (Math.random() * 8 | 0);
  for (let i = 0; i < n; i++) {
    const ang = Math.random() * Math.PI * 2;
    const spd = 2 + Math.random() * 9;
    state.parts.push({
      x, y,
      vx: Math.cos(ang) * spd,
      vy: Math.sin(ang) * spd - (0.5 + Math.random() * 2),
      r: 1.5 + Math.random() * 2.5,
      life: 1,
      decay: 0.032 + Math.random() * 0.04,
      color: bloody
        ? (Math.random() < 0.65 ? '#dc2626' : '#7f1d1d')
        : (Math.random() < 0.5  ? '#f97316' : '#fbbf24'),
      type: Math.random() < 0.35 ? 'spark' : 'pellet',
    });
  }
  /* muzzle flash */
  state.parts.push({ x, y, vx: 0, vy: 0, r: 16 + Math.random() * 12, life: 1, decay: 0.2, color: '#fff', type: 'flash' });
}

export function tickParticles() {
  for (let i = state.parts.length - 1; i >= 0; i--) {
    const p = state.parts[i];
    p.x += p.vx; p.vx *= 0.88;
    p.y += p.vy; p.vy  = p.vy * 0.88 + 0.28;
    p.life -= p.decay;
    if (p.life <= 0) { state.parts.splice(i, 1); continue; }

    ctx.save();
    ctx.globalAlpha = Math.max(0, p.life);
    if (p.type === 'flash') {
      const g = ctx.createRadialGradient(p.x, p.y, 0, p.x, p.y, p.r * p.life);
      g.addColorStop(0,    'rgba(255,255,180,0.95)');
      g.addColorStop(0.35, 'rgba(255,140,0,0.55)');
      g.addColorStop(1,    'rgba(200,30,0,0)');
      ctx.fillStyle = g;
      ctx.beginPath(); ctx.arc(p.x, p.y, p.r * p.life, 0, Math.PI * 2); ctx.fill();
    } else if (p.type === 'spark') {
      ctx.strokeStyle = p.color; ctx.lineWidth = 1.5;
      ctx.beginPath(); ctx.moveTo(p.x, p.y);
      ctx.lineTo(p.x - p.vx * 3, p.y - p.vy * 3); ctx.stroke();
    } else {
      ctx.fillStyle = p.color;
      ctx.beginPath(); ctx.arc(p.x, p.y, Math.max(0.1, p.r * p.life), 0, Math.PI * 2); ctx.fill();
    }
    ctx.restore();
  }
}
