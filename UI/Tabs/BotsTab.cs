using UnityEngine;
using BloodshedModToolkit.Coop.Bots;
using BloodshedModToolkit.Coop.Sync;

namespace BloodshedModToolkit.UI.Tabs
{
    internal sealed class BotsTab : IModTab
    {
        public void Draw(ModMenuContext ctx)
        {
            var l = ctx.L();
            ctx.ScrollBots = GUILayout.BeginScrollView(ctx.ScrollBots, GUILayout.ExpandHeight(true));

            ctx.SectionHeader("BOT PLAYERS");
            BotState.Enabled = GUILayout.Toggle(BotState.Enabled,
                BotState.Enabled ? l.BotPlayersOn : l.BotPlayersOff,
                BotState.Enabled ? ctx.StToggleOn! : ctx.StToggleOff!);

            if (BotState.Enabled)
            {
                GUILayout.Space(4);
                ctx.SectionHeader("COUNT");
                GUILayout.BeginHorizontal();
                for (int n = 1; n <= 3; n++)
                {
                    bool active = BotState.Count == n;
                    if (GUILayout.Button(n.ToString(), active ? ctx.StPresetOn! : ctx.StPresetOff!))
                        BotState.Count = n;
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(4);
                ctx.SectionHeader("STATUS");
                var bots = BotManager.Instance?.GetBots();
                if (bots == null || bots.Count == 0)
                    GUILayout.Label(l.BotStatusEmpty, ctx.StSliderName!);
                else
                    foreach (var bot in bots)
                    {
                        int hp = bot.MaxHp > 0 ? (int)(bot.CurrentHp / bot.MaxHp * 100f) : 0;
                        GUILayout.Label(
                            $"  \u25cf {BotState.BotNames[bot.BotIndex]}  " +
                            $"Lv{bot.Level}  HP {hp}%  " +
                            $"({bot.Position.x:F1}, {bot.Position.y:F1}, {bot.Position.z:F1})",
                            ctx.StSliderName!);
                    }

                GUILayout.Space(4);
                ctx.SectionHeader("RENDERER");
                int avatarCount = PlayerSyncHandler.States.Count;
                GUILayout.Label(string.Format(l.BotTracking, avatarCount), ctx.StSliderName!);
            }
            else
                GUILayout.Label(l.BotDisabledNote, ctx.StSliderName!);

            GUILayout.EndScrollView();
        }
    }
}
