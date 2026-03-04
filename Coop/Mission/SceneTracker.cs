using System.Collections.Generic;
using System.Text;
using UnityEngine.SceneManagement;

namespace BloodshedModToolkit.Coop.Mission
{
    /// <summary>
    /// 씬 전환의 from/to와 현재 로드된 모든 씬을 추적합니다.
    ///
    /// [버그 주의]
    /// IL2CPP 구조체 마샬링 문제로 activeSceneChanged 콜백의 'from' 파라미터가
    /// 항상 기본값(name="", buildIndex=-1)으로 전달됩니다.
    /// → FromSceneName은 이전 ToSceneName을 체이닝하는 방식으로 직접 추적합니다.
    /// </summary>
    public static class SceneTracker
    {
        public readonly struct SceneEntry
        {
            public readonly string Name;
            public readonly int    BuildIndex;
            public readonly bool   IsAdditive;   // 진짜 Additive 로드 여부

            public SceneEntry(string name, int buildIndex, bool isAdditive)
            { Name = name; BuildIndex = buildIndex; IsAdditive = isAdditive; }

            public override string ToString() =>
                IsAdditive ? $"+{Name}({BuildIndex})" : $"{Name}({BuildIndex})";
        }

        // ── From / To ────────────────────────────────────────────────────────
        // activeSceneChanged.from 은 IL2CPP 버그로 신뢰 불가 → ToSceneName 체이닝으로 보완
        public static string FromSceneName       { get; private set; } = "";
        public static int    FromSceneBuildIndex { get; private set; } = -1;
        public static string ToSceneName         { get; private set; } = "";
        public static int    ToSceneBuildIndex   { get; private set; } = -1;

        // ── 추적 목록 ────────────────────────────────────────────────────────
        private static readonly List<SceneEntry> _loadedScenes = new();
        public static IReadOnlyList<SceneEntry> LoadedScenes => _loadedScenes;

        // ── 이벤트 핸들러 ────────────────────────────────────────────────────

        public static void OnActiveSceneChanged(Scene from, Scene to)
        {
            // IL2CPP bug: from.name / from.buildIndex 항상 기본값 → 이전 ToSceneName으로 대체
            FromSceneName       = ToSceneName.Length > 0 ? ToSceneName : (from.name ?? "");
            FromSceneBuildIndex = ToSceneBuildIndex >= 0  ? ToSceneBuildIndex : from.buildIndex;
            ToSceneName         = to.name ?? "";
            ToSceneBuildIndex   = to.buildIndex;

            Plugin.Log.LogInfo(
                $"[SceneTracker] ── activeSceneChanged ──────────────────");
            Plugin.Log.LogInfo(
                $"[SceneTracker]   from : '{FromSceneName}' (idx={FromSceneBuildIndex}) [chained]");
            Plugin.Log.LogInfo(
                $"[SceneTracker]   to   : '{ToSceneName}' (idx={ToSceneBuildIndex})");
            Plugin.Log.LogInfo(
                $"[SceneTracker]   all  : {SnapshotAllScenes()}");
        }

        public static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // mode=2 등 IL2CPP 내부 값은 Additive(1)인지만 판단
            bool additive = (int)mode == 1;

            int  existing = _loadedScenes.FindIndex(e => e.Name == scene.name);
            var  entry    = new SceneEntry(scene.name ?? "", scene.buildIndex, additive);
            if (existing >= 0) _loadedScenes[existing] = entry;
            else               _loadedScenes.Add(entry);

            string modeStr = mode switch
            {
                LoadSceneMode.Single   => "Single",
                LoadSceneMode.Additive => "Additive",
                _                     => $"Unknown({(int)mode})"
            };

            Plugin.Log.LogInfo(
                $"[SceneTracker] ── sceneLoaded ({modeStr}) ─────────────────");
            Plugin.Log.LogInfo(
                $"[SceneTracker]   scene: '{scene.name}' (idx={scene.buildIndex})");
            Plugin.Log.LogInfo(
                $"[SceneTracker]   from : '{FromSceneName}' (idx={FromSceneBuildIndex})");
            Plugin.Log.LogInfo(
                $"[SceneTracker]   all  : {SnapshotAllScenes()}");
        }

        public static void OnSceneUnloaded(Scene scene)
        {
            _loadedScenes.RemoveAll(e => e.Name == scene.name);

            Plugin.Log.LogInfo(
                $"[SceneTracker] ── sceneUnloaded ───────────────────────");
            Plugin.Log.LogInfo(
                $"[SceneTracker]   scene: '{scene.name}' (idx={scene.buildIndex})");
            Plugin.Log.LogInfo(
                $"[SceneTracker]   all  : {SnapshotAllScenes()}");
        }

        // ── 스냅샷 ───────────────────────────────────────────────────────────

        /// <summary>
        /// SceneManager.sceneCount + GetSceneAt 으로 런타임 씬 목록을 열거합니다.
        ///   [Name(idx)]  = 현재 active 씬
        ///   +Name(idx)   = 진짜 Additive 로드된 서브씬 (tracked 목록 기준)
        ///   ~Name(idx)   = 로드됐으나 아직 active 아닌 씬 (전환 중 일시 상태)
        /// sceneCount = 0 이면 tracked 목록으로 폴백합니다.
        /// </summary>
        public static string SnapshotAllScenes()
        {
            int count = SceneManager.sceneCount;
            if (count <= 0) return TrackedSummary();

            var active = SceneManager.GetActiveScene();
            var sb     = new StringBuilder();

            for (int i = 0; i < count; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                if (sb.Length > 0) sb.Append(", ");

                bool isActive   = s.name == active.name;
                bool isAdditive = IsTrackedAdditive(s.name);

                if (isActive)
                    sb.Append($"[{s.name}({s.buildIndex})]");
                else if (isAdditive)
                    sb.Append($"+{s.name}({s.buildIndex})");
                else
                    sb.Append($"~{s.name}({s.buildIndex})");  // 전환 중 일시 로드
            }

            return sb.Length > 0 ? sb.ToString() : TrackedSummary();
        }

        private static bool IsTrackedAdditive(string name)
        {
            foreach (var e in _loadedScenes)
                if (e.Name == name) return e.IsAdditive;
            return false;
        }

        private static string TrackedSummary()
        {
            if (_loadedScenes.Count == 0) return "(none)";
            var sb = new StringBuilder();
            foreach (var e in _loadedScenes)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(e.ToString());
            }
            return sb.ToString();
        }

        // ── 리셋 ─────────────────────────────────────────────────────────────

        public static void Reset()
        {
            FromSceneName       = ToSceneName = "";
            FromSceneBuildIndex = ToSceneBuildIndex = -1;
            _loadedScenes.Clear();
        }
    }
}
