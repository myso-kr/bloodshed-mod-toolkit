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

        // ── 선택된 에셋 + 캐싱된 이름 (draw 타임 .name 접근 방지) ─────────────
        private PlayableCharacterData?  _debugSelectedChar             = null;
        private string                  _debugSelectedCharName          = "";
        private MissionAsset?           _debugSelectedMission          = null;
        private string                  _debugSelectedMissionAssetName  = "";  // 씬명 매칭용
        private string                  _debugSelectedMissionName       = "";  // 표시용

        // ── 스캔 결과: (에셋, 에셋명, 표시명) 튜플 배열 ─────────────────────────
        // AssetName = MissionAsset.name (= 씬명과 직접 비교되는 키)
        // DisplayName = strMissionTitle ?? AssetName (UI 표시용)
        private (PlayableCharacterData Data, string Name)[]             _debugCharList    = System.Array.Empty<(PlayableCharacterData, string)>();
        private (MissionAsset Data, string AssetName, string DisplayName)[] _debugMissionList = System.Array.Empty<(MissionAsset, string, string)>();
        private bool _debugScanned = false;

        // ── Debug 씬 로드 입력 및 검증 ─────────────────────────────────────────
        private string _debugSceneInput = "";
        private string _debugSceneValidationError = "";

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
                _debugSelectedChar            = null;
                _debugSelectedCharName        = "";
                _debugSelectedMission         = null;
                _debugSelectedMissionAssetName = "";
                _debugSelectedMissionName     = "";
                _debugScanned                 = false;
                _debugMetaChar                = "?";
                _debugMetaMission             = "?";
                _debugSceneValidationError    = "";
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
            // 표시명 + [에셋명] 으로 씬명과의 대응 관계를 명시
            string missionDisplay = _debugSelectedMissionAssetName.Length > 0
                ? $"{_debugMetaMission} [{_debugSelectedMissionAssetName}]"
                : _debugMetaMission;
            GUILayout.Label($"Mission : {missionDisplay}", ctx.StSliderName!);
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
                // isActive: 에셋명 기준 비교 (표시명 충돌 방지)
                var selectedAssetName = _debugSelectedMissionAssetName;
                ctx.DrawButtonGrid(_debugMissionList, 2,
                    e => e.DisplayName,
                    e => selectedAssetName == e.AssetName,
                    e => SelectMission(ctx, e.Data, e.AssetName, e.DisplayName));
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

        /// <summary>
        /// 씬 로드 전 검증.
        /// 스캔된 미션 목록에서 MissionAsset.name == sceneName 정확 일치로 미션 씬 판정.
        /// 키워드 추론 없음 — 에셋명이 씬명과 동일한 게임 구조를 직접 활용.
        /// </summary>
        private bool ValidateSceneLoad(string sceneName, out string reason)
        {
            // 스캔된 미션 중 에셋명이 씬명과 정확히 일치하는 항목 탐색
            string? requiredDisplayName = null;
            foreach (var (_, assetName, displayName) in _debugMissionList)
            {
                if (string.Equals(assetName, sceneName, System.StringComparison.OrdinalIgnoreCase))
                {
                    requiredDisplayName = displayName;
                    break;
                }
            }

            // 미션 씬이 아니면 검증 불필요 (MetaGame, SampleScene, Graveyard 등)
            if (requiredDisplayName == null)
            {
                reason = "";
                return true;
            }

            // ── SaveSlot ────────────────────────────────────────────────────
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

            // ── Character ───────────────────────────────────────────────────
            if (_debugSelectedChar == null)
            {
                reason = "Character: 캐릭터 미선택\nCHARACTER SELECT에서 선택 필요";
                return false;
            }

            // ── Mission — 에셋명 정확 일치 ─────────────────────────────────
            if (!string.Equals(_debugSelectedMissionAssetName, sceneName,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                reason = string.IsNullOrEmpty(_debugSelectedMissionAssetName)
                    ? $"Mission: 미션 미선택\n'{requiredDisplayName}' [{sceneName}] 을(를) MISSION SELECT에서 선택 필요"
                    : $"Mission 불일치: '{_debugSelectedMissionName}' [{_debugSelectedMissionAssetName}] ≠ '{sceneName}'\n'{requiredDisplayName}' [{sceneName}] 을(를) 선택 필요";
                return false;
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
        /// 스캔 시점에 .name을 안전하게 읽어 캐싱. draw 타임 접근 차단.
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

        /// <summary>
        /// MissionAsset을 스캔하여 에셋명(= 씬명 키)과 표시명을 캐싱.
        /// 이름이 숫자로 시작하는 표준 미션만 포함 (CustomSession_*, DEMO_* 제외).
        /// </summary>
        private void ScanMissions()
        {
            var raw  = ModMenuContext.FindAllAssets<MissionAsset>();
            var temp = new List<(MissionAsset, string, string)>(raw.Length);
            foreach (var m in raw)
            {
                if (m == null) continue;
                string assetName;
                try { assetName = m.name; } catch { continue; }
                if (string.IsNullOrEmpty(assetName) || !char.IsDigit(assetName[0])) continue;

                string displayName;
                try { displayName = !string.IsNullOrEmpty(m.strMissionTitle) ? m.strMissionTitle : assetName; }
                catch { displayName = assetName; }

                temp.Add((m, assetName, displayName));
            }
            _debugMissionList = temp.ToArray();
            Plugin.Log.LogInfo($"[Debug] 미션 스캔: raw={raw.Length} standard={_debugMissionList.Length}");
            if (_debugMissionList.Length > 0)
            {
                foreach (var (_, a, d) in _debugMissionList)
                    Plugin.Log.LogDebug($"  mission: assetName='{a}' display='{d}'");
            }
        }

        private void SelectCharacter(ModMenuContext ctx, PlayableCharacterData cd, string name)
        {
            _debugSelectedChar     = cd;
            _debugSelectedCharName = name;
            _debugMetaChar         = name;
            var ss = ctx.SS(); if (ss != null) ss.selectedCharacterData = cd;
            Plugin.Log.LogInfo($"[Debug] 캐릭터 선택: '{name}'");
        }

        private void SelectMission(ModMenuContext ctx, MissionAsset m, string assetName, string displayName)
        {
            _debugSelectedMission          = m;
            _debugSelectedMissionAssetName = assetName;
            _debugSelectedMissionName      = displayName;
            _debugMetaMission              = displayName;
            var ss = ctx.SS(); if (ss != null) ss.selectedMission = m;
            Plugin.Log.LogInfo($"[Debug] 미션 선택: assetName='{assetName}' display='{displayName}'");
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
                Plugin.Log.LogInfo($"[Debug] Apply: mission='{_debugSelectedMissionAssetName}'");
            }
        }
    }
}
