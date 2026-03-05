using UnityEngine;
using Steamworks;
using BloodshedModToolkit.Coop;
using BloodshedModToolkit.Coop.Net;
using BloodshedModToolkit.Coop.Sync;
using BloodshedModToolkit.Coop.Mission;
using BloodshedModToolkit.Coop.Friends;

namespace BloodshedModToolkit.UI.Tabs
{
    internal sealed class CoopTab : IModTab
    {
        private string _lobbyIdInput = "";

        public void Draw(ModMenuContext ctx)
        {
            var l = ctx.L();
            ctx.ScrollCoop = GUILayout.BeginScrollView(ctx.ScrollCoop, GUILayout.ExpandHeight(true));

            if (!CoopState.IsEnabled)
            {
                // ── 연결 안됨 ────────────────────────────────────────────────
                ctx.SectionHeader("STATUS");
                GUILayout.Label($"\u25c6 {l.CoopStatusDisconnected}", ctx.StSliderName!);

                GUILayout.Space(4);
                ctx.SectionHeader("HOST");
                if (GUILayout.Button(l.CoopCreateLobby, ctx.StActionBtn!))
                    NetManager.Instance?.CreateLobby(4);

                GUILayout.Space(4);
                ctx.SectionHeader("JOIN");
                GUILayout.Label(l.CoopLobbyIdLabel, ctx.StSliderName!);
                GUILayout.Label(
                    _lobbyIdInput.Length > 0 ? _lobbyIdInput : l.CoopLobbyIdEmpty,
                    ctx.StSliderValue!);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(l.CoopPasteClipboard, ctx.StActionBtn!))
                    _lobbyIdInput = GUIUtility.systemCopyBuffer?.Trim() ?? "";
                if (_lobbyIdInput.Length > 0 && GUILayout.Button(l.CoopClear, ctx.StResetBtn!))
                    _lobbyIdInput = "";
                GUILayout.EndHorizontal();
                if (GUILayout.Button(l.CoopJoin, ctx.StActionBtn!))
                {
                    if (ulong.TryParse(_lobbyIdInput.Trim(), out var rawId))
                        NetManager.Instance?.JoinLobby(new CSteamID(rawId));
                    else
                        Plugin.Log.LogWarning("[CoopTab] 유효하지 않은 로비 ID");
                }

                DrawFriendsSection(ctx, l);
            }
            else
            {
                // ── 연결됨 ───────────────────────────────────────────────────
                ctx.SectionHeader("STATUS");
                string connText = CoopState.IsConnected
                    ? $"\u25cf {l.CoopConnected}"
                    : $"\u25cb {l.CoopWaiting}";
                string roleText = CoopState.IsHost ? "HOST" : "GUEST";
                GUILayout.Label($"\u25c6 {connText}  [{roleText}]", ctx.StSliderName!);
                GUILayout.Label($"\u25c6 Lobby ID: {CoopState.LobbyId}", ctx.StSliderName!);

                ctx.SectionHeader("PEERS");
                var myId   = SteamUser.GetSteamID();
                var myName = SteamFriends.GetFriendPersonaName(myId);
                GUILayout.Label($"  \u2605 {myName} ({roleText})", ctx.StSliderName!);

                foreach (var peer in CoopState.Peers)
                {
                    var peerName = SteamFriends.GetFriendPersonaName(peer);
                    if (PlayerSyncHandler.TryGetState((ulong)peer, out var ps))
                    {
                        int hpPct = ps.MaxHp > 0f
                            ? (int)(ps.CurrentHp / ps.MaxHp * 100f)
                            : 0;
                        GUILayout.Label(
                            $"  \u25cf {peerName}  Lv{ps.Level}  HP {hpPct}%",
                            ctx.StSliderName!);
                    }
                    else
                    {
                        GUILayout.Label($"  \u25cf {peerName}", ctx.StSliderName!);
                    }
                }

                DrawFriendsSection(ctx, l);

                // ── CO-OP DEBUG PANEL ─────────────────────────────────────────
                GUILayout.Space(4);
                bool panelOn = UI.CoopDebugPanel.Instance?.Visible == true;
                if (GUILayout.Button("CO-OP DEBUG PANEL", panelOn ? ctx.StPresetOn! : ctx.StPresetOff!))
                    if (UI.CoopDebugPanel.Instance != null)
                        UI.CoopDebugPanel.Instance.Visible = !UI.CoopDebugPanel.Instance.Visible;

                // ── XP SYNC ──────────────────────────────────────────────────
                GUILayout.Space(4);
                ctx.SectionHeader("XP SYNC");
                GUILayout.BeginHorizontal();
                XpModeBtn(ctx, l.CoopXpIndependent, XpShareMode.Independent);
                XpModeBtn(ctx, l.CoopXpReplicate,   XpShareMode.Replicate);
                XpModeBtn(ctx, l.CoopXpSplit,        XpShareMode.Split);
                GUILayout.EndHorizontal();
                GUILayout.Label(XpModeDescription(l), ctx.StSliderName!);

                // ── MISSION GATE ──────────────────────────────────────────────
                GUILayout.Space(4);
                ctx.SectionHeader("MISSION GATE");

                if (CoopState.IsHost)
                {
                    int ready = 0;
                    foreach (var v in MissionState.GuestReadyMap.Values) if (v) ready++;
                    GUILayout.Label($"GUESTS READY: {ready} / {CoopState.Peers.Count}", ctx.StSliderName!);
                    GUILayout.Label($"Session: {MissionState.SessionState}", ctx.StSliderName!);
                }
                else
                {
                    GUILayout.Label($"Session: {MissionState.SessionState}", ctx.StSliderName!);
                    switch (MissionState.Status)
                    {
                        case MissionStatus.Idle:
                            GUILayout.Label("대기 중 — 호스트 미션 진입 시 자동 입장", ctx.StSliderName!);
                            break;
                        case MissionStatus.WaitingForHost:
                            GUILayout.Label("씬 로드 완료 — 호스트 신호 대기 중", ctx.StSliderName!);
                            GUILayout.Label($"({MissionState.PendingSceneName})", ctx.StSliderName!);
                            break;
                        case MissionStatus.NeedsCharacterSelect:
                            GUILayout.Label("캐릭터를 선택하고 게임을 시작하세요", ctx.StSliderName!);
                            GUILayout.Label($"Target: {MissionState.PendingSceneName}", ctx.StSliderName!);
                            break;
                        case MissionStatus.Permitted:
                            GUILayout.Label("미션 로딩 중...", ctx.StSliderName!);
                            break;
                    }
                }

                GUILayout.Space(6);
                if (GUILayout.Button(l.CoopLeave, ctx.StResetBtn!))
                    NetManager.Instance?.LeaveLobby();
            }

            GUILayout.EndScrollView();
        }

        private static void XpModeBtn(ModMenuContext ctx, string label, XpShareMode mode)
        {
            bool active = CoopConfig.XpShare == mode;
            if (GUILayout.Button(label, active ? ctx.StPresetOn! : ctx.StPresetOff!))
            {
                CoopConfig.XpShare = mode;
                Plugin.Log.LogInfo($"[CoopTab] XpShareMode → {mode}");
            }
        }

        private static string XpModeDescription(I18n.LangStrings l) =>
            CoopConfig.XpShare switch
            {
                XpShareMode.Independent => l.CoopXpIndependentDesc,
                XpShareMode.Split       => l.CoopXpSplitDesc,
                _                       => l.CoopXpReplicateDesc,
            };

        private static void DrawFriendsSection(ModMenuContext ctx, I18n.LangStrings l)
        {
            GUILayout.Space(4);
            ctx.SectionHeader("FRIENDS");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(l.CoopRefresh, ctx.StActionBtn!))
                FriendListCache.Refresh();
            if (FriendListCache.LastRefreshTime >= 0f)
                GUILayout.Label(
                    string.Format(l.CoopFriendsOnlineCount, FriendListCache.Entries.Count),
                    ctx.StSliderName!);
            GUILayout.EndHorizontal();

            if (FriendListCache.LastRefreshTime < 0f)
            {
                GUILayout.Label($"  {l.CoopFriendsLoadPrompt}", ctx.StSliderName!);
                return;
            }

            if (FriendListCache.Entries.Count == 0)
            {
                GUILayout.Label($"  {l.CoopFriendsNone}", ctx.StSliderName!);
                return;
            }

            foreach (var f in FriendListCache.Entries)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"  {f.Name}", ctx.StSliderName!);

                if (!CoopState.IsEnabled && f.LobbyId.IsValid() &&
                    GUILayout.Button(l.CoopJoin, ctx.StActionBtn!, GUILayout.Width(44)))
                {
                    NetManager.Instance?.JoinLobby(f.LobbyId);
                    Plugin.Log.LogInfo($"[Friends] {f.Name} 의 로비에 참가");
                }

                if (CoopState.IsEnabled &&
                    GUILayout.Button(l.CoopInvite, ctx.StActionBtn!, GUILayout.Width(44)))
                {
                    SteamMatchmaking.InviteUserToLobby(CoopState.LobbyId, f.SteamId);
                    Plugin.Log.LogInfo($"[Friends] {f.Name} 초대 전송");
                }

                GUILayout.EndHorizontal();
            }
        }
    }
}
