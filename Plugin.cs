using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Logging;
using HarmonyLib;

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

            var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            harmony.PatchAll();

            // IMGUI 치트 메뉴를 MonoBehaviour로 씬에 추가
            AddComponent<UI.CheatMenu>();

            Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION} loaded.");
        }
    }

    internal static class MyPluginInfo
    {
        public const string PLUGIN_GUID    = "com.bloodshed.modtoolkit";
        public const string PLUGIN_NAME    = "Bloodshed Mod Toolkit";
        public const string PLUGIN_VERSION = "1.0.2";
    }
}
