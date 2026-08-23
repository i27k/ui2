using cHub.Services.Commands;
using cHub.Modules.AdminPanel;
using cHub.Services.Config;
using cHub.Services.Permissions;
using cHub.Services.Players;
using cHub.Services.Roles;
using cHub.Services.Chat;

namespace cHub.Core
{
    public static class ServiceInstaller
    {
        public static void RegisterServices()
        {
            // =====================================================
            // CONFIG
            // =====================================================

            ServiceRegistry.Register(
                new ConfigService()
            );

            // =====================================================
            // PERMISSIONS
            // =====================================================

            ServiceRegistry.Register(
                new PermissionService()
            );

            // =====================================================
            // PLAYER IDENTITIES
            // =====================================================

            ServiceRegistry.Register(
                new PlayerIdentityService()
            );

            // =====================================================
            // ROLES
            // =====================================================

            ServiceRegistry.Register(
                new RoleService()
            );

            ServiceRegistry.Register(
                new RoleManagementService()
            );

            // =====================================================
            // COMMANDS
            // =====================================================

            ServiceRegistry.Register(
                new CommandService()
            );

            // =====================================================
            // ADMIN PANEL
            // =====================================================

            ServiceRegistry.Register(
                new AdminPanelService()
            );

            ServiceRegistry.Register(
                new DiscordBridgeService()
            );
        }
    }
}
