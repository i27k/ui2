using UnityEngine;

namespace cHub.Shared.Utils
{
    public static class Logger
    {
        private const string Prefix = "[cHub]";

        public static void Info(string message)
        {
            Debug.Log($"{Prefix} {message}");
        }

        public static void Warning(string message)
        {
            Debug.LogWarning($"{Prefix} {message}");
        }

        public static void Error(string message)
        {
            Debug.LogError($"{Prefix} {message}");
        }
    }
}