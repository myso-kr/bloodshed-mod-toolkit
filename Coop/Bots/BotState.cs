namespace BloodshedModToolkit.Coop.Bots
{
    public enum WeaponClass { Melee = 0, Pistol, Rifle, Launcher }

    public static class BotState
    {
        public static bool Enabled { get; set; } = false;
        public static int  Count   { get; set; } = 1;   // 1–3

        public static readonly ulong[] BotSteamIds = {
            0xB07000000001UL, 0xB07000000002UL, 0xB07000000003UL };
        public static readonly string[] BotNames = {
            "Bot-Alpha", "Bot-Beta", "Bot-Gamma" };

        public static readonly WeaponClass[] BotWeaponClasses = new WeaponClass[3];

        public static void AssignWeaponClasses()
        {
            for (int i = 0; i < BotWeaponClasses.Length; i++)
                BotWeaponClasses[i] = (WeaponClass)(i % 4);
        }

        public static bool IsBot(ulong id)
        {
            foreach (var b in BotSteamIds) if (b == id) return true;
            return false;
        }
    }
}
