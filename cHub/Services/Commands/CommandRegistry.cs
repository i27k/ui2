using System;
using System.Collections.Generic;
using System.Linq;
using cHub.Shared.Utils;

namespace cHub.Services.Commands
{
    public class CommandRegistry
    {
        private readonly Dictionary<string, CommandDefinition> _commands =
            new Dictionary<string, CommandDefinition>(
                StringComparer.OrdinalIgnoreCase
            );

        // =========================================================
        // COUNT
        // =========================================================

        public int Count =>
            _commands.Count;

        // =========================================================
        // REGISTER
        // =========================================================

        public bool Register(
            CommandDefinition command)
        {
            if (command == null)
            {
                Logger.Warning(
                    "Cannot register null command."
                );

                return false;
            }

            string name =
                NormalizeCommandName(
                    command.Name
                );

            if (string.IsNullOrWhiteSpace(name))
            {
                Logger.Warning(
                    "Cannot register command with empty name."
                );

                return false;
            }

            if (_commands.ContainsKey(name))
            {
                Logger.Warning(
                    $"Command '{name}' is already registered."
                );

                return false;
            }

            // Check aliases before registration.
            foreach (string alias in command.Aliases)
            {
                string normalizedAlias =
                    NormalizeCommandName(
                        alias
                    );

                if (string.IsNullOrWhiteSpace(
                    normalizedAlias))
                {
                    continue;
                }

                if (_commands.ContainsKey(
                    normalizedAlias))
                {
                    Logger.Warning(
                        $"Cannot register command '{name}'. Alias '{normalizedAlias}' is already registered."
                    );

                    return false;
                }
            }

            _commands.Add(
                name,
                command
            );

            // Register aliases pointing to same definition.
            foreach (string alias in command.Aliases)
            {
                string normalizedAlias =
                    NormalizeCommandName(
                        alias
                    );

                if (string.IsNullOrWhiteSpace(
                    normalizedAlias))
                {
                    continue;
                }

                if (_commands.ContainsKey(
                    normalizedAlias))
                {
                    continue;
                }

                _commands.Add(
                    normalizedAlias,
                    command
                );
            }

            Logger.Info(
                $"Registered command: /{command.Name}"
            );

            return true;
        }

        // =========================================================
        // UNREGISTER
        // =========================================================

        public bool Unregister(
            string commandName)
        {
            if (string.IsNullOrWhiteSpace(
                commandName))
            {
                return false;
            }

            CommandDefinition command =
                Get(commandName);

            if (command == null)
            {
                return false;
            }

            List<string> keysToRemove =
                _commands
                    .Where(
                        x => ReferenceEquals(
                            x.Value,
                            command
                        )
                    )
                    .Select(x => x.Key)
                    .ToList();

            foreach (string key in keysToRemove)
            {
                _commands.Remove(key);
            }

            Logger.Info(
                $"Unregistered command: /{command.Name}"
            );

            return true;
        }

        // =========================================================
        // GET
        // =========================================================

        public CommandDefinition Get(
            string commandName)
        {
            if (string.IsNullOrWhiteSpace(
                commandName))
            {
                return null;
            }

            string normalized =
                NormalizeCommandName(
                    commandName
                );

            if (_commands.TryGetValue(
                normalized,
                out CommandDefinition command))
            {
                return command;
            }

            return null;
        }

        // =========================================================
        // EXISTS
        // =========================================================

        public bool Exists(
            string commandName)
        {
            return Get(commandName) != null;
        }

        // =========================================================
        // GET ALL UNIQUE COMMANDS
        // =========================================================

        public IReadOnlyCollection<CommandDefinition> GetAll()
        {
            return _commands
                .Values
                .Distinct()
                .OrderBy(x => x.Name)
                .ToArray();
        }

        // =========================================================
        // GET ENABLED COMMANDS
        // =========================================================

        public IReadOnlyCollection<CommandDefinition> GetEnabled()
        {
            return GetAll()
                .Where(x => x.IsEnabled)
                .OrderBy(x => x.Name)
                .ToArray();
        }

        // =========================================================
        // GET PUBLIC COMMANDS
        // =========================================================

        public IReadOnlyCollection<CommandDefinition> GetPublic()
        {
            return GetEnabled()
                .Where(x => x.IsPublic)
                .OrderBy(x => x.Name)
                .ToArray();
        }

        // =========================================================
        // SEARCH
        // =========================================================

        public IReadOnlyCollection<CommandDefinition> Search(
            string query)
        {
            if (string.IsNullOrWhiteSpace(
                query))
            {
                return GetEnabled();
            }

            string normalized =
                query.Trim();

            return GetEnabled()
                .Where(
                    command =>
                        Contains(
                            command.Name,
                            normalized
                        ) ||
                        Contains(
                            command.Description,
                            normalized
                        ) ||
                        command.Aliases.Any(
                            alias =>
                                Contains(
                                    alias,
                                    normalized
                                )
                        )
                )
                .OrderBy(
                    command =>
                        StartsWith(
                            command.Name,
                            normalized
                        )
                            ? 0
                            : 1
                )
                .ThenBy(
                    command =>
                        command.Name
                )
                .ToArray();
        }

        // =========================================================
        // SEARCH AVAILABLE FOR PLAYER
        // =========================================================

        public IReadOnlyCollection<CommandDefinition> SearchAvailable(
            string query,
            Func<CommandDefinition, bool> accessFilter)
        {
            IEnumerable<CommandDefinition> commands =
                Search(query);

            if (accessFilter != null)
            {
                commands =
                    commands.Where(
                        accessFilter
                    );
            }

            return commands
                .ToArray();
        }

        // =========================================================
        // CLEAR
        // =========================================================

        public void Clear()
        {
            _commands.Clear();

            Logger.Info(
                "Command registry cleared."
            );
        }

        // =========================================================
        // INTERNAL HELPERS
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

        private static bool Contains(
            string value,
            string query)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                string.IsNullOrWhiteSpace(query))
            {
                return false;
            }

            return value.IndexOf(
                query,
                StringComparison.OrdinalIgnoreCase
            ) >= 0;
        }

        private static bool StartsWith(
            string value,
            string query)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                string.IsNullOrWhiteSpace(query))
            {
                return false;
            }

            return value.StartsWith(
                query,
                StringComparison.OrdinalIgnoreCase
            );
        }
    }
}