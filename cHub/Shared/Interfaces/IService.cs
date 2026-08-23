namespace cHub.Shared.Interfaces
{
    public interface IService
    {
        bool IsInitialized { get; }

        void Initialize();

        void Shutdown();
    }
}