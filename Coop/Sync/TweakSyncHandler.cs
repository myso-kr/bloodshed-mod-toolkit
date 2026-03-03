using Steamworks;
using UnityEngine;
using com8com1.SCFPS;
using Enemies.EnemyAi;
using BloodshedModToolkit.Tweaks;
using BloodshedModToolkit.Coop.Net;

namespace BloodshedModToolkit.Coop.Sync
{
    /// <summary>
    /// Host ↔ Guest TweakConfig 동기화.
    /// TweakState.Apply() 완료 후 Host에서 호출 → 모든 Guest에게 전파.
    /// Guest 참가 시 Handshake 응답에서 즉시 현재 설정 전송.
    /// </summary>
    public static class TweakSyncHandler
    {
        /// <summary>TweakState.Apply() 에서 호출 — 연결된 경우에만 브로드캐스트.</summary>
        public static void OnPresetApplied()
        {
            if (!CoopState.IsHost || !CoopState.IsConnected) return;
            Broadcast();
        }

        /// <summary>현재 TweakConfig를 모든 Peer에게 전송.</summary>
        public static void Broadcast()
        {
            var net = NetManager.Instance;
            if (net == null) return;
            net.BroadcastReliable(BuildPacket());
            Plugin.Log.LogInfo("[TweakSync] TweakConfig 브로드캐스트");
        }

        /// <summary>특정 Peer에게만 현재 TweakConfig 전송 (신규 Guest 연결 시).</summary>
        public static void SendTo(CSteamID peer)
        {
            var net = NetManager.Instance;
            if (net == null) return;
            net.SendReliable(peer, BuildPacket());
        }

        /// <summary>Host에서 받은 TweakSyncPacket을 Guest에 적용.</summary>
        public static void ApplyFromPacket(TweakSyncPacket pkt)
        {
            var c = TweakState.Current;
            c.PlayerHpMult          = pkt.PlayerHpMult;
            c.PlayerSpeedMult       = pkt.PlayerSpeedMult;
            c.WeaponDamageMult      = pkt.WeaponDamageMult;
            c.WeaponFireRateMult    = pkt.WeaponFireRateMult;
            c.WeaponReloadSpeedMult = pkt.WeaponReloadSpeedMult;
            c.EnemyHpMult           = pkt.EnemyHpMult;
            c.EnemySpeedMult        = pkt.EnemySpeedMult;
            c.EnemyDamageMult       = pkt.EnemyDamageMult;
            c.SpawnCountMult        = pkt.SpawnCountMult;

            // 즉시 반영
            Object.FindObjectOfType<PlayerStats>()?.RecalculateStats();
            var enemies = Object.FindObjectsOfType<EnemyAbilityController>();
            if (enemies != null)
                foreach (var ec in enemies) ec.RefreshAgentSpeed();

            Plugin.Log.LogInfo("[TweakSync] Host TweakConfig 적용 완료");
        }

        private static byte[] BuildPacket()
        {
            var c = TweakState.Current;
            return TweakSyncPacket.Encode(
                c.PlayerHpMult,    c.PlayerSpeedMult,
                c.WeaponDamageMult, c.WeaponFireRateMult, c.WeaponReloadSpeedMult,
                c.EnemyHpMult,     c.EnemySpeedMult, c.EnemyDamageMult,
                c.SpawnCountMult);
        }
    }
}
