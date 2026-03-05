/* ══════════════════════════════════════════════════════
   PAGE UI — navbar, hero particles, typewriter, reveals
══════════════════════════════════════════════════════ */

/* ── Navbar scroll opacity ── */
const nav = document.getElementById('home-nav');
const onScroll = () => nav.classList.toggle('scrolled', window.scrollY > 60);
window.addEventListener('scroll', onScroll, { passive: true });
onScroll();

/* ── Hero canvas particle system ── */
const heroCanvas = document.getElementById('hero-canvas');
const hCtx = heroCanvas.getContext('2d');
let W, H, heroParticles = [];

function heroResize() {
  W = heroCanvas.width  = heroCanvas.offsetWidth;
  H = heroCanvas.height = heroCanvas.offsetHeight;
}
window.addEventListener('resize', heroResize);
heroResize();

function randomParticle() {
  return {
    x: Math.random() * W, y: Math.random() * H,
    r: Math.random() * 1.8 + 0.3,
    vx: (Math.random() - 0.5) * 0.3,
    vy: Math.random() * 0.5 + 0.1,
    alpha: Math.random() * 0.5 + 0.1,
    hue: Math.random() > 0.8 ? 20 : 0,
  };
}
for (let i = 0; i < 120; i++) heroParticles.push(randomParticle());

function drawHeroParticles() {
  hCtx.clearRect(0, 0, W, H);
  heroParticles.forEach(p => {
    hCtx.beginPath();
    hCtx.arc(p.x, p.y, p.r, 0, Math.PI * 2);
    hCtx.fillStyle = `hsla(${p.hue}, 90%, 55%, ${p.alpha})`;
    hCtx.fill();
    p.x += p.vx; p.y += p.vy; p.alpha -= 0.0008;
    if (p.y > H + 4 || p.alpha <= 0) Object.assign(p, randomParticle(), { y: -4 });
  });
  hCtx.strokeStyle = 'rgba(220,38,38,0.025)';
  hCtx.lineWidth = 1;
  const gs = 60;
  for (let x = 0; x < W; x += gs) { hCtx.beginPath(); hCtx.moveTo(x, 0); hCtx.lineTo(x, H); hCtx.stroke(); }
  for (let y = 0; y < H; y += gs) { hCtx.beginPath(); hCtx.moveTo(0, y); hCtx.lineTo(W, y); hCtx.stroke(); }
  requestAnimationFrame(drawHeroParticles);
}
drawHeroParticles();

/* ── Parallax hero bg on mouse move ── */
const heroBg = document.getElementById('hero-bg-img');
document.getElementById('hero').addEventListener('mousemove', e => {
  const rx = (e.clientX / window.innerWidth  - 0.5) * 10;
  const ry = (e.clientY / window.innerHeight - 0.5) * 10;
  heroBg.style.transform = `scale(1.08) translate(${rx}px, ${ry}px)`;
});

/* ── Typewriter ── */
const words = ['No Cooldown', 'God Mode', 'Speed Hack ×10', 'Perfect Aim', 'Infinite Revive', '4-Player Co-op', 'One-Shot Kill'];
const tw = document.getElementById('typewriter');
let wi = 0, ci = 0, deleting = false;
function typeStep() {
  const word = words[wi];
  if (!deleting) {
    tw.textContent = word.slice(0, ++ci);
    if (ci === word.length) { deleting = true; setTimeout(typeStep, 1800); return; }
  } else {
    tw.textContent = word.slice(0, --ci);
    if (ci === 0) { deleting = false; wi = (wi + 1) % words.length; }
  }
  setTimeout(typeStep, deleting ? 55 : 90);
}
setTimeout(typeStep, 1200);

/* ── Intersection Observer: reveal + counters ── */
const io = new IntersectionObserver(entries => {
  entries.forEach(entry => {
    if (!entry.isIntersecting) return;
    const el = entry.target;
    el.classList.add('visible');
    const numEl = el.querySelector('[data-target]');
    if (numEl) {
      const target = +numEl.dataset.target;
      const prefix = numEl.dataset.prefix || '';
      const suffix = numEl.dataset.suffix || '';
      const dur = 1200, start = performance.now();
      function tick(now) {
        const t = Math.min((now - start) / dur, 1);
        numEl.textContent = prefix + Math.round(target * (1 - Math.pow(1 - t, 3))) + suffix;
        if (t < 1) requestAnimationFrame(tick);
      }
      requestAnimationFrame(tick);
    }
    io.unobserve(el);
  });
}, { threshold: 0.15 });
document.querySelectorAll('.reveal').forEach(el => io.observe(el));

/* ── 3D tilt on feature cards ── */
document.querySelectorAll('.feat-card').forEach(card => {
  card.addEventListener('mousemove', e => {
    const r = card.getBoundingClientRect();
    const x = (e.clientX - r.left) / r.width  - 0.5;
    const y = (e.clientY - r.top)  / r.height - 0.5;
    card.style.transform = `perspective(600px) rotateY(${x * 10}deg) rotateX(${-y * 10}deg) translateY(-4px)`;
    card.style.setProperty('--mx', `${(x + 0.5) * 100}%`);
    card.style.setProperty('--my', `${(y + 0.5) * 100}%`);
  });
  card.addEventListener('mouseleave', () => { card.style.transform = ''; });
});

/* ── Parallax sections on scroll ── */
const coopBg = document.querySelector('.coop-bg');
window.addEventListener('scroll', () => {
  const sy = window.scrollY;
  if (heroBg) heroBg.style.transform = `scale(1.06) translateY(${sy * 0.18}px)`;
  if (coopBg) {
    const rect = coopBg.parentElement.getBoundingClientRect();
    coopBg.style.transform = `scale(1.04) translateY(${rect.top * 0.08}px)`;
  }
}, { passive: true });

/* ── iframe 우클릭 차단 ──────────────────────────────────
 *  cross-origin iframe 내부 이벤트는 부모에서 직접 접근 불가.
 *  투명 오버레이(iframe-guard)가 마우스 이벤트를 중간에서 가로챔.
 *
 *  · 우클릭(button≠0) → preventDefault (컨텍스트 메뉴 차단)
 *  · 좌클릭(button=0) → 오버레이를 pointer-events:none으로 전환 →
 *    클릭이 아래 iframe으로 통과 → mouseup 이후 복원
 * ─────────────────────────────────────────────────────── */
document.querySelectorAll('.iframe-guard').forEach(guard => {
  guard.addEventListener('contextmenu', e => e.preventDefault());

  guard.addEventListener('mousedown', e => {
    if (e.button !== 0) return;              /* 좌클릭만 통과 처리 */
    guard.style.pointerEvents = 'none';      /* iframe에 클릭 전달 */
    window.addEventListener('mouseup', () => {
      guard.style.pointerEvents = '';        /* 복원 */
    }, { once: true });
  });
});
