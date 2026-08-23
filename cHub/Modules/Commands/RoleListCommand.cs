using System.Collections.Generic;
using System.Linq;
using cHub.Core;
using cHub.Services.Commands;
using cHub.Services.Players;
using cHub.Services.Roles;

namespace cHub.Modules.Commands
{
    public class RoleListCommand : CommandDefinition
    {
        // =========================================================
        // COMMAND INFO
        // =========================================================

        public override string Name =>
            "rolelist";

        public override string Description =>
            "Lists the cHub roles assigned to a player.";

        public override string Usage =>
            "/rolelist [player]";

        // =========================================================
        // ALIASES
        // =========================================================

        public override IReadOnlyCollection<string> Aliases =>
            new[]
            {
                "roles",
                "rlist",
                "rl"
            };

        // =========================================================
        // PERMISSION
        // =========================================================

        public override string RequiredPermission =>
            "roles.view";

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
            // TARGET PLAYER
            // =====================================================

            string targetPlayerId;
            string targetPlayerName;

            // No argument:
            // show the roles of the player executing the command.
            if (context.ArgumentCount == 0)
            {
                targetPlayerId =
                    context.ActorId;

                targetPlayerName =
                    context.ActorName;
            }
            else
            {
                string playerInput =
                    context.GetArgument(0);

                // Resolve friendly player name -> EOS ID.
                if (!PlayerResolver.TryResolvePlayerId(
                    playerInput,
                    out targetPlayerId,
                    out targetPlayerName))
                {
                    return CommandResult.Fail(
                        $"Player '{playerInput}' could not be found."
                    );
                }
            }

            // =====================================================
            // VALIDATE RESOLVED PLAYER
            // =====================================================

            if (string.IsNullOrWhiteSpace(targetPlayerId))
            {
                return CommandResult.Fail(
                    "Could not resolve player ID."
                );
            }

            if (string.IsNullOrWhiteSpace(targetPlayerName))
            {
                targetPlayerName =
                    targetPlayerId;
            }

            // =====================================================
            // GET PLAYER ROLES
            // =====================================================

            var roles =
                roleService
                    .GetPlayerRoles(targetPlayerId)
                    .OrderByDescending(
                        role => role.Priority
                    )
                    .ThenBy(
                        role => role.Name
                    )
                    .ToArray();

            // =====================================================
            // NO ROLES
            // =====================================================

            if (roles.Length == 0)
            {
                return CommandResult.Ok(
                    $"{targetPlayerName} has no assigned cHub roles."
                );
            }

            // =====================================================
            // BUILD ROLE LIST
            // =====================================================

            string roleList =
                string.Join(
                    ", ",
                    roles.Select(
                        role =>
                            $"{role.Name} ({role.Id})"
                    )
                );

            // =====================================================
            // SUCCESS
            // =====================================================

            return CommandResult.Ok(
                $"Roles for {targetPlayerName}: {roleList}"
            );
        }
    }
}