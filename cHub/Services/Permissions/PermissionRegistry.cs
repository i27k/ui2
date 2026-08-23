using System;
using System.Collections.Generic;
using System.Linq;
using cHub.Shared.Utils;

using PermissionIds = cHub.Shared.Constants.Permissions;

namespace cHub.Services.Permissions
{
    /// <summary>
    /// Central registry containing all permissions known by cHub.
    ///
    /// Responsibilities:
    /// - register permissions
    /// - validate known permission IDs
    /// - retrieve permission definitions
    /// - group permissions by category
    ///
    /// Permission persistence and player assignments are handled
    /// separately by PermissionService / persistence layers.
    /// </summary>
    public class PermissionRegistry
    {
        private readonly Dictionary<string, PermissionDefinition> _permissions =
            new Dictionary<string, PermissionDefinition>(
                StringComparer.OrdinalIgnoreCase
            );

        public int Count =>
            _permissions.Count;

        // =========================================================
        // REGISTER
        // =========================================================

        public bool Register(
            PermissionDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(
                    nameof(definition)
                );
            }

            if (string.IsNullOrWhiteSpace(
                definition.Id))
            {
                Logger.Warning(
                    "Cannot register permission with an empty ID."
                );

                return false;
            }

            string permissionId =
                definition.Id.Trim();

            if (_permissions.ContainsKey(
                permissionId))
            {
                Logger.Warning(
                    $"Permission '{permissionId}' is already registered."
                );

                return false;
            }

            _permissions.Add(
                permissionId,
                definition
            );

            return true;
        }

        // =========================================================
        // REGISTER HELPER
        // =========================================================

        private void RegisterPermission(
            string id,
            string name,
            string description,
            string category,
            bool dangerous = false)
        {
            if (string.IsNullOrWhiteSpace(
                id))
            {
                Logger.Warning(
                    "Cannot register permission with an empty ID."
                );

                return;
            }

            Register(
                new PermissionDefinition(
                    id.Trim(),
                    name ?? string.Empty,
                    description ?? string.Empty,
                    category ?? string.Empty,
                    dangerous
                )
            );
        }

        // =========================================================
        // GET
        // =========================================================

        public PermissionDefinition Get(
            string permissionId)
        {
            if (string.IsNullOrWhiteSpace(
                permissionId))
            {
                return null;
            }

            _permissions.TryGetValue(
                permissionId.Trim(),
                out PermissionDefinition definition
            );

            return definition;
        }

        // =========================================================
        // TRY GET
        // =========================================================

        public bool TryGet(
            string permissionId,
            out PermissionDefinition definition)
        {
            definition =
                null;

            if (string.IsNullOrWhiteSpace(
                permissionId))
            {
                return false;
            }

            return _permissions.TryGetValue(
                permissionId.Trim(),
                out definition
            );
        }

        // =========================================================
        // EXISTS
        // =========================================================

        public bool Exists(
            string permissionId)
        {
            if (string.IsNullOrWhiteSpace(
                permissionId))
            {
                return false;
            }

            return _permissions.ContainsKey(
                permissionId.Trim()
            );
        }

        // =========================================================
        // GET ALL
        // =========================================================

        public IEnumerable<PermissionDefinition> GetAll()
        {
            return _permissions.Values
                .OrderBy(
                    permission =>
                        permission.Category
                )
                .ThenBy(
                    permission =>
                        permission.Name
                );
        }

        // =========================================================
        // GET BY CATEGORY
        // =========================================================

        public IEnumerable<PermissionDefinition> GetByCategory(
            string category)
        {
            if (string.IsNullOrWhiteSpace(
                category))
            {
                return Enumerable.Empty<PermissionDefinition>();
            }

            string normalizedCategory =
                category.Trim();

            return _permissions.Values
                .Where(
                    permission =>
                        string.Equals(
                            permission.Category,
                            normalizedCategory,
                            StringComparison.OrdinalIgnoreCase
                        )
                )
                .OrderBy(
                    permission =>
                        permission.Name
                );
        }

        // =========================================================
        // UNREGISTER
        // =========================================================

        public bool Unregister(
            string permissionId)
        {
            if (string.IsNullOrWhiteSpace(
                permissionId))
            {
                return false;
            }

            string normalizedId =
                permissionId.Trim();

            bool removed =
                _permissions.Remove(
                    normalizedId
                );

            if (removed)
            {
                Logger.Info(
                    $"Unregistered permission '{normalizedId}'."
                );
            }

            return removed;
        }

        // =========================================================
        // CLEAR
        // =========================================================

        public void Clear()
        {
            _permissions.Clear();
        }

        // =========================================================
        // DEFAULT PERMISSIONS
        // =========================================================

        public void RegisterDefaults()
        {
            if (_permissions.Count > 0)
            {
                Logger.Warning(
                    "PermissionRegistry already contains permissions."
                );

                return;
            }

            RegisterCorePermissions();
            RegisterAdminPanelPermissions();
            RegisterPlayerPermissions();
            RegisterTeleportPermissions();
            RegisterWorldPermissions();
            RegisterServerPermissions();
            RegisterModulePermissions();
            RegisterRolePermissions();
            RegisterEconomyPermissions();
            RegisterMarketplacePermissions();
            RegisterDailyRewardsPermissions();
            RegisterLeaderboardPermissions();
            RegisterEventPermissions();
            RegisterNotificationPermissions();
            RegisterDonationPermissions();
            RegisterDiscordPermissions();
            RegisterConfigPermissions();
            RegisterDebugPermissions();

            Logger.Info(
                $"Registered {Count} cHub permissions."
            );
        }

        // =========================================================
        // CORE
        // =========================================================

        private void RegisterCorePermissions()
        {
            RegisterPermission(
                PermissionIds.All,
                "Full Access",
                "Grants access to every cHub permission.",
                "Core",
                true
            );

            RegisterPermission(
                PermissionIds.Owner,
                "Owner",
                "Identifies the cHub owner.",
                "Core",
                true
            );

            RegisterPermission(
                PermissionIds.Admin,
                "Administrator",
                "Identifies a cHub administrator.",
                "Core",
                true
            );
        }

        // =========================================================
        // ADMIN PANEL
        // =========================================================

        private void RegisterAdminPanelPermissions()
        {
            RegisterPermission(
                PermissionIds.AdminPanelAccess,
                "Access Admin Panel",
                "Allows access to the cHub administration panel.",
                "Admin Panel"
            );

            RegisterPermission(
                PermissionIds.AdminPanelView,
                "View Admin Panel",
                "Allows viewing administration information.",
                "Admin Panel"
            );

            RegisterPermission(
                PermissionIds.AdminPanelManage,
                "Manage Admin Panel",
                "Allows changing administration panel settings.",
                "Admin Panel",
                true
            );
        }

        // =========================================================
        // PLAYER MANAGEMENT
        // =========================================================

        private void RegisterPlayerPermissions()
        {
            RegisterPermission(
                PermissionIds.PlayersView,
                "View Players",
                "Allows viewing players.",
                "Player Management"
            );

            RegisterPermission(
                PermissionIds.PlayersManage,
                "Manage Players",
                "Allows general player management.",
                "Player Management",
                true
            );

            RegisterPermission(
                PermissionIds.PlayersKick,
                "Kick Players",
                "Allows kicking players.",
                "Player Management",
                true
            );

            RegisterPermission(
                PermissionIds.PlayersBan,
                "Ban Players",
                "Allows banning players.",
                "Player Management",
                true
            );

            RegisterPermission(
                PermissionIds.PlayersUnban,
                "Unban Players",
                "Allows removing player bans.",
                "Player Management",
                true
            );

            RegisterPermission(
                PermissionIds.PlayersMute,
                "Mute Players",
                "Allows muting players.",
                "Player Management",
                true
            );

            RegisterPermission(
                PermissionIds.PlayersUnmute,
                "Unmute Players",
                "Allows unmuting players.",
                "Player Management"
            );

            RegisterPermission(
                PermissionIds.PlayersKill,
                "Kill Players",
                "Allows killing a selected player.",
                "Player Management",
                true
            );

            RegisterPermission(
                PermissionIds.PlayersHeal,
                "Heal Players",
                "Allows healing a selected player.",
                "Player Management"
            );

            RegisterPermission(
                PermissionIds.PlayersInspect,
                "Inspect Players",
                "Allows inspecting player information.",
                "Player Management"
            );

            RegisterPermission(
                PermissionIds.PlayersInventoryView,
                "View Player Inventory",
                "Allows viewing player inventories.",
                "Player Management"
            );

            RegisterPermission(
                PermissionIds.PlayersInventoryManage,
                "Manage Player Inventory",
                "Allows modifying player inventories.",
                "Player Management",
                true
            );
        }

        // =========================================================
        // TELEPORT
        // =========================================================

        private void RegisterTeleportPermissions()
        {
            RegisterPermission(
                PermissionIds.TeleportUse,
                "Use Teleport",
                "Allows using the teleport system.",
                "Teleport"
            );

            RegisterPermission(
                PermissionIds.TeleportSelf,
                "Teleport Self",
                "Allows teleporting yourself.",
                "Teleport"
            );

            RegisterPermission(
                PermissionIds.TeleportPlayers,
                "Teleport Players",
                "Allows teleporting other players.",
                "Teleport",
                true
            );

            RegisterPermission(
                PermissionIds.TeleportToPlayer,
                "Teleport To Player",
                "Allows teleporting to another player.",
                "Teleport"
            );

            RegisterPermission(
                PermissionIds.TeleportBringPlayer,
                "Bring Player",
                "Allows bringing another player to you.",
                "Teleport",
                true
            );

            RegisterPermission(
                PermissionIds.TeleportLocations,
                "Teleport Locations",
                "Allows using predefined teleport locations.",
                "Teleport"
            );

            RegisterPermission(
                PermissionIds.TeleportManage,
                "Manage Teleport",
                "Allows managing the teleport system.",
                "Teleport",
                true
            );
        }

        // =========================================================
        // WORLD / CHUNKS
        // =========================================================

        private void RegisterWorldPermissions()
        {
            RegisterPermission(
                PermissionIds.ChunksView,
                "View Chunks",
                "Allows viewing chunk information.",
                "World Tools"
            );

            RegisterPermission(
                PermissionIds.ChunksManage,
                "Manage Chunks",
                "Allows managing chunks.",
                "World Tools",
                true
            );

            RegisterPermission(
                PermissionIds.ChunksReset,
                "Reset Chunks",
                "Allows resetting chunks.",
                "World Tools",
                true
            );

            RegisterPermission(
                PermissionIds.ChunksProtect,
                "Protect Chunks",
                "Allows managing chunk protection.",
                "World Tools",
                true
            );

            RegisterPermission(
                PermissionIds.WorldTools,
                "World Tools",
                "Allows access to world administration tools.",
                "World Tools",
                true
            );
        }

        // =========================================================
        // SERVER
        // =========================================================

        private void RegisterServerPermissions()
        {
            RegisterPermission(
                PermissionIds.ServerView,
                "View Server",
                "Allows viewing server information.",
                "Server"
            );

            RegisterPermission(
                PermissionIds.ServerManage,
                "Manage Server",
                "Allows server management.",
                "Server",
                true
            );

            RegisterPermission(
                PermissionIds.ServerRestart,
                "Restart Server",
                "Allows restarting the server.",
                "Server",
                true
            );

            RegisterPermission(
                PermissionIds.ServerShutdown,
                "Shutdown Server",
                "Allows shutting down the server.",
                "Server",
                true
            );

            RegisterPermission(
                PermissionIds.ServerSave,
                "Save Server",
                "Allows forcing a world save.",
                "Server"
            );
        }

        // =========================================================
        // MODULES
        // =========================================================

        private void RegisterModulePermissions()
        {
            RegisterPermission(
                PermissionIds.ModulesView,
                "View Modules",
                "Allows viewing cHub modules.",
                "Modules"
            );

            RegisterPermission(
                PermissionIds.ModulesControl,
                "Control Modules",
                "Allows controlling cHub modules.",
                "Modules",
                true
            );

            RegisterPermission(
                PermissionIds.ModulesEnable,
                "Enable Modules",
                "Allows enabling modules.",
                "Modules",
                true
            );

            RegisterPermission(
                PermissionIds.ModulesDisable,
                "Disable Modules",
                "Allows disabling modules.",
                "Modules",
                true
            );

            RegisterPermission(
                PermissionIds.ModulesConfigure,
                "Configure Modules",
                "Allows configuring modules.",
                "Modules",
                true
            );
        }

        // =========================================================
        // ROLES / PERMISSIONS
        // =========================================================

        private void RegisterRolePermissions()
        {
            RegisterPermission(
                PermissionIds.RolesView,
                "View Roles",
                "Allows viewing roles.",
                "Roles"
            );

            RegisterPermission(
                PermissionIds.RolesManage,
                "Manage Roles",
                "Allows managing roles.",
                "Roles",
                true
            );

            RegisterPermission(
                PermissionIds.RolesCreate,
                "Create Roles",
                "Allows creating roles.",
                "Roles",
                true
            );

            RegisterPermission(
                PermissionIds.RolesEdit,
                "Edit Roles",
                "Allows editing roles.",
                "Roles",
                true
            );

            RegisterPermission(
                PermissionIds.RolesDelete,
                "Delete Roles",
                "Allows deleting roles.",
                "Roles",
                true
            );

            RegisterPermission(
                PermissionIds.RolesAssign,
                "Assign Roles",
                "Allows assigning roles to players.",
                "Roles",
                true
            );

            RegisterPermission(
                PermissionIds.PermissionsView,
                "View Permissions",
                "Allows viewing permissions.",
                "Permissions"
            );

            RegisterPermission(
                PermissionIds.PermissionsManage,
                "Manage Permissions",
                "Allows managing permissions.",
                "Permissions",
                true
            );

            RegisterPermission(
                PermissionIds.PermissionsGrant,
                "Grant Permissions",
                "Allows granting permissions.",
                "Permissions",
                true
            );

            RegisterPermission(
                PermissionIds.PermissionsRevoke,
                "Revoke Permissions",
                "Allows revoking permissions.",
                "Permissions",
                true
            );

            RegisterPermission(
                PermissionIds.PermissionsClear,
                "Clear Permissions",
                "Allows clearing all direct permissions assigned to a player.",
                "Permissions",
                true
            );
        }

        // =========================================================
        // ECONOMY
        // =========================================================

        private void RegisterEconomyPermissions()
        {
            RegisterPermission(
                PermissionIds.EconomyView,
                "View Economy",
                "Allows viewing economy information.",
                "Economy"
            );

            RegisterPermission(
                PermissionIds.EconomyManage,
                "Manage Economy",
                "Allows managing the economy.",
                "Economy",
                true
            );

            RegisterPermission(
                PermissionIds.EconomyBalanceView,
                "View Balances",
                "Allows viewing player balances.",
                "Economy"
            );

            RegisterPermission(
                PermissionIds.EconomyBalanceSet,
                "Set Balance",
                "Allows setting player balances.",
                "Economy",
                true
            );

            RegisterPermission(
                PermissionIds.EconomyBalanceAdd,
                "Add Balance",
                "Allows adding currency.",
                "Economy",
                true
            );

            RegisterPermission(
                PermissionIds.EconomyBalanceRemove,
                "Remove Balance",
                "Allows removing currency.",
                "Economy",
                true
            );
        }

        // =========================================================
        // MARKETPLACE
        // =========================================================

        private void RegisterMarketplacePermissions()
        {
            RegisterPermission(
                PermissionIds.MarketplaceUse,
                "Use Marketplace",
                "Allows using the marketplace.",
                "Marketplace"
            );

            RegisterPermission(
                PermissionIds.MarketplaceSell,
                "Sell Items",
                "Allows selling items.",
                "Marketplace"
            );

            RegisterPermission(
                PermissionIds.MarketplaceBuy,
                "Buy Items",
                "Allows buying items.",
                "Marketplace"
            );

            RegisterPermission(
                PermissionIds.MarketplaceManage,
                "Manage Marketplace",
                "Allows managing the marketplace.",
                "Marketplace",
                true
            );

            RegisterPermission(
                PermissionIds.MarketplaceRemoveListing,
                "Remove Listing",
                "Allows removing marketplace listings.",
                "Marketplace",
                true
            );
        }

        // =========================================================
        // DAILY REWARDS
        // =========================================================

        private void RegisterDailyRewardsPermissions()
        {
            RegisterPermission(
                PermissionIds.DailyRewardsClaim,
                "Claim Daily Rewards",
                "Allows claiming daily rewards.",
                "Daily Rewards"
            );

            RegisterPermission(
                PermissionIds.DailyRewardsView,
                "View Daily Rewards",
                "Allows viewing daily rewards.",
                "Daily Rewards"
            );

            RegisterPermission(
                PermissionIds.DailyRewardsManage,
                "Manage Daily Rewards",
                "Allows managing daily rewards.",
                "Daily Rewards",
                true
            );

            RegisterPermission(
                PermissionIds.DailyRewardsReset,
                "Reset Daily Rewards",
                "Allows resetting daily reward progress.",
                "Daily Rewards",
                true
            );
        }

        // =========================================================
        // LEADERBOARDS
        // =========================================================

        private void RegisterLeaderboardPermissions()
        {
            RegisterPermission(
                PermissionIds.LeaderboardsView,
                "View Leaderboards",
                "Allows viewing leaderboards.",
                "Leaderboards"
            );

            RegisterPermission(
                PermissionIds.LeaderboardsManage,
                "Manage Leaderboards",
                "Allows managing leaderboards.",
                "Leaderboards",
                true
            );

            RegisterPermission(
                PermissionIds.LeaderboardsReset,
                "Reset Leaderboards",
                "Allows resetting leaderboards.",
                "Leaderboards",
                true
            );
        }

        // =========================================================
        // EVENTS
        // =========================================================

        private void RegisterEventPermissions()
        {
            RegisterPermission(
                PermissionIds.EventsView,
                "View Events",
                "Allows viewing events.",
                "Events"
            );

            RegisterPermission(
                PermissionIds.EventsManage,
                "Manage Events",
                "Allows managing events.",
                "Events",
                true
            );

            RegisterPermission(
                PermissionIds.EventsStart,
                "Start Events",
                "Allows starting events.",
                "Events",
                true
            );

            RegisterPermission(
                PermissionIds.EventsStop,
                "Stop Events",
                "Allows stopping events.",
                "Events",
                true
            );
        }

        // =========================================================
        // NOTIFICATIONS
        // =========================================================

        private void RegisterNotificationPermissions()
        {
            RegisterPermission(
                PermissionIds.NotificationsSend,
                "Send Notifications",
                "Allows sending notifications.",
                "Notifications"
            );

            RegisterPermission(
                PermissionIds.NotificationsBroadcast,
                "Broadcast Notifications",
                "Allows broadcasting notifications.",
                "Notifications",
                true
            );

            RegisterPermission(
                PermissionIds.NotificationsManage,
                "Manage Notifications",
                "Allows managing notifications.",
                "Notifications",
                true
            );
        }

        // =========================================================
        // DONATIONS
        // =========================================================

        private void RegisterDonationPermissions()
        {
            RegisterPermission(
                PermissionIds.DonationsView,
                "View Donations",
                "Allows viewing donation information.",
                "Donations"
            );

            RegisterPermission(
                PermissionIds.DonationsManage,
                "Manage Donations",
                "Allows managing donations.",
                "Donations",
                true
            );
        }

        // =========================================================
        // DISCORD
        // =========================================================

        private void RegisterDiscordPermissions()
        {
            RegisterPermission(
                PermissionIds.DiscordView,
                "View Discord",
                "Allows viewing Discord integration settings.",
                "Discord"
            );

            RegisterPermission(
                PermissionIds.DiscordManage,
                "Manage Discord",
                "Allows managing Discord integration.",
                "Discord",
                true
            );
        }

        // =========================================================
        // CONFIGURATION
        // =========================================================

        private void RegisterConfigPermissions()
        {
            RegisterPermission(
                PermissionIds.ConfigView,
                "View Configuration",
                "Allows viewing cHub configuration.",
                "Configuration"
            );

            RegisterPermission(
                PermissionIds.ConfigManage,
                "Manage Configuration",
                "Allows modifying cHub configuration.",
                "Configuration",
                true
            );

            RegisterPermission(
                PermissionIds.ConfigReload,
                "Reload Configuration",
                "Allows reloading cHub configuration.",
                "Configuration",
                true
            );

            RegisterPermission(
                PermissionIds.ConfigSave,
                "Save Configuration",
                "Allows saving cHub configuration.",
                "Configuration",
                true
            );
        }

        // =========================================================
        // DEBUG / DEVELOPMENT
        // =========================================================

        private void RegisterDebugPermissions()
        {
            RegisterPermission(
                PermissionIds.DebugAccess,
                "Debug Access",
                "Allows access to debugging functionality.",
                "Development",
                true
            );

            RegisterPermission(
                PermissionIds.DebugTools,
                "Debug Tools",
                "Allows using cHub development tools.",
                "Development",
                true
            );
        }
    }
}