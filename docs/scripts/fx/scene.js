/* ══════════════════════════════════════════════════════
   SCENE MANAGER — addEventListener/removeEventListener 기반
   씬 전환 시 리스너를 실제로 붙였다 떼는 방식으로 동작.

   SceneManager.init('game', ctx)      — 앱 시작
   SceneManager.transition('gameover') — 씬 전환
   SceneManager.is('gameover')         — 현재 씬 조회
   ══════════════════════════════════════════════════════ */

import { state } from './state.js';
import { IS_MOBILE } from './platform.js';

/* ── 컨텍스트 (main.js에서 주입) ── */
let _ctx = {};   /* { fire, doReload } */

/* ══════════════════════════════════════════════════════
   공용 핸들러 — 두 씬에서 모두 동일하게 사용
   ══════════════════════════════════════════════════════ */

function onDragstart(e) { e.preventDefault(); }
function onContextmenu(e) { e.preventDefault(); }

/* 마우스다운 텍스트-선택 / 포커스 제어
 *  · form 요소 · contenteditable : 기본 동작 허용 (타이핑 가능)
 *  · tabindex >= 0               : preventDefault 후 수동 focus
 *                                  (preventScroll = 모바일 스크롤 점프 방지)
 *  · 그 외                       : preventDefault (텍스트 드래그 선택 차단) */
function onSharedMousedown(e) {
  const tag = e.target.tagName;
  if (['INPUT', 'TEXTAREA', 'SELECT'].includes(tag)) return;
  if (e.target.isContentEditable) return;
  e.preventDefault();
  if (e.target.hasAttribute('tabindex') && e.target.tabIndex >= 0)
    e.target.focus({ preventScroll: true });
}

/* ══════════════════════════════════════════════════════
   game 씬 전용 핸들러
   ══════════════════════════════════════════════════════ */

function onGameMousedown(e) {
  if (e.button !== 0) return;
  /* 모바일: tap crosshair flash */
  if (IS_MOBILE) state.touchFlashes.push({ x: e.clientX, y: e.clientY, life: 1 });
  _ctx.fire?.(e.clientX, e.clientY);
}

/* ══════════════════════════════════════════════════════
   씬 정의
   listeners 배열: [target, type, handler, options?]
   onEnter: 리스너 등록 + 부수효과
   onExit:  리스너 해제 + 부수효과
   ══════════════════════════════════════════════════════ */
const SCENES = {

  /* ── 게임 진행 중 ─────────────────────────────────── */
  game: {
    listeners: [
      [window,   'mousedown',   onGameMousedown  ],
      [window,   'contextmenu', onContextmenu    ],
      [document, 'mousedown',   onSharedMousedown],
      [document, 'dragstart',   onDragstart      ],
    ],
    onEnter() {
      document.body.classList.add('game-active');
      this.listeners.forEach(([t, ev, fn, opt]) => t.addEventListener(ev, fn, opt));
    },
    onExit() {
      this.listeners.forEach(([t, ev, fn]) => t.removeEventListener(ev, fn));
    },
  },

  /* ── 게임오버 / 리더보드 ─────────────────────────── */
  gameover: {
    listeners: [
      [window,   'contextmenu', onContextmenu    ],
      [document, 'mousedown',   onSharedMousedown],
      [document, 'dragstart',   onDragstart      ],
    ],
    onEnter() {
      document.body.classList.remove('game-active');
      this.listeners.forEach(([t, ev, fn, opt]) => t.addEventListener(ev, fn, opt));
    },
    onExit() {
      this.listeners.forEach(([t, ev, fn]) => t.removeEventListener(ev, fn));
    },
  },
};

/* ══════════════════════════════════════════════════════
   SceneManager API
   ══════════════════════════════════════════════════════ */
let _current = null;

export const SceneManager = {
  get current() { return _current; },

  is: name => _current === name,

  /** 앱 시작 시 1회 호출. ctx = { fire, doReload } */
  init(name, ctx = {}) {
    _ctx = ctx;
    _current = name;
    SCENES[name].onEnter();
  },

  /** 씬 전환: 현재 씬 리스너 해제 → 새 씬 리스너 등록 */
  transition(name) {
    if (_current === name || !SCENES[name]) return;
    SCENES[_current].onExit();
    _current = name;
    SCENES[name].onEnter();
  },
};
