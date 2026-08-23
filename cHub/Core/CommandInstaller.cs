using cHub.Modules.Commands;
using cHub.Services.Commands;

namespace cHub.Core
{
    public static class CommandInstaller
    {
        public static void RegisterCommands()
        {
            CommandService commandService =
                ServiceRegistry.Get<CommandService>();

            if (commandService == null)
            {
                return;
            }

            // =====================================================
            // PUBLIC COMMANDS
            // =====================================================

            commandService.RegisterCommand(
                new RotCommand()
            );

            commandService.RegisterCommand(
                new TopRotCommand()
            );

            // =====================================================
            // ROLE COMMANDS
            // =====================================================

            commandService.RegisterCommand(
                new RoleListCommand()
            );

            commandService.RegisterCommand(
                new RoleAddCommand()
            );

            commandService.RegisterCommand(
                new RoleRemoveCommand()
            );

            commandService.RegisterCommand(
                new RoleClearCommand()
            );

            // =====================================================
            // PERMISSION COMMANDS
            // =====================================================

            commandService.RegisterCommand(
                new HasPermissionCommand()
            );

            commandService.RegisterCommand(
                new PermListCommand()
            );

            commandService.RegisterCommand(
                new PermGrantCommand()
            );

            commandService.RegisterCommand(
                new PermRevokeCommand()
            );

            commandService.RegisterCommand(
                new PermClearCommand()
            );

            commandService.RegisterCommand(
                new PermsCommand()
            );
        }
    }
}
