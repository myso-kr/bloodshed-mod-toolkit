using System.Collections.Generic;
using UnityEngine;
using com8com1.SCFPS;
using BloodshedModToolkit.I18n;
using BloodshedModToolkit.Tweaks;

namespace BloodshedModToolkit.UI.Overlay.Panels
{
    /// <summary>
    /// 활성 치트 목록과 버전을 표시하는 오버레이 패널.
    /// 클릭하면 메인 메뉴를 토글합니다.
    /// </summary>
    public sealed class StatusPanel : IOverlayPanel
    {
        public string     Id              => "status";
        public bool       Visible         => true;
        public EdgeInsets Padding         => EdgeInsets.All(OverlayManager.Margin);
        public float      BackgroundAlpha => 0.68f;

        /// <summary>
        /// 타이틀 1줄 + 활성 치트 N줄 × LineH + 상하 패딩.
        /// </summary>
        public float Height =>
            Padding.Vertical + (2 + CountActiveLines()) * OverlayManager.LineH; // +1 단축키 힌트 줄

        // ── 언어 조회 ───────────────────────────────────────────────────────────
        private GameSettings? _gs;

        private GameSettings? GS()
        {
            if (_gs != null && _gs.isActiveAndEnabled) return _gs;
            return _gs = Object.FindObjectOfType<GameSettings>();
        }

        private LangStrings Lang()
            => Strings.Get(GS()?.languageText ?? LocalizationManager.Language.English);

        // ── IOverlayPanel ───────────────────────────────────────────────────────
        public void Tick() { }

        public void Draw(UIContext ctx)
        {
            var   l    = Lang();
            float lh   = OverlayManager.LineH;
            string arrow = OverlayManager.IsMenuOpen ? "\u25b2" : "\u25bc";

            // 타이틀 행
            ctx.Label(OverlayStyle.Amber,
                      $"\u25c8 Mod Toolkit v{MyPluginInfo.PLUGIN_VERSION}  {arrow}",
                      OverlayStyle.Title!, lh, gap: 0f);

            // 활성 치트 항목
            foreach (var line in BuildLines(l))
                ctx.Label(OverlayStyle.Lime, line, OverlayStyle.Item!, lh, gap: 0f);

            // 단축키 힌트
            ctx.Label(OverlayStyle.Dim,
                      l.ShortcutHint,
                      OverlayStyle.Item!, lh, gap: 0f);
        }

        // ── 헬퍼 ────────────────────────────────────────────────────────────────
        private static int CountActiveLines()
        {
            int n = 0;
            if (CheatState.GodMode)            n++;
            if (CheatState.InfiniteGems)       n++;
            if (CheatState.InfiniteSkullCoins) n++;
            if (CheatState.MaxStats)           n++;
            if (CheatState.SpeedHack)          n++;
            if (CheatState.OneShotKill)        n++;
            if (CheatState.NoCooldown)         n++;
            if (CheatState.InfiniteRevive)     n++;
            if (CheatState.InfiniteAway)       n++;
            if (CheatState.NoReload)           n++;
            if (CheatState.RapidFire)          n++;
            if (CheatState.NoRecoil)           n++;
            if (CheatState.PerfectAim)         n++;
            if (TweakState.ActivePreset != TweakPresetType.Hunter) n++;
            return n;
        }

        private static List<string> BuildLines(LangStrings l)
        {
            var list = new List<string>(14);
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

            var preset = TweakState.ActivePreset;
            if (preset != TweakPresetType.Hunter)
                list.Add("\u25c6 " + PresetName(preset, l));

            return list;
        }

        private static string PresetName(TweakPresetType p, LangStrings l) => p switch
        {
            TweakPresetType.Mortal     => l.TweakMortal,
            TweakPresetType.Slayer     => l.TweakSlayer,
            TweakPresetType.Demon      => l.TweakDemon,
            TweakPresetType.Apocalypse => l.TweakApocalypse,
            _                          => l.TweakHunter,
        };
    }
}
