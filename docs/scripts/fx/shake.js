/* ══════════════════════════════════════════════════════
   SHAKE — element-level tilt shake (no body transform)
══════════════════════════════════════════════════════ */

export let shakeEls = [];

export function collectShakeEls() {
  shakeEls = Array.from(document.querySelectorAll(
    'h1,h2,h3,h4,h5,p,img,iframe,' +
    '.feature-card,.diff-card,.install-step,' +
    '.stat-num,.stat-label,.ticker-item,.steam-card-wrap,' +
    '.manifesto-line,.coop-stat,.preset-card'
  )).filter(el => !el.closest('#kill-hud') && el.id !== 'bgm-btn');
}

export function shake(pow) {
  if (!shakeEls.length) return;
  const sample = shakeEls.filter(() => Math.random() < 0.55);
  sample.forEach(el => {
    const angle = (Math.random() * 2 - 1) * pow * 1.1;
    const tx    = (Math.random() * 2 - 1) * pow * 0.7;
    const ty    = (Math.random() * 2 - 1) * pow * 0.4;
    el.style.transition = 'none';
    el.style.transform  = `translate(${tx.toFixed(1)}px,${ty.toFixed(1)}px) rotate(${angle.toFixed(2)}deg)`;
  });
  setTimeout(() => {
    sample.forEach(el => {
      el.style.transition = 'transform 0.28s ease-out';
      el.style.transform  = '';
    });
  }, 75 + (Math.random() * 35 | 0));
  if (pow >= 8) setTimeout(() => shake(pow * 0.42), 70);
}
