using System;
using System.IO;

namespace BloodshedModToolkit.Coop.Net
{
    public enum PacketType : byte
    {
        Handshake     = 0x01,
        Heartbeat     = 0x02,
        EntitySpawn   = 0x10,
        EntityDespawn = 0x11,
        StateSnapshot = 0x12,
        PlayerState   = 0x20,
        XpGained      = 0x21,
        LevelUp       = 0x22,
        ItemSelected  = 0x23,
        WaveAdvance   = 0x30,
        DamageEvent   = 0x40,
        DamageRequest = 0x41,
        PlayerInput   = 0x50,
        FullSnapshot  = 0x51,
    }

    // ── 직렬화 헬퍼 ──────────────────────────────────────────────────────────
    public static class Packet
    {
        // 헤더: [PacketType(1)] [PayloadLength(2)] [Payload...]
        public static byte[] Encode(PacketType type, Action<BinaryWriter> writePayload)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write((byte)type);
            bw.Write((ushort)0);        // 길이 자리 확보
            int headerEnd = (int)ms.Position;
            writePayload(bw);
            int payloadLen = (int)ms.Length - headerEnd;
            ms.Seek(1, SeekOrigin.Begin);
            bw.Write((ushort)payloadLen);
            return ms.ToArray();
        }

        public static (PacketType type, byte[] payload) Decode(byte[] data)
        {
            using var ms = new MemoryStream(data);
            using var br = new BinaryReader(ms);
            var  type    = (PacketType)br.ReadByte();
            int  payLen  = br.ReadUInt16();
            var  payload = br.ReadBytes(payLen);
            return (type, payload);
        }
    }

    // ── 패킷 구조체 ───────────────────────────────────────────────────────────
    public struct HandshakePacket
    {
        public string Version;
        public ulong  SteamId;
        public bool   IsHost;

        public static byte[] Encode(string version, ulong steamId, bool isHost)
            => Packet.Encode(PacketType.Handshake, w =>
            {
                w.Write(version);
                w.Write(steamId);
                w.Write(isHost);
            });

        public static HandshakePacket Decode(byte[] payload)
        {
            using var br = new BinaryReader(new MemoryStream(payload));
            return new HandshakePacket
            {
                Version = br.ReadString(),
                SteamId = br.ReadUInt64(),
                IsHost  = br.ReadBoolean(),
            };
        }
    }

    public struct HeartbeatPacket
    {
        public static byte[] Encode()
            => Packet.Encode(PacketType.Heartbeat, _ => { });
    }

    public struct XpGainedPacket
    {
        public float Amount;
        public uint  SourceEntityIndex;

        public static byte[] Encode(float amount, uint srcIdx = 0)
            => Packet.Encode(PacketType.XpGained, w =>
            {
                w.Write(amount);
                w.Write(srcIdx);
            });

        public static XpGainedPacket Decode(byte[] payload)
        {
            using var br = new BinaryReader(new MemoryStream(payload));
            return new XpGainedPacket
            {
                Amount            = br.ReadSingle(),
                SourceEntityIndex = br.ReadUInt32(),
            };
        }
    }

    public struct LevelUpPacket
    {
        public int NewLevel;

        public static byte[] Encode(int level)
            => Packet.Encode(PacketType.LevelUp, w => w.Write(level));

        public static LevelUpPacket Decode(byte[] payload)
        {
            using var br = new BinaryReader(new MemoryStream(payload));
            return new LevelUpPacket { NewLevel = br.ReadInt32() };
        }
    }

    public struct ItemSelectedPacket
    {
        public int ItemIndex;

        public static byte[] Encode(int index)
            => Packet.Encode(PacketType.ItemSelected, w => w.Write(index));

        public static ItemSelectedPacket Decode(byte[] payload)
        {
            using var br = new BinaryReader(new MemoryStream(payload));
            return new ItemSelectedPacket { ItemIndex = br.ReadInt32() };
        }
    }

    public struct WaveAdvancePacket
    {
        public int  WaveIndex;
        public uint WaveGroupSeed;

        public static byte[] Encode(int waveIdx, uint seed)
            => Packet.Encode(PacketType.WaveAdvance, w =>
            {
                w.Write(waveIdx);
                w.Write(seed);
            });

        public static WaveAdvancePacket Decode(byte[] payload)
        {
            using var br = new BinaryReader(new MemoryStream(payload));
            return new WaveAdvancePacket
            {
                WaveIndex     = br.ReadInt32(),
                WaveGroupSeed = br.ReadUInt32(),
            };
        }
    }

    public struct EntitySpawnPacket
    {
        public uint   HostEntityIndex;
        public ushort EnemyTypeId;
        public float  PosX, PosY, PosZ;
        public uint   RandomSeed;

        public static byte[] Encode(uint idx, ushort typeId,
            float x, float y, float z, uint seed)
            => Packet.Encode(PacketType.EntitySpawn, w =>
            {
                w.Write(idx);  w.Write(typeId);
                w.Write(x);    w.Write(y);    w.Write(z);
                w.Write(seed);
            });

        public static EntitySpawnPacket Decode(byte[] payload)
        {
            using var br = new BinaryReader(new MemoryStream(payload));
            return new EntitySpawnPacket
            {
                HostEntityIndex = br.ReadUInt32(),
                EnemyTypeId     = br.ReadUInt16(),
                PosX = br.ReadSingle(), PosY = br.ReadSingle(), PosZ = br.ReadSingle(),
                RandomSeed      = br.ReadUInt32(),
            };
        }
    }

    public struct EntityDespawnPacket
    {
        public uint HostEntityIndex;

        public static byte[] Encode(uint idx)
            => Packet.Encode(PacketType.EntityDespawn, w => w.Write(idx));

        public static EntityDespawnPacket Decode(byte[] payload)
        {
            using var br = new BinaryReader(new MemoryStream(payload));
            return new EntityDespawnPacket { HostEntityIndex = br.ReadUInt32() };
        }
    }

    // PlayerState 패킷 — Phase 2 이상에서 전송 (20 Hz)
    public struct PlayerStatePacket
    {
        public ulong SteamId;
        public float PosX, PosY, PosZ;
        public float CurrentHp;
        public float MaxHp;
        public int   Level;
        public float Experience;
        public float ExperienceCap;

        public static byte[] Encode(ulong steamId, float px, float py, float pz,
            float hp, float maxHp, int level, float xp, float xpCap)
            => Packet.Encode(PacketType.PlayerState, w =>
            {
                w.Write(steamId);
                w.Write(px); w.Write(py); w.Write(pz);
                w.Write(hp); w.Write(maxHp);
                w.Write(level);
                w.Write(xp); w.Write(xpCap);
            });

        public static PlayerStatePacket Decode(byte[] payload)
        {
            using var br = new BinaryReader(new MemoryStream(payload));
            return new PlayerStatePacket
            {
                SteamId       = br.ReadUInt64(),
                PosX = br.ReadSingle(), PosY = br.ReadSingle(), PosZ = br.ReadSingle(),
                CurrentHp     = br.ReadSingle(),
                MaxHp         = br.ReadSingle(),
                Level         = br.ReadInt32(),
                Experience    = br.ReadSingle(),
                ExperienceCap = br.ReadSingle(),
            };
        }
    }

    // DamageRequest — Guest → Host
    public struct DamageRequestPacket
    {
        public uint  HostEntityIndex;
        public float Damage;

        public static byte[] Encode(uint hostIdx, float damage)
            => Packet.Encode(PacketType.DamageRequest, w =>
            {
                w.Write(hostIdx);
                w.Write(damage);
            });

        public static DamageRequestPacket Decode(byte[] payload)
        {
            using var br = new BinaryReader(new MemoryStream(payload));
            return new DamageRequestPacket
            {
                HostEntityIndex = br.ReadUInt32(),
                Damage          = br.ReadSingle(),
            };
        }
    }
}
