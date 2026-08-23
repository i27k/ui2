using System;
using System.IO;
using System.Threading;
using cHub.Shared.Utils;

namespace cHub.Services.Config
{
    public class ConfigWatcher : IDisposable
    {
        private FileSystemWatcher _watcher;
        private Timer _reloadTimer;

        private readonly string _configPath;

        private bool _started;
        private bool _disposed;

        private const int DebounceMilliseconds = 500;

        public event Action ConfigChanged;

        public bool IsRunning => _started;

        public ConfigWatcher(string configPath)
        {
            if (string.IsNullOrWhiteSpace(configPath))
            {
                throw new ArgumentException(
                    "Config path cannot be null or empty.",
                    nameof(configPath)
                );
            }

            _configPath = Path.GetFullPath(configPath);
        }

        public void Start()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ConfigWatcher));

            if (_started)
            {
                Logger.Warning("ConfigWatcher is already running.");
                return;
            }

            string directory = Path.GetDirectoryName(_configPath);
            string fileName = Path.GetFileName(_configPath);

            if (string.IsNullOrWhiteSpace(directory))
            {
                Logger.Error(
                    $"Could not determine config directory for: {_configPath}"
                );

                return;
            }

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _watcher = new FileSystemWatcher
            {
                Path = directory,
                Filter = fileName,

                NotifyFilter =
                    NotifyFilters.LastWrite |
                    NotifyFilters.FileName |
                    NotifyFilters.Size,

                IncludeSubdirectories = false,
                EnableRaisingEvents = false
            };

            _watcher.Changed += OnFileChanged;
            _watcher.Created += OnFileChanged;
            _watcher.Renamed += OnFileRenamed;

            _reloadTimer = new Timer(
                OnDebounceElapsed,
                null,
                Timeout.Infinite,
                Timeout.Infinite
            );

            _watcher.EnableRaisingEvents = true;

            _started = true;

            Logger.Info(
                $"ConfigWatcher started: {_configPath}"
            );
        }

        public void Stop()
        {
            if (!_started)
                return;

            _started = false;

            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;

                _watcher.Changed -= OnFileChanged;
                _watcher.Created -= OnFileChanged;
                _watcher.Renamed -= OnFileRenamed;

                _watcher.Dispose();
                _watcher = null;
            }

            if (_reloadTimer != null)
            {
                _reloadTimer.Change(
                    Timeout.Infinite,
                    Timeout.Infinite
                );
            }

            Logger.Info("ConfigWatcher stopped.");
        }

        private void OnFileChanged(
            object sender,
            FileSystemEventArgs e
        )
        {
            ScheduleReload();
        }

        private void OnFileRenamed(
            object sender,
            RenamedEventArgs e
        )
        {
            ScheduleReload();
        }

        private void ScheduleReload()
        {
            if (!_started || _disposed)
                return;

            try
            {
                _reloadTimer?.Change(
                    DebounceMilliseconds,
                    Timeout.Infinite
                );
            }
            catch (ObjectDisposedException)
            {
                // Watcher is shutting down.
            }
        }

        private void OnDebounceElapsed(object state)
        {
            if (!_started || _disposed)
                return;

            try
            {
                Logger.Info(
                    "Configuration file change detected."
                );

                ConfigChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Logger.Error(
                    $"ConfigWatcher callback failed: {ex}"
                );
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            Stop();

            _reloadTimer?.Dispose();
            _reloadTimer = null;

            _disposed = true;
        }
    }
}