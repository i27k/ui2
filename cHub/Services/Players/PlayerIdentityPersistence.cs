using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using cHub.Shared.Utils;

namespace cHub.Services.Players
{
    /// <summary>
    /// Handles persistence for player identity data.
    ///
    /// File:
    /// Mods/cHub/Data/players.json
    ///
    /// This class only reads/writes player identity data.
    /// It does not resolve players and does not manage roles.
    /// </summary>
    public class PlayerIdentityPersistence
    {
        private readonly string _filePath;

        public string FilePath =>
            _filePath;

        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public PlayerIdentityPersistence(
            string filePath)
        {
            if (string.IsNullOrWhiteSpace(
                filePath))
            {
                throw new ArgumentException(
                    "Player identity file path cannot be empty.",
                    nameof(filePath)
                );
            }

            _filePath =
                filePath;
        }

        // =========================================================
        // LOAD
        // =========================================================

        public Dictionary<string, PlayerIdentityRecord> Load()
        {
            try
            {
                // =================================================
                // FILE DOES NOT EXIST
                // =================================================

                if (!File.Exists(
                    _filePath))
                {
                    Logger.Info(
                        $"Player identity data file does not exist yet: {_filePath}"
                    );

                    return new Dictionary<string, PlayerIdentityRecord>(
                        StringComparer.OrdinalIgnoreCase
                    );
                }

                // =================================================
                // READ JSON
                // =================================================

                string json =
                    File.ReadAllText(
                        _filePath
                    );

                if (string.IsNullOrWhiteSpace(
                    json))
                {
                    Logger.Warning(
                        $"Player identity data file is empty: {_filePath}"
                    );

                    return new Dictionary<string, PlayerIdentityRecord>(
                        StringComparer.OrdinalIgnoreCase
                    );
                }

                // =================================================
                // DESERIALIZE ROOT OBJECT
                // =================================================

                PlayerIdentityDataFile data =
                    JsonConvert.DeserializeObject<PlayerIdentityDataFile>(
                        json
                    );

                if (data == null)
                {
                    Logger.Warning(
                        "Player identity data deserialized to null."
                    );

                    return new Dictionary<string, PlayerIdentityRecord>(
                        StringComparer.OrdinalIgnoreCase
                    );
                }

                if (data.Players == null)
                {
                    return new Dictionary<string, PlayerIdentityRecord>(
                        StringComparer.OrdinalIgnoreCase
                    );
                }

                // =================================================
                // NORMALIZE DICTIONARY COMPARER
                // =================================================

                Dictionary<string, PlayerIdentityRecord> result =
                    new Dictionary<string, PlayerIdentityRecord>(
                        StringComparer.OrdinalIgnoreCase
                    );

                foreach (
                    KeyValuePair<string, PlayerIdentityRecord> entry
                    in data.Players)
                {
                    if (string.IsNullOrWhiteSpace(
                        entry.Key))
                    {
                        continue;
                    }

                    if (entry.Value == null)
                    {
                        continue;
                    }

                    string playerId =
                        entry.Key.Trim();

                    PlayerIdentityRecord record =
                        entry.Value.Clone();

                    record.PlayerId =
                        playerId;

                    result[playerId] =
                        record;
                }

                Logger.Info(
                    $"Player identity data loaded: {_filePath}"
                );

                return result;
            }
            catch (JsonException ex)
            {
                Logger.Error(
                    $"Failed to parse player identity data '{_filePath}': {ex}"
                );

                return new Dictionary<string, PlayerIdentityRecord>(
                    StringComparer.OrdinalIgnoreCase
                );
            }
            catch (Exception ex)
            {
                Logger.Error(
                    $"Failed to load player identity data '{_filePath}': {ex}"
                );

                return new Dictionary<string, PlayerIdentityRecord>(
                    StringComparer.OrdinalIgnoreCase
                );
            }
        }

        // =========================================================
        // SAVE
        // =========================================================

        public bool Save(
            Dictionary<string, PlayerIdentityRecord> players)
        {
            try
            {
                EnsureDirectory();

                Dictionary<string, PlayerIdentityRecord> cleanPlayers =
                    new Dictionary<string, PlayerIdentityRecord>(
                        StringComparer.OrdinalIgnoreCase
                    );

                if (players != null)
                {
                    foreach (
                        KeyValuePair<string, PlayerIdentityRecord> entry
                        in players)
                    {
                        if (string.IsNullOrWhiteSpace(
                            entry.Key))
                        {
                            continue;
                        }

                        if (entry.Value == null)
                        {
                            continue;
                        }

                        string playerId =
                            entry.Key.Trim();

                        PlayerIdentityRecord record =
                            entry.Value.Clone();

                        record.PlayerId =
                            playerId;

                        cleanPlayers[playerId] =
                            record;
                    }
                }

                PlayerIdentityDataFile data =
                    new PlayerIdentityDataFile
                    {
                        Players =
                            cleanPlayers
                    };

                string json =
                    JsonConvert.SerializeObject(
                        data,
                        Formatting.Indented
                    );

                // =================================================
                // ATOMIC-ISH SAVE
                // =================================================

                string tempFilePath =
                    _filePath +
                    ".tmp";

                File.WriteAllText(
                    tempFilePath,
                    json
                );

                if (File.Exists(
                    _filePath))
                {
                    File.Delete(
                        _filePath
                    );
                }

                File.Move(
                    tempFilePath,
                    _filePath
                );

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(
                    $"Failed to save player identity data '{_filePath}': {ex}"
                );

                return false;
            }
        }

        // =========================================================
        // ENSURE DIRECTORY
        // =========================================================

        private void EnsureDirectory()
        {
            string directory =
                Path.GetDirectoryName(
                    _filePath
                );

            if (string.IsNullOrWhiteSpace(
                directory))
            {
                return;
            }

            if (Directory.Exists(
                directory))
            {
                return;
            }

            Directory.CreateDirectory(
                directory
            );
        }

        // =========================================================
        // DATA FILE MODEL
        // =========================================================

        private class PlayerIdentityDataFile
        {
            public Dictionary<string, PlayerIdentityRecord> Players
            {
                get;
                set;
            }
        }
    }
}