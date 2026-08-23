using System;

namespace cHub.Shared.Interfaces
{
    public interface IModule
    {
        string Name { get; }

        Version Version { get; }

        bool IsInitialized { get; }

        bool IsEnabled { get; }

        void Initialize();

        void Enable();

        void Disable();

        void Shutdown();
    }
}