using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using cHub.Shared.Utils;

namespace cHub.Services.Permissions
{
    /// <summary>
    /// Handles persistent storage for direct player permissions.
    ///
    /// File format:
    ///
    /// {
    ///   "Players": {
    ///     "EOS_xxx": [
    ///       "players.kick",
    ///       "server.view"
    ///     ]
    ///   }
    /// }
    ///
    /// Role permissions are NOT stored here.
    /// This file contains only permissions granted directly
    /// to individual players.
    /// </summary>
    public class PermissionPersistence
    {
        private readonly string _filePath;

        // =========================================================
        // DATA MODEL
        // =========================================================

        private class PermissionData
        {
            public Dictionary<string, List<string>> Players
            {
                get;
                set;
            } =
                new Dictionary<string, List<string>>(
                    StringComparer.OrdinalIgnoreCase
                );
        }

        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public PermissionPersistence(
            string filePath)
        {
            if (string.IsNullOrWhiteSpace(
                filePath))
            {
                throw new ArgumentException(
                    "Permission persistence file path cannot be empty.",
                    nameof(filePath)
                );
            }

            _filePath =
                filePath;
        }

        // =========================================================
        // LOAD
        // =========================================================

        public Dictionary<string, List<string>> Load()
        {
            Dictionary<string, List<string>> emptyResult =
                CreateEmptyDictionary();

            try
            {
                if (!File.Exists(
                    _filePath))
                {
                    Logger.Info(
                        $"Permission data file does not exist yet: {_filePath}"
                    );

                    return emptyResult;
                }

                string json =
                    File.ReadAllText(
                        _filePath
                    );

                if (string.IsNullOrWhiteSpace(
                    json))
                {
                    Logger.Warning(
                        $"Permission data file is empty: {_filePath}"
                    );

                    return emptyResult;
                }

                PermissionData data =
                    JsonConvert.DeserializeObject<PermissionData>(
                        json
                    );

                if (data == null)
                {
                    Logger.Warning(
                        $"Permission data could not be deserialized: {_filePath}"
                    );

                    return emptyResult;
                }

                if (data.Players == null)
                {
                    Logger.Warning(
                        $"Permission data contains no Players collection: {_filePath}"
                    );

                    return emptyResult;
                }

                Logger.Info(
                    $"Permission data loaded: {_filePath}"
                );

                return Normalize(
                    data.Players
                );
            }
            catch (Exception ex)
            {
                Logger.Error(
                    $"Failed to load permission data from '{_filePath}': {ex}"
                );

                return emptyResult;
            }
        }

        // =========================================================
        // SAVE
        // =========================================================

        public bool Save(
            Dictionary<string, List<string>> playerPermissions)
        {
            try
            {
                EnsureDirectory();

                PermissionData data =
                    new PermissionData
                    {
                        Players =
                            Normalize(
                                playerPermissions
                            )
                    };

                string json =
                    JsonConvert.SerializeObject(
                        data,
                        Formatting.Indented
                    );

                string temporaryFilePath =
                    _filePath + ".tmp";

                File.WriteAllText(
                    temporaryFilePath,
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
                    temporaryFilePath,
                    _filePath
                );

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(
                    $"Failed to save permission data to '{_filePath}': {ex}"
                );

                return false;
            }
        }

        // =========================================================
        // NORMALIZE
        // =========================================================

        private static Dictionary<string, List<string>> Normalize(
            Dictionary<string, List<string>> data)
        {
            Dictionary<string, List<string>> result =
                CreateEmptyDictionary();

            if (data == null)
            {
                return result;
            }

            foreach (
                KeyValuePair<string, List<string>> entry
                in data)
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

                HashSet<string> permissions =
                    new HashSet<string>(
                        StringComparer.OrdinalIgnoreCase
                    );

                foreach (
                    string permissionId
                    in entry.Value)
                {
                    if (string.IsNullOrWhiteSpace(
                        permissionId))
                    {
                        continue;
                    }

                    permissions.Add(
                        permissionId.Trim()
                    );
                }

                if (permissions.Count == 0)
                {
                    continue;
                }

                List<string> normalizedPermissions =
                    new List<string>(
                        permissions
                    );

                normalizedPermissions.Sort(
                    StringComparer.OrdinalIgnoreCase
                );

                result[playerId] =
                    normalizedPermissions;
            }

            return result;
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

            Logger.Info(
                $"Created permission data directory: {directory}"
            );
        }

        // =========================================================
        // EMPTY DICTIONARY
        // =========================================================

        private static Dictionary<string, List<string>>
            CreateEmptyDictionary()
        {
            return new Dictionary<string, List<string>>(
                StringComparer.OrdinalIgnoreCase
            );
        }
    }
}