namespace cHub.Shared.Constants
{
    /// <summary>
    /// Central permission identifiers used by cHub.
    /// Keep permission IDs stable because they may be stored
    /// in configuration files, databases and player roles.
    /// </summary>
    public static class Permissions
    {
        // =========================================================
        // CORE
        // =========================================================

        public const string All = "*";

        public const string Owner = "core.owner";

        public const string Admin = "core.admin";


        // =========================================================
        // ADMIN PANEL
        // =========================================================

        public const string AdminPanelAccess = "adminpanel.access";

        public const string AdminPanelView = "adminpanel.view";

        public const string AdminPanelManage = "adminpanel.manage";


        // =========================================================
        // PLAYER MANAGEMENT
        // =========================================================

        public const string PlayersView = "players.view";

        public const string PlayersManage = "players.manage";

        public const string PlayersKick = "players.kick";

        public const string PlayersBan = "players.ban";

        public const string PlayersUnban = "players.unban";

        public const string PlayersMute = "players.mute";

        public const string PlayersUnmute = "players.unmute";

        public const string PlayersKill = "players.kill";

        public const string PlayersHeal = "players.heal";

        public const string PlayersInspect = "players.inspect";

        public const string PlayersInventoryView = "players.inventory.view";

        public const string PlayersInventoryManage = "players.inventory.manage";


        // =========================================================
        // TELEPORT
        // =========================================================

        public const string TeleportUse = "teleport.use";

        public const string TeleportSelf = "teleport.self";

        public const string TeleportPlayers = "teleport.players";

        public const string TeleportToPlayer = "teleport.to_player";

        public const string TeleportBringPlayer = "teleport.bring_player";

        public const string TeleportLocations = "teleport.locations";

        public const string TeleportManage = "teleport.manage";


        // =========================================================
        // CHUNK / WORLD TOOLS
        // =========================================================

        public const string ChunksView = "chunks.view";

        public const string ChunksManage = "chunks.manage";

        public const string ChunksReset = "chunks.reset";

        public const string ChunksProtect = "chunks.protect";

        public const string WorldTools = "world.tools";


        // =========================================================
        // SERVER TOOLS
        // =========================================================

        public const string ServerView = "server.view";

        public const string ServerManage = "server.manage";

        public const string ServerRestart = "server.restart";

        public const string ServerShutdown = "server.shutdown";

        public const string ServerSave = "server.save";


        // =========================================================
        // MODULE CONTROL
        // =========================================================

        public const string ModulesView = "modules.view";

        public const string ModulesControl = "modules.control";

        public const string ModulesEnable = "modules.enable";

        public const string ModulesDisable = "modules.disable";

        public const string ModulesConfigure = "modules.configure";


        // =========================================================
        // ROLES / PERMISSIONS
        // =========================================================

        public const string RolesView = "roles.view";

        public const string RolesManage = "roles.manage";

        public const string RolesCreate = "roles.create";

        public const string RolesEdit = "roles.edit";

        public const string RolesDelete = "roles.delete";

        public const string RolesAssign = "roles.assign";

        public const string PermissionsView = "permissions.view";

        public const string PermissionsManage = "permissions.manage";

        public const string PermissionsGrant = "permissions.grant";

        public const string PermissionsRevoke = "permissions.revoke";

        public const string PermissionsClear = "permissions.clear";


        // =========================================================
        // ECONOMY
        // =========================================================

        public const string EconomyView = "economy.view";

        public const string EconomyManage = "economy.manage";

        public const string EconomyBalanceView = "economy.balance.view";

        public const string EconomyBalanceSet = "economy.balance.set";

        public const string EconomyBalanceAdd = "economy.balance.add";

        public const string EconomyBalanceRemove = "economy.balance.remove";


        // =========================================================
        // MARKETPLACE
        // =========================================================

        public const string MarketplaceUse = "marketplace.use";

        public const string MarketplaceSell = "marketplace.sell";

        public const string MarketplaceBuy = "marketplace.buy";

        public const string MarketplaceManage = "marketplace.manage";

        public const string MarketplaceRemoveListing =
            "marketplace.remove_listing";


        // =========================================================
        // DAILY REWARDS
        // =========================================================

        public const string DailyRewardsClaim = "dailyrewards.claim";

        public const string DailyRewardsView = "dailyrewards.view";

        public const string DailyRewardsManage = "dailyrewards.manage";

        public const string DailyRewardsReset = "dailyrewards.reset";


        // =========================================================
        // LEADERBOARDS
        // =========================================================

        public const string LeaderboardsView = "leaderboards.view";

        public const string LeaderboardsManage = "leaderboards.manage";

        public const string LeaderboardsReset = "leaderboards.reset";


        // =========================================================
        // EVENTS
        // =========================================================

        public const string EventsView = "events.view";

        public const string EventsManage = "events.manage";

        public const string EventsStart = "events.start";

        public const string EventsStop = "events.stop";


        // =========================================================
        // NOTIFICATIONS
        // =========================================================

        public const string NotificationsSend = "notifications.send";

        public const string NotificationsBroadcast =
            "notifications.broadcast";

        public const string NotificationsManage =
            "notifications.manage";


        // =========================================================
        // DONATIONS
        // =========================================================

        public const string DonationsView = "donations.view";

        public const string DonationsManage = "donations.manage";


        // =========================================================
        // DISCORD
        // =========================================================

        public const string DiscordView = "discord.view";

        public const string DiscordManage = "discord.manage";


        // =========================================================
        // CONFIGURATION
        // =========================================================

        public const string ConfigView = "config.view";

        public const string ConfigManage = "config.manage";

        public const string ConfigReload = "config.reload";

        public const string ConfigSave = "config.save";


        // =========================================================
        // DEBUG / DEVELOPMENT
        // =========================================================

        public const string DebugAccess = "debug.access";

        public const string DebugTools = "debug.tools";
    }
}