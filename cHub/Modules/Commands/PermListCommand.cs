using System;
using System.Collections.Generic;
using System.Linq;
using cHub.Core;
using cHub.Services.Commands;
using cHub.Services.Permissions;
using cHub.Services.Players;
using cHub.Services.Roles;

namespace cHub.Modules.Commands
{
    /// <summary>
    /// Displays the direct and effective permissions
    /// assigned to a player.
    ///
    /// Direct permissions:
    /// - permissions explicitly granted to the player
    ///
    /// Effective permissions:
    /// - direct player permissions
    /// - role permissions
    /// - inherited role permissions
    /// - wildcard (*) access
    /// </summary>
    public class PermListCommand : CommandDefinition
    {
        // =========================================================
        // COMMAND INFO
        // =========================================================

        public override string Name =>
            "permlist";

        public override string Description =>
            "Lists direct and effective permissions for a player.";

        public override string Usage =>
            "/permlist <player>";

        // =========================================================
        // PERMISSION
        // =========================================================

        public override string RequiredPermission =>
            "permissions.view";

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
            // SERVICES
            // =====================================================

            PermissionService permissionService =
                ServiceRegistry.Get<PermissionService>();

            if (permissionService == null)
            {
                return CommandResult.Fail(
                    "Permission service is unavailable."
                );
            }

            RoleService roleService =
                ServiceRegistry.Get<RoleService>();

            if (roleService == null)
            {
                return CommandResult.Fail(
                    "Role service is unavailable."
                );
            }

            // =====================================================
            // DIRECT PERMISSIONS
            // =====================================================

            IReadOnlyCollection<string> directPermissions =
                permissionService.GetPlayerPermissions(
                    playerId
                );

            // =====================================================
            // EFFECTIVE PERMISSIONS
            // =====================================================

            IReadOnlyCollection<string> effectivePermissions =
                roleService.GetEffectivePermissions(
                    playerId
                );

            // =====================================================
            // EFFECTIVE ROLES
            // =====================================================

            IReadOnlyCollection<RoleDefinition> effectiveRoles =
                roleService.GetEffectiveRoles(
                    playerId
                );

            // =====================================================
            // BUILD RESPONSE
            // =====================================================

            List<string> lines =
                new List<string>();

            lines.Add(
                $"Permissions for {playerName}:"
            );

            lines.Add(
                $"Player ID: {playerId}"
            );

            // =====================================================
            // ROLES
            // =====================================================

            if (effectiveRoles.Count == 0)
            {
                lines.Add(
                    "Roles: none"
                );
            }
            else
            {
                lines.Add(
                    "Roles: " +
                    string.Join(
                        ", ",
                        effectiveRoles.Select(
                            role => role.Id
                        )
                    )
                );
            }

            // =====================================================
            // DIRECT
            // =====================================================

            if (directPermissions.Count == 0)
            {
                lines.Add(
                    "Direct permissions: none"
                );
            }
            else
            {
                lines.Add(
                    $"Direct permissions ({directPermissions.Count}):"
                );

                foreach (
                    string permission in
                    directPermissions.OrderBy(
                        value => value
                    ))
                {
                    lines.Add(
                        $"- {permission}"
                    );
                }
            }

            // =====================================================
            // EFFECTIVE
            // =====================================================

            if (effectivePermissions.Count == 0)
            {
                lines.Add(
                    "Effective permissions: none"
                );
            }
            else
            {
                lines.Add(
                    $"Effective permissions ({effectivePermissions.Count}):"
                );

                foreach (
                    string permission in
                    effectivePermissions.OrderBy(
                        value => value
                    ))
                {
                    lines.Add(
                        $"- {permission}"
                    );
                }
            }

            // =====================================================
            // RESPONSE
            // =====================================================

            return CommandResult.Ok(
                string.Join(
                    Environment.NewLine,
                    lines
                )
            );
        }
    }
}