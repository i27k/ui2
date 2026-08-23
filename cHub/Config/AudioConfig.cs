namespace cHub.Config
{
    public class AudioConfig
    {
        public bool Enabled { get; set; } = true;

        public float MasterVolume { get; set; } = 1.0f;

        public float EffectsVolume { get; set; } = 1.0f;

        public float MusicVolume { get; set; } = 1.0f;

        public bool MuteInBackground { get; set; } = false;
    }
}