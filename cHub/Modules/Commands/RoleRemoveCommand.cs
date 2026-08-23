using cHub.Core;
using cHub.Services.Commands;
using cHub.Services.Players;
using cHub.Services.Roles;

namespace cHub.Modules.Commands
{
    public class RoleRemoveCommand : CommandDefinition
    {
        // =========================================================
        // COMMAND INFO
        // =========================================================

        public override string Name =>
            "roleremove";

        public override string Description =>
            "Removes a cHub role from a player.";

        public override string Usage =>
            "/roleremove <player> <role>";

        // =========================================================
        // PERMISSION
        // =========================================================

        public override string RequiredPermission =>
            "roles.remove";

        // =========================================================
        // EXECUTE
        // =========================================================

        public override CommandResult Execute(
            CommandContext context)
        {
            if (context == null)
            {
                return CommandResult.Fail(
                    "Invalid command context."
                );
            }

            // =====================================================
            // ARGUMENTS
            // =========================================================

            if (context.ArgumentCount < 2)
            {
                return CommandResult.Fail(
                    $"Usage: {Usage}"
                );
            }

            string playerInput =
                context.GetArgument(0);

            string roleId =
                context.GetArgument(1);

            if (string.IsNullOrWhiteSpace(playerInput))
            {
                return CommandResult.Fail(
                    "Player cannot be empty."
                );
            }

            if (string.IsNullOrWhiteSpace(roleId))
            {
                return CommandResult.Fail(
                    "Role cannot be empty."
                );
            }

            // =====================================================
            // RESOLVE PLAYER
            // =========================================================

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
            // =========================================================

            RoleManagementService roleManagementService =
                ServiceRegistry.Get<RoleManagementService>();

            if (roleManagementService == null)
            {
                return CommandResult.Fail(
                    "Role management service is unavailable."
                );
            }

            // =====================================================
            // REMOVE ROLE
            //
            // targetPlayerId = EOS / internal ID
            // player name is never used as the persistence key.
            // =========================================================

            bool removed =
                roleManagementService.RemoveRole(
                    context.ActorId,
                    targetPlayerId,
                    roleId
                );

            if (!removed)
            {
                return CommandResult.Fail(
                    $"Could not remove role '{roleId}' from player '{targetPlayerName}'."
                );
            }

            // =====================================================
            // SUCCESS
            // =========================================================

            return CommandResult.Ok(
                $"Role '{roleId}' removed from {targetPlayerName} successfully."
            );
        }
    }
}