using System;
using UnityEngine;
using Steamworks;
using BloodshedModToolkit.Coop;

namespace BloodshedModToolkit.Coop.Net
{
    internal sealed class P2PTransport
    {
        public const int ReliableChannel   = 0;
        public const int UnreliableChannel = 1;

        private const int MaxPacketSize = 4096;
        private readonly byte[] _recvBuf = new byte[MaxPacketSize];

        public void Send(CSteamID to, byte[] data, int channel)
        {
            var sendType = channel == UnreliableChannel
                ? EP2PSend.k_EP2PSendUnreliable
                : EP2PSend.k_EP2PSendReliable;
            SteamNetworking.SendP2PPacket(to, data, (uint)data.Length, sendType, channel);
        }

        public void SendReliable(CSteamID to, byte[] data)   => Send(to, data, ReliableChannel);
        public void SendUnreliable(CSteamID to, byte[] data) => Send(to, data, UnreliableChannel);

        public void BroadcastReliable(byte[] data)
        {
            foreach (var peer in CoopState.Peers)
                SendReliable(peer, data);
        }

        public void BroadcastUnreliable(byte[] data)
        {
            foreach (var peer in CoopState.Peers)
                SendUnreliable(peer, data);
        }

        /// <summary>
        /// 두 채널을 폴링하고 수신 패킷을 router에 디스패치.
        /// from이 알려진 Peer이면 _lastHeartbeat 갱신을 위해 콜백 호출.
        /// </summary>
        public void Poll(PacketRouter router, Action<CSteamID>? onReceiveFromPeer = null)
        {
            PollChannel(router, onReceiveFromPeer, ReliableChannel);
            PollChannel(router, onReceiveFromPeer, UnreliableChannel);
        }

        private void PollChannel(PacketRouter router, Action<CSteamID>? onReceiveFromPeer, int channel)
        {
            while (SteamNetworking.IsP2PPacketAvailable(out var size, channel))
            {
                Plugin.Log.LogDebug($"[P2PTransport] 수신 대기 패킷: ch={channel} size={size}");

                if (size > MaxPacketSize)
                {
                    Plugin.Log.LogWarning($"[P2PTransport] 패킷 크기 초과: {size} > {MaxPacketSize}");
                    SteamNetworking.ReadP2PPacket(_recvBuf, (uint)_recvBuf.Length,
                        out _, out _, channel);
                    continue;
                }

                bool ok = SteamNetworking.ReadP2PPacket(_recvBuf, size,
                    out uint read, out CSteamID from, channel);
                Plugin.Log.LogDebug($"[P2PTransport] ReadP2PPacket: ok={ok} read={read} from={from}");

                if (ok && read > 0)
                {
                    var data = new byte[read];
                    Buffer.BlockCopy(_recvBuf, 0, data, 0, (int)read);
                    if (CoopState.Peers.Contains(from))
                        onReceiveFromPeer?.Invoke(from);
                    router.Dispatch(from, data);
                }
            }
        }
    }
}
