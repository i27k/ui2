using System;
using System.Collections.Generic;
using System.IO;
using cHub.Shared.Utils;
using Newtonsoft.Json;

namespace cHub.Services.Roles
{
    public class RolePersistence
    {
        private readonly string _filePath;

        public RolePersistence(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException(
                    "Role persistence path cannot be empty.",
                    nameof(filePath)
                );
            }

            _filePath = Path.GetFullPath(filePath);
        }

        public bool Save(
            Dictionary<string, List<string>> playerRoles)
        {
            if (playerRoles == null)
            {
                throw new ArgumentNullException(
                    nameof(playerRoles)
                );
            }

            try
            {
                string directory =
                    Path.GetDirectoryName(_filePath);

                if (!string.IsNullOrWhiteSpace(directory) &&
                    !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);

                    Logger.Info(
                        $"Created role data directory: {directory}"
                    );
                }

                RolePersistenceModel model =
                    new RolePersistenceModel
                    {
                        Players = playerRoles
                    };

                string json =
                    JsonConvert.SerializeObject(
                        model,
                        Formatting.Indented
                    );

                File.WriteAllText(
                    _filePath,
                    json
                );

                Logger.Info(
                    $"Role data saved: {_filePath}"
                );

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(
                    $"Failed to save role data '{_filePath}': {ex}"
                );

                return false;
            }
        }

        public Dictionary<string, List<string>> Load()
        {
            if (!File.Exists(_filePath))
            {
                Logger.Info(
                    $"Role data file not found: {_filePath}"
                );

                return new Dictionary<string, List<string>>(
                    StringComparer.OrdinalIgnoreCase
                );
            }

            try
            {
                string json =
                    File.ReadAllText(_filePath);

                if (string.IsNullOrWhiteSpace(json))
                {
                    return new Dictionary<string, List<string>>(
                        StringComparer.OrdinalIgnoreCase
                    );
                }

                RolePersistenceModel model =
                    JsonConvert.DeserializeObject<RolePersistenceModel>(
                        json
                    );

                if (model?.Players == null)
                {
                    return new Dictionary<string, List<string>>(
                        StringComparer.OrdinalIgnoreCase
                    );
                }

                Dictionary<string, List<string>> result =
                    new Dictionary<string, List<string>>(
                        StringComparer.OrdinalIgnoreCase
                    );

                foreach (
                    KeyValuePair<string, List<string>> entry
                    in model.Players)
                {
                    if (string.IsNullOrWhiteSpace(entry.Key))
                        continue;

                    result[entry.Key] =
                        entry.Value ??
                        new List<string>();
                }

                Logger.Info(
                    $"Role data loaded: {_filePath}"
                );

                return result;
            }
            catch (Exception ex)
            {
                Logger.Error(
                    $"Failed to load role data '{_filePath}': {ex}"
                );

                return new Dictionary<string, List<string>>(
                    StringComparer.OrdinalIgnoreCase
                );
            }
        }

        private class RolePersistenceModel
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
    }
}