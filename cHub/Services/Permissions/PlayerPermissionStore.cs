using System;
using System.Collections.Generic;
using System.Linq;
using cHub.Shared.Utils;

namespace cHub.Services.Permissions
{
    /// <summary>
    /// Stores direct player -> permission relationships in memory.
    ///
    /// These are direct permissions assigned specifically to players.
    /// Role permissions are handled separately by RoleService.
    ///
    /// Persistence is handled by PermissionPersistence.
    /// </summary>
    public class PlayerPermissionStore
    {
        private readonly Dictionary<string, HashSet<string>> _playerPermissions =
            new Dictionary<string, HashSet<string>>(
                StringComparer.OrdinalIgnoreCase
            );

        // =========================================================
        // PROPERTIES
        // =========================================================

        public int PlayerCount =>
            _playerPermissions.Count;

        // =========================================================
        // GRANT
        // =========================================================

        public bool Grant(
            string playerId,
            string permissionId)
        {
            if (!IsValid(playerId) ||
                !IsValid(permissionId))
            {
                return false;
            }

            string normalizedPlayerId =
                playerId.Trim();

            string normalizedPermissionId =
                permissionId.Trim();

            HashSet<string> permissions =
                GetOrCreatePermissionSet(
                    normalizedPlayerId
                );

            return permissions.Add(
                normalizedPermissionId
            );
        }

        // =========================================================
        // REVOKE
        // =========================================================

        public bool Revoke(
            string playerId,
            string permissionId)
        {
            if (!IsValid(playerId) ||
                !IsValid(permissionId))
            {
                return false;
            }

            string normalizedPlayerId =
                playerId.Trim();

            string normalizedPermissionId =
                permissionId.Trim();

            if (!_playerPermissions.TryGetValue(
                normalizedPlayerId,
                out HashSet<string> permissions))
            {
                return false;
            }

            bool removed =
                permissions.Remove(
                    normalizedPermissionId
                );

            if (!removed)
            {
                return false;
            }

            if (permissions.Count == 0)
            {
                _playerPermissions.Remove(
                    normalizedPlayerId
                );
            }

            return true;
        }

        // =========================================================
        // HAS
        // =========================================================

        public bool Has(
            string playerId,
            string permissionId)
        {
            if (!IsValid(playerId) ||
                !IsValid(permissionId))
            {
                return false;
            }

            if (!_playerPermissions.TryGetValue(
                playerId.Trim(),
                out HashSet<string> permissions))
            {
                return false;
            }

            return permissions.Contains(
                permissionId.Trim()
            );
        }

        // =========================================================
        // GET PLAYER PERMISSIONS
        // =========================================================

        public IReadOnlyCollection<string> GetPermissions(
            string playerId)
        {
            if (!IsValid(playerId))
            {
                return Array.Empty<string>();
            }

            if (!_playerPermissions.TryGetValue(
                playerId.Trim(),
                out HashSet<string> permissions))
            {
                return Array.Empty<string>();
            }

            return permissions
                .OrderBy(
                    permission =>
                        permission
                )
                .ToArray();
        }

        // =========================================================
        // SET PLAYER PERMISSIONS
        // =========================================================

        public void SetPermissions(
            string playerId,
            IEnumerable<string> permissionIds)
        {
            if (!IsValid(playerId))
            {
                return;
            }

            string normalizedPlayerId =
                playerId.Trim();

            _playerPermissions.Remove(
                normalizedPlayerId
            );

            if (permissionIds == null)
            {
                return;
            }

            HashSet<string> permissions =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase
                );

            foreach (string permissionId in permissionIds)
            {
                if (!IsValid(permissionId))
                {
                    continue;
                }

                permissions.Add(
                    permissionId.Trim()
                );
            }

            if (permissions.Count == 0)
            {
                return;
            }

            _playerPermissions.Add(
                normalizedPlayerId,
                permissions
            );
        }

        // =========================================================
        // CLEAR PLAYER
        // =========================================================

        public bool ClearPlayer(
            string playerId)
        {
            if (!IsValid(playerId))
            {
                return false;
            }

            return _playerPermissions.Remove(
                playerId.Trim()
            );
        }

        // =========================================================
        // EXPORT
        // =========================================================

        public Dictionary<string, List<string>> Export()
        {
            Dictionary<string, List<string>> result =
                new Dictionary<string, List<string>>(
                    StringComparer.OrdinalIgnoreCase
                );

            foreach (
                KeyValuePair<string, HashSet<string>> entry
                in _playerPermissions)
            {
                result[entry.Key] =
                    entry.Value
                        .OrderBy(
                            permission =>
                                permission
                        )
                        .ToList();
            }

            return result;
        }

        // =========================================================
        // IMPORT
        // =========================================================

        public void Import(
            Dictionary<string, List<string>> data)
        {
            _playerPermissions.Clear();

            if (data == null)
            {
                Logger.Warning(
                    "PlayerPermissionStore import received null data."
                );

                return;
            }

            int permissionCount =
                0;

            foreach (
                KeyValuePair<string, List<string>> entry
                in data)
            {
                if (!IsValid(entry.Key) ||
                    entry.Value == null)
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
                    if (!IsValid(permissionId))
                    {
                        continue;
                    }

                    if (permissions.Add(
                        permissionId.Trim()))
                    {
                        permissionCount++;
                    }
                }

                if (permissions.Count == 0)
                {
                    continue;
                }

                _playerPermissions[playerId] =
                    permissions;
            }

            Logger.Info(
                $"Imported direct permissions for {_playerPermissions.Count} players ({permissionCount} assignments)."
            );
        }

        // =========================================================
        // CLEAR ALL
        // =========================================================

        public void Clear()
        {
            _playerPermissions.Clear();
        }

        // =========================================================
        // INTERNAL
        // =========================================================

        private HashSet<string> GetOrCreatePermissionSet(
            string playerId)
        {
            if (_playerPermissions.TryGetValue(
                playerId,
                out HashSet<string> permissions))
            {
                return permissions;
            }

            permissions =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase
                );

            _playerPermissions.Add(
                playerId,
                permissions
            );

            return permissions;
        }

        private static bool IsValid(
            string value)
        {
            return !string.IsNullOrWhiteSpace(
                value
            );
        }
    }
}