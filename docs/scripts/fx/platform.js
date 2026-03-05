/* ══════════════════════════════════════════════════════
   PLATFORM — coarse-pointer / mobile detection
══════════════════════════════════════════════════════ */

export const IS_MOBILE = window.matchMedia('(pointer: coarse)').matches;
