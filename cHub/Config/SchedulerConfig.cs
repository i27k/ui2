namespace cHub.Config
{
    public class SchedulerConfig
    {
        public bool Enabled { get; set; } = true;

        public int MaxScheduledTasks { get; set; } = 1000;

        public int TickIntervalMilliseconds { get; set; } = 100;
    }
}