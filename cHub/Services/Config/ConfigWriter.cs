using System;
using System.IO;
using cHub.Config;
using cHub.Shared.Utils;
using Newtonsoft.Json;

namespace cHub.Services.Config
{
    public class ConfigWriter
    {
        public bool Write(string filePath, cHubConfig configuration)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException(
                    "Config path cannot be null or empty.",
                    nameof(filePath)
                );
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            try
            {
                string directory = Path.GetDirectoryName(filePath);

                if (!string.IsNullOrWhiteSpace(directory) &&
                    !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);

                    Logger.Info(
                        $"Created config directory: {directory}"
                    );
                }

                string json = JsonConvert.SerializeObject(
                    configuration,
                    Formatting.Indented
                );

                File.WriteAllText(filePath, json);

                Logger.Info(
                    $"Configuration saved: {filePath}"
                );

                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.Error(
                    $"Access denied while writing configuration '{filePath}': {ex}"
                );

                return false;
            }
            catch (IOException ex)
            {
                Logger.Error(
                    $"Could not write configuration '{filePath}': {ex}"
                );

                return false;
            }
            catch (JsonException ex)
            {
                Logger.Error(
                    $"Could not serialize configuration '{filePath}': {ex}"
                );

                return false;
            }
            catch (Exception ex)
            {
                Logger.Error(
                    $"Unexpected error writing configuration '{filePath}': {ex}"
                );

                return false;
            }
        }
    }
}