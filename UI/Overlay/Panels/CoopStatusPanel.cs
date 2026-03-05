using UnityEngine;
using Steamworks;
using BloodshedModToolkit.Coop;
using BloodshedModToolkit.Coop.Sync;

namespace BloodshedModToolkit.UI.Overlay.Panels
{
    /// <summary>
    /// Co-op 연결 중 피어의 HP / 레벨을 우상단 오버레이에 표시합니다.
    /// CoopState.IsConnected 이고 Peers > 0 일 때만 Visible.
    /// </summary>
    public sealed class CoopStatusPanel : IOverlayPanel
    {
        public string     Id              => "coop";
        public bool       Visible         => CoopState.IsConnected && CoopState.Peers.Count > 0;
        public EdgeInsets Padding         => EdgeInsets.All(OverlayManager.Margin);
        public float      BackgroundAlpha => 0.60f;

        private const float LH   = OverlayManager.LineH;
        private const float BarH = 4f;

        public float Height
        {
            get
            {
                var m = new UIContext(0, 0, OverlayManager.PanelWidth, Padding, drawing: false);
                Layout(m);
                return m.TotalHeight;
            }
        }

        public void Tick() { }
        public void Draw(UIContext ctx) => Layout(ctx);

        private static void Layout(UIContext ctx)
        {
            ctx.Label(OverlayStyle.Amber, "\u25c8 CO-OP", OverlayStyle.Title!, LH, gap: 3f);

            foreach (var peer in CoopState.Peers)
            {
                ulong  peerId   = (ulong)peer;
                string peerName = SteamFriends.GetFriendPersonaName(peer);

                if (PlayerSyncHandler.TryGetState(peerId, out var pst))
                {
                    if (pst.CurrentHp <= 0f)
                    {
                        // 피어 사망 — 붉은색으로 "DEAD" 표시
                        ctx.Label(new Color(0.90f, 0.20f, 0.10f),
                            $"\u2716 {peerName}  Lv{pst.Level}  DEAD",
                            OverlayStyle.Item!, LH, gap: BarH + 5f);
                    }
                    else
                    {
                        float ratio = pst.MaxHp > 0f ? pst.CurrentHp / pst.MaxHp : 0f;
                        Color fill  = ratio > 0.50f ? new Color(0.20f, 0.90f, 0.20f) :
                                      ratio > 0.25f ? new Color(0.90f, 0.80f, 0.10f) :
                                                      new Color(0.90f, 0.20f, 0.10f);

                        ctx.Label(OverlayStyle.Lime,
                            $"\u2605 {peerName}  Lv{pst.Level}  {pst.CurrentHp:F0}/{pst.MaxHp:F0}",
                            OverlayStyle.Item!, LH, gap: 1f);
                        ctx.Bar(ratio, fill, 0.9f, BarH, gap: 4f);
                    }
                }
                else
                {
                    ctx.Label(OverlayStyle.Dim,
                        $"\u25cb {peerName}  (connecting...)",
                        OverlayStyle.Item!, LH, gap: BarH + 5f);
                }
            }
        }
    }
}
