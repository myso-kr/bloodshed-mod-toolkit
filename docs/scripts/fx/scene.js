/* ══════════════════════════════════════════════════════
   SCENE MANAGER — centralised event preventDefault control
   ══════════════════════════════════════════════════════

   씬별로 mousedown / dragstart / contextmenu 의 preventDefault
   동작을 정의. 씬 전환 시 onEnter/onExit 훅 실행.

   사용법:
     import { SceneManager } from './scene.js';
     SceneManager.init('game');          // 앱 시작 시
     SceneManager.transition('gameover'); // 씬 전환
     SceneManager.is('game')             // 현재 씬 확인
*/

/* ── 공통 mousedown 핸들러 ──────────────────────────────
 * - form 요소 / contenteditable : 기본 동작 허용
 * - tabindex >= 0 요소         : preventDefault 후 수동 focus
 *                                (preventScroll = 모바일 스크롤 방지)
 * - 그 외                      : preventDefault (텍스트 선택 차단)
 * ─────────────────────────────────────────────────────── */
function sharedMousedown(e) {
  const tag = e.target.tagName;
  if (['INPUT', 'TEXTAREA', 'SELECT'].includes(tag) || e.target.isContentEditable) return;
  e.preventDefault();
  if (e.target.hasAttribute('tabindex') && e.target.tabIndex >= 0)
    e.target.focus({ preventScroll: true });
}

/* ── 씬 정의 ────────────────────────────────────────────
 *  각 씬은 다음을 정의한다:
 *    onEnter()           — 씬 진입 시 실행
 *    onExit()            — 씬 종료 시 실행
 *    mousedown(e)        — document mousedown 처리
 *    dragstart(e)        — document dragstart 처리
 *    contextmenu(e, ctx) — window contextmenu 처리
 *                          ctx = { doReload } (main.js 콜백)
 * ─────────────────────────────────────────────────────── */
const SCENES = {

  /* 게임 진행 중 ─────────────────────────────────────── */
  game: {
    onEnter() { document.body.classList.add('game-active'); },
    onExit()  { document.body.classList.remove('game-active'); },
    mousedown:   sharedMousedown,
    dragstart:   e => e.preventDefault(),
    contextmenu: (e, { doReload }) => { e.preventDefault(); doReload?.(); },
  },

  /* 게임오버 / 리더보드 ──────────────────────────────── */
  gameover: {
    onEnter() { document.body.classList.remove('game-active'); },
    onExit()  {},
    mousedown:   sharedMousedown,
    dragstart:   e => e.preventDefault(),
    contextmenu: e => e.preventDefault(), /* 우클릭 메뉴만 차단, 리로드 없음 */
  },
};

/* ── SceneManager ────────────────────────────────────── */
let _current = null;
let _ctx     = {};          /* 씬 핸들러에 주입할 컨텍스트 (doReload 등) */

export const SceneManager = {
  /** 현재 씬 이름 */
  get current() { return _current; },

  /** 현재 씬이 name 인지 확인 */
  is: name => _current === name,

  /** 앱 시작 시 1회 호출. 전역 리스너를 등록하고 초기 씬을 진입. */
  init(name, ctx = {}) {
    _ctx = ctx;

    document.addEventListener('mousedown',
      e => SCENES[_current]?.mousedown?.(e));

    document.addEventListener('dragstart',
      e => SCENES[_current]?.dragstart?.(e));

    window.addEventListener('contextmenu',
      e => SCENES[_current]?.contextmenu?.(e, _ctx));

    /* 초기 씬 진입 */
    _current = name;
    SCENES[name].onEnter();
  },

  /** 씬 전환. 동일 씬이면 무시. */
  transition(name) {
    if (_current === name || !SCENES[name]) return;
    SCENES[_current]?.onExit();
    _current = name;
    SCENES[name].onEnter();
  },
};
