using cHub.Services.Commands;
using cHub.Services.Events;
using cHub.Shared.Utils;
using System.Linq;
using cHub.Modules.Optimizations.Client;

namespace cHub.Core
{
    public class Bootstrap : IModApi
    {
        public void InitMod(Mod modInstance)
        {
            StartupTerminal.Create();
            Logger.Info(
                "Bootstrap"
            );

            //====== Modificari i27k ========

            FastEscMenuOptimization.Create();

            //===============================

            // =====================================================
            // HARMONY
            // =====================================================

            HarmonyLoader.Initialize();

            // =====================================================
            // SERVICES
            // =====================================================

            ServiceInstaller.RegisterServices();
            ServiceRegistry.Initialize();

            // =====================================================
            // GAME EVENT LISTENERS
            // =====================================================

            PlayerJoinedGameListener.Register();

            // =====================================================
            // COMMANDS
            // =====================================================

            CommandInstaller.RegisterCommands();

            ChatCommandListener.Register();

            // =====================================================
            // MODULES
            // =====================================================

            ModuleInstaller.RegisterModules();

            ModuleRegistry.Initialize();
            ModuleRegistry.EnableAll();

            LogFeatureManifest();

            // =====================================================
            // DONE
            // =====================================================

            Logger.Info(
                "Initialized"
            );
        }

        private static void LogFeatureManifest()
        {
            CommandService commands = ServiceRegistry.Get<CommandService>();
            var roles = ServiceRegistry.Get<cHub.Services.Roles.RoleService>();
            var permissions = ServiceRegistry.Get<cHub.Services.Permissions.PermissionService>();
            Logger.Info("========== c/Hub FEATURE MANIFEST ==========");
            Logger.Info($"Runtime: AdminPanel/XUi, launcher tab, drag, resize, CapsLock interaction [READY]");
            Logger.Info($"Access: roles + server permissions [roles={roles?.GetRoles().Count() ?? 0}]");
            Logger.Info($"Commands: chat bridge + Owner Command Center [registered={commands?.RegisteredCommandCount ?? 0}]");
            Logger.Info($"Permissions: direct grants + inherited roles [registered={permissions?.GetRegisteredPermissions().Count() ?? 0}]");
            Logger.Info("Gameplay: homes, player directory, teleport requests, admin teleport [READY]");
            Logger.Info("Administration: role manager, Zombie Director, Economy Test Console [READY]");
            Logger.Info("Progression footer: player, level, gamestage, XP, casinoCoin balance [READY]");
            Logger.Info("Rot/TopRot: personal score, global leaderboard, rotting flesh drops [READY]");
            Logger.Info("Appearance: themes, opacity, effects, keybind configuration [READY]");
            Logger.Info("Media: c/Hub startup terminal, main-menu branding, rotating loading screens [READY]");
            Logger.Info("Discord: open invite + copy-to-clipboard [READY]");
            Logger.Info("c/Hub AI: Responses API + review archive [" +
                (cHub.Modules.AdminPanel.GameAssistantService.IsConfigured ? "CONFIGURED" : "API KEY MISSING") + "]");
            Logger.Info("Dev Bridge: safe localhost architecture [PLANNED / NOT ACTIVE]");
            Logger.Info("Overhaul: realism + horror + sci-fi content foundation [READY]");
            Logger.Info("Overhaul effects: fatigue, nutrition, dehydration, radiation, toxin, EMP, temporal, nanites [READY]");
            Logger.Info("Overhaul infected: dormant, screecher, toxic, electric, stalker, volatile [READY]");
            Logger.Info("Overhaul equipment: radiation detector, anomaly scanner, energy cells, experimental medicine [READY]");
            Logger.Info("Overhaul authority: gameplay XML server-owned; presentation remains client-local [READY]");
            Logger.Info("=============================================");
        }
    }
}
