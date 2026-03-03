using System.Collections.Generic;
using Steamworks;
using UnityEngine;

namespace BloodshedModToolkit.Coop.Friends
{
    /// <summary>
    /// Steam 온라인 친구 목록을 캐싱합니다.
    /// UI에서 "새로고침" 버튼을 누를 때 수동으로 갱신됩니다.
    /// </summary>
    public struct FriendEntry
    {
        public CSteamID SteamId;
        public string   Name;
        public CSteamID LobbyId;   // 유효하면 참가 가능한 로비 존재
    }

    public static class FriendListCache
    {
        private static readonly List<FriendEntry> _entries = new();

        /// <summary>마지막 새로고침 결과 (읽기 전용).</summary>
        public static IReadOnlyList<FriendEntry> Entries => _entries;

        /// <summary>
        /// 마지막으로 Refresh() 를 호출한 Time.time.
        /// 초기값은 -1f (아직 새로고침 안 함).
        /// </summary>
        public static float LastRefreshTime { get; private set; } = -1f;

        /// <summary>
        /// Steam 친구 목록을 조회해 온라인(오프라인 제외) 친구만 캐시에 저장합니다.
        /// </summary>
        public static void Refresh()
        {
            _entries.Clear();

            // 현재 게임 AppID (하위 24비트 비교용)
            uint myAppId = SteamUtils.GetAppID().m_AppId;

            int count = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
            for (int i = 0; i < count; i++)
            {
                var id    = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
                var state = SteamFriends.GetFriendPersonaState(id);

                // 오프라인 / 숨김 친구는 표시하지 않음
                if (state == EPersonaState.k_EPersonaStateOffline ||
                    state == EPersonaState.k_EPersonaStateInvisible)
                    continue;

                // Bloodshed 실행 중인 친구만 표시
                bool inGame = SteamFriends.GetFriendGamePlayed(id, out var gameInfo);
                if (!inGame || (gameInfo.m_gameID.m_GameID & 0xFFFFFF) != myAppId)
                    continue;

                _entries.Add(new FriendEntry
                {
                    SteamId = id,
                    Name    = SteamFriends.GetFriendPersonaName(id),
                    LobbyId = gameInfo.m_steamIDLobby,
                });
            }

            LastRefreshTime = Time.time;
            Plugin.Log.LogInfo($"[FriendListCache] 새로고침 완료 — Bloodshed 플레이 중 {_entries.Count}명");
        }
    }
}
