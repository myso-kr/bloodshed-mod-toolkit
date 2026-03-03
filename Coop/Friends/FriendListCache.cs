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
        public bool     IsInGame;   // 현재 어떤 게임이든 플레이 중
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

            int count = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
            for (int i = 0; i < count; i++)
            {
                var id    = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
                var state = SteamFriends.GetFriendPersonaState(id);

                // 오프라인 / 숨김 친구는 표시하지 않음
                if (state == EPersonaState.k_EPersonaStateOffline ||
                    state == EPersonaState.k_EPersonaStateInvisible)
                    continue;

                bool inGame = SteamFriends.GetFriendGamePlayed(id, out _);

                _entries.Add(new FriendEntry
                {
                    SteamId  = id,
                    Name     = SteamFriends.GetFriendPersonaName(id),
                    IsInGame = inGame,
                });
            }

            LastRefreshTime = Time.time;
            Plugin.Log.LogInfo($"[FriendListCache] 새로고침 완료 — 온라인 {_entries.Count}명");
        }
    }
}
