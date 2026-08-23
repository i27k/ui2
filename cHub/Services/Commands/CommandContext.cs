using System;
using System.Collections.Generic;
using System.Linq;

namespace cHub.Services.Commands
{
    public class CommandContext
    {
        // =========================================================
        // PROPERTIES
        // =========================================================

        /// <summary>
        /// EOS / platform ID of the player executing the command.
        /// </summary>
        public string ActorId { get; }

        /// <summary>
        /// Name of the player executing the command.
        /// </summary>
        public string ActorName { get; }

        /// <summary>
        /// Command name without "/" prefix.
        /// Example: rot, toprot, role
        /// </summary>
        public string CommandName { get; }

        /// <summary>
        /// Command arguments.
        /// Example:
        /// /role add PLAYER moderator
        ///
        /// Arguments:
        /// [0] = add
        /// [1] = PLAYER
        /// [2] = moderator
        /// </summary>
        public IReadOnlyList<string> Arguments { get; }

        /// <summary>
        /// Original command text.
        /// </summary>
        public string RawInput { get; }

        /// <summary>
        /// Number of command arguments.
        /// </summary>
        public int ArgumentCount =>
            Arguments.Count;

        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public CommandContext(
            string actorId,
            string actorName,
            string commandName,
            IEnumerable<string> arguments,
            string rawInput = "")
        {
            ActorId =
                Normalize(actorId);

            ActorName =
                Normalize(actorName);

            CommandName =
                NormalizeCommandName(commandName);

            Arguments =
                arguments?
                    .Where(x => x != null)
                    .Select(x => x.Trim())
                    .ToArray()
                ?? Array.Empty<string>();

            RawInput =
                rawInput ?? string.Empty;
        }

        // =========================================================
        // ARGUMENT EXISTS
        // =========================================================

        public bool HasArgument(
            int index)
        {
            return index >= 0 &&
                   index < Arguments.Count;
        }

        // =========================================================
        // GET ARGUMENT
        // =========================================================

        public string GetArgument(
            int index)
        {
            if (!HasArgument(index))
            {
                return null;
            }

            return Arguments[index];
        }

        // =========================================================
        // GET ARGUMENT OR DEFAULT
        // =========================================================

        public string GetArgumentOrDefault(
            int index,
            string defaultValue = "")
        {
            string value =
                GetArgument(index);

            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            return value;
        }

        // =========================================================
        // ARGUMENT EQUALS
        // =========================================================

        public bool ArgumentEquals(
            int index,
            string value)
        {
            string argument =
                GetArgument(index);

            if (argument == null ||
                value == null)
            {
                return false;
            }

            return string.Equals(
                argument,
                value,
                StringComparison.OrdinalIgnoreCase
            );
        }

        // =========================================================
        // JOIN ARGUMENTS
        // =========================================================

        public string JoinArguments(
            int startIndex = 0)
        {
            if (startIndex < 0 ||
                startIndex >= Arguments.Count)
            {
                return string.Empty;
            }

            return string.Join(
                " ",
                Arguments.Skip(startIndex)
            );
        }

        // =========================================================
        // VALID ACTOR
        // =========================================================

        public bool HasActor()
        {
            return !string.IsNullOrWhiteSpace(
                ActorId
            );
        }

        // =========================================================
        // NORMALIZE
        // =========================================================

        private static string Normalize(
            string value)
        {
            return value?.Trim()
                   ?? string.Empty;
        }

        // =========================================================
        // NORMALIZE COMMAND NAME
        // =========================================================

        private static string NormalizeCommandName(
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
    }
}