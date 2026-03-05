/* ══════════════════════════════════════════════════════
   STATE — shared mutable game state (no DOM, no imports)
══════════════════════════════════════════════════════ */

export const MAX_AMMO    = 8;
export const RELOAD_DUR  = 1.1;
export const MAX_HP      = 5;
export const FIRE_COOLDOWN = 0.44; /* seconds between shots */
export const PX          = 3;     /* monster sprite pixel size */

export const state = {
  /* cursor position */
  mx: window.innerWidth  / 2,
  my: window.innerHeight / 2,

  /* ammo */
  ammo: MAX_AMMO,
  reloading: false,
  reloadT: 0,
  fireCooldown: 0,  /* remaining cooldown until next shot */

  /* game */
  kills: 0,
  hp: MAX_HP,
  invincible: 0,
  gameOver: false,
  damageFlash: 0,

  /* object pools */
  parts:       [],
  monsters:    [],
  projectiles: [],
};
