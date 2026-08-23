using System;
using System.Collections.Generic;
using cHub.Shared.Interfaces;
using cHub.Shared.Utils;

namespace cHub.Core
{
    public static class ServiceRegistry
    {
        private static readonly Dictionary<Type, IService> _services =
            new Dictionary<Type, IService>();

        public static int Count => _services.Count;

        public static void Register(IService service)
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            Type serviceType = service.GetType();

            if (_services.ContainsKey(serviceType))
            {
                Logger.Warning(
                    $"Service '{serviceType.Name}' is already registered."
                );

                return;
            }

            _services.Add(serviceType, service);

            Logger.Info(
                $"Registered service: {serviceType.Name}"
            );
        }

        public static T Get<T>() where T : class, IService
        {
            Type serviceType = typeof(T);

            if (_services.TryGetValue(serviceType, out IService service))
            {
                return service as T;
            }

            return null;
        }

        public static bool TryGet<T>(out T service) where T : class, IService
        {
            service = Get<T>();

            return service != null;
        }

        public static bool IsRegistered<T>() where T : class, IService
        {
            return _services.ContainsKey(typeof(T));
        }

        public static void Initialize()
        {
            Logger.Info(
                $"Initializing {_services.Count} services..."
            );

            foreach (IService service in _services.Values)
            {
                if (service.IsInitialized)
                    continue;

                try
                {
                    service.Initialize();

                    Logger.Info(
                        $"Initialized service: {service.GetType().Name}"
                    );
                }
                catch (Exception ex)
                {
                    Logger.Error(
                        $"Failed to initialize service '{service.GetType().Name}': {ex}"
                    );
                }
            }

            Logger.Info("Service initialization complete.");
        }

        public static void Shutdown()
        {
            Logger.Info("Shutting down services...");

            foreach (IService service in _services.Values)
            {
                if (!service.IsInitialized)
                    continue;

                try
                {
                    service.Shutdown();

                    Logger.Info(
                        $"Shutdown service: {service.GetType().Name}"
                    );
                }
                catch (Exception ex)
                {
                    Logger.Error(
                        $"Failed to shutdown service '{service.GetType().Name}': {ex}"
                    );
                }
            }

            Logger.Info("Service shutdown complete.");
        }

        public static void Clear()
        {
            _services.Clear();

            Logger.Info("Service registry cleared.");
        }
    }
}