using System.Collections.Generic;
using UnityEngine;
using com8com1.SCFPS;
using BloodshedModToolkit.I18n;

namespace BloodshedModToolkit.UI
{
    public class CheatMenu : MonoBehaviour
    {
        private bool _visible    = false;
        private Rect _windowRect = new Rect(20, 20, 360, 660);

        public CheatMenu(System.IntPtr ptr) : base(ptr) { }

        // ── 캐시 ─────────────────────────────────────────────────────────────────
        private PlayerStats?    _ps;
        private PersistentData? _pd;
        private GameSettings?   _gs;

        private PlayerStats? PS()
        {
            if (_ps != null && _ps.isActiveAndEnabled) return _ps;
            return _ps = FindObjectOfType<PlayerStats>();
        }

        private PersistentData? PD()
        {
            if (_pd != null && _pd.isActiveAndEnabled) return _pd;
            return _pd = FindObjectOfType<PersistentData>();
        }

        private GameSettings? GS()
        {
            if (_gs != null && _gs.isActiveAndEnabled) return _gs;
            return _gs = FindObjectOfType<GameSettings>();
        }

        private LangStrings L()
            => Strings.Get(GS()?.languageText ?? LocalizationManager.Language.English);

        // ── Update ────────────────────────────────────────────────────────────────
        void Update() => ApplyCheats();

        private void ApplyCheats()
        {
            var ps = PS();
            var pd = PD();

            if (CheatState.InfiniteGems && ps != null && ps.money < CheatState.GemsFloor)
                ps.SetMoney(CheatState.GemsFloor);

            if (CheatState.InfiniteGems && pd != null && pd.currentMoney < CheatState.GemsFloor)
                pd.currentMoney = CheatState.GemsFloor;

            if (CheatState.InfiniteSkullCoins && pd != null
                && pd.currentSuperTickets < CheatState.SkullCoinsFloor)
                pd.currentSuperTickets = CheatState.SkullCoinsFloor;

            if (CheatState.InfiniteAway)
            {
                if (ps != null && ps.LevelUpAway < 99) ps.LevelUpAway = 99;
                if (pd != null && pd.currentAways < 99) pd.currentAways = 99;
            }

            if (CheatState.MaxStats && ps != null)
                ps.RestoreHp(99999f);

            if (CheatState.InfiniteRevive && ps != null && ps.revivals < 99)
                ps.SetRevivals(99);
        }

        // ── GUI ──────────────────────────────────────────────────────────────────
        void OnGUI()
        {
            DrawStatusOverlay();

            if (_visible)
                _windowRect = GUI.Window(0, _windowRect,
                    (GUI.WindowFunction)DrawWindow,
                    "\u2605 Bloodshed Cheat Mod v1.0 \u2605");
        }

        // ── 우측 상단 상태 오버레이 ───────────────────────────────────────────────
        private const int OverlayWidth  = 200;
        private const int OverlayMargin = 8;
        private const int LineHeight    = 16;

        // GUIStyle은 OnGUI 안에서만 생성해야 합니다.
        private GUIStyle? _overlayTitle;
        private GUIStyle? _overlayItem;

        private void EnsureOverlayStyles()
        {
            if (_overlayTitle != null) return;

            _overlayTitle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 11,
                fontStyle = FontStyle.Bold,
                wordWrap  = false,
            };
            _overlayTitle.normal.textColor = new Color(1f, 0.92f, 0.3f);   // 노란색

            _overlayItem = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                wordWrap = false,
            };
            _overlayItem.normal.textColor = new Color(0.4f, 1f, 0.45f);    // 초록색
        }

        private void DrawStatusOverlay()
        {
            EnsureOverlayStyles();

            var activeLines = BuildActiveLines();
            int totalLines  = 1 + activeLines.Count;   // 제목 1줄 + 활성 치트 목록
            float panelH    = totalLines * LineHeight + OverlayMargin * 2;
            float panelX    = Screen.width - OverlayWidth - OverlayMargin;
            float panelY    = OverlayMargin;
            var   panelRect = new Rect(panelX, panelY, OverlayWidth, panelH);

            // 반투명 배경
            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.58f);
            GUI.DrawTexture(panelRect, Texture2D.whiteTexture);
            GUI.color = prev;

            // 패널 전체 클릭 → 메뉴 토글
            if (GUI.Button(panelRect, GUIContent.none, GUIStyle.none))
                _visible = !_visible;

            // 제목 줄
            float cx = panelX + OverlayMargin;
            float cy = panelY + OverlayMargin;
            string titleText = _visible
                ? "\u2605 Cheat Mod  \u25b2"     // ▲ 열림 표시
                : "\u2605 Cheat Mod  \u25bc";    // ▼ 닫힘 표시
            GUI.Label(new Rect(cx, cy, OverlayWidth - OverlayMargin * 2, LineHeight),
                      titleText, _overlayTitle!);

            // 활성 치트 목록 (초록색)
            for (int i = 0; i < activeLines.Count; i++)
            {
                cy += LineHeight;
                GUI.Label(new Rect(cx, cy, OverlayWidth - OverlayMargin * 2, LineHeight),
                          activeLines[i], _overlayItem!);
            }
        }

        private List<string> BuildActiveLines()
        {
            var l    = L();
            var list = new List<string>(13);

            if (CheatState.GodMode)            list.Add("\u25cf " + l.GodMode);
            if (CheatState.InfiniteGems)       list.Add("\u25cf " + l.InfiniteGems);
            if (CheatState.InfiniteSkullCoins) list.Add("\u25cf " + l.InfiniteSkullCoins);
            if (CheatState.MaxStats)           list.Add("\u25cf " + l.MaxStats);
            if (CheatState.SpeedHack)          list.Add("\u25cf " + string.Format(l.SpeedLabel,
                                                   CheatState.SpeedMultiplier.ToString("F1")));
            if (CheatState.OneShotKill)        list.Add("\u25cf " + l.OneShotKill);
            if (CheatState.NoCooldown)         list.Add("\u25cf " + l.NoCooldown);
            if (CheatState.InfiniteRevive)     list.Add("\u25cf " + l.InfiniteRevive);
            if (CheatState.InfiniteAway)       list.Add("\u25cf " + l.InfiniteAway);
            if (CheatState.NoReload)           list.Add("\u25cf " + l.NoReload);
            if (CheatState.RapidFire)          list.Add("\u25cf " + l.RapidFire);
            if (CheatState.NoRecoil)           list.Add("\u25cf " + l.NoRecoil);
            if (CheatState.PerfectAim)         list.Add("\u25cf " + l.PerfectAim);

            return list;
        }

        // ── 메인 창 ───────────────────────────────────────────────────────────────
        private void DrawWindow(int id)
        {
            var l = L();
            GUILayout.Space(4);

            CheatState.GodMode            = Toggle(CheatState.GodMode,            l.GodMode);
            CheatState.InfiniteGems       = Toggle(CheatState.InfiniteGems,       l.InfiniteGems);
            CheatState.InfiniteSkullCoins = Toggle(CheatState.InfiniteSkullCoins, l.InfiniteSkullCoins);
            CheatState.MaxStats           = Toggle(CheatState.MaxStats,            l.MaxStats);
            CheatState.SpeedHack          = Toggle(CheatState.SpeedHack,           l.SpeedHack);
            CheatState.OneShotKill        = Toggle(CheatState.OneShotKill,         l.OneShotKill);
            CheatState.NoCooldown         = Toggle(CheatState.NoCooldown,          l.NoCooldown);
            CheatState.InfiniteRevive     = Toggle(CheatState.InfiniteRevive,      l.InfiniteRevive);
            CheatState.InfiniteAway       = Toggle(CheatState.InfiniteAway,        l.InfiniteAway);
            CheatState.NoReload           = Toggle(CheatState.NoReload,            l.NoReload);
            CheatState.RapidFire          = Toggle(CheatState.RapidFire,           l.RapidFire);
            CheatState.NoRecoil           = Toggle(CheatState.NoRecoil,            l.NoRecoil);
            CheatState.PerfectAim         = Toggle(CheatState.PerfectAim,          l.PerfectAim);

            GUILayout.Space(6);
            GUILayout.Label("\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500");

            // 속도 배율 슬라이더
            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Format(l.SpeedLabel, CheatState.SpeedMultiplier.ToString("F1")),
                            GUILayout.Width(150));
            CheatState.SpeedMultiplier = GUILayout.HorizontalSlider(CheatState.SpeedMultiplier, 1f, 20f);
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            if (GUILayout.Button(l.ForceLevelUp))  ForceLevelUp();
            if (GUILayout.Button(l.AddGems))        AddGems();
            if (GUILayout.Button(l.AddSkullCoins))  AddSkullCoins();
            if (GUILayout.Button(l.HealFull))       HealFull();

            GUILayout.Space(4);
            GUILayout.Label("\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500");
            if (GUILayout.Button(l.AllCheatsOff))   CheatState.Initialize();

            GUI.DragWindow();
        }

        // ── 헬퍼 ─────────────────────────────────────────────────────────────────
        private static bool Toggle(bool current, string label)
        {
            var style = new GUIStyle(GUI.skin.toggle);
            style.normal.textColor = current ? Color.green : Color.white;
            style.hover.textColor  = current ? Color.green : Color.white;
            bool next = GUILayout.Toggle(current, label, style);
            if (next != current)
                Plugin.Log.LogInfo($"[CheatMenu] {label.Trim()} \u2192 {(next ? "ON" : "OFF")}");
            return next;
        }

        private void AddGems()
        {
            PS()?.SetMoney(CheatState.GemsFloor);
            var pd = PD();
            if (pd != null) pd.currentMoney = CheatState.GemsFloor;
            Plugin.Log.LogInfo("[AddGems] 젬 999999 지급");
        }

        private void AddSkullCoins()
        {
            var pd = PD();
            if (pd == null) { Plugin.Log.LogWarning("[SkullCoins] PersistentData 없음"); return; }
            pd.currentSuperTickets = CheatState.SkullCoinsFloor;
            Plugin.Log.LogInfo($"[SkullCoins] currentSuperTickets → {CheatState.SkullCoinsFloor}");
        }

        private void HealFull() => PS()?.RestoreHp(99999f);

        private void ForceLevelUp()
        {
            var ps = PS();
            if (ps == null) { Plugin.Log.LogWarning("[ForceLevelUp] PlayerStats 없음"); return; }
            // experienceCap = 현재 레벨의 다음 레벨업에 필요한 누적 XP 상한.
            // 현재 experience 에서 cap 을 1 이상 초과하면 LevelUpChecker 가 레벨업 화면을 발생시킵니다.
            float current = ps.experience;
            float cap     = ps.experienceCap;
            float needed  = cap - current + 1f;
            if (needed < 1f) needed = 1f;
            ps.AddXp(needed);
            Plugin.Log.LogInfo($"[ForceLevelUp] XP +{needed:F0}  ({current:F0} → {current + needed:F0} / cap {cap:F0})");
        }
    }
}
