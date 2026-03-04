using UnityEngine;
using BloodshedModToolkit.I18n;
using BloodshedModToolkit.UI.Overlay;

namespace BloodshedModToolkit.UI.Tabs
{
    internal sealed class CheatsTab : IModTab
    {
        public void Tick(ModMenuContext ctx) => GameActions.ApplyCheats(ctx);

        public void Draw(ModMenuContext ctx)
        {
            var l = ctx.L();
            ctx.ScrollCheats = GUILayout.BeginScrollView(ctx.ScrollCheats, GUILayout.ExpandHeight(true));

            // SURVIVAL ──────────────────────────────────────────────────────────
            ctx.SectionHeader("SURVIVAL");
            ctx.TwoCol(ref CheatState.GodMode,        l.GodMode,
                       ref CheatState.MaxStats,        l.MaxStats);
            ctx.TwoCol(ref CheatState.InfiniteRevive,  l.InfiniteRevive,
                       ref CheatState.InfiniteAway,    l.InfiniteAway);

            // ECONOMY ───────────────────────────────────────────────────────────
            ctx.SectionHeader("ECONOMY");
            ctx.TwoCol(ref CheatState.InfiniteGems,       l.InfiniteGems,
                       ref CheatState.InfiniteSkullCoins, l.InfiniteSkullCoins);

            // COMBAT ────────────────────────────────────────────────────────────
            ctx.SectionHeader("COMBAT");
            ctx.TwoCol(ref CheatState.OneShotKill, l.OneShotKill,
                       ref CheatState.NoCooldown,  l.NoCooldown);
            ctx.TwoCol(ref CheatState.RapidFire,   l.RapidFire,
                       ref CheatState.NoRecoil,    l.NoRecoil);
            ctx.TwoCol(ref CheatState.PerfectAim,  l.PerfectAim,
                       ref CheatState.NoReload,    l.NoReload);

            // MOVEMENT ──────────────────────────────────────────────────────────
            ctx.SectionHeader("MOVEMENT");
            CheatState.SpeedHack = ctx.Toggle(CheatState.SpeedHack, l.SpeedHack);
            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Format(l.SpeedLabel, CheatState.SpeedMultiplier.ToString("F1")),
                            ctx.StSliderName!, GUILayout.Width(110));
            CheatState.SpeedMultiplier = GUILayout.HorizontalSlider(
                CheatState.SpeedMultiplier, 1f, 20f);
            GUILayout.EndHorizontal();

            // OVERLAY ────────────────────────────────────────────────────────────
            ctx.SectionHeader("OVERLAY");
            GUILayout.BeginHorizontal();
            ctx.OverlayPosBtn(l.OverlayHidden,    OverlayPosition.Hidden);
            ctx.OverlayPosBtn(l.OverlayTopLeft,   OverlayPosition.TopLeft);
            ctx.OverlayPosBtn(l.OverlayTopCenter, OverlayPosition.TopCenter);
            ctx.OverlayPosBtn(l.OverlayTopRight,  OverlayPosition.TopRight);
            GUILayout.EndHorizontal();

            GUILayout.EndScrollView();

            // ── 액션 바 (스크롤 외부 고정) ──────────────────────────────────────
            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(l.ForceLevelUp, ctx.StActionBtn!)) GameActions.ForceLevelUp(ctx);
            if (GUILayout.Button(l.AddGems,       ctx.StActionBtn!)) GameActions.AddGems(ctx);
            if (GUILayout.Button(l.AddSkullCoins, ctx.StActionBtn!)) GameActions.AddSkullCoins(ctx);
            if (GUILayout.Button(l.HealFull,      ctx.StActionBtn!)) GameActions.HealFull(ctx);
            GUILayout.EndHorizontal();
            GUILayout.Space(3);
            if (GUILayout.Button(l.AllCheatsOff, ctx.StResetBtn!))
                CheatState.Initialize();
        }
    }
}
