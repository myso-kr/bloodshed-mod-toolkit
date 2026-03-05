/* ══════════════════════════════════════════════════════
   SCENE MANAGER — addEventListener/removeEventListener 기반
   씬 전환 시 리스너를 실제로 붙였다 떼는 방식으로 동작.

   dispatch 패턴이 아니라 씬마다 리스너를 등록/해제하므로
   gameover 씬에서는 게임 입력(touchstart fire 등)이
   DOM에 존재하지 않아 자연스럽게 차단된다.

   SceneManager.init('game', ctx)   — 앱 시작
   SceneManager.transition('gameover') — 씬 전환
   SceneManager.is('gameover')         — 현재 씬 조회
   ══════════════════════════════════════════════════════ */

import { state } from './state.js';

/* ── 컨텍스트 (main.js에서 주입) ── */
let _ctx = {};   /* { fire, doReload } */

/* ══════════════════════════════════════════════════════
   공용 핸들러 — 두 씬에서 모두 동일하게 사용
   ══════════════════════════════════════════════════════ */

function onDragstart(e) { e.preventDefault(); }

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
  if (e.button === 0) _ctx.fire?.(e.clientX, e.clientY);
}

/* 터치 탭 → 크로스헤어 즉시 이동 + 발사 */
function onGameTouchstart(e) {
  const t = e.touches[0];
  state.mx = t.clientX;
  state.my = t.clientY;
  _ctx.fire?.(t.clientX, t.clientY);
}

/* 우클릭 → 장전 */
function onGameContextmenu(e) {
  e.preventDefault();
  _ctx.doReload?.();
}

/* ══════════════════════════════════════════════════════
   gameover 씬 전용 핸들러
   ══════════════════════════════════════════════════════ */

/* gameover에서 우클릭 메뉴만 차단 (장전 없음) */
function onGameoverContextmenu(e) { e.preventDefault(); }

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
      /* 발사 */
      [window,   'mousedown',   onGameMousedown                       ],
      [window,   'touchstart',  onGameTouchstart, { passive: true }   ],
      /* 장전 (우클릭) */
      [window,   'contextmenu', onGameContextmenu                     ],
      /* 텍스트 선택 차단 */
      [document, 'mousedown',   onSharedMousedown                     ],
      [document, 'dragstart',   onDragstart                           ],
    ],
    onEnter() {
      document.body.classList.add('game-active');
      this.listeners.forEach(([t, ev, fn, opt]) => t.addEventListener(ev, fn, opt));
    },
    onExit() {
      /* removeEventListener에서 capture 일치 여부만 보므로 opt 생략 가능 */
      this.listeners.forEach(([t, ev, fn]) => t.removeEventListener(ev, fn));
    },
  },

  /* ── 게임오버 / 리더보드 ────────────────────────────
   *  touchstart 리스너 없음 → 모달 내 터치가 자연스럽게 동작
   *  (name-slot 포커스, 버튼 탭 등 브라우저 기본 동작 허용)
   * ─────────────────────────────────────────────────── */
  gameover: {
    listeners: [
      [window,   'contextmenu', onGameoverContextmenu                 ],
      [document, 'mousedown',   onSharedMousedown                     ],
      [document, 'dragstart',   onDragstart                           ],
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
