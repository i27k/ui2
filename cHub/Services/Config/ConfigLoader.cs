using System;
using System.IO;
using cHub.Config;
using cHub.Shared.Utils;
using Newtonsoft.Json;

namespace cHub.Services.Config
{
    public class ConfigLoader
    {
        public cHubConfig Load(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException(
                    "Config path cannot be null or empty.",
                    nameof(filePath)
                );
            }

            if (!File.Exists(filePath))
            {
                Logger.Warning(
                    $"Config file not found: {filePath}"
                );

                return null;
            }

            try
            {
                string json = File.ReadAllText(filePath);

                if (string.IsNullOrWhiteSpace(json))
                {
                    Logger.Warning(
                        $"Config file is empty: {filePath}"
                    );

                    return null;
                }

                cHubConfig configuration =
                    JsonConvert.DeserializeObject<cHubConfig>(json);

                if (configuration == null)
                {
                    Logger.Error(
                        $"Failed to deserialize config: {filePath}"
                    );

                    return null;
                }

                Logger.Info(
                    $"Configuration loaded: {filePath}"
                );

                return configuration;
            }
            catch (JsonException ex)
            {
                Logger.Error(
                    $"Invalid JSON configuration '{filePath}': {ex}"
                );

                return null;
            }
            catch (IOException ex)
            {
                Logger.Error(
                    $"Could not read configuration '{filePath}': {ex}"
                );

                return null;
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.Error(
                    $"Access denied while reading configuration '{filePath}': {ex}"
                );

                return null;
            }
            catch (Exception ex)
            {
                Logger.Error(
                    $"Unexpected error loading configuration '{filePath}': {ex}"
                );

                return null;
            }
        }
    }
}