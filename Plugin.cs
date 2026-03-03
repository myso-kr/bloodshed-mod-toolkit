using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Logging;
using HarmonyLib;
using BloodshedModToolkit.Coop.Net;
using BloodshedModToolkit.Coop.Ecs;
using BloodshedModToolkit.Coop.Bots;
using BloodshedModToolkit.Coop.Renderer;

namespace BloodshedModToolkit
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BasePlugin
    {
        public static Plugin Instance { get; private set; } = null!;
        public static new ManualLogSource Log { get; private set; } = null!;

        public override void Load()
        {
            Instance = this;
            Log = base.Log;

            CheatState.Initialize();
            Tweaks.TweakState.Initialize();

            var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            harmony.PatchAll();

            // IMGUI 치트 메뉴를 MonoBehaviour로 씬에 추가
            AddComponent<UI.CheatMenu>();

            // Co-op 네트워크 레이어
            AddComponent<NetManager>();

            // Co-op ECS 엔티티 스캐너 (Phase 3)
            AddComponent<EntityScanner>();

            // Co-op 상태 동기화 적용기 (Phase 5)
            AddComponent<StateApplicator>();

            // Phase 8 — AI 봇 + 아바타 렌더러 (검증용)
            AddComponent<BotManager>();
            AddComponent<RemotePlayerRenderer>();

            Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION} loaded.");
        }
    }

    internal static class MyPluginInfo
    {
        public const string PLUGIN_GUID    = "com.bloodshed.modtoolkit";
        public const string PLUGIN_NAME    = "Bloodshed Mod Toolkit";
        public const string PLUGIN_VERSION = "1.0.75";
    }
}
