using cHub.Shared.Base;
using cHub.Shared.Utils;

namespace cHub.Services.Test
{
    public class TestService : Service
    {
        public override void Initialize()
        {
            IsInitialized = true;

            Logger.Info("TestService initialized.");
        }

        public override void Shutdown()
        {
            IsInitialized = false;

            Logger.Info("TestService shutdown.");
        }
    }
}