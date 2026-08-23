using System;
using System.Collections.Generic;
using System.Linq;
using cHub.Shared.Utils;

namespace cHub.Services.Roles
{
    /// <summary>
    /// Stores player -> role relationships in memory.
    ///
    /// Persistence is handled separately by RolePersistence.
    ///
    /// This store intentionally does not log normal
    /// assign/remove/clear-player operations.
    /// Business-level logging is handled by RoleService.
    /// </summary>
    public class PlayerRoleStore
    {
        private readonly Dictionary<string, HashSet<string>> _playerRoles =
            new Dictionary<string, HashSet<string>>(
                StringComparer.OrdinalIgnoreCase
            );

        public int PlayerCount => _playerRoles.Count;

        // =========================================================
        // ASSIGN
        // =========================================================

        public bool AssignRole(
            string playerId,
            string roleId)
        {
            if (!IsValid(playerId) ||
                !IsValid(roleId))
            {
                return false;
            }

            HashSet<string> roles =
                GetOrCreateRoleSet(
                    playerId
                );

            return roles.Add(
                roleId.Trim()
            );
        }

        // =========================================================
        // REMOVE
        // =========================================================

        public bool RemoveRole(
            string playerId,
            string roleId)
        {
            if (!IsValid(playerId) ||
                !IsValid(roleId))
            {
                return false;
            }

            if (!_playerRoles.TryGetValue(
                playerId,
                out HashSet<string> roles))
            {
                return false;
            }

            bool removed =
                roles.Remove(
                    roleId.Trim()
                );

            if (!removed)
            {
                return false;
            }

            if (roles.Count == 0)
            {
                _playerRoles.Remove(
                    playerId
                );
            }

            return true;
        }

        // =========================================================
        // HAS ROLE
        // =========================================================

        public bool HasRole(
            string playerId,
            string roleId)
        {
            if (!IsValid(playerId) ||
                !IsValid(roleId))
            {
                return false;
            }

            if (!_playerRoles.TryGetValue(
                playerId,
                out HashSet<string> roles))
            {
                return false;
            }

            return roles.Contains(
                roleId.Trim()
            );
        }

        // =========================================================
        // GET ROLES
        // =========================================================

        public IReadOnlyCollection<string> GetRoles(
            string playerId)
        {
            if (!IsValid(playerId))
            {
                return Array.Empty<string>();
            }

            if (!_playerRoles.TryGetValue(
                playerId,
                out HashSet<string> roles))
            {
                return Array.Empty<string>();
            }

            return roles
                .OrderBy(
                    x => x
                )
                .ToArray();
        }

        // =========================================================
        // SET ROLES
        // =========================================================

        public void SetRoles(
            string playerId,
            IEnumerable<string> roleIds)
        {
            if (!IsValid(playerId))
            {
                return;
            }

            _playerRoles.Remove(
                playerId
            );

            if (roleIds == null)
            {
                return;
            }

            foreach (
                string roleId in
                roleIds)
            {
                if (!IsValid(roleId))
                {
                    continue;
                }

                HashSet<string> roles =
                    GetOrCreateRoleSet(
                        playerId
                    );

                roles.Add(
                    roleId.Trim()
                );
            }
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
                in _playerRoles)
            {
                result[entry.Key] =
                    entry.Value
                        .OrderBy(
                            x => x
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
            _playerRoles.Clear();

            if (data == null)
            {
                Logger.Warning(
                    "PlayerRoleStore import received null data."
                );

                return;
            }

            int roleCount =
                0;

            foreach (
                KeyValuePair<string, List<string>> entry
                in data)
            {
                string playerId =
                    entry.Key;

                if (!IsValid(playerId))
                {
                    continue;
                }

                if (entry.Value == null)
                {
                    continue;
                }

                foreach (
                    string roleId in
                    entry.Value)
                {
                    if (!IsValid(roleId))
                    {
                        continue;
                    }

                    HashSet<string> roles =
                        GetOrCreateRoleSet(
                            playerId
                        );

                    if (roles.Add(
                        roleId.Trim()))
                    {
                        roleCount++;
                    }
                }
            }

            Logger.Info(
                $"Imported roles for {_playerRoles.Count} players ({roleCount} assignments)."
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

            return _playerRoles.Remove(
                playerId
            );
        }

        // =========================================================
        // CLEAR ALL
        // =========================================================

        public void Clear()
        {
            _playerRoles.Clear();

            Logger.Info(
                "Player role store cleared."
            );
        }

        // =========================================================
        // INTERNAL
        // =========================================================

        private HashSet<string> GetOrCreateRoleSet(
            string playerId)
        {
            if (_playerRoles.TryGetValue(
                playerId,
                out HashSet<string> roles))
            {
                return roles;
            }

            roles =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase
                );

            _playerRoles.Add(
                playerId,
                roles
            );

            return roles;
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