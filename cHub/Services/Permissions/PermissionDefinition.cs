using System;

namespace cHub.Services.Permissions
{
    /// <summary>
    /// Describes a permission registered in cHub.
    /// This information can later be displayed automatically
    /// inside the Admin / Owner Panel.
    /// </summary>
    public class PermissionDefinition
    {
        public string Id { get; }

        public string Name { get; }

        public string Description { get; }

        public string Category { get; }

        public bool IsDangerous { get; }

        public PermissionDefinition(
            string id,
            string name,
            string description,
            string category,
            bool isDangerous = false)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException(
                    "Permission ID cannot be null or empty.",
                    nameof(id)
                );
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(
                    "Permission name cannot be null or empty.",
                    nameof(name)
                );
            }

            Id = id.Trim();
            Name = name.Trim();

            Description =
                description?.Trim() ?? string.Empty;

            Category =
                string.IsNullOrWhiteSpace(category)
                    ? "General"
                    : category.Trim();

            IsDangerous = isDangerous;
        }

        public override string ToString()
        {
            return $"{Id} ({Name})";
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;

            if (!(obj is PermissionDefinition other))
                return false;

            return string.Equals(
                Id,
                other.Id,
                StringComparison.OrdinalIgnoreCase
            );
        }

        public override int GetHashCode()
        {
            return StringComparer
                .OrdinalIgnoreCase
                .GetHashCode(Id);
        }
    }
}