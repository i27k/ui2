using System;
using cHub.Core;
using cHub.Shared.Utils;

namespace cHub.Services.Commands
{
    public static class ChatCommandBridge
    {
        // =========================================================
        // HANDLE CHAT MESSAGE
        // =========================================================

        public static CommandResult Handle(
            string actorId,
            string actorName,
            string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return null;
            }

            string trimmed =
                message.Trim();

            // Only intercept slash commands.
            if (!trimmed.StartsWith("/"))
            {
                return null;
            }

            CommandService commandService =
                ServiceRegistry.Get<CommandService>();

            if (commandService == null)
            {
                Logger.Warning(
                    "ChatCommandBridge could not resolve CommandService."
                );

                return CommandResult.Fail(
                    "Command system is unavailable."
                );
            }

            CommandResult result =
                commandService.ExecuteRaw(
                    actorId,
                    actorName,
                    trimmed
                );

            if (result == null)
            {
                Logger.Warning(
                    $"CommandService returned null for chat command '{trimmed}'."
                );

                return CommandResult.Fail(
                    "Command execution failed."
                );
            }

            return result;
        }

        // =========================================================
        // IS COMMAND
        // =========================================================

        public static bool IsCommand(
            string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return false;
            }

            return message
                .TrimStart()
                .StartsWith("/");
        }

        // =========================================================
        // IS REGISTERED COMMAND
        // =========================================================

        public static bool IsRegisteredCommand(
            string message)
        {
            if (!IsCommand(message))
            {
                return false;
            }

            CommandService commandService =
                ServiceRegistry.Get<CommandService>();

            if (commandService == null)
            {
                return false;
            }

            string trimmed =
                message.Trim();

            while (trimmed.StartsWith("/"))
            {
                trimmed =
                    trimmed.Substring(1);
            }

            if (string.IsNullOrWhiteSpace(trimmed))
            {
                return false;
            }

            string[] parts =
                trimmed.Split(
                    new[]
                    {
                        ' '
                    },
                    StringSplitOptions.RemoveEmptyEntries
                );

            if (parts.Length == 0)
            {
                return false;
            }

            string commandName =
                parts[0];

            return commandService.GetCommand(
                commandName
            ) != null;
        }
    }
}