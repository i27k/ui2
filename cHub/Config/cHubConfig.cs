using cHub.Modules.Marketplace;

namespace cHub.Config
{
    public class cHubConfig
    {
        public AudioConfig Audio { get; set; } = new AudioConfig();

        public DailyRewardsConfig DailyRewards { get; set; } = new DailyRewardsConfig();

        public DatabaseConfig Database { get; set; } = new DatabaseConfig();

        public DiscordConfig Discord { get; set; } = new DiscordConfig();

        public DonationConfig Donation { get; set; } = new DonationConfig();

        public EconomyConfig Economy { get; set; } = new EconomyConfig();

        public LeaderboardsConfig Leaderboards { get; set; } = new LeaderboardsConfig();

        public MarketplaceConfig Marketplace { get; set; } = new MarketplaceConfig();

        public NotificationConfig Notification { get; set; } = new NotificationConfig();

        public PermissionConfig Permissions { get; set; } = new PermissionConfig();

        public SchedulerConfig Scheduler { get; set; } = new SchedulerConfig();

        public UIConfig UI { get; set; } = new UIConfig();

        public WindowConfig Windows { get; set; } = new WindowConfig();
    }
}