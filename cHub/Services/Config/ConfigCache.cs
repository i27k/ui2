using cHub.Config;

namespace cHub.Services.Config
{
    public class ConfigCache
    {
        public cHubConfig Configuration { get; private set; }

        public bool IsLoaded => Configuration != null;

        public void Set(cHubConfig configuration)
        {
            Configuration = configuration;
        }

        public cHubConfig Get()
        {
            return Configuration;
        }

        public void Clear()
        {
            Configuration = null;
        }
    }
}