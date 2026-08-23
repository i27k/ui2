using System;
using System.Collections.Generic;
using System.Linq;

namespace cHub.Services.Commands
{
    public abstract class CommandDefinition
    {
        // =========================================================
        // COMMAND INFO
        // =========================================================

        /// <summary>
        /// Main command name without "/".
        /// Example: rot, toprot, role
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Short command description.
        /// </summary>
        public virtual string Description =>
            string.Empty;

        /// <summary>
        /// Usage displayed to the player.
        /// Example: /rot
        /// Example: /role add <player> <role>
        /// </summary>
        public virtual string Usage =>
            "/" + Name;

        // =========================================================
        // ALIASES
        // =========================================================

        /// <summary>
        /// Optional alternative command names.
        /// </summary>
        public virtual IReadOnlyCollection<string> Aliases =>
            Array.Empty<string>();

        // =========================================================
        // PERMISSION
        // =========================================================

        /// <summary>
        /// Permission required to execute this command.
        ///
        /// null / empty = PUBLIC command.
        ///
        /// Example:
        /// rot     -> null
        /// toprot  -> null
        /// role    -> roles.view / roles.assign / etc.
        /// </summary>
        public virtual string RequiredPermission =>
            null;

        /// <summary>
        /// True when the command is public.
        /// </summary>
        public bool IsPublic =>
            string.IsNullOrWhiteSpace(
                RequiredPermission
            );

        // =========================================================
        // ENABLED
        // =========================================================

        /// <summary>
        /// Allows a command to be disabled without removing it
        /// from the registry.
        /// </summary>
        public virtual bool IsEnabled =>
            true;

        // =========================================================
        // EXECUTE
        // =========================================================

        /// <summary>
        /// Executes the command.
        /// Permission checks are performed by CommandService
        /// before this method is called.
        /// </summary>
        public abstract CommandResult Execute(
            CommandContext context
        );

        // =========================================================
        // NAME MATCHING
        // =========================================================

        public bool Matches(
            string commandName)
        {
            if (string.IsNullOrWhiteSpace(
                commandName))
            {
                return false;
            }

            string normalized =
                NormalizeCommandName(
                    commandName
                );

            if (string.Equals(
                NormalizeCommandName(Name),
                normalized,
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return Aliases.Any(
                alias =>
                    string.Equals(
                        NormalizeCommandName(alias),
                        normalized,
                        StringComparison.OrdinalIgnoreCase
                    )
            );
        }

        // =========================================================
        // NORMALIZE COMMAND NAME
        // =========================================================

        protected static string NormalizeCommandName(
            string commandName)
        {
            if (string.IsNullOrWhiteSpace(
                commandName))
            {
                return string.Empty;
            }

            string normalized =
                commandName.Trim();

            while (normalized.StartsWith("/"))
            {
                normalized =
                    normalized.Substring(1);
            }

            return normalized
                .Trim()
                .ToLowerInvariant();
        }

        // =========================================================
        // DISPLAY
        // =========================================================

        public override string ToString()
        {
            return Name;
        }
    }
}