using UnityEngine;
using com8com1.SCFPS;
using Enemies.EnemyAi;
using BloodshedModToolkit.I18n;
using BloodshedModToolkit.Tweaks;
using BloodshedModToolkit.UI.Overlay;

namespace BloodshedModToolkit.UI
{
    internal sealed class ModMenuContext
    {
        // ── Styles ────────────────────────────────────────────────────────────
        private bool _stylesReady;

        public GUIStyle? StTabActive   { get; private set; }
        public GUIStyle? StTabInactive { get; private set; }
        public GUIStyle? StSection     { get; private set; }
        public GUIStyle? StToggleOn    { get; private set; }
        public GUIStyle? StToggleOff   { get; private set; }
        public GUIStyle? StSliderName  { get; private set; }
        public GUIStyle? StSliderValue { get; private set; }
        public GUIStyle? StActionBtn   { get; private set; }
        public GUIStyle? StResetBtn    { get; private set; }
        public GUIStyle? StPresetOn    { get; private set; }
        public GUIStyle? StPresetOff   { get; private set; }

        public void EnsureStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            StTabActive = new GUIStyle(GUI.skin.button) { fontSize = 12, fontStyle = FontStyle.Bold };
            StTabActive.normal.textColor = Color.white;
            StTabActive.hover.textColor  = Color.white;

            StTabInactive = new GUIStyle(GUI.skin.button) { fontSize = 12 };
            StTabInactive.normal.textColor = new Color(0.52f, 0.52f, 0.52f);
            StTabInactive.hover.textColor  = new Color(0.82f, 0.82f, 0.82f);

            StSection = new GUIStyle(GUI.skin.label) { fontSize = 10, fontStyle = FontStyle.Bold, wordWrap = false };
            StSection.normal.textColor = new Color(1f, 0.72f, 0f);

            StToggleOn = new GUIStyle(GUI.skin.toggle) { fontSize = 11 };
            StToggleOn.normal.textColor = new Color(0.27f, 1f, 0.33f);
            StToggleOn.hover.textColor  = new Color(0.27f, 1f, 0.33f);

            StToggleOff = new GUIStyle(GUI.skin.toggle) { fontSize = 11 };
            StToggleOff.normal.textColor = new Color(0.72f, 0.72f, 0.72f);
            StToggleOff.hover.textColor  = Color.white;

            StSliderName = new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = false };
            StSliderName.normal.textColor = new Color(0.85f, 0.85f, 0.85f);

            StSliderValue = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Bold, wordWrap = false };
            StSliderValue.normal.textColor = new Color(0.4f, 0.9f, 1f);

            StActionBtn = new GUIStyle(GUI.skin.button) { fontSize = 11 };
            StActionBtn.normal.textColor = new Color(1f, 0.55f, 0f);
            StActionBtn.hover.textColor  = new Color(1f, 0.78f, 0.35f);

            StResetBtn = new GUIStyle(GUI.skin.button) { fontSize = 11 };
            StResetBtn.normal.textColor = new Color(1f, 0.27f, 0.27f);
            StResetBtn.hover.textColor  = new Color(1f, 0.55f, 0.55f);

            StPresetOn = new GUIStyle(GUI.skin.button) { fontSize = 11, fontStyle = FontStyle.Bold };
            StPresetOn.normal.textColor = new Color(0.27f, 1f, 0.33f);
            StPresetOn.hover.textColor  = new Color(0.27f, 1f, 0.33f);

            StPresetOff = new GUIStyle(GUI.skin.button) { fontSize = 11 };
            StPresetOff.normal.textColor = new Color(0.65f, 0.65f, 0.65f);
            StPresetOff.hover.textColor  = Color.white;
        }

        // ── Component Cache ───────────────────────────────────────────────────
        private PlayerStats?                       _ps;
        private PersistentData?                    _pd;
        private GameSettings?                      _gs;
        private SessionSettings?                   _ss;
        private MetaGameCharacterSelectionManager? _csm;

        private T? CachedFind<T>(ref T? cache) where T : Behaviour
        {
            if (cache != null && cache.isActiveAndEnabled) return cache;
            return cache = UnityEngine.Object.FindObjectOfType<T>();
        }

        public PlayerStats?                       PS()  => CachedFind(ref _ps);
        public PersistentData?                    PD()  => CachedFind(ref _pd);
        public GameSettings?                      GS()  => CachedFind(ref _gs);
        public SessionSettings?                   SS()  => CachedFind(ref _ss);
        public MetaGameCharacterSelectionManager? CSM() => CachedFind(ref _csm);

        public LangStrings L()
            => Strings.Get(GS()?.languageText ?? LocalizationManager.Language.English);

        // ── Scroll Positions ──────────────────────────────────────────────────
        public Vector2 ScrollCheats;
        public Vector2 ScrollTweaks;
        public Vector2 ScrollCoop;
        public Vector2 ScrollBots;
        public Vector2 ScrollDebug;

        // ── Debounce Timers ───────────────────────────────────────────────────
        private float _hpRefreshAt;
        private float _eSpeedRefreshAt;
        private const float kRefreshDelay = 0.25f;

        public void ScheduleHpRefresh()         => _hpRefreshAt     = Time.time + kRefreshDelay;
        public void ScheduleEnemySpeedRefresh() => _eSpeedRefreshAt = Time.time + kRefreshDelay;

        public void Tick()
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
                GameActions.RefreshEnemySpeeds();
            }
        }

        // ── UI Helpers ────────────────────────────────────────────────────────
        public void SectionHeader(string title)
        {
            GUILayout.Space(5);
            GUILayout.Label($"\u2500\u2500 {title} " + new string('\u2500', 32), StSection!);
        }

        public float SliderRow(string name, float value, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(name,               StSliderName!,  GUILayout.Width(52));
            GUILayout.Label($"{value:F2}\u00d7", StSliderValue!, GUILayout.Width(46));
            float next = GUILayout.HorizontalSlider(value, min, max);
            GUILayout.EndHorizontal();
            return (float)System.Math.Round(next, 2);
        }

        public bool Toggle(bool current, string label)
        {
            bool next = GUILayout.Toggle(current, label,
                current ? StToggleOn! : StToggleOff!);
            if (next != current)
                Plugin.Log.LogInfo($"[ModMenu] {label.Trim()} \u2192 {(next ? "ON" : "OFF")}");
            return next;
        }

        public void TwoCol(ref bool a, string la, ref bool b, string lb)
        {
            GUILayout.BeginHorizontal();
            a = Toggle(a, la);
            b = Toggle(b, lb);
            GUILayout.EndHorizontal();
        }

        public void DrawButtonGrid<T>(
            T[] items, int perRow,
            System.Func<T, string> getLabel,
            System.Func<T, bool>   isActive,
            System.Action<T>       onSelect)
        {
            for (int i = 0; i < items.Length; i += perRow)
            {
                GUILayout.BeginHorizontal();
                for (int j = i; j < System.Math.Min(i + perRow, items.Length); j++)
                {
                    var item = items[j];
                    if (GUILayout.Button(getLabel(item), isActive(item) ? StPresetOn! : StPresetOff!))
                        onSelect(item);
                }
                GUILayout.EndHorizontal();
            }
        }

        public void PresetBtn(string label, TweakPresetType preset)
        {
            bool active = TweakState.ActivePreset == preset;
            if (GUILayout.Button(label, active ? StPresetOn! : StPresetOff!))
            {
                TweakState.Apply(preset);
                Plugin.Log.LogInfo($"[TweakMenu] {preset}");
            }
        }

        public void OverlayPosBtn(string label, OverlayPosition pos)
        {
            bool active = OverlayManager.Position == pos;
            if (GUILayout.Button(label, active ? StPresetOn! : StPresetOff!))
                OverlayManager.Position = pos;
        }

        public static string MissionDisplayName(MissionAsset m) =>
            !string.IsNullOrEmpty(m.strMissionTitle) ? m.strMissionTitle : m.name;

        public static T[] FindAllAssets<T>() where T : Object =>
            Resources.FindObjectsOfTypeAll<T>() ?? System.Array.Empty<T>();
    }
}
