using System;
using System.Collections.Generic;
using System.Linq;

namespace cHub.Services.Players
{
    /// <summary>
    /// Stores persistent player identity information in memory.
    ///
    /// Primary key:
    /// EOS / persistent player ID
    ///
    /// Example:
    /// EOS_00028ef93e494b4ead6248437af730e9
    ///
    /// Used for resolving players even when they are offline.
    ///
    /// Persistence is handled separately by
    /// PlayerIdentityPersistence.
    /// </summary>
    public class PlayerIdentityStore
    {
        private readonly Dictionary<string, PlayerIdentityRecord> _players =
            new Dictionary<string, PlayerIdentityRecord>(
                StringComparer.OrdinalIgnoreCase
            );

        public int Count =>
            _players.Count;

        // =========================================================
        // UPSERT
        // =========================================================

        public bool Upsert(
            string playerId,
            string playerName)
        {
            if (!IsValid(playerId))
            {
                return false;
            }

            if (!IsValid(playerName))
            {
                return false;
            }

            string normalizedId =
                playerId.Trim();

            string normalizedName =
                playerName.Trim();

            // =====================================================
            // EXISTING PLAYER
            // =====================================================

            if (_players.TryGetValue(
                normalizedId,
                out PlayerIdentityRecord existing))
            {
                bool changed =
                    false;

                if (!string.Equals(
                    existing.Name,
                    normalizedName,
                    StringComparison.Ordinal))
                {
                    existing.Name =
                        normalizedName;

                    changed =
                        true;
                }

                existing.LastSeenUtc =
                    DateTime.UtcNow;

                return changed;
            }

            // =====================================================
            // NEW PLAYER
            // =====================================================

            PlayerIdentityRecord record =
                new PlayerIdentityRecord
                {
                    PlayerId =
                        normalizedId,

                    Name =
                        normalizedName,

                    FirstSeenUtc =
                        DateTime.UtcNow,

                    LastSeenUtc =
                        DateTime.UtcNow
                };

            _players.Add(
                normalizedId,
                record
            );

            return true;
        }

        // =========================================================
        // GET BY PLAYER ID
        // =========================================================

        public PlayerIdentityRecord GetById(
            string playerId)
        {
            if (!IsValid(playerId))
            {
                return null;
            }

            _players.TryGetValue(
                playerId.Trim(),
                out PlayerIdentityRecord record
            );

            return record;
        }

        // =========================================================
        // TRY GET BY PLAYER ID
        // =========================================================

        public bool TryGetById(
            string playerId,
            out PlayerIdentityRecord record)
        {
            record =
                null;

            if (!IsValid(playerId))
            {
                return false;
            }

            return _players.TryGetValue(
                playerId.Trim(),
                out record
            );
        }

        // =========================================================
        // GET BY EXACT NAME
        // =========================================================

        public PlayerIdentityRecord GetByName(
            string playerName)
        {
            if (!IsValid(playerName))
            {
                return null;
            }

            string normalizedName =
                playerName.Trim();

            return _players.Values
                .Where(
                    player =>
                        player != null &&
                        !string.IsNullOrWhiteSpace(
                            player.Name
                        )
                )
                .OrderByDescending(
                    player =>
                        player.LastSeenUtc
                )
                .FirstOrDefault(
                    player =>
                        string.Equals(
                            player.Name,
                            normalizedName,
                            StringComparison.OrdinalIgnoreCase
                        )
                );
        }

        // =========================================================
        // TRY GET BY EXACT NAME
        // =========================================================

        public bool TryGetByName(
            string playerName,
            out PlayerIdentityRecord record)
        {
            record =
                GetByName(
                    playerName
                );

            return record != null;
        }

        // =========================================================
        // SEARCH BY NAME
        // =========================================================

        public IReadOnlyCollection<PlayerIdentityRecord> SearchByName(
            string query)
        {
            if (!IsValid(query))
            {
                return Array.Empty<PlayerIdentityRecord>();
            }

            string normalizedQuery =
                query.Trim();

            return _players.Values
                .Where(
                    player =>
                        player != null &&
                        !string.IsNullOrWhiteSpace(
                            player.Name
                        ) &&
                        player.Name.IndexOf(
                            normalizedQuery,
                            StringComparison.OrdinalIgnoreCase
                        ) >= 0
                )
                .OrderBy(
                    player =>
                        player.Name
                )
                .ThenByDescending(
                    player =>
                        player.LastSeenUtc
                )
                .ToArray();
        }

        // =========================================================
        // RESOLVE PLAYER ID FROM NAME
        // =========================================================

        public bool TryResolvePlayerId(
            string playerName,
            out string playerId)
        {
            playerId =
                null;

            PlayerIdentityRecord record =
                GetByName(
                    playerName
                );

            if (record == null)
            {
                return false;
            }

            if (!IsValid(
                record.PlayerId))
            {
                return false;
            }

            playerId =
                record.PlayerId;

            return true;
        }

        // =========================================================
        // EXISTS
        // =========================================================

        public bool ContainsPlayer(
            string playerId)
        {
            if (!IsValid(playerId))
            {
                return false;
            }

            return _players.ContainsKey(
                playerId.Trim()
            );
        }

        // =========================================================
        // REMOVE PLAYER
        // =========================================================

        public bool Remove(
            string playerId)
        {
            if (!IsValid(playerId))
            {
                return false;
            }

            return _players.Remove(
                playerId.Trim()
            );
        }

        // =========================================================
        // GET ALL
        // =========================================================

        public IReadOnlyCollection<PlayerIdentityRecord> GetAll()
        {
            return _players.Values
                .Where(
                    player =>
                        player != null
                )
                .OrderBy(
                    player =>
                        player.Name
                )
                .ThenBy(
                    player =>
                        player.PlayerId
                )
                .ToArray();
        }

        // =========================================================
        // EXPORT
        // =========================================================

        public Dictionary<string, PlayerIdentityRecord> Export()
        {
            Dictionary<string, PlayerIdentityRecord> result =
                new Dictionary<string, PlayerIdentityRecord>(
                    StringComparer.OrdinalIgnoreCase
                );

            foreach (
                KeyValuePair<string, PlayerIdentityRecord> entry
                in _players)
            {
                if (entry.Value == null)
                {
                    continue;
                }

                result[entry.Key] =
                    entry.Value.Clone();
            }

            return result;
        }

        // =========================================================
        // IMPORT
        // =========================================================

        public void Import(
            Dictionary<string, PlayerIdentityRecord> data)
        {
            _players.Clear();

            if (data == null)
            {
                return;
            }

            foreach (
                KeyValuePair<string, PlayerIdentityRecord> entry
                in data)
            {
                if (!IsValid(
                    entry.Key))
                {
                    continue;
                }

                PlayerIdentityRecord record =
                    entry.Value;

                if (record == null)
                {
                    continue;
                }

                string playerId =
                    entry.Key.Trim();

                string playerName =
                    record.Name != null
                        ? record.Name.Trim()
                        : string.Empty;

                if (!IsValid(
                    playerName))
                {
                    continue;
                }

                PlayerIdentityRecord imported =
                    new PlayerIdentityRecord
                    {
                        PlayerId =
                            playerId,

                        Name =
                            playerName,

                        FirstSeenUtc =
                            record.FirstSeenUtc,

                        LastSeenUtc =
                            record.LastSeenUtc
                    };

                _players[playerId] =
                    imported;
            }
        }

        // =========================================================
        // CLEAR
        // =========================================================

        public void Clear()
        {
            _players.Clear();
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
    }

    // =============================================================
    // PLAYER IDENTITY RECORD
    // =============================================================

    public class PlayerIdentityRecord
    {
        public string PlayerId { get; set; }

        public string Name { get; set; }

        public DateTime FirstSeenUtc { get; set; }

        public DateTime LastSeenUtc { get; set; }

        // =========================================================
        // CLONE
        // =========================================================

        public PlayerIdentityRecord Clone()
        {
            return new PlayerIdentityRecord
            {
                PlayerId =
                    PlayerId,

                Name =
                    Name,

                FirstSeenUtc =
                    FirstSeenUtc,

                LastSeenUtc =
                    LastSeenUtc
            };
        }

        // =========================================================
        // DISPLAY
        // =========================================================

        public override string ToString()
        {
            return
                $"{Name} ({PlayerId})";
        }
    }
}