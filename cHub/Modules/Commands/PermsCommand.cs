using System;
using System.Collections.Generic;
using System.Linq;
using cHub.Core;
using cHub.Services.Commands;
using cHub.Services.Permissions;
using cHub.Services.Players;
using cHub.Services.Roles;

using PermissionIds = cHub.Shared.Constants.Permissions;

namespace cHub.Modules.Commands
{
    /// <summary>
    /// Detailed permission diagnostic command.
    ///
    /// Displays:
    /// - direct roles
    /// - effective roles
    /// - direct permissions
    /// - effective permissions
    ///
    /// Supports:
    /// - online players
    /// - offline players stored in players.json
    /// - direct EOS IDs
    /// </summary>
    public class PermsCommand : CommandDefinition
    {
        // =========================================================
        // COMMAND INFO
        // =========================================================

        public override string Name =>
            "perms";

        public override string Description =>
            "Displays detailed role and permission information for a player.";

        public override string Usage =>
            "/perms <player>";

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
            // DIRECT ROLES
            // =====================================================

            IReadOnlyCollection<RoleDefinition> directRoles =
                roleService.GetPlayerRoles(
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
            // BUILD RESPONSE
            // =====================================================

            List<string> lines =
                new List<string>();

            lines.Add(
                $"Permission report for {playerName}:"
            );

            lines.Add(
                $"Player ID: {playerId}"
            );

            // =====================================================
            // DIRECT ROLES
            // =====================================================

            if (directRoles.Count == 0)
            {
                lines.Add(
                    "Direct roles: none"
                );
            }
            else
            {
                lines.Add(
                    "Direct roles: " +
                    string.Join(
                        ", ",
                        directRoles
                            .OrderByDescending(
                                role => role.Priority
                            )
                            .Select(
                                role =>
                                    $"{role.Name} ({role.Id})"
                            )
                    )
                );
            }

            // =====================================================
            // EFFECTIVE ROLES
            // =====================================================

            if (effectiveRoles.Count == 0)
            {
                lines.Add(
                    "Effective roles: none"
                );
            }
            else
            {
                lines.Add(
                    "Effective roles: " +
                    string.Join(
                        " -> ",
                        effectiveRoles
                            .OrderByDescending(
                                role => role.Priority
                            )
                            .Select(
                                role =>
                                    role.Name
                            )
                    )
                );
            }

            // =====================================================
            // DIRECT PERMISSIONS
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
                    string permissionId
                    in directPermissions.OrderBy(
                        value => value
                    ))
                {
                    lines.Add(
                        $"- {permissionId}"
                    );
                }
            }

            // =====================================================
            // EFFECTIVE PERMISSIONS
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
                    string permissionId
                    in effectivePermissions.OrderBy(
                        value => value
                    ))
                {
                    lines.Add(
                        $"- {permissionId}"
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