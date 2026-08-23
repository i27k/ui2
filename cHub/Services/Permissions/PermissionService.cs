using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using cHub.Shared.Base;
using cHub.Shared.Utils;

using PermissionIds = cHub.Shared.Constants.Permissions;

namespace cHub.Services.Permissions
{
    /// <summary>
    /// Central permission service for cHub.
    ///
    /// Responsibilities:
    /// - register known permissions
    /// - load/save direct player permissions
    /// - check direct player permissions
    /// - grant permissions
    /// - revoke permissions
    /// - clear direct permissions
    /// - wildcard (*) access
    ///
    /// IMPORTANT:
    ///
    /// This service manages DIRECT player permissions only.
    ///
    /// Role permissions and role inheritance are handled
    /// separately by RoleService.
    /// </summary>
    public class PermissionService : Service
    {
        private readonly PermissionRegistry _registry;
        private readonly PlayerPermissionStore _playerPermissionStore;

        private PermissionPersistence _persistence;

        private readonly string _dataDirectory;
        private readonly string _permissionsFilePath;

        // =========================================================
        // PROPERTIES
        // =========================================================

        public PermissionRegistry Registry =>
            _registry;

        public PlayerPermissionStore PlayerPermissionStore =>
            _playerPermissionStore;

        public int RegisteredPermissionCount =>
            _registry.Count;

        public int PlayerEntryCount =>
            _playerPermissionStore.PlayerCount;

        public string PermissionsFilePath =>
            _permissionsFilePath;

        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public PermissionService()
        {
            _registry =
                new PermissionRegistry();

            _playerPermissionStore =
                new PlayerPermissionStore();

            _dataDirectory =
                Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Mods",
                    "cHub",
                    "Data"
                );

            _permissionsFilePath =
                Path.Combine(
                    _dataDirectory,
                    "permissions.json"
                );
        }

        // =========================================================
        // INITIALIZE
        // =========================================================

        public override void Initialize()
        {
            if (IsInitialized)
            {
                Logger.Warning(
                    "PermissionService is already initialized."
                );

                return;
            }

            Logger.Info(
                "Initializing PermissionService..."
            );

            try
            {
                // =================================================
                // REGISTER KNOWN PERMISSIONS
                // =================================================

                _registry.RegisterDefaults();

                // =================================================
                // PERSISTENCE
                // =================================================

                EnsureDataDirectory();

                _persistence =
                    new PermissionPersistence(
                        _permissionsFilePath
                    );

                LoadPlayerPermissions();

                IsInitialized =
                    true;

                Logger.Info(
                    $"PermissionService initialized with {_registry.Count} registered permissions."
                );

                Logger.Info(
                    $"Loaded direct permissions for {_playerPermissionStore.PlayerCount} players."
                );

                Logger.Info(
                    $"Permission data path: {_permissionsFilePath}"
                );
            }
            catch (Exception ex)
            {
                Logger.Error(
                    $"Failed to initialize PermissionService: {ex}"
                );

                throw;
            }
        }

        // =========================================================
        // DATA DIRECTORY
        // =========================================================

        private void EnsureDataDirectory()
        {
            if (Directory.Exists(
                _dataDirectory))
            {
                return;
            }

            Directory.CreateDirectory(
                _dataDirectory
            );

            Logger.Info(
                $"Created permission data directory: {_dataDirectory}"
            );
        }

        // =========================================================
        // LOAD PLAYER PERMISSIONS
        // =========================================================

        public bool LoadPlayerPermissions()
        {
            if (_persistence == null)
            {
                Logger.Warning(
                    "Cannot load direct permissions because PermissionPersistence is not initialized."
                );

                return false;
            }

            Dictionary<string, List<string>> data =
                _persistence.Load();

            Dictionary<string, List<string>> validData =
                new Dictionary<string, List<string>>(
                    StringComparer.OrdinalIgnoreCase
                );

            int skippedPermissions =
                0;

            foreach (
                KeyValuePair<string, List<string>> entry
                in data)
            {
                if (!IsValidPlayerId(
                    entry.Key))
                {
                    continue;
                }

                if (entry.Value == null)
                {
                    continue;
                }

                List<string> validPermissions =
                    new List<string>();

                foreach (
                    string permissionId
                    in entry.Value)
                {
                    if (!IsValidPermissionId(
                        permissionId))
                    {
                        continue;
                    }

                    string normalizedPermissionId =
                        permissionId.Trim();

                    if (!_registry.Exists(
                        normalizedPermissionId))
                    {
                        skippedPermissions++;

                        Logger.Warning(
                            $"Ignoring unknown persisted permission '{normalizedPermissionId}' for player '{entry.Key}'."
                        );

                        continue;
                    }

                    validPermissions.Add(
                        normalizedPermissionId
                    );
                }

                if (validPermissions.Count == 0)
                {
                    continue;
                }

                validData[entry.Key.Trim()] =
                    validPermissions;
            }

            _playerPermissionStore.Import(
                validData
            );

            if (skippedPermissions > 0)
            {
                Logger.Warning(
                    $"Skipped {skippedPermissions} unknown persisted permission assignments."
                );
            }

            return true;
        }

        // =========================================================
        // SAVE PLAYER PERMISSIONS
        // =========================================================

        public bool SavePlayerPermissions()
        {
            if (_persistence == null)
            {
                Logger.Warning(
                    "Cannot save direct permissions because PermissionPersistence is not initialized."
                );

                return false;
            }

            Dictionary<string, List<string>> data =
                _playerPermissionStore.Export();

            bool success =
                _persistence.Save(
                    data
                );

            if (success)
            {
                Logger.Info(
                    $"Saved direct permissions for {_playerPermissionStore.PlayerCount} players."
                );
            }

            return success;
        }

        // =========================================================
        // HAS PERMISSION
        //
        // DIRECT PLAYER PERMISSIONS ONLY.
        //
        // RoleService combines:
        //
        // direct permissions
        // +
        // role permissions
        // +
        // inherited role permissions
        // =========================================================

        public bool HasPermission(
            string playerId,
            string permissionId)
        {
            if (!IsValidPlayerId(
                playerId))
            {
                return false;
            }

            if (!IsValidPermissionId(
                permissionId))
            {
                return false;
            }

            string normalizedPlayerId =
                playerId.Trim();

            string normalizedPermissionId =
                permissionId.Trim();

            // =====================================================
            // DIRECT FULL ACCESS
            // =====================================================

            if (_playerPermissionStore.Has(
                normalizedPlayerId,
                PermissionIds.All))
            {
                return true;
            }

            // =====================================================
            // DIRECT PERMISSION
            // =====================================================

            return _playerPermissionStore.Has(
                normalizedPlayerId,
                normalizedPermissionId
            );
        }

        // =========================================================
        // HAS ANY
        // =========================================================

        public bool HasAnyPermission(
            string playerId,
            params string[] permissionIds)
        {
            if (!IsValidPlayerId(
                playerId))
            {
                return false;
            }

            if (permissionIds == null ||
                permissionIds.Length == 0)
            {
                return false;
            }

            foreach (
                string permissionId
                in permissionIds)
            {
                if (HasPermission(
                    playerId,
                    permissionId))
                {
                    return true;
                }
            }

            return false;
        }

        // =========================================================
        // HAS ALL
        // =========================================================

        public bool HasAllPermissions(
            string playerId,
            params string[] permissionIds)
        {
            if (!IsValidPlayerId(
                playerId))
            {
                return false;
            }

            if (permissionIds == null ||
                permissionIds.Length == 0)
            {
                return false;
            }

            foreach (
                string permissionId
                in permissionIds)
            {
                if (!HasPermission(
                    playerId,
                    permissionId))
                {
                    return false;
                }
            }

            return true;
        }

        // =========================================================
        // GRANT
        // =========================================================

        public bool GrantPermission(
            string playerId,
            string permissionId)
        {
            bool granted =
                GrantPermissionInternal(
                    playerId,
                    permissionId
                );

            if (!granted)
            {
                return false;
            }

            SavePlayerPermissions();

            return true;
        }

        // =========================================================
        // GRANT INTERNAL
        // =========================================================

        private bool GrantPermissionInternal(
            string playerId,
            string permissionId)
        {
            if (!IsValidPlayerId(
                playerId))
            {
                Logger.Warning(
                    "Cannot grant permission: invalid player ID."
                );

                return false;
            }

            if (!IsValidPermissionId(
                permissionId))
            {
                Logger.Warning(
                    "Cannot grant permission: invalid permission ID."
                );

                return false;
            }

            string normalizedPlayerId =
                playerId.Trim();

            string normalizedPermissionId =
                permissionId.Trim();

            if (!_registry.Exists(
                normalizedPermissionId))
            {
                Logger.Warning(
                    $"Cannot grant unknown permission '{normalizedPermissionId}'."
                );

                return false;
            }

            bool added =
                _playerPermissionStore.Grant(
                    normalizedPlayerId,
                    normalizedPermissionId
                );

            if (!added)
            {
                return false;
            }

            Logger.Info(
                $"Granted direct permission '{normalizedPermissionId}' to player '{normalizedPlayerId}'."
            );

            return true;
        }

        // =========================================================
        // REVOKE
        // =========================================================

        public bool RevokePermission(
            string playerId,
            string permissionId)
        {
            bool revoked =
                RevokePermissionInternal(
                    playerId,
                    permissionId
                );

            if (!revoked)
            {
                return false;
            }

            SavePlayerPermissions();

            return true;
        }

        // =========================================================
        // REVOKE INTERNAL
        // =========================================================

        private bool RevokePermissionInternal(
            string playerId,
            string permissionId)
        {
            if (!IsValidPlayerId(
                playerId))
            {
                return false;
            }

            if (!IsValidPermissionId(
                permissionId))
            {
                return false;
            }

            string normalizedPlayerId =
                playerId.Trim();

            string normalizedPermissionId =
                permissionId.Trim();

            bool removed =
                _playerPermissionStore.Revoke(
                    normalizedPlayerId,
                    normalizedPermissionId
                );

            if (!removed)
            {
                return false;
            }

            Logger.Info(
                $"Revoked direct permission '{normalizedPermissionId}' from player '{normalizedPlayerId}'."
            );

            return true;
        }

        // =========================================================
        // GRANT MULTIPLE
        // =========================================================

        public int GrantPermissions(
            string playerId,
            IEnumerable<string> permissionIds)
        {
            if (!IsValidPlayerId(
                playerId))
            {
                return 0;
            }

            if (permissionIds == null)
            {
                return 0;
            }

            int granted =
                0;

            foreach (
                string permissionId
                in permissionIds)
            {
                if (GrantPermissionInternal(
                    playerId,
                    permissionId))
                {
                    granted++;
                }
            }

            if (granted > 0)
            {
                SavePlayerPermissions();
            }

            return granted;
        }

        // =========================================================
        // REVOKE MULTIPLE
        // =========================================================

        public int RevokePermissions(
            string playerId,
            IEnumerable<string> permissionIds)
        {
            if (!IsValidPlayerId(
                playerId))
            {
                return 0;
            }

            if (permissionIds == null)
            {
                return 0;
            }

            int revoked =
                0;

            foreach (
                string permissionId
                in permissionIds)
            {
                if (RevokePermissionInternal(
                    playerId,
                    permissionId))
                {
                    revoked++;
                }
            }

            if (revoked > 0)
            {
                SavePlayerPermissions();
            }

            return revoked;
        }

        // =========================================================
        // FULL ACCESS
        // =========================================================

        public bool GrantFullAccess(
            string playerId)
        {
            return GrantPermission(
                playerId,
                PermissionIds.All
            );
        }

        public bool RevokeFullAccess(
            string playerId)
        {
            return RevokePermission(
                playerId,
                PermissionIds.All
            );
        }

        public bool HasFullAccess(
            string playerId)
        {
            if (!IsValidPlayerId(
                playerId))
            {
                return false;
            }

            return _playerPermissionStore.Has(
                playerId.Trim(),
                PermissionIds.All
            );
        }

        // =========================================================
        // GET PLAYER DIRECT PERMISSIONS
        // =========================================================

        public IReadOnlyCollection<string> GetPlayerPermissions(
            string playerId)
        {
            if (!IsValidPlayerId(
                playerId))
            {
                return Array.Empty<string>();
            }

            return _playerPermissionStore.GetPermissions(
                playerId.Trim()
            );
        }

        // =========================================================
        // GET PLAYER PERMISSION DEFINITIONS
        // =========================================================

        public IReadOnlyCollection<PermissionDefinition>
            GetPlayerPermissionDefinitions(
                string playerId)
        {
            if (!IsValidPlayerId(
                playerId))
            {
                return Array.Empty<PermissionDefinition>();
            }

            List<PermissionDefinition> definitions =
                new List<PermissionDefinition>();

            foreach (
                string permissionId
                in GetPlayerPermissions(playerId))
            {
                PermissionDefinition definition =
                    _registry.Get(
                        permissionId
                    );

                if (definition != null)
                {
                    definitions.Add(
                        definition
                    );
                }
            }

            return definitions
                .OrderBy(
                    definition =>
                        definition.Category
                )
                .ThenBy(
                    definition =>
                        definition.Name
                )
                .ToArray();
        }

        // =========================================================
        // CLEAR PLAYER DIRECT PERMISSIONS
        // =========================================================

        public bool ClearPlayerPermissions(
            string playerId)
        {
            if (!IsValidPlayerId(
                playerId))
            {
                return false;
            }

            string normalizedPlayerId =
                playerId.Trim();

            bool removed =
                _playerPermissionStore.ClearPlayer(
                    normalizedPlayerId
                );

            if (!removed)
            {
                return false;
            }

            Logger.Info(
                $"Cleared all direct permissions for player '{normalizedPlayerId}'."
            );

            SavePlayerPermissions();

            return true;
        }

        // =========================================================
        // REGISTRY ACCESS
        // =========================================================

        public bool PermissionExists(
            string permissionId)
        {
            return _registry.Exists(
                permissionId
            );
        }

        public PermissionDefinition GetPermissionDefinition(
            string permissionId)
        {
            return _registry.Get(
                permissionId
            );
        }

        public IEnumerable<PermissionDefinition>
            GetRegisteredPermissions()
        {
            return _registry.GetAll();
        }

        public IEnumerable<PermissionDefinition>
            GetPermissionsByCategory(
                string category)
        {
            return _registry.GetByCategory(
                category
            );
        }

        // =========================================================
        // VALIDATION
        // =========================================================

        private static bool IsValidPlayerId(
            string playerId)
        {
            return !string.IsNullOrWhiteSpace(
                playerId
            );
        }

        private static bool IsValidPermissionId(
            string permissionId)
        {
            return !string.IsNullOrWhiteSpace(
                permissionId
            );
        }

        // =========================================================
        // SHUTDOWN
        // =========================================================

        public override void Shutdown()
        {
            if (!IsInitialized)
            {
                return;
            }

            Logger.Info(
                "Shutting down PermissionService..."
            );

            SavePlayerPermissions();

            _playerPermissionStore.Clear();
            _registry.Clear();

            _persistence =
                null;

            IsInitialized =
                false;

            Logger.Info(
                "PermissionService shutdown."
            );
        }
    }
}