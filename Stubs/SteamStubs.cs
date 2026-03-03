// CI 컴파일 전용 스텁 — 런타임에서는 BepInEx/interop/com.rlabrecque.steamworks.net.dll이 사용됩니다.
// csproj의 <Compile Remove="Stubs\**"> 로 로컬 빌드에서는 자동 제외됩니다.
using System;

namespace Steamworks
{
    public struct CSteamID : IEquatable<CSteamID>
    {
        private ulong _id;
        public CSteamID(ulong id) { _id = id; }
        public ulong m_SteamID => _id;           // 실제 Steamworks.NET 필드명 (대문자 S)
        public bool IsValid() => _id != 0;
        public static readonly CSteamID Nil = new CSteamID(0);
        public bool Equals(CSteamID other) => _id == other._id;
        public override bool Equals(object? obj) => obj is CSteamID s && Equals(s);
        public override int  GetHashCode()      => _id.GetHashCode();
        public static bool operator ==(CSteamID a, CSteamID b) => a._id == b._id;
        public static bool operator !=(CSteamID a, CSteamID b) => a._id != b._id;
        public static implicit operator ulong(CSteamID that) => that._id;
        public override string ToString() => _id.ToString();
    }

    public struct SteamAPICall_t { public ulong m_SteamAPICall; }

    public enum ELobbyType
    {
        k_ELobbyTypeFriendsOnly = 1,
        k_ELobbyTypePublic      = 2,
        k_ELobbyTypeInvisible   = 3,
    }

    public enum EP2PSend
    {
        k_EP2PSendUnreliable            = 0,
        k_EP2PSendUnreliableNoDelay     = 1,
        k_EP2PSendReliable              = 2,
        k_EP2PSendReliableWithBuffering = 3,
    }

    public enum EFriendFlags
    {
        k_EFriendFlagNone      = 0x0000,
        k_EFriendFlagBlocked   = 0x0001,
        k_EFriendFlagImmediate = 0x0004,   // 일반 친구
        k_EFriendFlagAll       = 0xFFFF,
    }

    public enum EPersonaState
    {
        k_EPersonaStateOffline    = 0,
        k_EPersonaStateOnline     = 1,
        k_EPersonaStateBusy       = 2,
        k_EPersonaStateAway       = 3,
        k_EPersonaStateSnooze     = 4,
        k_EPersonaStateInvisible  = 7,
        k_EPersonaStateMax        = 8,
    }

    public struct CGameID
    {
        public ulong m_GameID;
    }

    public struct FriendGameInfo_t
    {
        public CGameID  m_gameID;
        public CSteamID m_steamIDLobby;
    }

    public enum EResult
    {
        k_EResultOK   = 1,
        k_EResultFail = 2,
    }

    public enum EChatRoomEnterResponse
    {
        k_EChatRoomEnterResponseSuccess = 1,
    }

    // ── 콜백 구조체 ──────────────────────────────────────────────────────────
    public struct LobbyCreated_t
    {
        public EResult m_eResult;
        public ulong   m_ulSteamIDLobby;
    }

    public struct LobbyEnter_t
    {
        public ulong m_ulSteamIDLobby;
        public uint  m_rgfChatPermissions;
        public bool  m_bLocked;
        public uint  m_EChatRoomEnterResponse;  // EChatRoomEnterResponse enum as uint
    }

    public struct GameLobbyJoinRequested_t { public CSteamID m_steamIDLobby; }
    public struct P2PSessionRequest_t      { public CSteamID m_steamIDRemote; }
    public struct P2PSessionConnectFail_t  { public CSteamID m_steamIDRemote; public byte m_eP2PSessionError; }

    // ── Steam API ─────────────────────────────────────────────────────────────
    public static class SteamMatchmaking
    {
        public static SteamAPICall_t CreateLobby(ELobbyType eLobbyType, int cMaxMembers) => default;
        public static SteamAPICall_t JoinLobby(CSteamID steamIDLobby) => default;
        public static void      LeaveLobby(CSteamID steamIDLobby) { }
        public static int       GetNumLobbyMembers(CSteamID steamIDLobby) => 0;
        public static CSteamID  GetLobbyMemberByIndex(CSteamID steamIDLobby, int iMember) => CSteamID.Nil;
        public static CSteamID  GetLobbyOwner(CSteamID steamIDLobby) => CSteamID.Nil;
        public static bool      SetLobbyData(CSteamID steamIDLobby, string pchKey, string pchValue) => false;
        public static string    GetLobbyData(CSteamID steamIDLobby, string pchKey) => "";
        public static bool      InviteUserToLobby(CSteamID steamIDLobby, CSteamID steamIDInvitee) => false;
    }

    public static class SteamNetworking
    {
        public static bool SendP2PPacket(CSteamID steamIDRemote, byte[] pubData,
            uint cubData, EP2PSend eP2PSendType, int nChannel = 0) => false;
        public static bool IsP2PPacketAvailable(out uint pcubMsgSize, int nChannel = 0)
        { pcubMsgSize = 0; return false; }
        public static bool ReadP2PPacket(byte[] pubDest, uint cubDest,
            out uint pcubMsgSize, out CSteamID psteamIDRemote, int nChannel = 0)
        { pcubMsgSize = 0; psteamIDRemote = CSteamID.Nil; return false; }
        public static bool AcceptP2PSessionWithUser(CSteamID steamIDRemote) => false;
        public static bool CloseP2PSessionWithUser(CSteamID steamIDRemote) => false;
    }

    public static class SteamUser
    {
        public static CSteamID GetSteamID() => CSteamID.Nil;
    }

    public static class SteamFriends
    {
        public static string        GetFriendPersonaName(CSteamID steamIDFriend) => "";
        public static int           GetFriendCount(EFriendFlags iFriendFlags) => 0;
        public static CSteamID      GetFriendByIndex(int iFriend, EFriendFlags iFriendFlags) => CSteamID.Nil;
        public static EPersonaState GetFriendPersonaState(CSteamID steamIDFriend) => EPersonaState.k_EPersonaStateOffline;
        public static bool          GetFriendGamePlayed(CSteamID steamIDFriend, out FriendGameInfo_t pFriendGameInfo)
        { pFriendGameInfo = default; return false; }
    }

    // ── 콜백 / 콜 리절트 래퍼 ────────────────────────────────────────────────
    // IL2CPP interop에서 DispatchDelegate는 클래스 + Action<T> 암묵적 변환 연산자를 가집니다
    public class Callback<T>
    {
        public class DispatchDelegate
        {
            private readonly Action<T> _action;
            public DispatchDelegate(Action<T> action) { _action = action; }
            public static implicit operator DispatchDelegate(Action<T> a) => new DispatchDelegate(a);
        }
        public static Callback<T> Create(DispatchDelegate func) => new Callback<T>();
        public void Dispose() { }
    }

    public class CallResult<T>
    {
        public class APIDispatchDelegate
        {
            private readonly Action<T, bool> _action;
            public APIDispatchDelegate(Action<T, bool> action) { _action = action; }
            public static implicit operator APIDispatchDelegate(Action<T, bool> a) => new APIDispatchDelegate(a);
        }
        public static CallResult<T> Create(APIDispatchDelegate func) => new CallResult<T>();
        public void Set(SteamAPICall_t hAPICall) { }
        public void Dispose() { }
    }
}
