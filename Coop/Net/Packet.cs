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
        MoneyUpdate   = 0x24,   // Host → Guest: 젬(money) 델타 동기화
        WaveAdvance   = 0x30,
        DamageRequest = 0x41,
        AttackEvent   = 0x42,   // 양방향: 공격 이벤트 (아바타 공격 애니메이션 동기화)
        FullSnapshot  = 0x51,

        // Phase 7 — 밸런스 설정 동기화
        TweakSync     = 0x60,

        // Co-op 디버그 정보 (3초 주기, 양방향)
        PeerInfo      = 0x61,

        // Phase 9 — 미션 진입 게이트
        MissionStart     = 0x70,   // Host → Guest: 씬 이름 + 빌드 인덱스
        PlayerReady      = 0x71,   // Guest → Host: 준비 완료
        MissionBriefing  = 0x72,   // Host → Guest: 미션 사전 알림 + 캐릭터 선택 요청
        MissionEnd       = 0x73,   // Host → Guest: 미션 종료 (성공/실패, MetaGame 복귀 신호)

        // Phase 10 — 인게임 채팅
        ChatMessage   = 0x80,   // 양방향: 채팅 메시지
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

    // MoneyUpdate — Host → Guest: 젬(money) 델타 동기화
    public static class MoneyUpdatePacket
    {
        public static byte[] Encode(float delta)
            => Packet.Encode(PacketType.MoneyUpdate, w => w.Write(delta));

        public static float Decode(byte[] payload)
        {
            using var br = new System.IO.BinaryReader(new System.IO.MemoryStream(payload));
            return br.ReadSingle();
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
    // v1.2.0: CharacterId(1B) 추가 → 총 46 bytes
    public struct PlayerStatePacket
    {
        public ulong SteamId;
        public float PosX, PosY, PosZ;
        public float CurrentHp;
        public float MaxHp;
        public int   Level;
        public float Experience;
        public float ExperienceCap;
        public float RotY;           // Y축 회전각 (도)
        public byte  WeaponClassId;  // 0=Melee 1=Pistol 2=Rifle 3=Launcher
        public byte  CharacterId;    // selectedCharacterData 이름 해시 % 16

        public static byte[] Encode(ulong steamId, float px, float py, float pz,
            float hp, float maxHp, int level, float xp, float xpCap,
            float rotY = 0f, byte weaponClassId = 0, byte charId = 0)
            => Packet.Encode(PacketType.PlayerState, w =>
            {
                w.Write(steamId);
                w.Write(px); w.Write(py); w.Write(pz);
                w.Write(hp); w.Write(maxHp);
                w.Write(level);
                w.Write(xp); w.Write(xpCap);
                w.Write(rotY);
                w.Write(weaponClassId);
                w.Write(charId);
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
                RotY          = br.ReadSingle(),
                WeaponClassId = br.ReadByte(),
                CharacterId   = br.ReadByte(),
            };
        }
    }

    // TweakSync — Host → Guest (밸런스 프리셋 전파)
    public struct TweakSyncPacket
    {
        public float PlayerHpMult, PlayerSpeedMult;
        public float WeaponDamageMult, WeaponFireRateMult, WeaponReloadSpeedMult;
        public float EnemyHpMult, EnemySpeedMult, EnemyDamageMult;
        public float SpawnCountMult;

        public static byte[] Encode(
            float playerHp,  float playerSpd,
            float wpnDmg,    float wpnFire,  float wpnReload,
            float eneHp,     float eneSpd,   float eneDmg,
            float spawn)
            => Packet.Encode(PacketType.TweakSync, w =>
            {
                w.Write(playerHp);  w.Write(playerSpd);
                w.Write(wpnDmg);    w.Write(wpnFire);   w.Write(wpnReload);
                w.Write(eneHp);     w.Write(eneSpd);    w.Write(eneDmg);
                w.Write(spawn);
            });

        public static TweakSyncPacket Decode(byte[] payload)
        {
            using var br = new BinaryReader(new MemoryStream(payload));
            return new TweakSyncPacket
            {
                PlayerHpMult          = br.ReadSingle(),
                PlayerSpeedMult       = br.ReadSingle(),
                WeaponDamageMult      = br.ReadSingle(),
                WeaponFireRateMult    = br.ReadSingle(),
                WeaponReloadSpeedMult = br.ReadSingle(),
                EnemyHpMult           = br.ReadSingle(),
                EnemySpeedMult        = br.ReadSingle(),
                EnemyDamageMult       = br.ReadSingle(),
                SpawnCountMult        = br.ReadSingle(),
            };
        }
    }

    // MissionStart — Host → Guest
    public static class MissionStartPacket
    {
        public static byte[] Encode(string sceneName, int buildIndex)
            => Packet.Encode(PacketType.MissionStart, w =>
            {
                w.Write(sceneName);
                w.Write(buildIndex);
            });

        public static (string sceneName, int buildIndex) Decode(byte[] payload)
        {
            using var ms = new System.IO.MemoryStream(payload);
            using var br = new System.IO.BinaryReader(ms);
            return (br.ReadString(), br.ReadInt32());
        }
    }

    // PlayerReady — Guest → Host
    public static class PlayerReadyPacket
    {
        public static byte[] Encode()
            => Packet.Encode(PacketType.PlayerReady, _ => { });
    }

    // MissionBriefing — Host → Guest
    public static class MissionBriefingPacket
    {
        public static byte[] Encode(string sceneName, int buildIndex)
            => Packet.Encode(PacketType.MissionBriefing, w =>
            {
                w.Write(sceneName);
                w.Write(buildIndex);
            });

        public static (string sceneName, int buildIndex) Decode(byte[] payload)
        {
            using var ms = new System.IO.MemoryStream(payload);
            using var br = new System.IO.BinaryReader(ms);
            return (br.ReadString(), br.ReadInt32());
        }
    }

    // MissionEnd — Host → Guest: 미션 종료 알림 (MetaGame 복귀 신호)
    public static class MissionEndPacket
    {
        public static byte[] Encode(bool success)
            => Packet.Encode(PacketType.MissionEnd, w => w.Write(success));

        public static bool Decode(byte[] payload)
        {
            using var br = new System.IO.BinaryReader(new System.IO.MemoryStream(payload));
            return br.ReadBoolean();
        }
    }

    // ChatMessage — 양방향
    public static class ChatMessagePacket
    {
        public static byte[] Encode(string senderName, string message)
            => Packet.Encode(PacketType.ChatMessage, w =>
            {
                w.Write(senderName);
                w.Write(message);
            });

        public static (string senderName, string message) Decode(byte[] payload)
        {
            using var ms = new System.IO.MemoryStream(payload);
            using var br = new System.IO.BinaryReader(ms, System.Text.Encoding.UTF8);
            return (br.ReadString(), br.ReadString());
        }
    }

    // PeerInfo — 양방향: 씬/캐릭터/미션 디버그 정보 (3초 주기)
    public static class PeerInfoPacket
    {
        public static byte[] Encode(string scene, string charName, string mission)
            => Packet.Encode(PacketType.PeerInfo, w =>
            {
                w.Write(scene);
                w.Write(charName);
                w.Write(mission);
            });

        public static (string scene, string charName, string mission) Decode(byte[] payload)
        {
            using var ms = new System.IO.MemoryStream(payload);
            using var br = new System.IO.BinaryReader(ms);
            return (br.ReadString(), br.ReadString(), br.ReadString());
        }
    }

    // AttackEvent — 양방향: 공격 이벤트 (아바타 공격 애니메이션 동기화)
    public struct AttackEventPacket
    {
        public ulong SteamId;

        public static byte[] Encode(ulong steamId)
            => Packet.Encode(PacketType.AttackEvent, w => w.Write(steamId));

        public static AttackEventPacket Decode(byte[] payload)
        {
            using var br = new BinaryReader(new MemoryStream(payload));
            return new AttackEventPacket { SteamId = br.ReadUInt64() };
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
