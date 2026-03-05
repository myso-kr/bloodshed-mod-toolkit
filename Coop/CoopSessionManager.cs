using Steamworks;
using UnityEngine.SceneManagement;
using BloodshedModToolkit.Coop.Role;

namespace BloodshedModToolkit.Coop
{
    public static class CoopSessionManager
    {
        internal static ICoopRole? Role { get; private set; }

        public static void InitAsHost()
        {
            Role = new HostRole();
            Plugin.Log.LogInfo("[CoopSessionManager] HostRole 초기화");
        }

        public static void InitAsGuest()
        {
            Role = new GuestRole();
            Plugin.Log.LogInfo("[CoopSessionManager] GuestRole 초기화");
        }

        public static void Reset()
        {
            Role = null;
            Plugin.Log.LogInfo("[CoopSessionManager] Role 리셋");
        }

        // 이벤트 전달 — MissionGateBehaviour, NetManager 등에서 호출
        public static void NotifySceneLoaded(Scene scene, LoadSceneMode mode)
            => Role?.OnSceneLoaded(scene, mode);

        public static void NotifyMissionBriefing(string scene, int idx)
            => Role?.OnMissionBriefingReceived(scene, idx);

        public static void NotifyGuestReady(ulong steamId)
            => Role?.OnGuestReadyReceived(steamId);

        public static void NotifyMissionStart(string scene, int idx)
            => Role?.OnMissionStartReceived(scene, idx);

        public static void NotifyMissionEnd(bool success)
            => Role?.OnMissionEndReceived(success);

        public static void NotifyPeerConnected(CSteamID peer)
            => Role?.OnPeerConnected(peer);

        public static void NotifyPeerDisconnected(CSteamID peer)
            => Role?.OnPeerDisconnected(peer);
    }
}
