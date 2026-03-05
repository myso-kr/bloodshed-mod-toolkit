using System.Collections.Generic;
using Steamworks;
using BloodshedModToolkit.Coop.Debug;
using BloodshedModToolkit.Coop.Ecs;
using BloodshedModToolkit.Coop.Mission;

namespace BloodshedModToolkit.Coop
{
    public static class CoopState
    {
        public static bool IsEnabled      { get; set; }
        public static bool IsHost         { get; set; }
        public static bool IsConnected    { get; set; }

        public static CSteamID      LobbyId { get; set; } = CSteamID.Nil;
        public static List<CSteamID> Peers  { get; }      = new();

        public const string CoopVersion = "1.3.0";

        public static void InitAsHost()
            => CoopSessionManager.InitAsHost();

        public static void InitAsGuest()
            => CoopSessionManager.InitAsGuest();

        public static void Reset()
        {
            IsEnabled   = false;
            IsHost      = false;
            IsConnected = false;
            LobbyId     = CSteamID.Nil;
            Peers.Clear();
            EntityRegistry.Reset();
            MissionState.Reset();
            CoopSessionManager.Reset();
            PeerInfoStore.Reset();
        }
    }
}
