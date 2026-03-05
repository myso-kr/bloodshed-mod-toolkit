/* ══════════════════════════════════════════════════════
   CANVAS — shared 2D context
══════════════════════════════════════════════════════ */

export const cvs = document.getElementById('fx-cvs');
export const ctx = cvs.getContext('2d');

export function resizeCanvas() {
  cvs.width  = window.innerWidth;
  cvs.height = window.innerHeight;
}

resizeCanvas();
window.addEventListener('resize', resizeCanvas, { passive: true });
