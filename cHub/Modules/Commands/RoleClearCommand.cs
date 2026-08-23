using cHub.Core;
using cHub.Services.Commands;
using cHub.Services.Players;
using cHub.Services.Roles;

namespace cHub.Modules.Commands
{
    public class RoleClearCommand : CommandDefinition
    {
        public override string Name =>
            "roleclear";

        public override string Description =>
            "Removes all cHub roles from a player.";

        public override string Usage =>
            "/roleclear <player>";

        public override string RequiredPermission =>
            "roles.clear";

        public override CommandResult Execute(
            CommandContext context)
        {
            if (context == null)
            {
                return CommandResult.Fail(
                    "Invalid command context."
                );
            }

            if (context.ArgumentCount < 1)
            {
                return CommandResult.Fail(
                    $"Usage: {Usage}"
                );
            }

            string playerInput =
                context.GetArgument(0);

            if (string.IsNullOrWhiteSpace(playerInput))
            {
                return CommandResult.Fail(
                    "Player cannot be empty."
                );
            }

            // =====================================================
            // RESOLVE PLAYER
            // =====================================================

            string targetPlayerId;
            string targetPlayerName;

            if (!PlayerResolver.TryResolvePlayerId(
                playerInput,
                out targetPlayerId,
                out targetPlayerName))
            {
                return CommandResult.Fail(
                    $"Player '{playerInput}' could not be found."
                );
            }

            if (string.IsNullOrWhiteSpace(targetPlayerId))
            {
                return CommandResult.Fail(
                    $"Could not resolve player ID for '{playerInput}'."
                );
            }

            // =====================================================
            // ROLE MANAGEMENT SERVICE
            // =====================================================

            RoleManagementService roleManagementService =
                ServiceRegistry.Get<RoleManagementService>();

            if (roleManagementService == null)
            {
                return CommandResult.Fail(
                    "Role management service is unavailable."
                );
            }

            // =====================================================
            // CLEAR PLAYER ROLES
            // =====================================================

            bool cleared =
                roleManagementService.ClearPlayerRoles(
                    context.ActorId,
                    targetPlayerId
                );

            if (!cleared)
            {
                return CommandResult.Fail(
                    $"Could not clear roles for player '{targetPlayerName}'."
                );
            }

            return CommandResult.Ok(
                $"All roles removed from {targetPlayerName} successfully."
            );
        }
    }
}