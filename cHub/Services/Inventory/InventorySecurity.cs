using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using cHub.Core;

namespace cHub.Modules.Inventory
{
    /// <summary>
    /// Server-side integrity protection for cHub multi-inventory files.
    ///
    /// IMPORTANT:
    /// The secret key is NEVER hardcoded in the DLL and is NEVER stored
    /// inside a CHUBINV4 inventory file.
    /// </summary>
    internal static class InventorySecurity
    {
        private const int KeySizeBytes = 32;

        private static readonly object Sync = new object();

        private static byte[] cachedKey;

        private static string SecurityDirectory =>
            Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Mods",
                "cHub",
                "Data",
                "security");

        private static string KeyPath =>
            Path.Combine(
                SecurityDirectory,
                "inventory-server.key");

        /// <summary>
        /// Returns the server secret, generating it if this server
        /// does not have one yet.
        /// </summary>
        private static byte[] GetServerKey()
        {
            lock (Sync)
            {
                if (cachedKey != null && cachedKey.Length == KeySizeBytes)
                    return cachedKey;

                Directory.CreateDirectory(SecurityDirectory);

                if (File.Exists(KeyPath))
                {
                    byte[] existing = File.ReadAllBytes(KeyPath);

                    if (existing.Length != KeySizeBytes)
                    {
                        throw new InvalidDataException(
                            "cHub inventory security key has invalid length: " +
                            existing.Length +
                            " bytes. Expected " +
                            KeySizeBytes +
                            ".");
                    }

                    cachedKey = existing;

                    StartupTerminal.Audit(
                        "INVENTORY_SECURITY",
                        "KEY_LOAD",
                        "OK",
                        "existing=true bytes=" + existing.Length);

                    return cachedKey;
                }

                byte[] generated = new byte[KeySizeBytes];

                using (RandomNumberGenerator rng =
                       RandomNumberGenerator.Create())
                {
                    rng.GetBytes(generated);
                }

                File.WriteAllBytes(KeyPath, generated);

                cachedKey = generated;

                StartupTerminal.Audit(
                    "INVENTORY_SECURITY",
                    "KEY_CREATE",
                    "OK",
                    "generated=true bytes=" + generated.Length);

                return cachedKey;
            }
        }

        /// <summary>
        /// Generates an HMAC-SHA256 for an exact byte payload.
        /// </summary>
        public static byte[] Sign(byte[] payload)
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));

            byte[] key = GetServerKey();

            using (HMACSHA256 hmac = new HMACSHA256(key))
            {
                return hmac.ComputeHash(payload);
            }
        }

        /// <summary>
        /// Validates an HMAC without normal byte-by-byte early exit.
        /// </summary>
        public static bool Verify(byte[] payload, byte[] expectedSignature)
        {
            if (payload == null ||
                expectedSignature == null ||
                expectedSignature.Length != 32)
                return false;

            byte[] actualSignature = Sign(payload);

            return FixedTimeEquals(
                actualSignature,
                expectedSignature);
        }

        /// <summary>
        /// Constant-time-ish comparison compatible with older
        /// framework versions used by the game.
        /// </summary>
        private static bool FixedTimeEquals(byte[] left, byte[] right)
        {
            if (left == null || right == null)
                return false;

            if (left.Length != right.Length)
                return false;

            int difference = 0;

            for (int i = 0; i < left.Length; i++)
                difference |= left[i] ^ right[i];

            return difference == 0;
        }

        /// <summary>
        /// Helper for signing textual metadata if needed.
        /// </summary>
        public static byte[] SignText(string value)
        {
            return Sign(
                Encoding.UTF8.GetBytes(
                    value ?? string.Empty));
        }
    }
}