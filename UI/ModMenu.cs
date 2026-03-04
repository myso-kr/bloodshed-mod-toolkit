using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using BloodshedModToolkit.UI.Overlay;
using BloodshedModToolkit.UI.Overlay.Panels;
using BloodshedModToolkit.UI.Tabs;

namespace BloodshedModToolkit.UI
{
    public class ModMenu : MonoBehaviour
    {
        public ModMenu(IntPtr ptr) : base(ptr) { }

        private enum Tab { Cheats, Tweaks, Coop, Bots, Debug }
        private Tab  _activeTab  = Tab.Cheats;
        private bool _visible    = false;
        private Rect _windowRect = new Rect(20, 20, 390, 470);

        private readonly ModMenuContext _ctx = new();
        private IModTab[] _tabs = null!;

        private Action<Scene, LoadSceneMode>? _onSceneLoaded;

        void Awake()
        {
            _tabs = new IModTab[]
            {
                new CheatsTab(),
                new TweaksTab(),
                new CoopTab(),
                new BotsTab(),
                new DebugTab(),
            };
            OverlayManager.Register(new StatusPanel());
            OverlayManager.Register(new DpsPanel());
            OverlayManager.Register(new UpdateNoticePanel());
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
            foreach (var tab in _tabs) tab.OnSceneLoaded(scene, mode);
        }

        void Update()
        {
            _ctx.Tick();
            foreach (var tab in _tabs) tab.Tick(_ctx);
            HandleHotkeys();
            OverlayManager.Tick();
        }

        void OnGUI()
        {
            _ctx.EnsureStyles();

            OverlayManager.IsMenuOpen = _visible;
            if (OverlayManager.Draw()) _visible = !_visible;

            if (_visible)
                _windowRect = GUI.Window(0, _windowRect,
                    (GUI.WindowFunction)DrawWindow,
                    "\u25c8  Bloodshed Mod Toolkit");
        }

        private void DrawWindow(int id)
        {
            // ── 탭 바 ────────────────────────────────────────────────────────
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("CHEATS",
                    _activeTab == Tab.Cheats ? _ctx.StTabActive! : _ctx.StTabInactive!,
                    GUILayout.Height(26)))
                _activeTab = Tab.Cheats;
            if (GUILayout.Button("TWEAKS",
                    _activeTab == Tab.Tweaks ? _ctx.StTabActive! : _ctx.StTabInactive!,
                    GUILayout.Height(26)))
                _activeTab = Tab.Tweaks;
            if (GUILayout.Button("CO-OP",
                    _activeTab == Tab.Coop ? _ctx.StTabActive! : _ctx.StTabInactive!,
                    GUILayout.Height(26)))
                _activeTab = Tab.Coop;
            if (GUILayout.Button("BOTS",
                    _activeTab == Tab.Bots ? _ctx.StTabActive! : _ctx.StTabInactive!,
                    GUILayout.Height(26)))
                _activeTab = Tab.Bots;
            if (GUILayout.Button("DEBUG",
                    _activeTab == Tab.Debug ? _ctx.StTabActive! : _ctx.StTabInactive!,
                    GUILayout.Height(26)))
                _activeTab = Tab.Debug;
            GUILayout.EndHorizontal();

            GUILayout.Space(3);
            _tabs[(int)_activeTab].Draw(_ctx);

            GUI.DragWindow(new Rect(0, 0, _windowRect.width, 22));
        }

        private void HandleHotkeys()
        {
            if (Input.GetKeyDown(KeyCode.F5)) _visible = !_visible;
            if (Input.GetKeyDown(KeyCode.F6)) GameActions.HealFull(_ctx);
            if (Input.GetKeyDown(KeyCode.F7)) GameActions.ForceLevelUp(_ctx);
        }
    }
}
