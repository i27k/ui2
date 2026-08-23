using System;
using cHub.Shared.Interfaces;

namespace cHub.Shared.Base
{
    public abstract class Module : IModule
    {
        public abstract string Name { get; }

        public abstract Version Version { get; }

        public bool IsInitialized { get; protected set; }

        public bool IsEnabled { get; protected set; }

        public abstract void Initialize();

        public abstract void Enable();

        public abstract void Disable();

        public abstract void Shutdown();
    }
}