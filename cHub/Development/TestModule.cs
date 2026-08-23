using System;
using cHub.Shared.Base;
using cHub.Shared.Utils;

namespace cHub.Development
{
    public class TestModule : Module
    {
        public override string Name => "Test Module";

        public override Version Version => new Version(1, 0, 0, 0);

        public override void Initialize()
        {
            IsInitialized = true;

            Logger.Info($"{Name} initialized.");
        }

        public override void Enable()
        {
            IsEnabled = true;

            Logger.Info($"{Name} enabled.");
        }

        public override void Disable()
        {
            IsEnabled = false;

            Logger.Info($"{Name} disabled.");
        }

        public override void Shutdown()
        {
            IsEnabled = false;
            IsInitialized = false;

            Logger.Info($"{Name} shutdown.");
        }
    }
}