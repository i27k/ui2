using System;
using System.Collections.Generic;
using cHub.Shared.Interfaces;
using cHub.Shared.Utils;

namespace cHub.Core
{
    public static class ModuleRegistry
    {
        private static readonly Dictionary<Type, IModule> _modules =
            new Dictionary<Type, IModule>();

        public static int Count => _modules.Count;

        public static void Register(IModule module)
        {
            if (module == null)
                throw new ArgumentNullException(nameof(module));

            Type moduleType = module.GetType();

            if (_modules.ContainsKey(moduleType))
            {
                Logger.Warning(
                    $"Module '{moduleType.Name}' is already registered."
                );

                return;
            }

            _modules.Add(moduleType, module);

            Logger.Info(
                $"Registered module: {module.Name} v{module.Version}"
            );
        }

        public static T Get<T>() where T : class, IModule
        {
            Type moduleType = typeof(T);

            if (_modules.TryGetValue(moduleType, out IModule module))
            {
                return module as T;
            }

            return null;
        }

        public static bool TryGet<T>(out T module) where T : class, IModule
        {
            module = Get<T>();

            return module != null;
        }

        public static bool IsRegistered<T>() where T : class, IModule
        {
            return _modules.ContainsKey(typeof(T));
        }

        public static void Initialize()
        {
            Logger.Info(
                $"Initializing {_modules.Count} modules..."
            );

            foreach (IModule module in _modules.Values)
            {
                if (module.IsInitialized)
                    continue;

                try
                {
                    module.Initialize();

                    Logger.Info(
                        $"Initialized module: {module.Name}"
                    );
                }
                catch (Exception ex)
                {
                    Logger.Error(
                        $"Failed to initialize module '{module.Name}': {ex}"
                    );
                }
            }

            Logger.Info("Module initialization complete.");
        }

        public static void EnableAll()
        {
            Logger.Info("Enabling modules...");

            foreach (IModule module in _modules.Values)
            {
                if (!module.IsInitialized || module.IsEnabled)
                    continue;

                try
                {
                    module.Enable();

                    Logger.Info(
                        $"Enabled module: {module.Name}"
                    );
                }
                catch (Exception ex)
                {
                    Logger.Error(
                        $"Failed to enable module '{module.Name}': {ex}"
                    );
                }
            }

            Logger.Info("Module enable complete.");
        }

        public static void DisableAll()
        {
            Logger.Info("Disabling modules...");

            foreach (IModule module in _modules.Values)
            {
                if (!module.IsEnabled)
                    continue;

                try
                {
                    module.Disable();

                    Logger.Info(
                        $"Disabled module: {module.Name}"
                    );
                }
                catch (Exception ex)
                {
                    Logger.Error(
                        $"Failed to disable module '{module.Name}': {ex}"
                    );
                }
            }

            Logger.Info("Module disable complete.");
        }

        public static void Shutdown()
        {
            Logger.Info("Shutting down modules...");

            DisableAll();

            foreach (IModule module in _modules.Values)
            {
                if (!module.IsInitialized)
                    continue;

                try
                {
                    module.Shutdown();

                    Logger.Info(
                        $"Shutdown module: {module.Name}"
                    );
                }
                catch (Exception ex)
                {
                    Logger.Error(
                        $"Failed to shutdown module '{module.Name}': {ex}"
                    );
                }
            }

            Logger.Info("Module shutdown complete.");
        }

        public static void Clear()
        {
            _modules.Clear();

            Logger.Info("Module registry cleared.");
        }
    }
}