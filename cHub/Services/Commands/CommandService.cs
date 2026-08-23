using System;
using System.Collections.Generic;
using System.Linq;
using cHub.Core;
using cHub.Services.Roles;
using cHub.Shared.Base;
using cHub.Shared.Utils;

namespace cHub.Services.Commands
{
    public class CommandService : Service
    {
        private readonly CommandRegistry _registry;

        private RoleService _roleService;

        public CommandRegistry Registry =>
            _registry;

        public int RegisteredCommandCount =>
            _registry.Count;

        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public CommandService()
        {
            _registry =
                new CommandRegistry();
        }

        // =========================================================
        // INITIALIZE
        // =========================================================

        public override void Initialize()
        {
            if (IsInitialized)
            {
                Logger.Warning(
                    "CommandService is already initialized."
                );

                return;
            }

            Logger.Info(
                "Initializing CommandService..."
            );

            _roleService =
                ServiceRegistry.Get<RoleService>();

            if (_roleService == null)
            {
                throw new InvalidOperationException(
                    "RoleService must be initialized before CommandService."
                );
            }

            IsInitialized =
                true;

            Logger.Info(
                "CommandService initialized."
            );
        }

        // =========================================================
        // REGISTER COMMAND
        // =========================================================

        public bool RegisterCommand(
            CommandDefinition command)
        {
            if (command == null)
            {
                Logger.Warning(
                    "Cannot register null command."
                );

                return false;
            }

            return _registry.Register(
                command
            );
        }

        // =========================================================
        // UNREGISTER COMMAND
        // =========================================================

        public bool UnregisterCommand(
            string commandName)
        {
            if (string.IsNullOrWhiteSpace(
                commandName))
            {
                return false;
            }

            return _registry.Unregister(
                commandName
            );
        }

        // =========================================================
        // EXECUTE
        // =========================================================

        public CommandResult Execute(
            CommandContext context)
        {
            if (context == null)
            {
                return CommandResult.Fail(
                    "Invalid command context."
                );
            }

            if (string.IsNullOrWhiteSpace(
                context.CommandName))
            {
                return CommandResult.Fail(
                    "Command name cannot be empty."
                );
            }

            CommandDefinition command =
                _registry.Get(
                    context.CommandName
                );

            if (command == null)
            {
                return CommandResult.Fail(
                    $"Unknown command '/{context.CommandName}'."
                );
            }

            if (!command.IsEnabled)
            {
                return CommandResult.Fail(
                    $"Command '/{command.Name}' is currently disabled."
                );
            }

            // =====================================================
            // PERMISSION CHECK
            // =====================================================

            if (!command.IsPublic)
            {
                if (!context.HasActor())
                {
                    Logger.Warning(
                        $"Command '/{command.Name}' denied because no actor ID was provided."
                    );

                    return CommandResult.Fail(
                        "Unable to verify command permissions."
                    );
                }

                if (string.IsNullOrWhiteSpace(
                    command.RequiredPermission))
                {
                    Logger.Warning(
                        $"Command '/{command.Name}' is not public but has no required permission configured."
                    );

                    return CommandResult.Fail(
                        "Command permission configuration is invalid."
                    );
                }

                if (!_roleService.HasPermission(
                    context.ActorId,
                    command.RequiredPermission))
                {
                    Logger.Warning(
                        $"Command '/{command.Name}' denied for actor '{context.ActorId}'. Missing permission '{command.RequiredPermission}'."
                    );

                    return CommandResult.Fail(
                        "You do not have permission to use this command."
                    );
                }
            }

            // =====================================================
            // EXECUTE COMMAND
            // =====================================================

            try
            {
                CommandResult result =
                    command.Execute(
                        context
                    );

                if (result == null)
                {
                    Logger.Warning(
                        $"Command '/{command.Name}' returned null result."
                    );

                    return CommandResult.Fail(
                        "Command execution failed."
                    );
                }

                // Only successful executions are logged here.
                //
                // Expected failures such as:
                // - invalid arguments
                // - player not found
                // - role already assigned
                // - role not found
                //
                // are returned to the caller without generating
                // another generic failure log.
                if (result.Success)
                {
                    string actorId =
                        string.IsNullOrWhiteSpace(
                            context.ActorId)
                            ? "unknown"
                            : context.ActorId;

                    string actorName =
                        string.IsNullOrWhiteSpace(
                            context.ActorName)
                            ? "Unknown"
                            : context.ActorName;

                    Logger.Info(
                        $"Command '/{command.Name}' executed by '{actorId}' ({actorName})."
                    );
                }

                return result;
            }
            catch (Exception ex)
            {
                Logger.Error(
                    $"Command '/{command.Name}' execution failed: {ex}"
                );

                return CommandResult.Fail(
                    "An internal error occurred while executing the command."
                );
            }
        }

        // =========================================================
        // EXECUTE RAW INPUT
        // =========================================================

        public CommandResult ExecuteRaw(
            string actorId,
            string actorName,
            string rawInput)
        {
            if (string.IsNullOrWhiteSpace(
                rawInput))
            {
                return CommandResult.Fail(
                    "Command input cannot be empty."
                );
            }

            string input =
                rawInput.Trim();

            // Remove one or more leading slashes.
            while (input.StartsWith("/"))
            {
                input =
                    input.Substring(1);
            }

            if (string.IsNullOrWhiteSpace(
                input))
            {
                return CommandResult.Fail(
                    "Command input cannot be empty."
                );
            }

            string[] parts =
                input.Split(
                    new[]
                    {
                        ' '
                    },
                    StringSplitOptions.RemoveEmptyEntries
                );

            if (parts.Length == 0)
            {
                return CommandResult.Fail(
                    "Command input cannot be empty."
                );
            }

            string commandName =
                parts[0];

            string[] arguments =
                parts
                    .Skip(1)
                    .ToArray();

            CommandContext context =
                new CommandContext(
                    actorId,
                    actorName,
                    commandName,
                    arguments,
                    rawInput
                );

            return Execute(
                context
            );
        }

        // =========================================================
        // CAN EXECUTE
        // =========================================================

        public bool CanExecute(
            string playerId,
            CommandDefinition command)
        {
            if (command == null)
            {
                return false;
            }

            if (!command.IsEnabled)
            {
                return false;
            }

            if (command.IsPublic)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(
                playerId))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(
                command.RequiredPermission))
            {
                return false;
            }

            return _roleService.HasPermission(
                playerId,
                command.RequiredPermission
            );
        }

        // =========================================================
        // CAN EXECUTE BY NAME
        // =========================================================

        public bool CanExecute(
            string playerId,
            string commandName)
        {
            if (string.IsNullOrWhiteSpace(
                commandName))
            {
                return false;
            }

            CommandDefinition command =
                _registry.Get(
                    commandName
                );

            return CanExecute(
                playerId,
                command
            );
        }

        // =========================================================
        // AVAILABLE COMMANDS
        // =========================================================

        public IReadOnlyCollection<CommandDefinition>
            GetAvailableCommands(
                string playerId)
        {
            return _registry
                .GetEnabled()
                .Where(
                    command =>
                        CanExecute(
                            playerId,
                            command
                        )
                )
                .OrderBy(
                    command =>
                        command.Name
                )
                .ToArray();
        }

        // =========================================================
        // SEARCH AVAILABLE COMMANDS
        // =========================================================

        public IReadOnlyCollection<CommandDefinition>
            SearchAvailableCommands(
                string playerId,
                string query)
        {
            return _registry.SearchAvailable(
                query,
                command =>
                    CanExecute(
                        playerId,
                        command
                    )
            );
        }

        // =========================================================
        // GET COMMAND
        // =========================================================

        public CommandDefinition GetCommand(
            string commandName)
        {
            if (string.IsNullOrWhiteSpace(
                commandName))
            {
                return null;
            }

            return _registry.Get(
                commandName
            );
        }

        // =========================================================
        // GET ALL COMMANDS
        // =========================================================

        public IReadOnlyCollection<CommandDefinition>
            GetAllCommands()
        {
            return _registry.GetAll();
        }

        // =========================================================
        // GET PUBLIC COMMANDS
        // =========================================================

        public IReadOnlyCollection<CommandDefinition>
            GetPublicCommands()
        {
            return _registry.GetPublic();
        }

        // =========================================================
        // SEARCH ALL
        // =========================================================

        public IReadOnlyCollection<CommandDefinition>
            SearchCommands(
                string query)
        {
            return _registry.Search(
                query
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
                "Shutting down CommandService..."
            );

            _registry.Clear();

            _roleService =
                null;

            IsInitialized =
                false;

            Logger.Info(
                "CommandService shutdown."
            );
        }
    }
}