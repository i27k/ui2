using System;
using System.Collections.Generic;
using System.Linq;
using cHub.Shared.Utils;

using PermissionIds = cHub.Shared.Constants.Permissions;

namespace cHub.Services.Roles
{
    /// <summary>
    /// Central registry containing all roles known by cHub.
    ///
    /// Staff hierarchy:
    ///
    /// Owner
    ///   -> Administrator
    ///       -> Moderator
    ///           -> Player
    ///
    /// VIP is a separate secondary role / entitlement.
    /// </summary>
    public class RoleRegistry
    {
        private readonly Dictionary<string, RoleDefinition> _roles =
            new Dictionary<string, RoleDefinition>(
                StringComparer.OrdinalIgnoreCase
            );

        public int Count => _roles.Count;

        // =========================================================
        // REGISTER
        // =========================================================

        public bool Register(RoleDefinition role)
        {
            if (role == null)
            {
                throw new ArgumentNullException(
                    nameof(role)
                );
            }

            if (_roles.ContainsKey(role.Id))
            {
                Logger.Warning(
                    $"Role '{role.Id}' is already registered."
                );

                return false;
            }

            _roles.Add(
                role.Id,
                role
            );

            Logger.Info(
                $"Registered role: {role.Name} ({role.Id})"
            );

            return true;
        }

        // =========================================================
        // LOOKUP
        // =========================================================

        public RoleDefinition Get(string roleId)
        {
            if (string.IsNullOrWhiteSpace(roleId))
            {
                return null;
            }

            _roles.TryGetValue(
                roleId,
                out RoleDefinition role
            );

            return role;
        }

        public bool TryGet(
            string roleId,
            out RoleDefinition role)
        {
            role = null;

            if (string.IsNullOrWhiteSpace(roleId))
            {
                return false;
            }

            return _roles.TryGetValue(
                roleId,
                out role
            );
        }

        public bool Exists(string roleId)
        {
            if (string.IsNullOrWhiteSpace(roleId))
            {
                return false;
            }

            return _roles.ContainsKey(roleId);
        }

        public IEnumerable<RoleDefinition> GetAll()
        {
            return _roles.Values
                .OrderByDescending(
                    role => role.Priority
                )
                .ThenBy(
                    role => role.Name
                );
        }

        public IEnumerable<RoleDefinition> GetEnabled()
        {
            return _roles.Values
                .Where(
                    role => role.IsEnabled
                )
                .OrderByDescending(
                    role => role.Priority
                )
                .ThenBy(
                    role => role.Name
                );
        }

        public IEnumerable<RoleDefinition> GetSystemRoles()
        {
            return _roles.Values
                .Where(
                    role => role.IsSystemRole
                )
                .OrderByDescending(
                    role => role.Priority
                )
                .ThenBy(
                    role => role.Name
                );
        }

        // =========================================================
        // UNREGISTER
        // =========================================================

        public bool Unregister(string roleId)
        {
            if (string.IsNullOrWhiteSpace(roleId))
            {
                return false;
            }

            if (!_roles.TryGetValue(
                roleId,
                out RoleDefinition role))
            {
                return false;
            }

            if (role.IsSystemRole)
            {
                Logger.Warning(
                    $"Cannot unregister protected system role '{role.Id}'."
                );

                return false;
            }

            bool removed =
                _roles.Remove(roleId);

            if (removed)
            {
                Logger.Info(
                    $"Unregistered role: {role.Name} ({role.Id})"
                );
            }

            return removed;
        }

        // =========================================================
        // ENABLE / DISABLE
        // =========================================================

        public bool Enable(string roleId)
        {
            RoleDefinition role =
                Get(roleId);

            if (role == null)
            {
                return false;
            }

            if (role.IsEnabled)
            {
                return false;
            }

            role.IsEnabled =
                true;

            Logger.Info(
                $"Enabled role: {role.Name} ({role.Id})"
            );

            return true;
        }

        public bool Disable(string roleId)
        {
            RoleDefinition role =
                Get(roleId);

            if (role == null)
            {
                return false;
            }

            if (role.IsSystemRole &&
                string.Equals(
                    role.Id,
                    "owner",
                    StringComparison.OrdinalIgnoreCase))
            {
                Logger.Warning(
                    "Owner role cannot be disabled."
                );

                return false;
            }

            if (!role.IsEnabled)
            {
                return false;
            }

            role.IsEnabled =
                false;

            Logger.Info(
                $"Disabled role: {role.Name} ({role.Id})"
            );

            return true;
        }

        // =========================================================
        // DEFAULT ROLES
        // =========================================================

        public void RegisterDefaults()
        {
            if (_roles.Count > 0)
            {
                Logger.Warning(
                    "RoleRegistry already contains roles."
                );

                return;
            }

            // Register from lowest -> highest.
            //
            // This is not strictly required because inheritance
            // currently stores role IDs, but it makes the hierarchy
            // easier to understand and maintain.

            RegisterPlayerRole();
            RegisterVipRole();
            RegisterModeratorRole();
            RegisterAdministratorRole();
            RegisterOwnerRole();

            Logger.Info(
                $"Registered {Count} default cHub roles."
            );

            Logger.Info(
                "Role hierarchy: Owner -> Administrator -> Moderator -> Player"
            );

            Logger.Info(
                "VIP registered as a separate secondary role."
            );
        }

        // =========================================================
        // PLAYER
        // =========================================================

        private void RegisterPlayerRole()
        {
            RoleDefinition role =
                new RoleDefinition(
                    "player",
                    "Player",
                    "Default role for regular players.",
                    0,
                    true
                );

            role.AddPermissions(
                new[]
                {
                    PermissionIds.TeleportUse,
                    PermissionIds.TeleportSelf,
                    PermissionIds.TeleportToPlayer,

                    PermissionIds.MarketplaceUse,
                    PermissionIds.MarketplaceSell,
                    PermissionIds.MarketplaceBuy,

                    PermissionIds.DailyRewardsClaim,
                    PermissionIds.DailyRewardsView,

                    PermissionIds.LeaderboardsView,

                    PermissionIds.DonationsView,
                    PermissionIds.DiscordView
                }
            );

            Register(role);
        }

        // =========================================================
        // VIP
        // =========================================================

        private void RegisterVipRole()
        {
            RoleDefinition role =
                new RoleDefinition(
                    "vip",
                    "VIP",
                    "Premium role for VIP player benefits.",
                    100,
                    true
                );

            /*
             * IMPORTANT:
             *
             * VIP is NOT part of the staff hierarchy.
             *
             * A player may therefore have:
             *
             * player + vip
             * moderator + vip
             * administrator + vip
             *
             * We intentionally do NOT use:
             *
             * role.AddInheritedRole("player");
             *
             * because VIP represents an additional entitlement,
             * not a replacement for the player's primary role.
             *
             * VIP-specific permissions such as reserved slots
             * will be added here later.
             */

            Register(role);
        }

        // =========================================================
        // MODERATOR
        // =========================================================

        private void RegisterModeratorRole()
        {
            RoleDefinition role =
                new RoleDefinition(
                    "moderator",
                    "Moderator",
                    "Moderation role for day-to-day player management.",
                    500,
                    true
                );

            // Moderator inherits all Player permissions.
            role.AddInheritedRole(
                "player"
            );

            role.AddPermissions(
                new[]
                {
                    PermissionIds.AdminPanelAccess,
                    PermissionIds.AdminPanelView,

                    PermissionIds.PlayersView,
                    PermissionIds.PlayersKick,
                    PermissionIds.PlayersMute,
                    PermissionIds.PlayersUnmute,
                    PermissionIds.PlayersInspect,

                    PermissionIds.ServerView,

                    PermissionIds.RolesView,
                    PermissionIds.PermissionsView,

                    PermissionIds.NotificationsSend
                }
            );

            Register(role);
        }

        // =========================================================
        // ADMINISTRATOR
        // =========================================================

        private void RegisterAdministratorRole()
        {
            RoleDefinition role =
                new RoleDefinition(
                    "administrator",
                    "Administrator",
                    "Full administration access without ownership privileges.",
                    900,
                    true
                );

            // Administrator inherits:
            //
            // Administrator
            //      ↓
            // Moderator
            //      ↓
            // Player

            role.AddInheritedRole(
                "moderator"
            );

            role.AddPermissions(
                new[]
                {
                    PermissionIds.Admin,

                    PermissionIds.AdminPanelAccess,
                    PermissionIds.AdminPanelView,
                    PermissionIds.AdminPanelManage,

                    PermissionIds.PlayersView,
                    PermissionIds.PlayersManage,
                    PermissionIds.PlayersKick,
                    PermissionIds.PlayersBan,
                    PermissionIds.PlayersUnban,
                    PermissionIds.PlayersMute,
                    PermissionIds.PlayersUnmute,
                    PermissionIds.PlayersKill,
                    PermissionIds.PlayersHeal,
                    PermissionIds.PlayersInspect,
                    PermissionIds.PlayersInventoryView,
                    PermissionIds.PlayersInventoryManage,

                    PermissionIds.TeleportUse,
                    PermissionIds.TeleportSelf,
                    PermissionIds.TeleportPlayers,
                    PermissionIds.TeleportToPlayer,
                    PermissionIds.TeleportBringPlayer,
                    PermissionIds.TeleportLocations,
                    PermissionIds.TeleportManage,

                    PermissionIds.ChunksView,
                    PermissionIds.ChunksManage,
                    PermissionIds.ChunksReset,
                    PermissionIds.ChunksProtect,
                    PermissionIds.WorldTools,

                    PermissionIds.ServerView,
                    PermissionIds.ServerManage,
                    PermissionIds.ServerSave,

                    PermissionIds.ModulesView,
                    PermissionIds.ModulesControl,
                    PermissionIds.ModulesEnable,
                    PermissionIds.ModulesDisable,
                    PermissionIds.ModulesConfigure,

                    PermissionIds.RolesView,
                    PermissionIds.RolesAssign,

                    PermissionIds.PermissionsView,

                    PermissionIds.EconomyView,
                    PermissionIds.EconomyManage,
                    PermissionIds.EconomyBalanceView,
                    PermissionIds.EconomyBalanceSet,
                    PermissionIds.EconomyBalanceAdd,
                    PermissionIds.EconomyBalanceRemove,

                    PermissionIds.MarketplaceUse,
                    PermissionIds.MarketplaceSell,
                    PermissionIds.MarketplaceBuy,
                    PermissionIds.MarketplaceManage,
                    PermissionIds.MarketplaceRemoveListing,

                    PermissionIds.DailyRewardsView,
                    PermissionIds.DailyRewardsManage,
                    PermissionIds.DailyRewardsReset,

                    PermissionIds.LeaderboardsView,
                    PermissionIds.LeaderboardsManage,
                    PermissionIds.LeaderboardsReset,

                    PermissionIds.EventsView,
                    PermissionIds.EventsManage,
                    PermissionIds.EventsStart,
                    PermissionIds.EventsStop,

                    PermissionIds.NotificationsSend,
                    PermissionIds.NotificationsBroadcast,
                    PermissionIds.NotificationsManage,

                    PermissionIds.DonationsView,

                    PermissionIds.DiscordView,

                    PermissionIds.ConfigView
                }
            );

            Register(role);
        }

        // =========================================================
        // OWNER
        // =========================================================

        private void RegisterOwnerRole()
        {
            RoleDefinition role =
                new RoleDefinition(
                    "owner",
                    "Owner",
                    "Full access to all cHub systems.",
                    1000,
                    true
                );

            // Owner inherits:
            //
            // Owner
            //   ↓
            // Administrator
            //   ↓
            // Moderator
            //   ↓
            // Player

            role.AddInheritedRole(
                "administrator"
            );

            // Owner still receives wildcard permission.
            //
            // This guarantees that the Primary Owner retains
            // unrestricted cHub access even if new permissions
            // are added later.

            role.AddPermission(
                PermissionIds.All
            );

            Register(role);
        }

        // =========================================================
        // CLEAR
        // =========================================================

        public void Clear()
        {
            _roles.Clear();

            Logger.Info(
                "Role registry cleared."
            );
        }
    }
}
