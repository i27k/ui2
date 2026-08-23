using System;
using System.Collections.Generic;
using System.Linq;

namespace cHub.Services.Roles
{
    /// <summary>
    /// Defines a cHub role.
    ///
    /// A role represents a named collection of permissions
    /// and can optionally inherit permissions from other roles.
    ///
    /// Examples:
    /// Owner
    /// Administrator
    /// Moderator
    /// VIP
    /// Player
    /// </summary>
    public class RoleDefinition
    {
        private readonly HashSet<string> _permissions =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase
            );

        private readonly HashSet<string> _inheritedRoles =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase
            );

        // =========================================================
        // PROPERTIES
        // =========================================================

        public string Id { get; }

        public string Name { get; set; }

        public string Description { get; set; }

        /// <summary>
        /// Higher priority roles take precedence
        /// when a player owns multiple roles.
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Marks roles created internally by cHub.
        /// System roles can be protected from deletion.
        /// </summary>
        public bool IsSystemRole { get; set; }

        /// <summary>
        /// Determines whether this role is currently enabled.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        public int PermissionCount =>
            _permissions.Count;

        public int InheritedRoleCount =>
            _inheritedRoles.Count;

        public IReadOnlyCollection<string> Permissions =>
            _permissions
                .OrderBy(x => x)
                .ToArray();

        public IReadOnlyCollection<string> InheritedRoles =>
            _inheritedRoles
                .OrderBy(x => x)
                .ToArray();

        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public RoleDefinition(
            string id,
            string name,
            string description = "",
            int priority = 0,
            bool isSystemRole = false)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException(
                    "Role ID cannot be empty.",
                    nameof(id)
                );
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(
                    "Role name cannot be empty.",
                    nameof(name)
                );
            }

            Id =
                id.Trim();

            Name =
                name.Trim();

            Description =
                description ?? string.Empty;

            Priority =
                priority;

            IsSystemRole =
                isSystemRole;
        }

        // =========================================================
        // PERMISSIONS
        // =========================================================

        public bool AddPermission(
            string permissionId)
        {
            if (string.IsNullOrWhiteSpace(
                permissionId))
            {
                return false;
            }

            return _permissions.Add(
                permissionId.Trim()
            );
        }

        public bool RemovePermission(
            string permissionId)
        {
            if (string.IsNullOrWhiteSpace(
                permissionId))
            {
                return false;
            }

            return _permissions.Remove(
                permissionId.Trim()
            );
        }

        public bool HasPermission(
            string permissionId)
        {
            if (string.IsNullOrWhiteSpace(
                permissionId))
            {
                return false;
            }

            return _permissions.Contains(
                permissionId.Trim()
            );
        }

        // =========================================================
        // MULTIPLE PERMISSIONS
        // =========================================================

        public int AddPermissions(
            IEnumerable<string> permissionIds)
        {
            if (permissionIds == null)
            {
                return 0;
            }

            int added =
                0;

            foreach (
                string permissionId in
                permissionIds)
            {
                if (AddPermission(
                    permissionId))
                {
                    added++;
                }
            }

            return added;
        }

        public int RemovePermissions(
            IEnumerable<string> permissionIds)
        {
            if (permissionIds == null)
            {
                return 0;
            }

            int removed =
                0;

            foreach (
                string permissionId in
                permissionIds)
            {
                if (RemovePermission(
                    permissionId))
                {
                    removed++;
                }
            }

            return removed;
        }

        // =========================================================
        // ROLE INHERITANCE
        // =========================================================

        public bool AddInheritedRole(
            string roleId)
        {
            if (string.IsNullOrWhiteSpace(
                roleId))
            {
                return false;
            }

            string normalized =
                roleId.Trim();

            // Prevent direct self inheritance.
            if (string.Equals(
                normalized,
                Id,
                StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return _inheritedRoles.Add(
                normalized
            );
        }

        public bool RemoveInheritedRole(
            string roleId)
        {
            if (string.IsNullOrWhiteSpace(
                roleId))
            {
                return false;
            }

            return _inheritedRoles.Remove(
                roleId.Trim()
            );
        }

        public bool InheritsRole(
            string roleId)
        {
            if (string.IsNullOrWhiteSpace(
                roleId))
            {
                return false;
            }

            return _inheritedRoles.Contains(
                roleId.Trim()
            );
        }

        public int AddInheritedRoles(
            IEnumerable<string> roleIds)
        {
            if (roleIds == null)
            {
                return 0;
            }

            int added =
                0;

            foreach (
                string roleId in
                roleIds)
            {
                if (AddInheritedRole(
                    roleId))
                {
                    added++;
                }
            }

            return added;
        }

        // =========================================================
        // CLEAR
        // =========================================================

        public void ClearPermissions()
        {
            _permissions.Clear();
        }

        public void ClearInheritedRoles()
        {
            _inheritedRoles.Clear();
        }

        // =========================================================
        // CLONE
        // =========================================================

        public RoleDefinition Clone()
        {
            RoleDefinition clone =
                new RoleDefinition(
                    Id,
                    Name,
                    Description,
                    Priority,
                    IsSystemRole
                );

            clone.IsEnabled =
                IsEnabled;

            clone.AddPermissions(
                _permissions
            );

            clone.AddInheritedRoles(
                _inheritedRoles
            );

            return clone;
        }

        // =========================================================
        // DISPLAY
        // =========================================================

        public override string ToString()
        {
            return
                $"{Name} ({Id}) - " +
                $"{PermissionCount} permissions, " +
                $"{InheritedRoleCount} inherited roles";
        }
    }
}