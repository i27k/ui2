using cHub.Shared.Utils;

namespace cHub.Core
{
    public static class HarmonyLoader
    {
        private const string HarmonyId = "cHub";

        private static HarmonyLib.Harmony _instance;

        public static bool IsInitialized
        {
            get { return _instance != null; }
        }

        public static void Initialize()
        {
            if (IsInitialized)
            {
                Logger.Warning("Harmony is already initialized.");
                return;
            }

            _instance = new HarmonyLib.Harmony(HarmonyId);

            _instance.PatchAll();

            Logger.Info("Harmony patches applied.");
        }

        public static void Shutdown()
        {
            if (!IsInitialized)
            {
                return;
            }

            _instance.UnpatchSelf();
            _instance = null;

            Logger.Info("Harmony unloaded.");
        }
    }
}

[HarmonyLib.HarmonyPatch(typeof(GameManager), "Update")]
internal static class cHubGlobalKeybindPatch
{
    private static void Postfix()
    {
        cHub.Modules.AdminPanel.AdminPanelService.ProcessGlobalKeybinds();
    }
}

[HarmonyLib.HarmonyPatch(typeof(GameManager), "AwardKill")]
internal static class cHubRotKillPatch
{
    private static void Postfix(EntityAlive __0, EntityAlive __1)
    {
        cHub.Modules.AdminPanel.RotService.AwardZombieKill(
            __0 as EntityPlayer, __1);
    }
}
