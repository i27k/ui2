using System.Collections.Generic;
using cHub.Core;
using cHub.Services.Commands;
using cHub.Services.Players;
using cHub.Services.Roles;

namespace cHub.Modules.Commands
{
    public class RoleAddCommand : CommandDefinition
    {
        // =========================================================
        // COMMAND INFO
        // =========================================================

        public override string Name =>
            "roleadd";

        public override string Description =>
            "Assigns a cHub role to a player.";

        public override string Usage =>
            "/roleadd <player> <role>";

        // =========================================================
        // ALIASES
        // =========================================================

        public override IReadOnlyCollection<string> Aliases =>
            new[]
            {
                "addrole",
                "radd",
                "ra"
            };

        // =========================================================
        // PERMISSION
        // =========================================================

        public override string RequiredPermission =>
            "roles.assign";

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

            if (string.IsNullOrWhiteSpace(targetPlayerName))
            {
                targetPlayerName =
                    playerInput;
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
            // ASSIGN ROLE
            // =========================================================

            bool assigned =
                roleManagementService.AssignRole(
                    context.ActorId,
                    targetPlayerId,
                    roleId
                );

            if (!assigned)
            {
                return CommandResult.Fail(
                    $"Could not assign role '{roleId}' to player '{targetPlayerName}'."
                );
            }

            // =====================================================
            // SUCCESS
            // =========================================================

            return CommandResult.Ok(
                $"Role '{roleId}' assigned to {targetPlayerName} successfully."
            );
        }
    }
}