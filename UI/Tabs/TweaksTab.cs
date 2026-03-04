using UnityEngine;
using BloodshedModToolkit.Tweaks;

namespace BloodshedModToolkit.UI.Tabs
{
    internal sealed class TweaksTab : IModTab
    {
        public void Draw(ModMenuContext ctx)
        {
            var l = ctx.L();
            var c = TweakState.Current;

            ctx.ScrollTweaks = GUILayout.BeginScrollView(ctx.ScrollTweaks, GUILayout.ExpandHeight(true));

            // DIFFICULTY ────────────────────────────────────────────────────────
            ctx.SectionHeader("DIFFICULTY");
            GUILayout.BeginHorizontal();
            ctx.PresetBtn(l.TweakMortal,     TweakPresetType.Mortal);
            ctx.PresetBtn(l.TweakHunter,     TweakPresetType.Hunter);
            ctx.PresetBtn(l.TweakSlayer,     TweakPresetType.Slayer);
            ctx.PresetBtn(l.TweakDemon,      TweakPresetType.Demon);
            ctx.PresetBtn(l.TweakApocalypse, TweakPresetType.Apocalypse);
            GUILayout.EndHorizontal();
            GUILayout.Space(2);

            // PLAYER ────────────────────────────────────────────────────────────
            ctx.SectionHeader("PLAYER");
            float prevHpMult = c.PlayerHpMult;
            c.PlayerHpMult    = ctx.SliderRow("HP",    c.PlayerHpMult,    0.10f, 4.00f);
            if (c.PlayerHpMult != prevHpMult) ctx.ScheduleHpRefresh();
            c.PlayerSpeedMult = ctx.SliderRow("Speed", c.PlayerSpeedMult, 0.50f, 3.00f);

            // WEAPON ────────────────────────────────────────────────────────────
            ctx.SectionHeader("WEAPON");
            c.WeaponDamageMult      = ctx.SliderRow("Damage", c.WeaponDamageMult,      0.50f, 3.00f);
            c.WeaponFireRateMult    = ctx.SliderRow("Fire",   c.WeaponFireRateMult,    0.50f, 3.00f);
            c.WeaponReloadSpeedMult = ctx.SliderRow("Reload", c.WeaponReloadSpeedMult, 0.50f, 3.00f);

            // ENEMY ─────────────────────────────────────────────────────────────
            ctx.SectionHeader("ENEMY");
            c.EnemyHpMult     = ctx.SliderRow("HP",     c.EnemyHpMult,     0.25f, 5.00f);
            float prevESpd = c.EnemySpeedMult;
            c.EnemySpeedMult  = ctx.SliderRow("Speed",  c.EnemySpeedMult,  0.25f, 3.00f);
            if (c.EnemySpeedMult != prevESpd) ctx.ScheduleEnemySpeedRefresh();
            c.EnemyDamageMult = ctx.SliderRow("Damage", c.EnemyDamageMult, 0.25f, 5.00f);

            // SPAWN ─────────────────────────────────────────────────────────────
            ctx.SectionHeader("SPAWN");
            c.SpawnCountMult = ctx.SliderRow("Count", c.SpawnCountMult, 0.25f, 4.00f);
            GUILayout.Label(l.SpawnNote, ctx.StSection!);

            GUILayout.Space(4);
            GUILayout.EndScrollView();
        }
    }
}
