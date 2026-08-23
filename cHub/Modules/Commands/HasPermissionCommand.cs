using System;
using cHub.Core;
using cHub.Services.Commands;
using cHub.Services.Roles;

using PermissionIds = cHub.Shared.Constants.Permissions;

namespace cHub.Modules.Commands
{
    /// <summary>
    /// Checks whether a player effectively has a specific permission.
    ///
    /// Effective permissions include:
    /// - direct player permissions
    /// - direct role permissions
    /// - inherited role permissions
    /// - wildcard (*) access
    ///
    /// Supports player IDs directly.
    /// </summary>
    public class HasPermissionCommand : CommandDefinition
    {
        // =========================================================
        // COMMAND INFO
        // =========================================================

        public override string Name =>
            "hasperm";

        public override string Description =>
            "Checks whether a player has a specific permission.";

        public override string Usage =>
            "/hasperm <playerId> <permission>";

        // =========================================================
        // REQUIRED PERMISSION
        // =========================================================

        public override string RequiredPermission =>
            PermissionIds.PermissionsView;

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

            string playerId =
                context.GetArgument(0);

            string permissionId =
                context.GetArgument(1);

            if (string.IsNullOrWhiteSpace(
                playerId) ||
                string.IsNullOrWhiteSpace(
                    permissionId))
            {
                return CommandResult.Fail(
                    $"Usage: {Usage}"
                );
            }

            playerId =
                playerId.Trim();

            permissionId =
                permissionId.Trim();

            // =====================================================
            // ROLE SERVICE
            // =====================================================

            RoleService roleService =
                ServiceRegistry.Get<RoleService>();

            if (roleService == null)
            {
                return CommandResult.Fail(
                    "Role service is unavailable."
                );
            }

            // =====================================================
            // CHECK EFFECTIVE PERMISSION
            // =====================================================

            bool hasPermission =
                roleService.HasPermission(
                    playerId,
                    permissionId
                );

            // =====================================================
            // RESPONSE
            // =====================================================

            if (hasPermission)
            {
                return CommandResult.Ok(
                    $"YES - '{playerId}' has permission '{permissionId}'."
                );
            }

            return CommandResult.Ok(
                $"NO - '{playerId}' does not have permission '{permissionId}'."
            );
        }
    }
}