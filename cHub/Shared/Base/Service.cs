using cHub.Shared.Interfaces;

namespace cHub.Shared.Base
{
    public abstract class Service : IService
    {
        public bool IsInitialized { get; protected set; }

        public abstract void Initialize();

        public abstract void Shutdown();
    }
}