using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Steamworks;
using com8com1.SCFPS;
using Enemies.EnemyAi;
using BloodshedModToolkit.I18n;
using BloodshedModToolkit.Tweaks;
using BloodshedModToolkit.Coop;
using BloodshedModToolkit.Coop.Net;
using BloodshedModToolkit.Coop.Sync;
using BloodshedModToolkit.Coop.Mission;
using BloodshedModToolkit.Coop.Friends;
using BloodshedModToolkit.Coop.Bots;
using BloodshedModToolkit.Coop.Renderer;
using BloodshedModToolkit.UI.Overlay;
using BloodshedModToolkit.UI.Overlay.Panels;

namespace BloodshedModToolkit.UI
{
    public class ModMenu : MonoBehaviour
    {
        public ModMenu(System.IntPtr ptr) : base(ptr) { }

        // ── 탭 / 윈도우 상태 ─────────────────────────────────────────────────────
        private enum Tab { Cheats, Tweaks, Coop, Bots, Debug }
        private Tab  _activeTab  = Tab.Cheats;
        private bool _visible    = false;
        private Rect _windowRect = new Rect(20, 20, 390, 470);

        // ── 스크롤 ────────────────────────────────────────────────────────────────
        private Vector2 _scrollCheats = Vector2.zero;
        private Vector2 _scrollTweaks = Vector2.zero;
        private Vector2 _scrollCoop   = Vector2.zero;
        private Vector2 _scrollBots   = Vector2.zero;
        private Vector2 _scrollDebug  = Vector2.zero;

        // ── Co-op UI 상태 ─────────────────────────────────────────────────────────
        private string _lobbyIdInput    = "";
        private string _debugSceneInput = "";

        // ── Debug META SELECTION 캐시 ─────────────────────────────────────────────
        private string _debugMetaSaveSlot = "?";
        private string _debugMetaMission  = "?";
        private string _debugMetaChar     = "?";
        private SaveDataManager? _debugSdm;

        // ── Debug CHARACTER / MISSION 명시 선택 ───────────────────────────────────
        private com8com1.SCFPS.PlayableCharacterData?  _debugSelectedChar    = null;
        private com8com1.SCFPS.MissionAsset?           _debugSelectedMission = null;
        private com8com1.SCFPS.PlayableCharacterData[] _debugCharList        = System.Array.Empty<com8com1.SCFPS.PlayableCharacterData>();
        private com8com1.SCFPS.MissionAsset[]          _debugMissionList     = System.Array.Empty<com8com1.SCFPS.MissionAsset>();
        private bool _debugScanned = false;  // 최초 1회 자동 스캔 완료 여부

        // ── Debug 씬 로드 검증 오류 ───────────────────────────────────────────────
        private string _debugSceneValidationError = "";

        // GC 방지 — SceneManager.sceneLoaded IL2CPP delegate
        private System.Action<Scene, LoadSceneMode>? _onSceneLoaded;

        private static readonly Dictionary<string, string[]> _sceneMissionHints =
            new(System.StringComparer.OrdinalIgnoreCase)
        {
            { "Graveyard",           new[] { "graveyard" } },
            { "Village",             new[] { "village" } },
            { "01_Forest",           new[] { "forest" } },
            { "01_AltarOfSacrifice", new[] { "altar", "sacrifice" } },
            { "01_Challenge_Flynn",      new[] { "flynn" } },
            { "01_Challenge_Jared",      new[] { "jared" } },
            { "01_Challenge_Seraphina",  new[] { "seraphina" } },
            { "02_Boat",             new[] { "boat" } },
            { "02_StiltVillage",     new[] { "stilt", "harbour" } },
            { "02_CultistTemple",    new[] { "cultist", "temple" } },
            { "02_Boss",             new[] { "boss", "goddess" } },
            { "02_Challenge_Maze",   new[] { "maze" } },
            { "02_Challenge_Final",  new[] { "final" } },
            { "03_SkyCathedral",     new[] { "skycathedral", "sky" } },
        };

        // ── 슬라이더 디바운스 타이머 (Time.time 기준 예약 시각, 0 = 예약 없음) ──────
        // 드래그 중 매 OnGUI 이벤트마다 RefreshEnemySpeeds/RecalculateStats 를 반복
        // 호출하는 성능 문제를 방지합니다. 마지막 변경 후 kRefreshDelay 초 뒤에 실행.
        private const float kRefreshDelay = 0.25f;
        private float _hpRefreshAt     = 0f;
        private float _eSpeedRefreshAt = 0f;

        // ── 컴포넌트 캐시 ─────────────────────────────────────────────────────────
        private PlayerStats?    _ps;
        private PersistentData? _pd;
        private GameSettings?   _gs;

        private PlayerStats? PS()
        {
            if (_ps != null && _ps.isActiveAndEnabled) return _ps;
            return _ps = FindObjectOfType<PlayerStats>();
        }
        private PersistentData? PD()
        {
            if (_pd != null && _pd.isActiveAndEnabled) return _pd;
            return _pd = FindObjectOfType<PersistentData>();
        }
        private GameSettings? GS()
        {
            if (_gs != null && _gs.isActiveAndEnabled) return _gs;
            return _gs = FindObjectOfType<GameSettings>();
        }
        private LangStrings L()
            => Strings.Get(GS()?.languageText ?? LocalizationManager.Language.English);

        // ════════════════════════════════════════════════════════════════════════
        // GUIStyle 캐시 — OnGUI 내부에서 최초 1회만 초기화
        // ════════════════════════════════════════════════════════════════════════
        private bool _stylesReady = false;

        private GUIStyle? _stTabActive;      // 활성 탭 버튼
        private GUIStyle? _stTabInactive;    // 비활성 탭 버튼
        private GUIStyle? _stSection;        // 섹션 헤더 레이블
        private GUIStyle? _stToggleOn;       // 켜진 토글
        private GUIStyle? _stToggleOff;      // 꺼진 토글
        private GUIStyle? _stSliderName;     // 슬라이더 항목명
        private GUIStyle? _stSliderValue;    // 슬라이더 수치 (시안)
        private GUIStyle? _stActionBtn;      // 액션 버튼 (주황)
        private GUIStyle? _stResetBtn;       // 초기화 버튼 (빨강)
        private GUIStyle? _stPresetOn;       // 활성 프리셋 버튼 (초록)
        private GUIStyle? _stPresetOff;      // 비활성 프리셋 버튼 (회색)

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            // ── 탭 ───────────────────────────────────────────────────────────────
            _stTabActive = new GUIStyle(GUI.skin.button) { fontSize = 12, fontStyle = FontStyle.Bold };
            _stTabActive.normal.textColor = Color.white;
            _stTabActive.hover.textColor  = Color.white;

            _stTabInactive = new GUIStyle(GUI.skin.button) { fontSize = 12 };
            _stTabInactive.normal.textColor = new Color(0.52f, 0.52f, 0.52f);
            _stTabInactive.hover.textColor  = new Color(0.82f, 0.82f, 0.82f);

            // ── 섹션 헤더 ────────────────────────────────────────────────────────
            _stSection = new GUIStyle(GUI.skin.label) { fontSize = 10, fontStyle = FontStyle.Bold, wordWrap = false };
            _stSection.normal.textColor = new Color(1f, 0.72f, 0f);

            // ── 토글 ─────────────────────────────────────────────────────────────
            _stToggleOn = new GUIStyle(GUI.skin.toggle) { fontSize = 11 };
            _stToggleOn.normal.textColor = new Color(0.27f, 1f, 0.33f);
            _stToggleOn.hover.textColor  = new Color(0.27f, 1f, 0.33f);

            _stToggleOff = new GUIStyle(GUI.skin.toggle) { fontSize = 11 };
            _stToggleOff.normal.textColor = new Color(0.72f, 0.72f, 0.72f);
            _stToggleOff.hover.textColor  = Color.white;

            // ── 슬라이더 ─────────────────────────────────────────────────────────
            _stSliderName = new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = false };
            _stSliderName.normal.textColor = new Color(0.85f, 0.85f, 0.85f);

            _stSliderValue = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Bold, wordWrap = false };
            _stSliderValue.normal.textColor = new Color(0.4f, 0.9f, 1f);

            // ── 버튼 ─────────────────────────────────────────────────────────────
            _stActionBtn = new GUIStyle(GUI.skin.button) { fontSize = 11 };
            _stActionBtn.normal.textColor = new Color(1f, 0.55f, 0f);
            _stActionBtn.hover.textColor  = new Color(1f, 0.78f, 0.35f);

            _stResetBtn = new GUIStyle(GUI.skin.button) { fontSize = 11 };
            _stResetBtn.normal.textColor = new Color(1f, 0.27f, 0.27f);
            _stResetBtn.hover.textColor  = new Color(1f, 0.55f, 0.55f);

            // ── 프리셋 ───────────────────────────────────────────────────────────
            _stPresetOn = new GUIStyle(GUI.skin.button) { fontSize = 11, fontStyle = FontStyle.Bold };
            _stPresetOn.normal.textColor = new Color(0.27f, 1f, 0.33f);
            _stPresetOn.hover.textColor  = new Color(0.27f, 1f, 0.33f);

            _stPresetOff = new GUIStyle(GUI.skin.button) { fontSize = 11 };
            _stPresetOff.normal.textColor = new Color(0.65f, 0.65f, 0.65f);
            _stPresetOff.hover.textColor  = Color.white;
        }

        // ════════════════════════════════════════════════════════════════════════
        // Awake / Update
        // ════════════════════════════════════════════════════════════════════════
        void Awake()
        {
            OverlayManager.Register(new StatusPanel());
            OverlayManager.Register(new DpsPanel());
            _onSceneLoaded = OnSceneLoaded;
            SceneManager.sceneLoaded += _onSceneLoaded;
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= _onSceneLoaded;
            _onSceneLoaded = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // MetaGame 재진입 시 DEBUG 선택 초기화 — 이전 세션 캐릭터/미션 오염 방지
            if (scene.name == MissionState.MetaGameScene)
            {
                _debugSelectedChar    = null;
                _debugSelectedMission = null;
                _debugScanned         = false;
                _debugMetaChar        = "?";
                _debugMetaMission     = "?";
                _debugSceneValidationError = "";
                Plugin.Log.LogInfo("[ModMenu] MetaGame 재진입 — DEBUG 선택 초기화");
            }
        }

        void Update()
        {
            ApplyCheats();
            TickRefreshTimers();
            HandleHotkeys();
            OverlayManager.Tick();
        }

        private void HandleHotkeys()
        {
            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.F5)) _visible = !_visible;
            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.F6)) HealFull();
            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.F7)) ForceLevelUp();
        }

        /// <summary>
        /// 슬라이더 디바운스 타이머 처리.
        /// OnGUI 드래그 중 반복 예약되지만 Time.time 기준으로 딱 한 번만 실행됩니다.
        /// </summary>
        private void TickRefreshTimers()
        {
            float now = Time.time;
            if (_hpRefreshAt > 0f && now >= _hpRefreshAt)
            {
                _hpRefreshAt = 0f;
                PS()?.RecalculateStats();
            }
            if (_eSpeedRefreshAt > 0f && now >= _eSpeedRefreshAt)
            {
                _eSpeedRefreshAt = 0f;
                RefreshEnemySpeeds();
            }
        }

        private void ApplyCheats()
        {
            var ps = PS();
            var pd = PD();

            if (CheatState.InfiniteGems && ps != null && ps.money < CheatState.GemsFloor)
                ps.SetMoney(CheatState.GemsFloor);
            if (CheatState.InfiniteGems && pd != null && pd.currentMoney < CheatState.GemsFloor)
                pd.currentMoney = CheatState.GemsFloor;
            if (CheatState.InfiniteSkullCoins && pd != null
                && pd.currentSuperTickets < CheatState.SkullCoinsFloor)
                pd.currentSuperTickets = CheatState.SkullCoinsFloor;
            if (CheatState.InfiniteAway)
            {
                if (ps != null && ps.LevelUpAway < 99) ps.LevelUpAway = 99;
                if (pd != null && pd.currentAways < 99) pd.currentAways = 99;
            }
            if (CheatState.MaxStats && ps != null)
                ps.RestoreHp(99999f);
            if (CheatState.InfiniteRevive && ps != null && ps.revivals < 99)
                ps.SetRevivals(99);
        }

        // ════════════════════════════════════════════════════════════════════════
        // OnGUI
        // ════════════════════════════════════════════════════════════════════════
        void OnGUI()
        {
            EnsureStyles();

            OverlayManager.IsMenuOpen = _visible;
            if (OverlayManager.Draw()) _visible = !_visible;

            if (_visible)
                _windowRect = GUI.Window(0, _windowRect,
                    (GUI.WindowFunction)DrawWindow,
                    "\u25c8  Bloodshed Mod Toolkit");
        }

        // ════════════════════════════════════════════════════════════════════════
        // 메인 창
        // ════════════════════════════════════════════════════════════════════════
        private void DrawWindow(int id)
        {
            var l = L();

            // ── 탭 바 ────────────────────────────────────────────────────────────
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("CHEATS",
                    _activeTab == Tab.Cheats ? _stTabActive! : _stTabInactive!,
                    GUILayout.Height(26)))
                _activeTab = Tab.Cheats;
            if (GUILayout.Button("TWEAKS",
                    _activeTab == Tab.Tweaks ? _stTabActive! : _stTabInactive!,
                    GUILayout.Height(26)))
                _activeTab = Tab.Tweaks;
            if (GUILayout.Button("CO-OP",
                    _activeTab == Tab.Coop ? _stTabActive! : _stTabInactive!,
                    GUILayout.Height(26)))
                _activeTab = Tab.Coop;
            if (GUILayout.Button("BOTS",
                    _activeTab == Tab.Bots ? _stTabActive! : _stTabInactive!,
                    GUILayout.Height(26)))
                _activeTab = Tab.Bots;
            if (GUILayout.Button("DEBUG",
                    _activeTab == Tab.Debug ? _stTabActive! : _stTabInactive!,
                    GUILayout.Height(26)))
                _activeTab = Tab.Debug;
            GUILayout.EndHorizontal();

            GUILayout.Space(3);

            if      (_activeTab == Tab.Cheats) DrawCheatsTab(l);
            else if (_activeTab == Tab.Tweaks) DrawTweaksTab(l);
            else if (_activeTab == Tab.Coop)   DrawCoopTab(l);
            else if (_activeTab == Tab.Bots)   DrawBotsTab();
            else                               DrawDebugTab();

            // 타이틀바 영역만 드래그 허용
            GUI.DragWindow(new Rect(0, 0, _windowRect.width, 22));
        }

        // ════════════════════════════════════════════════════════════════════════
        // CHEATS 탭
        // ════════════════════════════════════════════════════════════════════════
        private void DrawCheatsTab(LangStrings l)
        {
            _scrollCheats = GUILayout.BeginScrollView(_scrollCheats, GUILayout.ExpandHeight(true));

            // SURVIVAL ──────────────────────────────────────────────────────────
            SectionHeader("SURVIVAL");
            TwoCol(ref CheatState.GodMode,        l.GodMode,
                   ref CheatState.MaxStats,        l.MaxStats);
            TwoCol(ref CheatState.InfiniteRevive,  l.InfiniteRevive,
                   ref CheatState.InfiniteAway,    l.InfiniteAway);

            // ECONOMY ───────────────────────────────────────────────────────────
            SectionHeader("ECONOMY");
            TwoCol(ref CheatState.InfiniteGems,       l.InfiniteGems,
                   ref CheatState.InfiniteSkullCoins, l.InfiniteSkullCoins);

            // COMBAT ────────────────────────────────────────────────────────────
            SectionHeader("COMBAT");
            TwoCol(ref CheatState.OneShotKill, l.OneShotKill,
                   ref CheatState.NoCooldown,  l.NoCooldown);
            TwoCol(ref CheatState.RapidFire,   l.RapidFire,
                   ref CheatState.NoRecoil,    l.NoRecoil);
            TwoCol(ref CheatState.PerfectAim,  l.PerfectAim,
                   ref CheatState.NoReload,    l.NoReload);

            // MOVEMENT ──────────────────────────────────────────────────────────
            SectionHeader("MOVEMENT");
            CheatState.SpeedHack = Toggle(CheatState.SpeedHack, l.SpeedHack);
            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Format(l.SpeedLabel, CheatState.SpeedMultiplier.ToString("F1")),
                            _stSliderName!, GUILayout.Width(110));
            CheatState.SpeedMultiplier = GUILayout.HorizontalSlider(
                CheatState.SpeedMultiplier, 1f, 20f);
            GUILayout.EndHorizontal();

            // OVERLAY ────────────────────────────────────────────────────────
            SectionHeader("OVERLAY");
            GUILayout.BeginHorizontal();
            OverlayPosBtn(l.OverlayHidden,    OverlayPosition.Hidden);
            OverlayPosBtn(l.OverlayTopLeft,   OverlayPosition.TopLeft);
            OverlayPosBtn(l.OverlayTopCenter, OverlayPosition.TopCenter);
            OverlayPosBtn(l.OverlayTopRight,  OverlayPosition.TopRight);
            GUILayout.EndHorizontal();

            GUILayout.EndScrollView();

            // ── 액션 바 (스크롤 외부 고정) ──────────────────────────────────────
            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(l.ForceLevelUp, _stActionBtn!)) ForceLevelUp();
            if (GUILayout.Button(l.AddGems,       _stActionBtn!)) AddGems();
            if (GUILayout.Button(l.AddSkullCoins, _stActionBtn!)) AddSkullCoins();
            if (GUILayout.Button(l.HealFull,      _stActionBtn!)) HealFull();
            GUILayout.EndHorizontal();
            GUILayout.Space(3);
            if (GUILayout.Button(l.AllCheatsOff, _stResetBtn!))
                CheatState.Initialize();
        }

        // ════════════════════════════════════════════════════════════════════════
        // TWEAKS 탭
        // ════════════════════════════════════════════════════════════════════════
        private void DrawTweaksTab(LangStrings l)
        {
            var c = TweakState.Current;

            _scrollTweaks = GUILayout.BeginScrollView(_scrollTweaks, GUILayout.ExpandHeight(true));

            // DIFFICULTY ────────────────────────────────────────────────────────
            SectionHeader("DIFFICULTY");
            GUILayout.BeginHorizontal();
            PresetBtn(l.TweakMortal,     TweakPresetType.Mortal);
            PresetBtn(l.TweakHunter,     TweakPresetType.Hunter);
            PresetBtn(l.TweakSlayer,     TweakPresetType.Slayer);
            PresetBtn(l.TweakDemon,      TweakPresetType.Demon);
            PresetBtn(l.TweakApocalypse, TweakPresetType.Apocalypse);
            GUILayout.EndHorizontal();
            GUILayout.Space(2);

            // PLAYER ────────────────────────────────────────────────────────────
            SectionHeader("PLAYER");
            float prevHpMult = c.PlayerHpMult;
            c.PlayerHpMult    = SliderRow("HP",    c.PlayerHpMult,    0.10f, 4.00f);
            if (c.PlayerHpMult != prevHpMult) _hpRefreshAt = Time.time + kRefreshDelay;
            c.PlayerSpeedMult = SliderRow("Speed", c.PlayerSpeedMult, 0.50f, 3.00f);

            // WEAPON ────────────────────────────────────────────────────────────
            SectionHeader("WEAPON");
            c.WeaponDamageMult      = SliderRow("Damage", c.WeaponDamageMult,      0.50f, 3.00f);
            c.WeaponFireRateMult    = SliderRow("Fire",   c.WeaponFireRateMult,    0.50f, 3.00f);
            c.WeaponReloadSpeedMult = SliderRow("Reload", c.WeaponReloadSpeedMult, 0.50f, 3.00f);

            // ENEMY ─────────────────────────────────────────────────────────────
            SectionHeader("ENEMY");
            c.EnemyHpMult     = SliderRow("HP",     c.EnemyHpMult,     0.25f, 5.00f);
            float prevESpd = c.EnemySpeedMult;
            c.EnemySpeedMult  = SliderRow("Speed",  c.EnemySpeedMult,  0.25f, 3.00f);
            if (c.EnemySpeedMult != prevESpd) _eSpeedRefreshAt = Time.time + kRefreshDelay;
            c.EnemyDamageMult = SliderRow("Damage", c.EnemyDamageMult, 0.25f, 5.00f);

            // SPAWN ─────────────────────────────────────────────────────────────
            SectionHeader("SPAWN");
            c.SpawnCountMult = SliderRow("Count", c.SpawnCountMult, 0.25f, 4.00f);
            GUILayout.Label(l.SpawnNote, _stSection!);

            GUILayout.Space(4);
            GUILayout.EndScrollView();
        }

        // ════════════════════════════════════════════════════════════════════════
        // CO-OP 탭
        // ════════════════════════════════════════════════════════════════════════
        private void DrawCoopTab(LangStrings l)
        {
            _scrollCoop = GUILayout.BeginScrollView(_scrollCoop, GUILayout.ExpandHeight(true));

            if (!CoopState.IsEnabled)
            {
                // ── 연결 안됨 ────────────────────────────────────────────────
                SectionHeader("STATUS");
                GUILayout.Label($"\u25c6 {l.CoopStatusDisconnected}", _stSliderName!);

                GUILayout.Space(4);
                SectionHeader("HOST");
                if (GUILayout.Button(l.CoopCreateLobby, _stActionBtn!))
                    NetManager.Instance?.CreateLobby(4);

                GUILayout.Space(4);
                SectionHeader("JOIN");
                // GUILayout.TextField 는 이 게임의 IL2CPP 빌드에서 strip 됨 →
                // 클립보드 붙여넣기 방식으로 대체합니다.
                GUILayout.Label(l.CoopLobbyIdLabel, _stSliderName!);
                GUILayout.Label(
                    _lobbyIdInput.Length > 0 ? _lobbyIdInput : l.CoopLobbyIdEmpty,
                    _stSliderValue!);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(l.CoopPasteClipboard, _stActionBtn!))
                    _lobbyIdInput = GUIUtility.systemCopyBuffer?.Trim() ?? "";
                if (_lobbyIdInput.Length > 0 && GUILayout.Button(l.CoopClear, _stResetBtn!))
                    _lobbyIdInput = "";
                GUILayout.EndHorizontal();
                if (GUILayout.Button(l.CoopJoin, _stActionBtn!))
                {
                    if (ulong.TryParse(_lobbyIdInput.Trim(), out var rawId))
                        NetManager.Instance?.JoinLobby(new CSteamID(rawId));
                    else
                        Plugin.Log.LogWarning("[CoopTab] 유효하지 않은 로비 ID");
                }

                DrawFriendsSection(l);
            }
            else
            {
                // ── 연결됨 ───────────────────────────────────────────────────
                SectionHeader("STATUS");
                string connText = CoopState.IsConnected
                    ? $"\u25cf {l.CoopConnected}"
                    : $"\u25cb {l.CoopWaiting}";
                string roleText = CoopState.IsHost ? "HOST" : "GUEST";
                GUILayout.Label($"\u25c6 {connText}  [{roleText}]", _stSliderName!);
                GUILayout.Label($"\u25c6 Lobby ID: {CoopState.LobbyId}", _stSliderName!);

                SectionHeader("PEERS");
                var myId   = SteamUser.GetSteamID();
                var myName = SteamFriends.GetFriendPersonaName(myId);
                GUILayout.Label($"  \u2605 {myName} ({roleText})", _stSliderName!);

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
                            _stSliderName!);
                    }
                    else
                    {
                        GUILayout.Label($"  \u25cf {peerName}", _stSliderName!);
                    }
                }

                DrawFriendsSection(l);

                // ── XP SYNC ──────────────────────────────────────────────────
                GUILayout.Space(4);
                SectionHeader("XP SYNC");
                GUILayout.BeginHorizontal();
                XpModeBtn(l.CoopXpIndependent, XpShareMode.Independent);
                XpModeBtn(l.CoopXpReplicate,   XpShareMode.Replicate);
                XpModeBtn(l.CoopXpSplit,        XpShareMode.Split);
                GUILayout.EndHorizontal();
                GUILayout.Label(XpModeDescription(l), _stSliderName!);

                // ── MISSION GATE ──────────────────────────────────────────────
                GUILayout.Space(4);
                SectionHeader("MISSION GATE");

                if (CoopState.IsHost)
                {
                    int ready = 0;
                    foreach (var v in MissionState.GuestReadyMap.Values) if (v) ready++;
                    GUILayout.Label($"GUESTS READY: {ready} / {CoopState.Peers.Count}", _stSliderName!);
                }
                else
                {
                    switch (MissionState.Status)
                    {
                        case MissionStatus.Idle:
                            GUILayout.Label("대기 중 — 호스트 미션 진입 시 자동 입장", _stSliderName!);
                            break;
                        case MissionStatus.WaitingForHost:
                            GUILayout.Label("씬 로드 완료 — 호스트 신호 대기 중", _stSliderName!);
                            GUILayout.Label($"({MissionState.PendingSceneName})", _stSliderName!);
                            break;
                        case MissionStatus.NeedsCharacterSelect:
                            GUILayout.Label("캐릭터를 선택하고 게임을 시작하세요", _stSliderName!);
                            GUILayout.Label($"Target: {MissionState.PendingSceneName}", _stSliderName!);
                            break;
                        case MissionStatus.Permitted:
                            GUILayout.Label("미션 로딩 중...", _stSliderName!);
                            break;
                    }
                }

                GUILayout.Space(6);
                if (GUILayout.Button(l.CoopLeave, _stResetBtn!))
                    NetManager.Instance?.LeaveLobby();
            }

            GUILayout.EndScrollView();
        }

        private void XpModeBtn(string label, XpShareMode mode)
        {
            bool active = CoopConfig.XpShare == mode;
            if (GUILayout.Button(label, active ? _stPresetOn! : _stPresetOff!))
            {
                CoopConfig.XpShare = mode;
                Plugin.Log.LogInfo($"[CoopTab] XpShareMode → {mode}");
            }
        }

        private static string XpModeDescription(LangStrings l) =>
            CoopConfig.XpShare switch
            {
                XpShareMode.Independent => l.CoopXpIndependentDesc,
                XpShareMode.Split       => l.CoopXpSplitDesc,
                _                       => l.CoopXpReplicateDesc,
            };

        // ════════════════════════════════════════════════════════════════════════
        // BOTS 탭
        // ════════════════════════════════════════════════════════════════════════
        private void DrawBotsTab()
        {
            var l = L();
            _scrollBots = GUILayout.BeginScrollView(_scrollBots, GUILayout.ExpandHeight(true));

            SectionHeader("BOT PLAYERS");
            BotState.Enabled = GUILayout.Toggle(BotState.Enabled,
                BotState.Enabled ? l.BotPlayersOn : l.BotPlayersOff,
                BotState.Enabled ? _stToggleOn! : _stToggleOff!);

            if (BotState.Enabled)
            {
                GUILayout.Space(4);
                SectionHeader("COUNT");
                GUILayout.BeginHorizontal();
                for (int n = 1; n <= 3; n++)
                {
                    bool active = BotState.Count == n;
                    if (GUILayout.Button(n.ToString(), active ? _stPresetOn! : _stPresetOff!))
                        BotState.Count = n;
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(4);
                SectionHeader("STATUS");
                var bots = BotManager.Instance?.GetBots();
                if (bots == null || bots.Count == 0)
                    GUILayout.Label(l.BotStatusEmpty, _stSliderName!);
                else
                    foreach (var bot in bots)
                    {
                        int hp = bot.MaxHp > 0 ? (int)(bot.CurrentHp / bot.MaxHp * 100f) : 0;
                        GUILayout.Label(
                            $"  \u25cf {BotState.BotNames[bot.BotIndex]}  " +
                            $"Lv{bot.Level}  HP {hp}%  " +
                            $"({bot.Position.x:F1}, {bot.Position.y:F1}, {bot.Position.z:F1})",
                            _stSliderName!);
                    }

                GUILayout.Space(4);
                SectionHeader("RENDERER");
                int avatarCount = PlayerSyncHandler.States.Count;
                GUILayout.Label(string.Format(l.BotTracking, avatarCount), _stSliderName!);
            }
            else
                GUILayout.Label(l.BotDisabledNote, _stSliderName!);

            GUILayout.EndScrollView();
        }

        // ════════════════════════════════════════════════════════════════════════
        // DEBUG 탭
        // ════════════════════════════════════════════════════════════════════════
        /// <summary>
        /// 미션 씬 진입 전 SaveSlot·Character·Mission 일치 여부를 검증.
        /// 비미션 씬이면 항상 통과. 실패 시 reason에 오류 메시지 반환.
        /// </summary>
        private bool ValidateSceneLoad(string sceneName, out string reason)
        {
            // 비미션 씬 — 검증 불필요
            if (!_sceneMissionHints.ContainsKey(sceneName))
            {
                reason = "";
                return true;
            }

            // 1. SaveSlot
            var sdm = UnityEngine.Object.FindObjectOfType<SaveDataManager>();
            if (sdm == null)
            {
                reason = "SaveSlot: SaveDataManager 미발견 (MetaGame 씬에서 REFRESH 필요)";
                return false;
            }
            int slot = (int)sdm.activeSaveSlot;
            if (slot < 0 || slot > 2)
            {
                reason = $"SaveSlot: 유효하지 않은 슬롯 ({slot}), Slot 0~2 중 선택 필요";
                return false;
            }

            // 2. Character — DEBUG 패널 명시 선택만 허용 (save data 기본값 오염 방지)
            if (_debugSelectedChar == null)
            {
                reason = "Character: 캐릭터 미선택\nDEBUG 패널 CHARACTER SELECT에서 선택 필요";
                return false;
            }

            // 3. Mission ↔ Scene — DEBUG 패널 명시 선택만 허용 (ss.selectedMission 폴백 제거)
            var mission = _debugSelectedMission;
            if (mission == null)
            {
                reason = "Mission: 미션 미선택\nDEBUG 패널 MISSION SELECT에서 선택 필요";
                return false;
            }

            if (_sceneMissionHints.TryGetValue(sceneName, out var hints))
            {
                string mName = (mission.name ?? "").ToLower();
                bool match = System.Array.Exists(hints, h => mName.Contains(h));
                if (!match)
                {
                    reason = $"Mission 불일치: '{mission.name}' → '{sceneName}'\n" +
                             $"예상 키워드: [{string.Join(", ", hints)}]";
                    return false;
                }
            }

            reason = "";
            return true;
        }

        private void RefreshMetaSelection()
        {
            _debugScanned = false;  // 다음 DrawDebugTab에서 캐릭터·미션 재스캔
            _debugSdm = UnityEngine.Object.FindObjectOfType<SaveDataManager>();
            var ss    = UnityEngine.Object.FindObjectOfType<SessionSettings>();
            var csm   = UnityEngine.Object.FindObjectOfType<MetaGameCharacterSelectionManager>();
            _debugMetaSaveSlot = _debugSdm != null ? _debugSdm.activeSaveSlot.ToString() : "N/A";

            // DEBUG 명시 선택만 표시 — game state(csm/ss) 폴백 제거 (save data 기본값 오염 방지)
            _debugMetaChar = _debugSelectedChar?.name ?? "(none)";
            _debugMetaMission = _debugSelectedMission != null
                ? (!string.IsNullOrEmpty(_debugSelectedMission.strMissionTitle)
                    ? _debugSelectedMission.strMissionTitle
                    : _debugSelectedMission.name)
                : "(none)";
        }

        private void ScanCharacters()
        {
            var raw = Resources.FindObjectsOfTypeAll<PlayableCharacterData>()
                      ?? System.Array.Empty<PlayableCharacterData>();
            // 이름 기준 중복 제거 (동일 asset이 복수 로드되는 경우 방어)
            var seen  = new System.Collections.Generic.HashSet<string>();
            var dedup = new System.Collections.Generic.List<PlayableCharacterData>(raw.Length);
            foreach (var c in raw)
            {
                if (c == null || string.IsNullOrEmpty(c.name)) continue;
                if (seen.Add(c.name)) dedup.Add(c);
            }
            _debugCharList = dedup.ToArray();
            Plugin.Log.LogInfo($"[Debug] 캐릭터 스캔: raw={raw.Length} unique={_debugCharList.Length}");
        }

        private void ScanMissions()
        {
            var raw = Resources.FindObjectsOfTypeAll<MissionAsset>()
                      ?? System.Array.Empty<MissionAsset>();
            // 표준 미션만 포함: 이름이 숫자로 시작(01_, 02_, 03_...)
            // CustomSession_*, DEMO_*, Mission_DEV_* 제외
            var filtered = new System.Collections.Generic.List<MissionAsset>(raw.Length);
            foreach (var m in raw)
            {
                if (m == null || string.IsNullOrEmpty(m.name)) continue;
                if (char.IsDigit(m.name[0])) filtered.Add(m);
            }
            _debugMissionList = filtered.ToArray();
            Plugin.Log.LogInfo($"[Debug] 미션 스캔: raw={raw.Length} standard={_debugMissionList.Length}");
        }

        private void SelectCharacter(PlayableCharacterData cd)
        {
            _debugSelectedChar = cd;
            _debugMetaChar     = cd.name;
            // SessionSettings에 즉시 반영 (REFRESH 시 확인 가능)
            var ss = UnityEngine.Object.FindObjectOfType<SessionSettings>();
            if (ss != null) ss.selectedCharacterData = cd;
            Plugin.Log.LogInfo($"[Debug] 캐릭터 선택: '{cd.name}'");
        }

        private void SelectMission(MissionAsset m)
        {
            _debugSelectedMission = m;
            _debugMetaMission = !string.IsNullOrEmpty(m.strMissionTitle) ? m.strMissionTitle : m.name;
            // SessionSettings에 즉시 반영
            var ss = UnityEngine.Object.FindObjectOfType<SessionSettings>();
            if (ss != null) ss.selectedMission = m;
            Plugin.Log.LogInfo($"[Debug] 미션 선택: '{m.name}' ({_debugMetaMission})");
        }

        /// <summary>
        /// LoadScene 직전 호출 — Debug 명시 선택값을 게임 상태에 완전히 적용.
        /// MetaGameCharacterSelectionManager.SetSelectedCharacter도 포함.
        /// </summary>
        private void ApplyDebugSelections()
        {
            var ss  = UnityEngine.Object.FindObjectOfType<SessionSettings>();
            var csm = UnityEngine.Object.FindObjectOfType<MetaGameCharacterSelectionManager>();
            if (_debugSelectedChar != null)
            {
                if (ss  != null) ss.selectedCharacterData = _debugSelectedChar;
                if (csm != null) { csm.selectedCharacter = _debugSelectedChar; csm.SetSelectedCharacter(_debugSelectedChar); }
                Plugin.Log.LogInfo($"[Debug] Apply: char='{_debugSelectedChar.name}'");
            }
            if (_debugSelectedMission != null && ss != null)
            {
                ss.selectedMission = _debugSelectedMission;
                Plugin.Log.LogInfo($"[Debug] Apply: mission='{_debugSelectedMission.name}'");
            }
        }
        private static readonly (string Group, (string Scene, string Label)[] Entries)[] _sceneGroups =
        {
            ("Nav", new[] { ("MetaGame", "MetaGame") }),
            ("Dev", new[] { ("SampleScene", "Sample") }),
            ("P",   new[] { ("Graveyard", "Graveyard"), ("Village", "Village") }),
            ("E1",  new[] { ("01_Forest", "Forest"), ("01_AltarOfSacrifice", "Altar"),
                            ("01_Challenge_Flynn", "Flynn"), ("01_Challenge_Jared", "Jared"),
                            ("01_Challenge_Seraphina", "Seraphina") }),
            ("E2",  new[] { ("02_Boat", "Boat"), ("02_StiltVillage", "Stilt"),
                            ("02_CultistTemple", "Temple"), ("02_Boss", "Boss"),
                            ("02_Challenge_Maze", "Maze"), ("02_Challenge_Final", "Final") }),
            ("E3",  new[] { ("03_SkyCathedral", "SkyCath") }),
        };

        private void DrawDebugTab()
        {
            // 최초 진입 시 자동 스캔 (이후 REFRESH 버튼으로 재스캔)
            if (!_debugScanned)
            {
                ScanCharacters();
                ScanMissions();
                _debugScanned = true;
            }

            _scrollDebug = GUILayout.BeginScrollView(_scrollDebug, GUILayout.ExpandHeight(true));

            // ── CURRENT STATE ────────────────────────────────────────────────
            SectionHeader("CURRENT STATE");
            var cur = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            GUILayout.Label($"Active : {cur.name} (idx={cur.buildIndex})", _stSliderName!);
            GUILayout.Label($"From   : {(SceneTracker.FromSceneName.Length > 0 ? SceneTracker.FromSceneName + $"(idx={SceneTracker.FromSceneBuildIndex})" : "(none)")}", _stSliderName!);
            GUILayout.Label($"Status : {MissionState.Status}", _stSliderName!);
            GUILayout.Label($"Pending: {(MissionState.PendingSceneName.Length > 0 ? MissionState.PendingSceneName : "(none)")}", _stSliderName!);
            GUILayout.Label($"CharSel: {MissionState.GuestCharacterSelected}", _stSliderName!);
            GUILayout.Label($"Coop   : Enabled={CoopState.IsEnabled} Host={CoopState.IsHost} Connected={CoopState.IsConnected}", _stSliderName!);

            // ── LOADED SCENES ────────────────────────────────────────────────
            SectionHeader("LOADED SCENES");
            var scenes = SceneTracker.LoadedScenes;
            if (scenes.Count == 0)
                GUILayout.Label("  (none)", _stSliderName!);
            else
                foreach (var s in scenes)
                    GUILayout.Label(
                        $"  {(s.IsAdditive ? "[+]" : "[ ]")} {s.Name} (idx={s.BuildIndex})",
                        _stSliderName!);

            // ── META SELECTION ───────────────────────────────────────────────
            SectionHeader("META SELECTION");
            GUILayout.Label($"SaveSlot: {_debugMetaSaveSlot}", _stSliderName!);
            GUILayout.Label($"Mission : {_debugMetaMission}", _stSliderName!);
            GUILayout.Label($"Char    : {_debugMetaChar}", _stSliderName!);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("REFRESH", _stActionBtn!))
                RefreshMetaSelection();
            if (_debugSdm != null)
            {
                for (int slot = 0; slot < 3; slot++)
                {
                    bool active = _debugMetaSaveSlot == slot.ToString();
                    int s = slot;
                    if (GUILayout.Button($"Slot {s}", active ? _stPresetOn! : _stPresetOff!))
                    {
                        _debugSdm.SetActiveSaveSlot(s);
                        _debugMetaSaveSlot = s.ToString();
                    }
                }
            }
            GUILayout.EndHorizontal();

            // ── CHARACTER SELECT ─────────────────────────────────────────────
            SectionHeader("CHARACTER SELECT");
            if (_debugCharList.Length == 0)
            {
                GUILayout.Label("(MetaGame 씬에서 REFRESH 필요)", _stSliderName!);
            }
            else
            {
                const int kCharsPerRow = 3;
                for (int i = 0; i < _debugCharList.Length; i += kCharsPerRow)
                {
                    GUILayout.BeginHorizontal();
                    for (int j = i; j < System.Math.Min(i + kCharsPerRow, _debugCharList.Length); j++)
                    {
                        var cd = _debugCharList[j];
                        bool active = _debugSelectedChar != null && _debugSelectedChar.name == cd.name;
                        if (GUILayout.Button(cd.name, active ? _stPresetOn! : _stPresetOff!))
                            SelectCharacter(cd);
                    }
                    GUILayout.EndHorizontal();
                }
            }

            // ── MISSION SELECT ────────────────────────────────────────────────
            SectionHeader("MISSION SELECT");
            if (_debugMissionList.Length == 0)
            {
                GUILayout.Label("(MetaGame 씬에서 REFRESH 필요)", _stSliderName!);
            }
            else
            {
                const int kMissPerRow = 2;
                for (int i = 0; i < _debugMissionList.Length; i += kMissPerRow)
                {
                    GUILayout.BeginHorizontal();
                    for (int j = i; j < System.Math.Min(i + kMissPerRow, _debugMissionList.Length); j++)
                    {
                        var m = _debugMissionList[j];
                        string label = !string.IsNullOrEmpty(m.strMissionTitle) ? m.strMissionTitle : m.name;
                        bool active = _debugSelectedMission != null && _debugSelectedMission.name == m.name;
                        if (GUILayout.Button(label, active ? _stPresetOn! : _stPresetOff!))
                            SelectMission(m);
                    }
                    GUILayout.EndHorizontal();
                }
            }

            // ── FORCE SCENE LOAD ────────────────────────────────────────────
            SectionHeader("FORCE SCENE LOAD");
            GUILayout.Label("씬 이름 (클립보드 붙여넣기):", _stSliderName!);
            GUILayout.Label(
                _debugSceneInput.Length > 0 ? _debugSceneInput : "(empty)",
                _stSliderValue!);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("PASTE", _stActionBtn!))
                _debugSceneInput = GUIUtility.systemCopyBuffer?.Trim() ?? "";
            if (_debugSceneInput.Length > 0 && GUILayout.Button("CLEAR", _stResetBtn!))
                _debugSceneInput = "";
            GUILayout.EndHorizontal();

            GUILayout.Space(2);
            foreach (var (group, entries) in _sceneGroups)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(group + ":", _stSliderName!, GUILayout.Width(28f));
                foreach (var (scene, label) in entries)
                {
                    bool active = _debugSceneInput == scene;
                    if (GUILayout.Button(label, active ? _stPresetOn! : _stPresetOff!))
                        _debugSceneInput = scene;
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(4);

            // 검증 오류 표시
            if (_debugSceneValidationError.Length > 0)
            {
                var errStyle = new GUIStyle(_stSliderName!)
                    { normal = { textColor = new Color(1f, 0.2f, 0.2f) }, wordWrap = true };
                GUILayout.Label(_debugSceneValidationError, errStyle);
            }

            if (GUILayout.Button("LOAD SCENE", _stActionBtn!))
            {
                if (_debugSceneInput.Length == 0)
                {
                    _debugSceneValidationError = "씬 이름이 비어있습니다";
                    Plugin.Log.LogWarning("[Debug] 씬 이름이 비어있습니다");
                }
                else if (ValidateSceneLoad(_debugSceneInput, out string reason))
                {
                    _debugSceneValidationError = "";
                    ApplyDebugSelections();  // char/mission을 게임 상태에 완전 적용
                    _visible = false;        // IMGUI 마우스 흡수 방지 — 씬 전환 전 메뉴 닫기
                    Plugin.Log.LogInfo($"[Debug] ForceLoadScene → '{_debugSceneInput}'");
                    UnityEngine.SceneManagement.SceneManager.LoadScene(_debugSceneInput);
                }
                else
                {
                    _debugSceneValidationError = reason;
                    Plugin.Log.LogWarning($"[Debug] 씬 로드 검증 실패: {reason}");
                }
            }

            GUILayout.EndScrollView();
        }

        // ════════════════════════════════════════════════════════════════════════
        // FRIENDS 섹션
        // ════════════════════════════════════════════════════════════════════════
        private void DrawFriendsSection(LangStrings l)
        {
            GUILayout.Space(4);
            SectionHeader("FRIENDS");

            // 새로고침 버튼 + 카운트
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(l.CoopRefresh, _stActionBtn!))
                FriendListCache.Refresh();
            if (FriendListCache.LastRefreshTime >= 0f)
                GUILayout.Label(
                    string.Format(l.CoopFriendsOnlineCount, FriendListCache.Entries.Count),
                    _stSliderName!);
            GUILayout.EndHorizontal();

            if (FriendListCache.LastRefreshTime < 0f)
            {
                GUILayout.Label($"  {l.CoopFriendsLoadPrompt}", _stSliderName!);
                return;
            }

            if (FriendListCache.Entries.Count == 0)
            {
                GUILayout.Label($"  {l.CoopFriendsNone}", _stSliderName!);
                return;
            }

            foreach (var f in FriendListCache.Entries)
            {
                GUILayout.BeginHorizontal();

                GUILayout.Label($"  {f.Name}", _stSliderName!);

                // 참가 버튼 — 친구가 Bloodshed 로비에 있고 우리가 미연결 상태일 때
                if (!CoopState.IsEnabled && f.LobbyId.IsValid() &&
                    GUILayout.Button(l.CoopJoin, _stActionBtn!, GUILayout.Width(44)))
                {
                    NetManager.Instance?.JoinLobby(f.LobbyId);
                    Plugin.Log.LogInfo($"[Friends] {f.Name} 의 로비에 참가");
                }

                // 초대 버튼 — 우리가 로비를 열었을 때
                if (CoopState.IsEnabled &&
                    GUILayout.Button(l.CoopInvite, _stActionBtn!, GUILayout.Width(44)))
                {
                    SteamMatchmaking.InviteUserToLobby(CoopState.LobbyId, f.SteamId);
                    Plugin.Log.LogInfo($"[Friends] {f.Name} 초대 전송");
                }

                GUILayout.EndHorizontal();
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // 드로잉 헬퍼
        // ════════════════════════════════════════════════════════════════════════
        private void SectionHeader(string title)
        {
            GUILayout.Space(5);
            GUILayout.Label($"\u2500\u2500 {title} " + new string('\u2500', 32), _stSection!);
        }

        private float SliderRow(string name, float value, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(name,              _stSliderName!,  GUILayout.Width(52));
            GUILayout.Label($"{value:F2}\u00d7", _stSliderValue!, GUILayout.Width(46));
            float next = GUILayout.HorizontalSlider(value, min, max);
            GUILayout.EndHorizontal();
            return (float)System.Math.Round(next, 2);
        }

        private void OverlayPosBtn(string label, OverlayPosition pos)
        {
            bool active = OverlayManager.Position == pos;
            if (GUILayout.Button(label, active ? _stPresetOn! : _stPresetOff!))
                OverlayManager.Position = pos;
        }

        private void PresetBtn(string label, TweakPresetType preset)
        {
            bool active = TweakState.ActivePreset == preset;
            if (GUILayout.Button(label, active ? _stPresetOn! : _stPresetOff!))
            {
                TweakState.Apply(preset);
                Plugin.Log.LogInfo($"[TweakMenu] {preset}");
            }
        }

        /// <summary>2열 토글 행. ref를 직접 수정합니다.</summary>
        private void TwoCol(ref bool a, string la, ref bool b, string lb)
        {
            GUILayout.BeginHorizontal();
            a = Toggle(a, la);
            b = Toggle(b, lb);
            GUILayout.EndHorizontal();
        }

        private bool Toggle(bool current, string label)
        {
            bool next = GUILayout.Toggle(current, label,
                current ? _stToggleOn! : _stToggleOff!);
            if (next != current)
                Plugin.Log.LogInfo($"[ModMenu] {label.Trim()} \u2192 {(next ? "ON" : "OFF")}");
            return next;
        }

        // ════════════════════════════════════════════════════════════════════════
        // 액션 구현
        // ════════════════════════════════════════════════════════════════════════
        private void AddGems()
        {
            PS()?.SetMoney(CheatState.GemsFloor);
            var pd = PD();
            if (pd != null) pd.currentMoney = CheatState.GemsFloor;
            Plugin.Log.LogInfo("[AddGems] 젬 999999 지급");
        }

        private void AddSkullCoins()
        {
            var pd = PD();
            if (pd == null) { Plugin.Log.LogWarning("[SkullCoins] PersistentData 없음"); return; }
            pd.currentSuperTickets = CheatState.SkullCoinsFloor;
            Plugin.Log.LogInfo($"[SkullCoins] → {CheatState.SkullCoinsFloor}");
        }

        private void HealFull() => PS()?.RestoreHp(99999f);

        private void RefreshEnemySpeeds()
        {
            var enemies = Object.FindObjectsOfType<EnemyAbilityController>();
            if (enemies == null) return;
            foreach (var ec in enemies) ec.RefreshAgentSpeed();
        }

        private void ForceLevelUp()
        {
            var ps = PS();
            if (ps == null) { Plugin.Log.LogWarning("[ForceLevelUp] PlayerStats 없음"); return; }
            float current = ps.experience;
            float cap     = ps.experienceCap;
            if (current >= cap)
            {
                Plugin.Log.LogInfo("[ForceLevelUp] 경험치 이미 최대 — 스킵");
                return;
            }
            ps.AddXp(cap - current + 1f);
            Plugin.Log.LogInfo($"[ForceLevelUp] +{cap - current + 1f:F0} XP");
        }
    }
}
