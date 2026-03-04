using UnityEngine;
using com8com1.SCFPS;
using Enemies.EnemyAi;

namespace BloodshedModToolkit.UI
{
    internal static class GameActions
    {
        public static void AddGems(ModMenuContext ctx)
        {
            ctx.PS()?.SetMoney(CheatState.GemsFloor);
            var pd = ctx.PD();
            if (pd != null) pd.currentMoney = CheatState.GemsFloor;
            Plugin.Log.LogInfo("[AddGems] 젬 999999 지급");
        }

        public static void AddSkullCoins(ModMenuContext ctx)
        {
            var pd = ctx.PD();
            if (pd == null) { Plugin.Log.LogWarning("[SkullCoins] PersistentData 없음"); return; }
            pd.currentSuperTickets = CheatState.SkullCoinsFloor;
            Plugin.Log.LogInfo($"[SkullCoins] → {CheatState.SkullCoinsFloor}");
        }

        public static void HealFull(ModMenuContext ctx) => ctx.PS()?.RestoreHp(99999f);

        public static void ForceLevelUp(ModMenuContext ctx)
        {
            var ps = ctx.PS();
            if (ps == null) { Plugin.Log.LogWarning("[ForceLevelUp] PlayerStats 없음"); return; }
            float current = ps.experience;
            float cap     = ps.experienceCap;
            if (current >= cap)
            {
                Plugin.Log.LogInfo("[ForceLevelUp] 경험치 이미 최대 — 스킵");
                return;
            }
            ps.AddXp(cap - current + 1f);
            Plugin.Log.LogInfo($"[ForceLevelUp] +{cap - current + 1f:F0} XP");
        }

        public static void RefreshEnemySpeeds()
        {
            var enemies = Object.FindObjectsOfType<EnemyAbilityController>();
            if (enemies == null) return;
            foreach (var ec in enemies) ec.RefreshAgentSpeed();
        }

        public static void ApplyCheats(ModMenuContext ctx)
        {
            var ps = ctx.PS();
            var pd = ctx.PD();

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
    }
}
