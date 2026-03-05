using System;
using System.Collections.Generic;
using Steamworks;

namespace BloodshedModToolkit.Coop.Net
{
    public class PacketRouter
    {
        private readonly Dictionary<PacketType, Action<CSteamID, byte[]>> _handlers = new();

        public void Register(PacketType type, Action<CSteamID, byte[]> handler)
            => _handlers[type] = handler;

        public void Dispatch(CSteamID from, byte[] raw)
        {
            try
            {
                var (type, payload) = Packet.Decode(raw);
                BloodshedModToolkit.Coop.Debug.PeerInfoStore.OnPacketReceived((ulong)from, (byte)type);
                if (_handlers.TryGetValue(type, out var handler))
                    handler(from, payload);
                else
                    Plugin.Log.LogDebug($"[PacketRouter] 핸들러 없음: {type}");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[PacketRouter] 패킷 처리 오류:\n{ex}");
            }
        }
    }
}
