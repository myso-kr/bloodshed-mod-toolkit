using Steamworks;
using UnityEngine.SceneManagement;

namespace BloodshedModToolkit.Coop.Role
{
    internal interface ICoopRole
    {
        bool IsHost { get; }

        // 연결 이벤트
        void OnPeerConnected(CSteamID peer);
        void OnPeerDisconnected(CSteamID peer);

        // 씬 이벤트
        void OnSceneLoaded(Scene scene, LoadSceneMode mode);

        // 미션 흐름 (역할별 유효 메서드만 동작, 나머지 no-op)
        void OnMissionBriefingReceived(string sceneName, int buildIndex); // Guest
        void OnGuestReadyReceived(ulong guestSteamId);                    // Host
        void OnMissionStartReceived(string sceneName, int buildIndex);    // Guest
        void OnMissionEndReceived(bool success);                          // Guest
    }
}
