using System;
using System.Collections.Generic;
using System.IO;
using cHub.Shared.Base;
using cHub.Shared.Utils;

namespace cHub.Services.Players
{
    /// <summary>
    /// Central service for persistent player identities.
    ///
    /// Responsibilities:
    /// - load players.json
    /// - keep player identities in memory
    /// - save changes
    /// - resolve offline players by name
    /// - track first seen / last seen
    ///
    /// Persistent key:
    /// EOS / internal persistent player ID
    /// </summary>
    public class PlayerIdentityService : Service
    {
        private readonly PlayerIdentityStore _store;

        private PlayerIdentityPersistence _persistence;

        private readonly string _dataDirectory;
        private readonly string _playersFilePath;

        // =========================================================
        // PROPERTIES
        // =========================================================

        public PlayerIdentityStore Store =>
            _store;

        public int PlayerCount =>
            _store.Count;

        public string PlayersFilePath =>
            _playersFilePath;

        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public PlayerIdentityService()
        {
            _store =
                new PlayerIdentityStore();

            _dataDirectory =
                Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Mods",
                    "cHub",
                    "Data"
                );

            _playersFilePath =
                Path.Combine(
                    _dataDirectory,
                    "players.json"
                );
        }

        // =========================================================
        // INITIALIZE
        // =========================================================

        public override void Initialize()
        {
            if (IsInitialized)
            {
                Logger.Warning(
                    "PlayerIdentityService is already initialized."
                );

                return;
            }

            Logger.Info(
                "Initializing PlayerIdentityService..."
            );

            try
            {
                EnsureDataDirectory();

                _persistence =
                    new PlayerIdentityPersistence(
                        _playersFilePath
                    );

                LoadPlayers();

                IsInitialized =
                    true;

                Logger.Info(
                    $"PlayerIdentityService initialized with {_store.Count} known players."
                );

                Logger.Info(
                    $"Player identity data path: {_playersFilePath}"
                );
            }
            catch (Exception ex)
            {
                Logger.Error(
                    $"Failed to initialize PlayerIdentityService: {ex}"
                );

                throw;
            }
        }

        // =========================================================
        // ENSURE DATA DIRECTORY
        // =========================================================

        private void EnsureDataDirectory()
        {
            if (Directory.Exists(
                _dataDirectory))
            {
                return;
            }

            Directory.CreateDirectory(
                _dataDirectory
            );

            Logger.Info(
                $"Created player identity data directory: {_dataDirectory}"
            );
        }

        // =========================================================
        // LOAD
        // =========================================================

        public bool LoadPlayers()
        {
            if (_persistence == null)
            {
                Logger.Warning(
                    "Cannot load player identities because persistence is unavailable."
                );

                return false;
            }

            Dictionary<string, PlayerIdentityRecord> data =
                _persistence.Load();

            _store.Import(
                data
            );

            Logger.Info(
                $"Loaded {_store.Count} player identities."
            );

            return true;
        }

        // =========================================================
        // SAVE
        // =========================================================

        public bool SavePlayers()
        {
            if (_persistence == null)
            {
                Logger.Warning(
                    "Cannot save player identities because persistence is unavailable."
                );

                return false;
            }

            Dictionary<string, PlayerIdentityRecord> data =
                _store.Export();

            bool success =
                _persistence.Save(
                    data
                );

            if (success)
            {
                Logger.Info(
                    $"Saved {_store.Count} player identities."
                );
            }

            return success;
        }

        // =========================================================
        // UPSERT PLAYER
        // =========================================================

        public bool UpsertPlayer(
            string playerId,
            string playerName)
        {
            if (!IsValid(
                playerId))
            {
                return false;
            }

            if (!IsValid(
                playerName))
            {
                return false;
            }

            PlayerIdentityRecord existing =
                _store.GetById(
                    playerId
                );

            string previousName =
                existing?.Name;

            bool changed =
                _store.Upsert(
                    playerId,
                    playerName
                );

            // Existing player with unchanged name:
            // LastSeenUtc was still updated by the store.
            //
            // We save here as well so LastSeenUtc remains persistent.
            bool saveRequired =
                changed ||
                existing != null;

            if (saveRequired)
            {
                SavePlayers();
            }

            if (existing == null)
            {
                Logger.Info(
                    $"Registered player identity: '{playerName}' -> '{playerId}'."
                );
            }
            else if (
                !string.Equals(
                    previousName,
                    playerName,
                    StringComparison.Ordinal))
            {
                Logger.Info(
                    $"Updated player name for '{playerId}': '{previousName}' -> '{playerName}'."
                );
            }

            return true;
        }

        // =========================================================
        // GET BY ID
        // =========================================================

        public PlayerIdentityRecord GetById(
            string playerId)
        {
            return _store.GetById(
                playerId
            );
        }

        // =========================================================
        // GET BY NAME
        // =========================================================

        public PlayerIdentityRecord GetByName(
            string playerName)
        {
            return _store.GetByName(
                playerName
            );
        }

        // =========================================================
        // TRY GET BY ID
        // =========================================================

        public bool TryGetById(
            string playerId,
            out PlayerIdentityRecord record)
        {
            return _store.TryGetById(
                playerId,
                out record
            );
        }

        // =========================================================
        // TRY GET BY NAME
        // =========================================================

        public bool TryGetByName(
            string playerName,
            out PlayerIdentityRecord record)
        {
            return _store.TryGetByName(
                playerName,
                out record
            );
        }

        // =========================================================
        // RESOLVE PLAYER ID FROM NAME
        // =========================================================

        public bool TryResolvePlayerId(
            string playerName,
            out string playerId)
        {
            return _store.TryResolvePlayerId(
                playerName,
                out playerId
            );
        }

        // =========================================================
        // SEARCH BY NAME
        // =========================================================

        public IReadOnlyCollection<PlayerIdentityRecord>
            SearchByName(
                string query)
        {
            return _store.SearchByName(
                query
            );
        }

        // =========================================================
        // GET ALL
        // =========================================================

        public IReadOnlyCollection<PlayerIdentityRecord>
            GetPlayers()
        {
            return _store.GetAll();
        }

        // =========================================================
        // EXISTS
        // =========================================================

        public bool ContainsPlayer(
            string playerId)
        {
            return _store.ContainsPlayer(
                playerId
            );
        }

        // =========================================================
        // REMOVE
        // =========================================================

        public bool RemovePlayer(
            string playerId)
        {
            if (!IsValid(
                playerId))
            {
                return false;
            }

            bool removed =
                _store.Remove(
                    playerId
                );

            if (!removed)
            {
                return false;
            }

            SavePlayers();

            Logger.Info(
                $"Removed player identity '{playerId}'."
            );

            return true;
        }

        // =========================================================
        // INTERNAL
        // =========================================================

        private static bool IsValid(
            string value)
        {
            return !string.IsNullOrWhiteSpace(
                value
            );
        }

        // =========================================================
        // SHUTDOWN
        // =========================================================

        public override void Shutdown()
        {
            if (!IsInitialized)
            {
                return;
            }

            Logger.Info(
                "Shutting down PlayerIdentityService..."
            );

            SavePlayers();

            _store.Clear();

            _persistence =
                null;

            IsInitialized =
                false;

            Logger.Info(
                "PlayerIdentityService shutdown."
            );
        }
    }
}