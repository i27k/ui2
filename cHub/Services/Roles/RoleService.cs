using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using cHub.Core;
using cHub.Services.Config;
using cHub.Services.Permissions;
using cHub.Shared.Base;
using cHub.Shared.Utils;

using PermissionIds = cHub.Shared.Constants.Permissions;

namespace cHub.Services.Roles
{
    public class RoleService : Service
    {
        private readonly RoleRegistry _registry;
        private readonly PlayerRoleStore _playerRoleStore;

        private PermissionService _permissionService;
        private RolePersistence _persistence;

        private readonly string _dataDirectory;
        private readonly string _rolesFilePath;

        public RoleRegistry Registry => _registry;

        public PlayerRoleStore PlayerRoleStore => _playerRoleStore;

        public int RegisteredRoleCount => _registry.Count;

        public int PlayerRoleEntryCount => _playerRoleStore.PlayerCount;

        public string RolesFilePath => _rolesFilePath;

        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public RoleService()
        {
            _registry =
                new RoleRegistry();

            _playerRoleStore =
                new PlayerRoleStore();

            _dataDirectory =
                Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Mods",
                    "cHub",
                    "Data"
                );

            _rolesFilePath =
                Path.Combine(
                    _dataDirectory,
                    "roles.json"
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
                    "RoleService is already initialized."
                );

                return;
            }

            Logger.Info(
                "Initializing RoleService..."
            );

            try
            {
                _permissionService =
                    ServiceRegistry.Get<PermissionService>();

                if (_permissionService == null)
                {
                    throw new InvalidOperationException(
                        "PermissionService must be initialized before RoleService."
                    );
                }

                _registry.RegisterDefaults();

                EnsureDataDirectory();

                _persistence =
                    new RolePersistence(
                        _rolesFilePath
                    );

                LoadPlayerRoles();

                IsInitialized =
                    true;

                Logger.Info(
                    $"RoleService initialized with {_registry.Count} registered roles."
                );

                Logger.Info(
                    $"Role data path: {_rolesFilePath}"
                );
            }
            catch (Exception ex)
            {
                Logger.Error(
                    $"Failed to initialize RoleService: {ex}"
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
                $"Created role data directory: {_dataDirectory}"
            );
        }

        // =========================================================
        // LOAD
        // =========================================================

        public bool LoadPlayerRoles()
        {
            if (_persistence == null)
            {
                Logger.Warning(
                    "Cannot load roles because RolePersistence is not initialized."
                );

                return false;
            }

            Dictionary<string, List<string>> data =
                _persistence.Load();

            _playerRoleStore.Import(
                data
            );

            Logger.Info(
                $"Loaded role assignments for {_playerRoleStore.PlayerCount} players."
            );

            return true;
        }

        // =========================================================
        // SAVE
        // =========================================================

        public bool SavePlayerRoles()
        {
            if (_persistence == null)
            {
                Logger.Warning(
                    "Cannot save roles because RolePersistence is not initialized."
                );

                return false;
            }

            Dictionary<string, List<string>> data =
                _playerRoleStore.Export();

            bool success =
                _persistence.Save(
                    data
                );

            if (success)
            {
                Logger.Info(
                    $"Saved role assignments for {_playerRoleStore.PlayerCount} players."
                );
            }

            return success;
        }

        // =========================================================
        // ROLE LOOKUP
        // =========================================================

        public RoleDefinition GetRole(
            string roleId)
        {
            return _registry.Get(
                roleId
            );
        }

        public bool RoleExists(
            string roleId)
        {
            return _registry.Exists(
                roleId
            );
        }

        public IEnumerable<RoleDefinition> GetRoles()
        {
            return _registry.GetAll();
        }

        public IEnumerable<RoleDefinition> GetEnabledRoles()
        {
            return _registry.GetEnabled();
        }

        // =========================================================
        // ASSIGN ROLE
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

            RoleDefinition role =
                _registry.Get(
                    roleId
                );

            if (role == null)
            {
                Logger.Warning(
                    $"Cannot assign unknown role '{roleId}'."
                );

                return false;
            }

            if (!role.IsEnabled)
            {
                Logger.Warning(
                    $"Cannot assign disabled role '{roleId}'."
                );

                return false;
            }

            bool assigned =
                _playerRoleStore.AssignRole(
                    playerId,
                    role.Id
                );

            if (!assigned)
            {
                return false;
            }

            Logger.Info(
                $"Assigned role '{role.Id}' to player '{playerId}'."
            );

            SavePlayerRoles();

            return true;
        }

        // =========================================================
        // REMOVE ROLE
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

            // =====================================================
            // PRIMARY OWNER PROTECTION
            // =====================================================

            ConfigService configService =
                ServiceRegistry.Get<ConfigService>();

            if (configService != null &&
                configService.Current != null &&
                configService.Current.Permissions != null)
            {
                var permissionConfig =
                    configService.Current.Permissions;

                bool isPrimaryOwner =
                    string.Equals(
                        playerId,
                        permissionConfig.PrimaryOwnerId,
                        StringComparison.OrdinalIgnoreCase
                    );

                bool removingOwnerRole =
                    string.Equals(
                        roleId,
                        "owner",
                        StringComparison.OrdinalIgnoreCase
                    );

                if (permissionConfig.ProtectPrimaryOwner &&
                    isPrimaryOwner &&
                    removingOwnerRole)
                {
                    Logger.Warning(
                        $"Blocked attempt to remove Owner role from protected Primary Owner '{playerId}'."
                    );

                    return false;
                }
            }

            // =====================================================
            // REMOVE ROLE
            // =====================================================

            bool removed =
                _playerRoleStore.RemoveRole(
                    playerId,
                    roleId
                );

            if (!removed)
            {
                return false;
            }

            Logger.Info(
                $"Removed role '{roleId}' from player '{playerId}'."
            );

            // =====================================================
            // RESTORE DEFAULT PRIMARY ROLE IF REQUIRED
            // =====================================================

            EnsurePlayerRoleInternal(
                playerId
            );

            // Save once after the complete operation.
            SavePlayerRoles();

            return true;
        }

        // =========================================================
        // HAS DIRECT ROLE
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

            return _playerRoleStore.HasRole(
                playerId,
                roleId
            );
        }

        // =========================================================
        // HAS EFFECTIVE ROLE
        // =========================================================

        public bool HasEffectiveRole(
            string playerId,
            string roleId)
        {
            if (!IsValid(playerId) ||
                !IsValid(roleId))
            {
                return false;
            }

            IReadOnlyCollection<RoleDefinition> roles =
                GetPlayerRoles(
                    playerId
                );

            foreach (RoleDefinition role in roles)
            {
                HashSet<string> visitedRoles =
                    new HashSet<string>(
                        StringComparer.OrdinalIgnoreCase
                    );

                if (RoleInheritsRoleRecursive(
                    role,
                    roleId,
                    visitedRoles))
                {
                    return true;
                }
            }

            return false;
        }

        // =========================================================
        // PLAYER ROLE IDS
        // =========================================================

        public IReadOnlyCollection<string> GetPlayerRoleIds(
            string playerId)
        {
            if (!IsValid(playerId))
            {
                return Array.Empty<string>();
            }

            return _playerRoleStore.GetRoles(
                playerId
            );
        }

        // =========================================================
        // DIRECT PLAYER ROLE DEFINITIONS
        // =========================================================

        public IReadOnlyCollection<RoleDefinition> GetPlayerRoles(
            string playerId)
        {
            if (!IsValid(playerId))
            {
                return Array.Empty<RoleDefinition>();
            }

            List<RoleDefinition> roles =
                new List<RoleDefinition>();

            foreach (
                string roleId in
                _playerRoleStore.GetRoles(playerId))
            {
                RoleDefinition role =
                    _registry.Get(
                        roleId
                    );

                if (role == null)
                {
                    continue;
                }

                if (!role.IsEnabled)
                {
                    continue;
                }

                roles.Add(
                    role
                );
            }

            return roles
                .OrderByDescending(
                    x => x.Priority
                )
                .ThenBy(
                    x => x.Name
                )
                .ToArray();
        }

        // =========================================================
        // EFFECTIVE PLAYER ROLES
        // =========================================================

        public IReadOnlyCollection<RoleDefinition> GetEffectiveRoles(
            string playerId)
        {
            if (!IsValid(playerId))
            {
                return Array.Empty<RoleDefinition>();
            }

            Dictionary<string, RoleDefinition> roles =
                new Dictionary<string, RoleDefinition>(
                    StringComparer.OrdinalIgnoreCase
                );

            HashSet<string> visitedRoles =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase
                );

            foreach (
                RoleDefinition role in
                GetPlayerRoles(playerId))
            {
                CollectEffectiveRolesRecursive(
                    role,
                    roles,
                    visitedRoles
                );
            }

            return roles.Values
                .OrderByDescending(
                    role => role.Priority
                )
                .ThenBy(
                    role => role.Name
                )
                .ToArray();
        }

        // =========================================================
        // HIGHEST DIRECT ROLE
        // =========================================================

        public RoleDefinition GetHighestRole(
            string playerId)
        {
            return GetPlayerRoles(
                    playerId
                )
                .OrderByDescending(
                    x => x.Priority
                )
                .FirstOrDefault();
        }

        // =========================================================
        // HAS PERMISSION
        // =========================================================

        public bool HasPermission(
            string playerId,
            string permissionId)
        {
            if (!IsValid(playerId) ||
                !IsValid(permissionId))
            {
                return false;
            }

            // Direct player permissions.
            if (_permissionService.HasPermission(
                playerId,
                permissionId))
            {
                return true;
            }

            // Direct wildcard permission.
            if (_permissionService.HasPermission(
                playerId,
                PermissionIds.All))
            {
                return true;
            }

            // Role permissions + inheritance.
            IReadOnlyCollection<RoleDefinition> roles =
                GetPlayerRoles(
                    playerId
                );

            foreach (RoleDefinition role in roles)
            {
                HashSet<string> visitedRoles =
                    new HashSet<string>(
                        StringComparer.OrdinalIgnoreCase
                    );

                if (RoleHasPermissionRecursive(
                    role,
                    permissionId,
                    visitedRoles))
                {
                    return true;
                }
            }

            return false;
        }

        // =========================================================
        // ROLE HAS PERMISSION RECURSIVELY
        // =========================================================

        private bool RoleHasPermissionRecursive(
            RoleDefinition role,
            string permissionId,
            HashSet<string> visitedRoles)
        {
            if (role == null ||
                visitedRoles == null)
            {
                return false;
            }

            if (!role.IsEnabled)
            {
                return false;
            }

            if (!visitedRoles.Add(
                role.Id))
            {
                return false;
            }

            if (role.HasPermission(
                PermissionIds.All))
            {
                return true;
            }

            if (role.HasPermission(
                permissionId))
            {
                return true;
            }

            foreach (
                string inheritedRoleId in
                role.InheritedRoles)
            {
                RoleDefinition inheritedRole =
                    _registry.Get(
                        inheritedRoleId
                    );

                if (inheritedRole == null)
                {
                    Logger.Warning(
                        $"Role '{role.Id}' inherits unknown role '{inheritedRoleId}'."
                    );

                    continue;
                }

                if (RoleHasPermissionRecursive(
                    inheritedRole,
                    permissionId,
                    visitedRoles))
                {
                    return true;
                }
            }

            return false;
        }

        // =========================================================
        // ROLE INHERITANCE CHECK
        // =========================================================

        private bool RoleInheritsRoleRecursive(
            RoleDefinition role,
            string targetRoleId,
            HashSet<string> visitedRoles)
        {
            if (role == null ||
                visitedRoles == null ||
                !IsValid(targetRoleId))
            {
                return false;
            }

            if (!role.IsEnabled)
            {
                return false;
            }

            if (!visitedRoles.Add(
                role.Id))
            {
                return false;
            }

            if (string.Equals(
                role.Id,
                targetRoleId,
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            foreach (
                string inheritedRoleId in
                role.InheritedRoles)
            {
                RoleDefinition inheritedRole =
                    _registry.Get(
                        inheritedRoleId
                    );

                if (inheritedRole == null)
                {
                    continue;
                }

                if (RoleInheritsRoleRecursive(
                    inheritedRole,
                    targetRoleId,
                    visitedRoles))
                {
                    return true;
                }
            }

            return false;
        }

        // =========================================================
        // HAS ANY
        // =========================================================

        public bool HasAnyPermission(
            string playerId,
            params string[] permissionIds)
        {
            if (permissionIds == null ||
                permissionIds.Length == 0)
            {
                return false;
            }

            foreach (
                string permissionId in
                permissionIds)
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
            if (permissionIds == null ||
                permissionIds.Length == 0)
            {
                return false;
            }

            foreach (
                string permissionId in
                permissionIds)
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
        // EFFECTIVE PERMISSIONS
        // =========================================================

        public IReadOnlyCollection<string> GetEffectivePermissions(
            string playerId)
        {
            if (!IsValid(playerId))
            {
                return Array.Empty<string>();
            }

            HashSet<string> permissions =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase
                );

            // Direct player permissions.
            foreach (
                string permission in
                _permissionService.GetPlayerPermissions(playerId))
            {
                permissions.Add(
                    permission
                );
            }

            // Role permissions + inheritance.
            HashSet<string> visitedRoles =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase
                );

            foreach (
                RoleDefinition role in
                GetPlayerRoles(playerId))
            {
                CollectRolePermissionsRecursive(
                    role,
                    permissions,
                    visitedRoles
                );
            }

            return permissions
                .OrderBy(
                    x => x
                )
                .ToArray();
        }

        // =========================================================
        // COLLECT ROLE PERMISSIONS RECURSIVELY
        // =========================================================

        private void CollectRolePermissionsRecursive(
            RoleDefinition role,
            HashSet<string> permissions,
            HashSet<string> visitedRoles)
        {
            if (role == null ||
                permissions == null ||
                visitedRoles == null)
            {
                return;
            }

            if (!role.IsEnabled)
            {
                return;
            }

            if (!visitedRoles.Add(
                role.Id))
            {
                return;
            }

            foreach (
                string permission in
                role.Permissions)
            {
                permissions.Add(
                    permission
                );
            }

            foreach (
                string inheritedRoleId in
                role.InheritedRoles)
            {
                RoleDefinition inheritedRole =
                    _registry.Get(
                        inheritedRoleId
                    );

                if (inheritedRole == null)
                {
                    Logger.Warning(
                        $"Role '{role.Id}' inherits unknown role '{inheritedRoleId}'."
                    );

                    continue;
                }

                CollectRolePermissionsRecursive(
                    inheritedRole,
                    permissions,
                    visitedRoles
                );
            }
        }

        // =========================================================
        // COLLECT EFFECTIVE ROLES RECURSIVELY
        // =========================================================

        private void CollectEffectiveRolesRecursive(
            RoleDefinition role,
            Dictionary<string, RoleDefinition> roles,
            HashSet<string> visitedRoles)
        {
            if (role == null ||
                roles == null ||
                visitedRoles == null)
            {
                return;
            }

            if (!role.IsEnabled)
            {
                return;
            }

            if (!visitedRoles.Add(
                role.Id))
            {
                return;
            }

            roles[role.Id] =
                role;

            foreach (
                string inheritedRoleId in
                role.InheritedRoles)
            {
                RoleDefinition inheritedRole =
                    _registry.Get(
                        inheritedRoleId
                    );

                if (inheritedRole == null)
                {
                    continue;
                }

                CollectEffectiveRolesRecursive(
                    inheritedRole,
                    roles,
                    visitedRoles
                );
            }
        }

        // =========================================================
        // DEFAULT PLAYER ROLE
        // =========================================================

        public bool EnsurePlayerRole(
            string playerId)
        {
            if (!IsValid(playerId))
            {
                return false;
            }

            bool assigned =
                EnsurePlayerRoleInternal(
                    playerId
                );

            if (assigned)
            {
                SavePlayerRoles();
            }

            return assigned;
        }

        // =========================================================
        // ENSURE PRIMARY ROLE INTERNAL
        // =========================================================

        private bool EnsurePlayerRoleInternal(
            string playerId)
        {
            if (!IsValid(playerId))
            {
                return false;
            }

            IReadOnlyCollection<string> roles =
                _playerRoleStore.GetRoles(
                    playerId
                );

            // Primary roles:
            //
            // owner
            // administrator
            // moderator
            // player
            //
            // VIP is intentionally excluded because VIP is
            // a secondary entitlement and never replaces the
            // player's primary role.

            bool hasPrimaryRole =
                roles.Any(
                    roleId =>
                        string.Equals(
                            roleId,
                            "owner",
                            StringComparison.OrdinalIgnoreCase
                        ) ||
                        string.Equals(
                            roleId,
                            "administrator",
                            StringComparison.OrdinalIgnoreCase
                        ) ||
                        string.Equals(
                            roleId,
                            "moderator",
                            StringComparison.OrdinalIgnoreCase
                        ) ||
                        string.Equals(
                            roleId,
                            "player",
                            StringComparison.OrdinalIgnoreCase
                        )
                );

            if (hasPrimaryRole)
            {
                return false;
            }

            RoleDefinition playerRole =
                _registry.Get(
                    "player"
                );

            if (playerRole == null)
            {
                Logger.Warning(
                    $"Cannot assign default Player role to '{playerId}' because the Player role is not registered."
                );

                return false;
            }

            if (!playerRole.IsEnabled)
            {
                Logger.Warning(
                    $"Cannot assign default Player role to '{playerId}' because the Player role is disabled."
                );

                return false;
            }

            bool assigned =
                _playerRoleStore.AssignRole(
                    playerId,
                    playerRole.Id
                );

            if (assigned)
            {
                Logger.Info(
                    $"Default Player role assigned to '{playerId}'."
                );
            }

            return assigned;
        }

        // =========================================================
        // CLEAR PLAYER ROLES
        // =========================================================

        public bool ClearPlayerRoles(
            string playerId)
        {
            if (!IsValid(playerId))
            {
                return false;
            }

            // =====================================================
            // PRIMARY OWNER PROTECTION
            // =====================================================

            ConfigService configService =
                ServiceRegistry.Get<ConfigService>();

            if (configService != null &&
                configService.Current != null &&
                configService.Current.Permissions != null)
            {
                var permissionConfig =
                    configService.Current.Permissions;

                bool isPrimaryOwner =
                    string.Equals(
                        playerId,
                        permissionConfig.PrimaryOwnerId,
                        StringComparison.OrdinalIgnoreCase
                    );

                if (permissionConfig.ProtectPrimaryOwner &&
                    isPrimaryOwner)
                {
                    Logger.Warning(
                        $"Blocked attempt to clear roles from protected Primary Owner '{playerId}'."
                    );

                    return false;
                }
            }

            // =====================================================
            // CLEAR ALL ASSIGNED ROLES
            // =====================================================

            bool cleared =
                _playerRoleStore.ClearPlayer(
                    playerId
                );

            if (!cleared)
            {
                return false;
            }

            Logger.Info(
                $"Cleared all roles for player '{playerId}'."
            );

            // =====================================================
            // RESTORE DEFAULT PRIMARY ROLE
            // =====================================================

            EnsurePlayerRoleInternal(
                playerId
            );

            SavePlayerRoles();

            return true;
        }

        // =========================================================
        // CREATE CUSTOM ROLE
        // =========================================================

        public bool CreateRole(
            string id,
            string name,
            string description = "",
            int priority = 0)
        {
            if (!IsValid(id) ||
                !IsValid(name))
            {
                return false;
            }

            if (_registry.Exists(id))
            {
                Logger.Warning(
                    $"Role '{id}' already exists."
                );

                return false;
            }

            RoleDefinition role =
                new RoleDefinition(
                    id,
                    name,
                    description,
                    priority,
                    false
                );

            return _registry.Register(
                role
            );
        }

        // =========================================================
        // DELETE CUSTOM ROLE
        // =========================================================

        public bool DeleteRole(
            string roleId)
        {
            if (!IsValid(roleId))
            {
                return false;
            }

            return _registry.Unregister(
                roleId
            );
        }

        // =========================================================
        // ADD PERMISSION TO ROLE
        // =========================================================

        public bool AddPermissionToRole(
            string roleId,
            string permissionId)
        {
            if (!IsValid(roleId) ||
                !IsValid(permissionId))
            {
                return false;
            }

            RoleDefinition role =
                _registry.Get(
                    roleId
                );

            if (role == null)
            {
                return false;
            }

            if (!_permissionService.PermissionExists(
                permissionId))
            {
                Logger.Warning(
                    $"Cannot add unknown permission '{permissionId}' to role '{roleId}'."
                );

                return false;
            }

            bool added =
                role.AddPermission(
                    permissionId
                );

            if (added)
            {
                Logger.Info(
                    $"Added permission '{permissionId}' to role '{roleId}'."
                );
            }

            return added;
        }

        // =========================================================
        // REMOVE PERMISSION FROM ROLE
        // =========================================================

        public bool RemovePermissionFromRole(
            string roleId,
            string permissionId)
        {
            if (!IsValid(roleId) ||
                !IsValid(permissionId))
            {
                return false;
            }

            RoleDefinition role =
                _registry.Get(
                    roleId
                );

            if (role == null)
            {
                return false;
            }

            if (role.IsSystemRole &&
                string.Equals(
                    role.Id,
                    "owner",
                    StringComparison.OrdinalIgnoreCase
                ) &&
                string.Equals(
                    permissionId,
                    PermissionIds.All,
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                Logger.Warning(
                    "Cannot remove full access from the Owner role."
                );

                return false;
            }

            bool removed =
                role.RemovePermission(
                    permissionId
                );

            if (removed)
            {
                Logger.Info(
                    $"Removed permission '{permissionId}' from role '{roleId}'."
                );
            }

            return removed;
        }

        // =========================================================
        // ADD ROLE INHERITANCE
        // =========================================================

        public bool AddRoleInheritance(
            string roleId,
            string inheritedRoleId)
        {
            if (!IsValid(roleId) ||
                !IsValid(inheritedRoleId))
            {
                return false;
            }

            RoleDefinition role =
                _registry.Get(
                    roleId
                );

            RoleDefinition inheritedRole =
                _registry.Get(
                    inheritedRoleId
                );

            if (role == null ||
                inheritedRole == null)
            {
                return false;
            }

            if (string.Equals(
                role.Id,
                inheritedRole.Id,
                StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            HashSet<string> visitedRoles =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase
                );

            if (RoleInheritsRoleRecursive(
                inheritedRole,
                role.Id,
                visitedRoles))
            {
                Logger.Warning(
                    $"Cannot make role '{role.Id}' inherit '{inheritedRole.Id}' because it would create a circular role hierarchy."
                );

                return false;
            }

            bool added =
                role.AddInheritedRole(
                    inheritedRole.Id
                );

            if (added)
            {
                Logger.Info(
                    $"Role '{role.Id}' now inherits role '{inheritedRole.Id}'."
                );
            }

            return added;
        }

        // =========================================================
        // REMOVE ROLE INHERITANCE
        // =========================================================

        public bool RemoveRoleInheritance(
            string roleId,
            string inheritedRoleId)
        {
            if (!IsValid(roleId) ||
                !IsValid(inheritedRoleId))
            {
                return false;
            }

            RoleDefinition role =
                _registry.Get(
                    roleId
                );

            if (role == null)
            {
                return false;
            }

            bool removed =
                role.RemoveInheritedRole(
                    inheritedRoleId
                );

            if (removed)
            {
                Logger.Info(
                    $"Role '{role.Id}' no longer inherits role '{inheritedRoleId}'."
                );
            }

            return removed;
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
                "Shutting down RoleService..."
            );

            SavePlayerRoles();

            _playerRoleStore.Clear();
            _registry.Clear();

            _permissionService =
                null;

            _persistence =
                null;

            IsInitialized =
                false;

            Logger.Info(
                "RoleService shutdown."
            );
        }
    }
}