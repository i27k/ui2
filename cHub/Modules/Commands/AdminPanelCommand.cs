using cHub.Core;
using cHub.Modules.AdminPanel;
using cHub.Services.Commands;

using PermissionIds = cHub.Shared.Constants.Permissions;

namespace cHub.Modules.Commands
{
    public class AdminPanelCommand : CommandDefinition
    {
        public override string Name =>
            "chub";

        public override string Description =>
            "Opens the cHub administration panel.";

        public override string RequiredPermission =>
            PermissionIds.AdminPanelAccess;

        public override CommandResult Execute(
            CommandContext context)
        {
            if (context == null)
            {
                return CommandResult.Fail(
                    "Invalid command context."
                );
            }

            AdminPanelService adminPanelService =
                ServiceRegistry.Get<AdminPanelService>();

            if (adminPanelService == null)
            {
                return CommandResult.Fail(
                    "Admin Panel service is unavailable."
                );
            }

            if (!adminPanelService.TryOpen(
                context.ActorId,
                out string error))
            {
                return CommandResult.Fail(error);
            }

            return CommandResult.Silent();
        }
    }
}
