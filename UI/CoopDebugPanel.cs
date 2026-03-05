using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using Steamworks;
using com8com1.SCFPS;
using BloodshedModToolkit.Coop;
using BloodshedModToolkit.Coop.Debug;
using BloodshedModToolkit.Coop.Mission;
using BloodshedModToolkit.Coop.Net;
using BloodshedModToolkit.Coop.Sync;

namespace BloodshedModToolkit.UI
{
    public class CoopDebugPanel : MonoBehaviour
    {
        public CoopDebugPanel(IntPtr ptr) : base(ptr) { }

        public static CoopDebugPanel? Instance { get; private set; }

        private bool  _visible;
        public  bool  Visible { get => _visible; set => _visible = value; }

        private Rect    _windowRect = new Rect(0, 20, 400, 460);
        private Vector2 _scroll;
        private bool    _rectInit;

        private float _selfAccum;
        private float _broadcastAccum;

        private bool      _stylesReady;
        private GUIStyle? _stHeader;
        private GUIStyle? _stRow;
        private GUIStyle? _stSmall;

        private static readonly Dictionary<byte, string> _typeAbbr = new()
        {
            [(byte)PacketType.Handshake]      = "HS",
            [(byte)PacketType.Heartbeat]      = "HB",
            [(byte)PacketType.EntitySpawn]    = "ES",
            [(byte)PacketType.EntityDespawn]  = "ED",
            [(byte)PacketType.StateSnapshot]  = "SS",
            [(byte)PacketType.PlayerState]    = "PS",
            [(byte)PacketType.XpGained]       = "XP",
            [(byte)PacketType.LevelUp]        = "LU",
            [(byte)PacketType.ItemSelected]   = "IS",
            [(byte)PacketType.MoneyUpdate]    = "MU",
            [(byte)PacketType.WaveAdvance]    = "WA",
            [(byte)PacketType.DamageRequest]  = "DR",
            [(byte)PacketType.AttackEvent]    = "AE",
            [(byte)PacketType.FullSnapshot]   = "FS",
            [(byte)PacketType.TweakSync]      = "TS",
            [(byte)PacketType.PeerInfo]       = "PI",
            [(byte)PacketType.MissionStart]   = "MS",
            [(byte)PacketType.PlayerReady]    = "PR",
            [(byte)PacketType.MissionBriefing]= "MB",
            [(byte)PacketType.MissionEnd]     = "ME",
            [(byte)PacketType.ChatMessage]    = "CM",
        };

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Update()
        {
            _selfAccum      += Time.deltaTime;
            _broadcastAccum += Time.deltaTime;

            if (_selfAccum >= 1f)
            {
                _selfAccum = 0f;
                RefreshSelfInfo();
            }

            if (CoopState.IsConnected && _broadcastAccum >= 3f)
            {
                _broadcastAccum = 0f;
                BroadcastPeerInfo();
            }
        }

        private void RefreshSelfInfo()
        {
            PeerInfoStore.SelfScene         = SceneManager.GetActiveScene().name;
            PeerInfoStore.SelfCharacterName = UnityEngine.Object.FindObjectOfType<SessionSettings>()
                                                             ?.selectedCharacterData?.name ?? "";
            PeerInfoStore.SelfMissionScene  = CoopState.IsHost
                ? MissionState.HostCurrentScene
                : MissionState.PendingSceneName;
        }

        private void BroadcastPeerInfo()
        {
            NetManager.Instance?.BroadcastReliable(PeerInfoPacket.Encode(
                PeerInfoStore.SelfScene,
                PeerInfoStore.SelfCharacterName,
                PeerInfoStore.SelfMissionScene));
        }

        void OnGUI()
        {
            if (!_visible) return;
            EnsureStyles();

            if (!_rectInit)
            {
                _rectInit = true;
                _windowRect.x = Screen.width - _windowRect.width - 20f;
            }

            _windowRect = GUI.Window(99, _windowRect,
                (GUI.WindowFunction)DrawWindow,
                "Co-op Debug  " + CoopState.CoopVersion);
        }

        private void DrawWindow(int id)
        {
            if (GUI.Button(new Rect(_windowRect.width - 24f, 2f, 20f, 18f), "X"))
            {
                _visible = false;
                return;
            }

            // Session status row
            string sessState = MissionState.SessionState.ToString();
            int    peerCount = CoopState.Peers.Count;
            GUILayout.Label($"Session: {sessState}  Peers: {peerCount}", _stSmall!);

            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));

            // ── YOU ──────────────────────────────────────────────────────────
            string roleStr = CoopState.IsHost ? "Host" : "Guest";
            var    myId    = SteamUser.GetSteamID();
            var    myName  = SteamFriends.GetFriendPersonaName(myId);
            GUILayout.Label($"── YOU ({roleStr}) ──────────────────────────", _stHeader!);
            GUILayout.Label($"  {myName}  [{(ulong)myId}]", _stRow!);
            GUILayout.Label($"  Scene:   {PeerInfoStore.SelfScene}", _stRow!);
            GUILayout.Label($"  Char:    {PeerInfoStore.SelfCharacterName}", _stRow!);
            GUILayout.Label($"  Mission: {PeerInfoStore.SelfMissionScene}", _stRow!);

            var ps = UnityEngine.Object.FindObjectOfType<PlayerStats>();
            if (ps != null)
            {
                GUILayout.Label(
                    $"  HP: {ps.CurrentHp:F0}/{ps.MaxHp:F0}  Lv{ps.level}  XP: {ps.experience:F0}/{ps.experienceCap:F0}",
                    _stRow!);
            }

            // ── PEERS ────────────────────────────────────────────────────────
            foreach (var peer in CoopState.Peers)
            {
                ulong peerId   = (ulong)peer;
                var   peerName = SteamFriends.GetFriendPersonaName(peer);
                string lastStr = "?";
                if (PeerInfoStore.TryGetPeer(peerId, out var peerDebug))
                {
                    float elapsed = Time.time - peerDebug.LastSeenTime;
                    lastStr = $"{elapsed:F1}s";
                }

                GUILayout.Label($"── {peerName}  Last: {lastStr} ──────────────────", _stHeader!);
                GUILayout.Label($"  [{peerId}]", _stSmall!);

                if (PeerInfoStore.TryGetPeer(peerId, out var pd))
                {
                    GUILayout.Label($"  Scene:   {pd.SceneName}", _stRow!);
                    GUILayout.Label($"  Char:    {pd.CharacterName}", _stRow!);
                    GUILayout.Label($"  Mission: {pd.MissionScene}", _stRow!);
                }

                if (PlayerSyncHandler.TryGetState(peerId, out var pst))
                {
                    GUILayout.Label(
                        $"  HP: {pst.CurrentHp:F0}/{pst.MaxHp:F0}  Lv{pst.Level}  XP: {pst.Experience:F0}/{pst.ExperienceCap:F0}",
                        _stRow!);
                    GUILayout.Label(
                        $"  Pos: ({pst.PosX:F1}, {pst.PosY:F1}, {pst.PosZ:F1})  Wpn: {WcAbbr(pst.WeaponClassId)}",
                        _stRow!);
                }
            }

            // ── NETWORK ──────────────────────────────────────────────────────
            if (CoopState.Peers.Count > 0)
            {
                GUILayout.Label("── NETWORK ──────────────────────────────────", _stHeader!);
                foreach (var peer in CoopState.Peers)
                {
                    ulong peerId   = (ulong)peer;
                    var   peerName = SteamFriends.GetFriendPersonaName(peer);
                    var   rxCounts = PeerInfoStore.GetRxCounts(peerId);
                    var   sb       = new StringBuilder();
                    sb.Append($"  RX [{peerName}]: ");
                    foreach (var (typeB, count) in rxCounts)
                    {
                        if (_typeAbbr.TryGetValue(typeB, out var abbr))
                            sb.Append($"{abbr}:{count}  ");
                        else
                            sb.Append($"0x{typeB:X2}:{count}  ");
                    }
                    GUILayout.Label(sb.ToString(), _stSmall!);
                }
            }

            GUILayout.EndScrollView();
            GUI.DragWindow(new Rect(0, 0, _windowRect.width - 24f, 22f));
        }

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            _stHeader = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Bold };
            _stHeader.normal.textColor = new Color(0.9f, 0.85f, 0.35f);

            _stRow = new GUIStyle(GUI.skin.label) { fontSize = 11 };
            _stRow.normal.textColor = Color.white;

            _stSmall = new GUIStyle(GUI.skin.label) { fontSize = 10 };
            _stSmall.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
        }

        private static string WcAbbr(byte wc) => wc switch
        {
            0 => "Melee",
            1 => "Pistol",
            2 => "Rifle",
            3 => "Launcher",
            _ => "?",
        };
    }
}
