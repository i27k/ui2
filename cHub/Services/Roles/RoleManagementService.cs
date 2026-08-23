using System;
using System.Collections.Generic;
using cHub.Core;
using cHub.Shared.Base;
using cHub.Shared.Utils;

using PermissionIds = cHub.Shared.Constants.Permissions;

namespace cHub.Services.Roles
{
    public class RoleManagementService : Service
    {
        private RoleService _roleService;

        // =========================================================
        // INITIALIZE
        // =========================================================

        public override void Initialize()
        {
            if (IsInitialized)
            {
                Logger.Warning(
                    "RoleManagementService is already initialized."
                );

                return;
            }

            Logger.Info(
                "Initializing RoleManagementService..."
            );

            _roleService =
                ServiceRegistry.Get<RoleService>();

            if (_roleService == null)
            {
                throw new InvalidOperationException(
                    "RoleService must be initialized before RoleManagementService."
                );
            }

            IsInitialized = true;

            Logger.Info(
                "RoleManagementService initialized."
            );
        }

        // =========================================================
        // ASSIGN ROLE
        // =========================================================

        public bool AssignRole(
            string actorId,
            string targetPlayerId,
            string roleId)
        {
            if (!IsValid(actorId) ||
                !IsValid(targetPlayerId) ||
                !IsValid(roleId))
            {
                return false;
            }

            if (!_roleService.HasPermission(
                actorId,
                PermissionIds.RolesAssign))
            {
                Logger.Warning(
                    $"Role assignment denied. Actor '{actorId}' does not have '{PermissionIds.RolesAssign}'."
                );

                return false;
            }

            RoleDefinition targetRole =
                _roleService.GetRole(roleId);

            if (targetRole == null)
            {
                Logger.Warning(
                    $"Cannot assign unknown role '{roleId}'."
                );

                return false;
            }

            if (!CanManageRole(
                actorId,
                targetRole))
            {
                Logger.Warning(
                    $"Role assignment denied. Actor '{actorId}' cannot manage role '{roleId}'."
                );

                return false;
            }

            bool assigned =
                _roleService.AssignRole(
                    targetPlayerId,
                    roleId
                );

            if (assigned)
            {
                Logger.Info(
                    $"Actor '{actorId}' assigned role '{roleId}' to player '{targetPlayerId}'."
                );
            }

            return assigned;
        }

        // =========================================================
        // REMOVE ROLE
        // =========================================================

        public bool RemoveRole(
            string actorId,
            string targetPlayerId,
            string roleId)
        {
            if (!IsValid(actorId) ||
                !IsValid(targetPlayerId) ||
                !IsValid(roleId))
            {
                return false;
            }

            if (!_roleService.HasPermission(
                actorId,
                PermissionIds.RolesAssign))
            {
                Logger.Warning(
                    $"Role removal denied. Actor '{actorId}' does not have '{PermissionIds.RolesAssign}'."
                );

                return false;
            }

            RoleDefinition targetRole =
                _roleService.GetRole(roleId);

            if (targetRole == null)
            {
                Logger.Warning(
                    $"Cannot remove unknown role '{roleId}'."
                );

                return false;
            }

            if (!CanManageRole(
                actorId,
                targetRole))
            {
                Logger.Warning(
                    $"Role removal denied. Actor '{actorId}' cannot manage role '{roleId}'."
                );

                return false;
            }

            bool removed =
                _roleService.RemoveRole(
                    targetPlayerId,
                    roleId
                );

            if (removed)
            {
                Logger.Info(
                    $"Actor '{actorId}' removed role '{roleId}' from player '{targetPlayerId}'."
                );
            }

            return removed;
        }

        // =========================================================
        // CLEAR PLAYER ROLES
        // =========================================================

        public bool ClearPlayerRoles(
            string actorId,
            string targetPlayerId)
        {
            if (!IsValid(actorId) ||
                !IsValid(targetPlayerId))
            {
                return false;
            }

            if (!_roleService.HasPermission(
                actorId,
                PermissionIds.RolesManage))
            {
                Logger.Warning(
                    $"Clear roles denied. Actor '{actorId}' does not have '{PermissionIds.RolesManage}'."
                );

                return false;
            }

            RoleDefinition actorHighestRole =
                _roleService.GetHighestRole(
                    actorId
                );

            RoleDefinition targetHighestRole =
                _roleService.GetHighestRole(
                    targetPlayerId
                );

            if (targetHighestRole != null &&
                actorHighestRole != null &&
                targetHighestRole.Priority >= actorHighestRole.Priority &&
                !IsOwner(actorId))
            {
                Logger.Warning(
                    $"Clear roles denied. Actor '{actorId}' cannot clear roles from equal or higher priority player '{targetPlayerId}'."
                );

                return false;
            }

            bool cleared =
                _roleService.ClearPlayerRoles(
                    targetPlayerId
                );

            if (cleared)
            {
                Logger.Info(
                    $"Actor '{actorId}' cleared all roles from player '{targetPlayerId}'."
                );
            }

            return cleared;
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

            return _roleService.HasRole(
                playerId,
                roleId
            );
        }

        // =========================================================
        // GET PLAYER ROLES
        // =========================================================

        public IReadOnlyCollection<RoleDefinition> GetPlayerRoles(
            string actorId,
            string targetPlayerId)
        {
            if (!IsValid(actorId) ||
                !IsValid(targetPlayerId))
            {
                return Array.Empty<RoleDefinition>();
            }

            if (!_roleService.HasPermission(
                actorId,
                PermissionIds.RolesView))
            {
                Logger.Warning(
                    $"Role view denied. Actor '{actorId}' does not have '{PermissionIds.RolesView}'."
                );

                return Array.Empty<RoleDefinition>();
            }

            return _roleService.GetPlayerRoles(
                targetPlayerId
            );
        }

        // =========================================================
        // GET HIGHEST ROLE
        // =========================================================

        public RoleDefinition GetHighestRole(
            string actorId,
            string targetPlayerId)
        {
            if (!IsValid(actorId) ||
                !IsValid(targetPlayerId))
            {
                return null;
            }

            if (!_roleService.HasPermission(
                actorId,
                PermissionIds.RolesView))
            {
                return null;
            }

            return _roleService.GetHighestRole(
                targetPlayerId
            );
        }

        // =========================================================
        // ENSURE PLAYER ROLE
        // =========================================================

        public bool EnsurePlayerRole(
            string playerId)
        {
            if (!IsValid(playerId))
            {
                return false;
            }

            return _roleService.EnsurePlayerRole(
                playerId
            );
        }

        // =========================================================
        // INTERNAL ROLE PRIORITY CHECK
        // =========================================================

        private bool CanManageRole(
            string actorId,
            RoleDefinition targetRole)
        {
            if (targetRole == null)
                return false;

            // Owner can manage everything.
            if (IsOwner(actorId))
                return true;

            RoleDefinition actorHighestRole =
                _roleService.GetHighestRole(
                    actorId
                );

            if (actorHighestRole == null)
                return false;

            // Cannot manage same or higher priority roles.
            return actorHighestRole.Priority >
                   targetRole.Priority;
        }

        // =========================================================
        // OWNER CHECK
        // =========================================================

        private bool IsOwner(
            string playerId)
        {
            if (!IsValid(playerId))
                return false;

            return _roleService.HasRole(
                playerId,
                "owner"
            );
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
                "Shutting down RoleManagementService..."
            );

            _roleService = null;

            IsInitialized = false;

            Logger.Info(
                "RoleManagementService shutdown."
            );
        }
    }
}