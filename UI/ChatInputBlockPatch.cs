using HarmonyLib;

namespace BloodshedModToolkit.UI
{
    [HarmonyPatch(typeof(com8com1.SCFPS.Q3PlayerController))]
    static class ChatInputBlockPatch
    {
        [HarmonyPatch("Accelerate")]
        [HarmonyPrefix]
        static bool BlockAccelerate() => !ChatWindow.IsTyping;

        [HarmonyPatch("AirControl")]
        [HarmonyPrefix]
        static bool BlockAirControl() => !ChatWindow.IsTyping;
    }
}
