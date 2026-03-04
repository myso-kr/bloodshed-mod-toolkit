using System.Collections.Generic;
using Steamworks;
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

        public const string CoopVersion = "1.0.0";

        public static void Reset()
        {
            IsEnabled   = false;
            IsHost      = false;
            IsConnected = false;
            LobbyId     = CSteamID.Nil;
            Peers.Clear();
            EntityRegistry.Reset();
            MissionState.Reset();
        }
    }
}
