namespace cHub.Config
{
    public class DailyRewardsConfig
    {
        public bool Enabled { get; set; } = true;

        public int ClaimCooldownHours { get; set; } = 24;

        public bool ResetStreakOnMiss { get; set; } = true;

        public int MaximumStreak { get; set; } = 30;

        public int StartingReward { get; set; } = 100;

        public int DailyIncrease { get; set; } = 25;

        public int MaximumReward { get; set; } = 500;

        public bool GiveCurrency { get; set; } = true;

        public bool GiveItems { get; set; } = false;

        public bool GiveExperience { get; set; } = false;

        public bool ShowPopup { get; set; } = true;

        public bool PlaySound { get; set; } = true;
    }
}