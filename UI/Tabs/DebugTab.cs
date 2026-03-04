using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using com8com1.SCFPS;
using BloodshedModToolkit.Coop;
using BloodshedModToolkit.Coop.Mission;
using BloodshedModToolkit.Coop.Sync;

namespace BloodshedModToolkit.UI.Tabs
{
    internal sealed class DebugTab : IModTab
    {
        // ── Debug META SELECTION 캐시 ─────────────────────────────────────────
        private string _debugMetaSaveSlot = "?";
        private string _debugMetaMission  = "?";
        private string _debugMetaChar     = "?";
        private SaveDataManager? _debugSdm;

        // ── 선택된 에셋 + 미리 캐싱된 이름 (draw 타임 .name 접근 방지) ──────────
        private PlayableCharacterData?  _debugSelectedChar         = null;
        private string                  _debugSelectedCharName      = "";
        private MissionAsset?           _debugSelectedMission      = null;
        private string                  _debugSelectedMissionName   = "";

        // ── 스캔 결과: (에셋, 미리 캐싱된 이름) 튜플 배열 ────────────────────────
        private (PlayableCharacterData Data, string Name)[]    _debugCharList    = System.Array.Empty<(PlayableCharacterData, string)>();
        private (MissionAsset Data, string DisplayName)[]      _debugMissionList = System.Array.Empty<(MissionAsset, string)>();
        private bool _debugScanned = false;

        // ── Debug 씬 로드 입력 및 검증 ─────────────────────────────────────────
        private string _debugSceneInput = "";
        private string _debugSceneValidationError = "";

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

        public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == MissionState.MetaGameScene)
            {
                _debugSelectedChar         = null;
                _debugSelectedCharName     = "";
                _debugSelectedMission      = null;
                _debugSelectedMissionName  = "";
                _debugScanned              = false;
                _debugMetaChar             = "?";
                _debugMetaMission          = "?";
                _debugSceneValidationError = "";
                Plugin.Log.LogInfo("[ModMenu] MetaGame 재진입 — DEBUG 선택 초기화");
            }
        }

        public void Draw(ModMenuContext ctx)
        {
            if (!_debugScanned)
            {
                ScanCharacters();
                ScanMissions();
                _debugScanned = true;
            }

            ctx.ScrollDebug = GUILayout.BeginScrollView(ctx.ScrollDebug, GUILayout.ExpandHeight(true));

            // ── CURRENT STATE ────────────────────────────────────────────────
            ctx.SectionHeader("CURRENT STATE");
            var cur = SceneManager.GetActiveScene();
            GUILayout.Label($"Active : {cur.name} (idx={cur.buildIndex})", ctx.StSliderName!);
            GUILayout.Label($"From   : {(SceneTracker.FromSceneName.Length > 0 ? SceneTracker.FromSceneName + $"(idx={SceneTracker.FromSceneBuildIndex})" : "(none)")}", ctx.StSliderName!);
            GUILayout.Label($"Status : {MissionState.Status}", ctx.StSliderName!);
            GUILayout.Label($"Pending: {(MissionState.PendingSceneName.Length > 0 ? MissionState.PendingSceneName : "(none)")}", ctx.StSliderName!);
            GUILayout.Label($"CharSel: {MissionState.GuestCharacterSelected}", ctx.StSliderName!);
            GUILayout.Label($"Coop   : Enabled={CoopState.IsEnabled} Host={CoopState.IsHost} Connected={CoopState.IsConnected}", ctx.StSliderName!);

            // ── LOADED SCENES ────────────────────────────────────────────────
            ctx.SectionHeader("LOADED SCENES");
            var scenes = SceneTracker.LoadedScenes;
            if (scenes.Count == 0)
                GUILayout.Label("  (none)", ctx.StSliderName!);
            else
                foreach (var s in scenes)
                    GUILayout.Label(
                        $"  {(s.IsAdditive ? "[+]" : "[ ]")} {s.Name} (idx={s.BuildIndex})",
                        ctx.StSliderName!);

            // ── META SELECTION ───────────────────────────────────────────────
            ctx.SectionHeader("META SELECTION");
            GUILayout.Label($"SaveSlot: {_debugMetaSaveSlot}", ctx.StSliderName!);
            GUILayout.Label($"Mission : {_debugMetaMission}", ctx.StSliderName!);
            GUILayout.Label($"Char    : {_debugMetaChar}", ctx.StSliderName!);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("REFRESH", ctx.StActionBtn!))
                RefreshMetaSelection(ctx);
            if (_debugSdm != null)
            {
                for (int slot = 0; slot < 3; slot++)
                {
                    bool active = _debugMetaSaveSlot == slot.ToString();
                    int s = slot;
                    if (GUILayout.Button($"Slot {s}", active ? ctx.StPresetOn! : ctx.StPresetOff!))
                    {
                        _debugSdm.SetActiveSaveSlot(s);
                        _debugMetaSaveSlot = s.ToString();
                    }
                }
            }
            GUILayout.EndHorizontal();

            // ── CHARACTER SELECT ─────────────────────────────────────────────
            ctx.SectionHeader("CHARACTER SELECT");
            if (_debugCharList.Length == 0)
                GUILayout.Label("(MetaGame 씬에서 REFRESH 필요)", ctx.StSliderName!);
            else
            {
                // 캐싱된 Name 사용 — draw 타임 .name 접근 없음
                var selectedCharName = _debugSelectedCharName;
                ctx.DrawButtonGrid(_debugCharList, 3,
                    e => e.Name,
                    e => selectedCharName == e.Name,
                    e => SelectCharacter(ctx, e.Data, e.Name));
            }

            // ── MISSION SELECT ────────────────────────────────────────────────
            ctx.SectionHeader("MISSION SELECT");
            if (_debugMissionList.Length == 0)
                GUILayout.Label("(MetaGame 씬에서 REFRESH 필요)", ctx.StSliderName!);
            else
            {
                // 캐싱된 DisplayName 사용 — draw 타임 .name 접근 없음
                var selectedMissionName = _debugSelectedMissionName;
                ctx.DrawButtonGrid(_debugMissionList, 2,
                    e => e.DisplayName,
                    e => selectedMissionName == e.DisplayName,
                    e => SelectMission(ctx, e.Data, e.DisplayName));
            }

            // ── FORCE SCENE LOAD ─────────────────────────────────────────────
            ctx.SectionHeader("FORCE SCENE LOAD");
            GUILayout.Label("씬 이름 (클립보드 붙여넣기):", ctx.StSliderName!);
            GUILayout.Label(
                _debugSceneInput.Length > 0 ? _debugSceneInput : "(empty)",
                ctx.StSliderValue!);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("PASTE", ctx.StActionBtn!))
                _debugSceneInput = GUIUtility.systemCopyBuffer?.Trim() ?? "";
            if (_debugSceneInput.Length > 0 && GUILayout.Button("CLEAR", ctx.StResetBtn!))
                _debugSceneInput = "";
            GUILayout.EndHorizontal();

            GUILayout.Space(2);
            foreach (var (group, entries) in _sceneGroups)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(group + ":", ctx.StSliderName!, GUILayout.Width(28f));
                foreach (var (scene, label) in entries)
                {
                    bool active = _debugSceneInput == scene;
                    if (GUILayout.Button(label, active ? ctx.StPresetOn! : ctx.StPresetOff!))
                        _debugSceneInput = scene;
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(4);

            if (_debugSceneValidationError.Length > 0)
            {
                var errStyle = new GUIStyle(ctx.StSliderName!)
                    { normal = { textColor = new Color(1f, 0.2f, 0.2f) }, wordWrap = true };
                GUILayout.Label(_debugSceneValidationError, errStyle);
            }

            if (GUILayout.Button("LOAD SCENE", ctx.StActionBtn!))
            {
                if (_debugSceneInput.Length == 0)
                {
                    _debugSceneValidationError = "씬 이름이 비어있습니다";
                    Plugin.Log.LogWarning("[Debug] 씬 이름이 비어있습니다");
                }
                else if (ValidateSceneLoad(_debugSceneInput, out string reason))
                {
                    _debugSceneValidationError = "";
                    ApplyDebugSelections(ctx);
                    Plugin.Log.LogInfo($"[Debug] ForceLoadScene → '{_debugSceneInput}'");
                    SceneManager.LoadScene(_debugSceneInput);
                }
                else
                {
                    _debugSceneValidationError = reason;
                    Plugin.Log.LogWarning($"[Debug] 씬 로드 검증 실패: {reason}");
                }
            }

            GUILayout.EndScrollView();
        }

        private bool ValidateSceneLoad(string sceneName, out string reason)
        {
            if (!_sceneMissionHints.ContainsKey(sceneName))
            {
                reason = "";
                return true;
            }

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

            if (_debugSelectedChar == null)
            {
                reason = "Character: 캐릭터 미선택\nDEBUG 패널 CHARACTER SELECT에서 선택 필요";
                return false;
            }

            // 캐싱된 미션 이름으로 검증 (live .name 접근 불필요)
            if (string.IsNullOrEmpty(_debugSelectedMissionName))
            {
                reason = "Mission: 미션 미선택\nDEBUG 패널 MISSION SELECT에서 선택 필요";
                return false;
            }

            if (_sceneMissionHints.TryGetValue(sceneName, out var hints))
            {
                string mName = _debugSelectedMissionName.ToLower();
                bool match = System.Array.Exists(hints, h => mName.Contains(h));
                if (!match)
                {
                    reason = $"Mission 불일치: '{_debugSelectedMissionName}' → '{sceneName}'\n" +
                             $"예상 키워드: [{string.Join(", ", hints)}]";
                    return false;
                }
            }

            reason = "";
            return true;
        }

        private void RefreshMetaSelection(ModMenuContext ctx)
        {
            _debugScanned      = false;
            _debugSdm          = UnityEngine.Object.FindObjectOfType<SaveDataManager>();
            _debugMetaSaveSlot = _debugSdm != null ? _debugSdm.activeSaveSlot.ToString() : "N/A";
            _debugMetaChar     = _debugSelectedCharName.Length > 0 ? _debugSelectedCharName : "(none)";
            _debugMetaMission  = _debugSelectedMissionName.Length > 0 ? _debugSelectedMissionName : "(none)";
        }

        /// <summary>
        /// 스캔 시점에 이름을 안전하게 읽어 캐싱. draw 타임 .name 접근을 차단.
        /// IL2CPP 네이티브 포인터 무효화 대비 try-catch 포함.
        /// </summary>
        private void ScanCharacters()
        {
            var raw  = ModMenuContext.FindAllAssets<PlayableCharacterData>();
            var seen  = new HashSet<string>();
            var temp  = new List<(PlayableCharacterData, string)>(raw.Length);
            foreach (var c in raw)
            {
                if (c == null) continue;
                string name;
                try { name = c.name; } catch { continue; }
                if (string.IsNullOrEmpty(name)) continue;
                if (seen.Add(name)) temp.Add((c, name));
            }
            _debugCharList = temp.ToArray();
            Plugin.Log.LogInfo($"[Debug] 캐릭터 스캔: raw={raw.Length} unique={_debugCharList.Length}");
        }

        private void ScanMissions()
        {
            var raw  = ModMenuContext.FindAllAssets<MissionAsset>();
            var temp = new List<(MissionAsset, string)>(raw.Length);
            foreach (var m in raw)
            {
                if (m == null) continue;
                string name;
                try { name = m.name; } catch { continue; }
                if (string.IsNullOrEmpty(name) || !char.IsDigit(name[0])) continue;

                string displayName;
                try { displayName = !string.IsNullOrEmpty(m.strMissionTitle) ? m.strMissionTitle : name; }
                catch { displayName = name; }

                temp.Add((m, displayName));
            }
            _debugMissionList = temp.ToArray();
            Plugin.Log.LogInfo($"[Debug] 미션 스캔: raw={raw.Length} standard={_debugMissionList.Length}");
        }

        private void SelectCharacter(ModMenuContext ctx, PlayableCharacterData cd, string name)
        {
            _debugSelectedChar     = cd;
            _debugSelectedCharName = name;
            _debugMetaChar         = name;
            var ss = ctx.SS(); if (ss != null) ss.selectedCharacterData = cd;
            Plugin.Log.LogInfo($"[Debug] 캐릭터 선택: '{name}'");
        }

        private void SelectMission(ModMenuContext ctx, MissionAsset m, string displayName)
        {
            _debugSelectedMission     = m;
            _debugSelectedMissionName = displayName;
            _debugMetaMission         = displayName;
            var ss = ctx.SS(); if (ss != null) ss.selectedMission = m;
            Plugin.Log.LogInfo($"[Debug] 미션 선택: '{displayName}'");
        }

        private void ApplyDebugSelections(ModMenuContext ctx)
        {
            var ss  = ctx.SS();
            var csm = ctx.CSM();
            if (_debugSelectedChar != null)
            {
                if (ss  != null) ss.selectedCharacterData = _debugSelectedChar;
                if (csm != null) { csm.selectedCharacter = _debugSelectedChar; csm.SetSelectedCharacter(_debugSelectedChar); }
                Plugin.Log.LogInfo($"[Debug] Apply: char='{_debugSelectedCharName}'");
            }
            if (_debugSelectedMission != null && ss != null)
            {
                ss.selectedMission = _debugSelectedMission;
                Plugin.Log.LogInfo($"[Debug] Apply: mission='{_debugSelectedMissionName}'");
            }
        }
    }
}
