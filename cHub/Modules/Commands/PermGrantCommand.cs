using System;
using cHub.Core;
using cHub.Services.Commands;
using cHub.Services.Permissions;
using cHub.Services.Players;

using PermissionIds = cHub.Shared.Constants.Permissions;

namespace cHub.Modules.Commands
{
    /// <summary>
    /// Grants a direct permission to a player.
    ///
    /// Supports:
    /// - online players
    /// - offline players stored in players.json
    /// - direct EOS IDs
    ///
    /// This does NOT modify role permissions.
    /// It grants a permission directly to the selected player.
    /// </summary>
    public class PermGrantCommand : CommandDefinition
    {
        // =========================================================
        // COMMAND INFO
        // =========================================================

        public override string Name =>
            "permgrant";

        public override string Description =>
            "Grants a direct permission to a player.";

        public override string Usage =>
            "/permgrant <player> <permission>";

        // =========================================================
        // REQUIRED PERMISSION
        // =========================================================

        public override string RequiredPermission =>
            PermissionIds.PermissionsGrant;

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
            // =====================================================

            if (context.ArgumentCount < 2)
            {
                return CommandResult.Fail(
                    $"Usage: {Usage}"
                );
            }

            string playerInput =
                context.GetArgument(0);

            string permissionId =
                context.GetArgument(1);

            if (string.IsNullOrWhiteSpace(
                playerInput) ||
                string.IsNullOrWhiteSpace(
                    permissionId))
            {
                return CommandResult.Fail(
                    $"Usage: {Usage}"
                );
            }

            playerInput =
                playerInput.Trim();

            permissionId =
                permissionId.Trim();

            // =====================================================
            // RESOLVE PLAYER
            // =====================================================

            if (!PlayerResolver.TryResolvePlayerId(
                playerInput,
                out string playerId,
                out string playerName))
            {
                return CommandResult.Fail(
                    $"Player '{playerInput}' could not be resolved."
                );
            }

            // =====================================================
            // PERMISSION SERVICE
            // =====================================================

            PermissionService permissionService =
                ServiceRegistry.Get<PermissionService>();

            if (permissionService == null)
            {
                return CommandResult.Fail(
                    "Permission service is unavailable."
                );
            }

            // =====================================================
            // VALIDATE PERMISSION
            // =====================================================

            if (!permissionService.PermissionExists(
                permissionId))
            {
                return CommandResult.Fail(
                    $"Unknown permission '{permissionId}'."
                );
            }

            // =====================================================
            // ALREADY DIRECTLY GRANTED
            // =====================================================

            if (permissionService.HasPermission(
                playerId,
                permissionId))
            {
                return CommandResult.Fail(
                    $"'{playerName}' already has direct permission '{permissionId}'."
                );
            }

            // =====================================================
            // GRANT DIRECT PERMISSION
            // =====================================================

            bool granted =
                permissionService.GrantPermission(
                    playerId,
                    permissionId
                );

            if (!granted)
            {
                return CommandResult.Fail(
                    $"Failed to grant permission '{permissionId}' to '{playerName}'."
                );
            }

            // =====================================================
            // RESPONSE
            // =====================================================

            return CommandResult.Ok(
                $"Granted direct permission '{permissionId}' to '{playerName}' ({playerId})."
            );
        }
    }
}