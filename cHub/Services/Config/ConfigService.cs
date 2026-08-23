using System;
using System.IO;
using cHub.Config;
using cHub.Shared.Base;
using cHub.Shared.Utils;

namespace cHub.Services.Config
{
    public class ConfigService : Service
    {
        private readonly ConfigCache _cache;
        private readonly ConfigLoader _loader;
        private readonly ConfigWriter _writer;

        private ConfigWatcher _watcher;

        private readonly string _configDirectory;
        private readonly string _configPath;

        private bool _isInternalSave;

        public cHubConfig Current => _cache.Get();

        public string ConfigDirectory => _configDirectory;

        public string ConfigPath => _configPath;

        public bool IsLoaded => _cache.IsLoaded;

        public ConfigService()
        {
            _cache = new ConfigCache();
            _loader = new ConfigLoader();
            _writer = new ConfigWriter();

            _configDirectory = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Mods",
                "cHub",
                "Config"
            );

            _configPath = Path.Combine(
                _configDirectory,
                "cHubConfig.json"
            );
        }

        public override void Initialize()
        {
            if (IsInitialized)
            {
                Logger.Warning(
                    "ConfigService is already initialized."
                );

                return;
            }

            Logger.Info("Initializing ConfigService...");

            try
            {
                EnsureConfigDirectory();

                if (!File.Exists(_configPath))
                {
                    Logger.Info(
                        "Config file not found. Creating default configuration."
                    );

                    CreateDefault();
                }
                else
                {
                    Load();
                }

                if (!_cache.IsLoaded)
                {
                    Logger.Warning(
                        "Configuration could not be loaded. Using default configuration in memory."
                    );

                    _cache.Set(new cHubConfig());
                }

                StartWatcher();

                IsInitialized = true;

                Logger.Info("ConfigService initialized.");
                Logger.Info($"Config path: {_configPath}");
            }
            catch (Exception ex)
            {
                Logger.Error(
                    $"Failed to initialize ConfigService: {ex}"
                );

                throw;
            }
        }

        public bool Load()
        {
            Logger.Info("Loading configuration...");

            cHubConfig configuration =
                _loader.Load(_configPath);

            if (configuration == null)
            {
                Logger.Error(
                    "Configuration load failed."
                );

                return false;
            }

            _cache.Set(configuration);

            Logger.Info(
                "Configuration loaded into cache."
            );

            return true;
        }

        public bool Save()
        {
            if (!_cache.IsLoaded)
            {
                Logger.Warning(
                    "Cannot save configuration because no configuration is loaded."
                );

                return false;
            }

            Logger.Info("Saving configuration...");

            try
            {
                _isInternalSave = true;

                bool success = _writer.Write(
                    _configPath,
                    _cache.Get()
                );

                if (success)
                {
                    Logger.Info(
                        "Configuration save complete."
                    );
                }

                return success;
            }
            finally
            {
                _isInternalSave = false;
            }
        }

        public bool Reload()
        {
            Logger.Info("Reloading configuration...");

            cHubConfig previousConfiguration =
                _cache.Get();

            cHubConfig configuration =
                _loader.Load(_configPath);

            if (configuration == null)
            {
                Logger.Error(
                    "Configuration reload failed. Keeping previous configuration."
                );

                if (previousConfiguration != null)
                {
                    _cache.Set(previousConfiguration);
                }

                return false;
            }

            _cache.Set(configuration);

            Logger.Info(
                "Configuration reload complete."
            );

            return true;
        }

        public bool CreateDefault()
        {
            Logger.Info(
                "Creating default configuration..."
            );

            cHubConfig configuration =
                new cHubConfig();

            _cache.Set(configuration);

            bool success = Save();

            if (!success)
            {
                Logger.Error(
                    "Failed to save default configuration."
                );

                return false;
            }

            Logger.Info(
                "Default configuration created."
            );

            return true;
        }

        private void EnsureConfigDirectory()
        {
            if (Directory.Exists(_configDirectory))
                return;

            Directory.CreateDirectory(
                _configDirectory
            );

            Logger.Info(
                $"Created config directory: {_configDirectory}"
            );
        }

        private void StartWatcher()
        {
            if (_watcher != null)
            {
                Logger.Warning(
                    "ConfigWatcher already exists."
                );

                return;
            }

            _watcher = new ConfigWatcher(
                _configPath
            );

            _watcher.ConfigChanged +=
                OnConfigChanged;

            _watcher.Start();

            Logger.Info(
                "ConfigWatcher connected to ConfigService."
            );
        }

        private void StopWatcher()
        {
            if (_watcher == null)
                return;

            _watcher.ConfigChanged -=
                OnConfigChanged;

            _watcher.Dispose();
            _watcher = null;

            Logger.Info(
                "ConfigWatcher disconnected from ConfigService."
            );
        }

        private void OnConfigChanged()
        {
            if (_isInternalSave)
                return;

            Logger.Info(
                "External configuration change detected."
            );

            bool success = Reload();

            if (success)
            {
                Logger.Info(
                    "Hot reload completed successfully."
                );
            }
            else
            {
                Logger.Warning(
                    "Hot reload failed."
                );
            }
        }

        public override void Shutdown()
        {
            if (!IsInitialized)
                return;

            Logger.Info(
                "Shutting down ConfigService..."
            );

            StopWatcher();

            if (_cache.IsLoaded)
            {
                Save();
            }

            _cache.Clear();

            IsInitialized = false;

            Logger.Info(
                "ConfigService shutdown."
            );
        }
    }
}