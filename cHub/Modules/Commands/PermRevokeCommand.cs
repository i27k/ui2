using System;
using System.Linq;
using cHub.Core;
using cHub.Services.Commands;
using cHub.Services.Permissions;
using cHub.Services.Players;

using PermissionIds = cHub.Shared.Constants.Permissions;

namespace cHub.Modules.Commands
{
    /// <summary>
    /// Revokes a direct permission from a player.
    ///
    /// Supports:
    /// - online players
    /// - offline players stored in players.json
    /// - direct EOS IDs
    ///
    /// IMPORTANT:
    /// This command only removes permissions granted directly
    /// to the player.
    ///
    /// It does NOT remove permissions inherited from roles.
    /// </summary>
    public class PermRevokeCommand : CommandDefinition
    {
        // =========================================================
        // COMMAND INFO
        // =========================================================

        public override string Name =>
            "permrevoke";

        public override string Description =>
            "Revokes a direct permission from a player.";

        public override string Usage =>
            "/permrevoke <player> <permission>";

        // =========================================================
        // REQUIRED PERMISSION
        // =========================================================

        public override string RequiredPermission =>
            PermissionIds.PermissionsRevoke;

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
            // CHECK DIRECT PERMISSION
            // =====================================================

            bool hasDirectPermission =
                permissionService
                    .GetPlayerPermissions(playerId)
                    .Contains(
                        permissionId,
                        StringComparer.OrdinalIgnoreCase
                    );

            if (!hasDirectPermission)
            {
                return CommandResult.Fail(
                    $"'{playerName}' does not have direct permission '{permissionId}'."
                );
            }

            // =====================================================
            // REVOKE DIRECT PERMISSION
            // =====================================================

            bool revoked =
                permissionService.RevokePermission(
                    playerId,
                    permissionId
                );

            if (!revoked)
            {
                return CommandResult.Fail(
                    $"Failed to revoke permission '{permissionId}' from '{playerName}'."
                );
            }

            // =====================================================
            // RESPONSE
            // =====================================================

            return CommandResult.Ok(
                $"Revoked direct permission '{permissionId}' from '{playerName}' ({playerId})."
            );
        }
    }
}