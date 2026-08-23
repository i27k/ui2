using System;
using cHub.Core;
using cHub.Services.Commands;
using cHub.Services.Permissions;
using cHub.Services.Players;

using PermissionIds = cHub.Shared.Constants.Permissions;

namespace cHub.Modules.Commands
{
    /// <summary>
    /// Clears all direct permissions assigned to a player.
    ///
    /// Supports:
    /// - online players
    /// - offline players stored in players.json
    /// - direct EOS IDs
    ///
    /// IMPORTANT:
    /// This only clears DIRECT player permissions.
    ///
    /// It does NOT remove:
    /// - roles
    /// - role permissions
    /// - inherited role permissions
    /// </summary>
    public class PermClearCommand : CommandDefinition
    {
        // =========================================================
        // COMMAND INFO
        // =========================================================

        public override string Name =>
            "permclear";

        public override string Description =>
            "Clears all direct permissions assigned to a player.";

        public override string Usage =>
            "/permclear <player>";

        // =========================================================
        // REQUIRED PERMISSION
        // =========================================================

        public override string RequiredPermission =>
            PermissionIds.PermissionsClear;

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

            if (context.ArgumentCount < 1)
            {
                return CommandResult.Fail(
                    $"Usage: {Usage}"
                );
            }

            string playerInput =
                context.GetArgument(0);

            if (string.IsNullOrWhiteSpace(
                playerInput))
            {
                return CommandResult.Fail(
                    $"Usage: {Usage}"
                );
            }

            playerInput =
                playerInput.Trim();

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
            // CURRENT DIRECT PERMISSIONS
            // =====================================================

            var directPermissions =
                permissionService.GetPlayerPermissions(
                    playerId
                );

            if (directPermissions.Count == 0)
            {
                return CommandResult.Fail(
                    $"'{playerName}' has no direct permissions to clear."
                );
            }

            int permissionCount =
                directPermissions.Count;

            // =====================================================
            // CLEAR DIRECT PERMISSIONS
            // =====================================================

            bool cleared =
                permissionService.ClearPlayerPermissions(
                    playerId
                );

            if (!cleared)
            {
                return CommandResult.Fail(
                    $"Failed to clear direct permissions for '{playerName}'."
                );
            }

            // =====================================================
            // RESPONSE
            // =====================================================

            return CommandResult.Ok(
                $"Cleared {permissionCount} direct permission(s) from '{playerName}' ({playerId})."
            );
        }
    }
}