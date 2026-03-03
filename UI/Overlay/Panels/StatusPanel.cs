using System.Collections.Generic;
using UnityEngine;
using com8com1.SCFPS;
using BloodshedModToolkit.I18n;
using BloodshedModToolkit.Tweaks;

namespace BloodshedModToolkit.UI.Overlay.Panels
{
    /// <summary>
    /// 활성 치트 목록과 Co-op 상태를 표시하는 오버레이 패널.
    /// 버전 표시 + 클릭하면 메인 메뉴를 토글합니다.
    /// </summary>
    public sealed class StatusPanel : IOverlayPanel
    {
        public string Id => "status";

        // 항상 표시
        public bool Visible => true;

        public float Height
        {
            get
            {
                int lines = CountActiveLines();
                return OverlayManager.Margin * 2 + (1 + lines) * OverlayManager.LineH;
            }
        }

        // ── 언어 조회 ───────────────────────────────────────────────────────
        private GameSettings? _gs;

        private GameSettings? GS()
        {
            if (_gs != null && _gs.isActiveAndEnabled) return _gs;
            return _gs = Object.FindObjectOfType<GameSettings>();
        }

        private LangStrings Lang()
            => Strings.Get(GS()?.languageText ?? LocalizationManager.Language.English);

        // ── IOverlayPanel ───────────────────────────────────────────────────
        public void Tick() { }   // 상태는 Draw 시점에 직접 읽음

        public void Draw(Rect rect)
        {
            var saved = GUI.color;
            OverlayStyle.DrawBg(rect, 0.68f);

            float cx = rect.x + OverlayManager.Margin;
            float cy = rect.y + OverlayManager.Margin;
            float cw = rect.width - OverlayManager.Margin * 2;

            // ── 타이틀 줄 ────────────────────────────────────────────────────
            string arrow = OverlayManager.IsMenuOpen ? "\u25b2" : "\u25bc";
            GUI.color = OverlayStyle.Amber;
            GUI.Label(
                new Rect(cx, cy, cw, OverlayManager.LineH),
                $"\u25c8 Mod Toolkit v{MyPluginInfo.PLUGIN_VERSION}  {arrow}",
                OverlayStyle.Title!);

            // ── 활성 치트 항목 ────────────────────────────────────────────────
            GUI.color = OverlayStyle.Lime;
            var l = Lang();
            foreach (var line in BuildLines(l))
            {
                cy += OverlayManager.LineH;
                GUI.Label(new Rect(cx, cy, cw, OverlayManager.LineH),
                    line, OverlayStyle.Item!);
            }

            GUI.color = saved;
        }

        // ── 헬퍼 ───────────────────────────────────────────────────────────
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
