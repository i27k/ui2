using cHub.Shared.Utils;

using cHub.Core;
using cHub.Services.Chat;

namespace cHub.Services.Commands
{
    public static class ChatCommandListener
    {
        private static bool _registered;

        // =========================================================
        // REGISTER
        // =========================================================

        public static void Register()
        {
            if (_registered)
            {
                Logger.Warning(
                    "ChatCommandListener is already registered."
                );

                return;
            }

            ModEvents.ChatMessage.RegisterHandler(
                OnChatMessage
            );

            _registered = true;

            Logger.Info(
                "ChatCommandListener registered."
            );
        }

        // =========================================================
        // UNREGISTER
        // =========================================================

        public static void Unregister()
        {
            if (!_registered)
            {
                return;
            }

            ModEvents.ChatMessage.UnregisterHandler(
                OnChatMessage
            );

            _registered = false;

            Logger.Info(
                "ChatCommandListener unregistered."
            );
        }

        // =========================================================
        // CHAT EVENT
        // =========================================================

        private static ModEvents.EModEventResult OnChatMessage(
            ref ModEvents.SChatMessageData data)
        {
            string message =
                data.Message;

            if (string.IsNullOrWhiteSpace(message))
            {
                return ModEvents.EModEventResult.Continue;
            }

            // Only handle commands registered by cHub.
            if (!ChatCommandBridge.IsRegisteredCommand(message))
            {
                ServiceRegistry.Get<DiscordBridgeService>()?.ForwardGameChat(
                    !string.IsNullOrWhiteSpace(data.MainName) ? data.MainName : "Unknown",
                    message);
                return ModEvents.EModEventResult.Continue;
            }

            // =====================================================
            // ACTOR NAME
            // =====================================================

            string actorName =
                !string.IsNullOrWhiteSpace(data.MainName)
                    ? data.MainName
                    : "Unknown";

            // =====================================================
            // RESOLVE CLIENT INFO
            // =====================================================

            ClientInfo clientInfo =
                data.ClientInfo;

            if (clientInfo == null &&
                ConnectionManager.Instance != null &&
                ConnectionManager.Instance.Clients != null)
            {
                clientInfo =
                    ConnectionManager.Instance.Clients.ForEntityId(
                        data.SenderEntityId
                    );
            }

            // =====================================================
            // RESOLVE ACTOR ID
            // =====================================================

            string actorId =
                string.Empty;

            // Remote player
            if (clientInfo != null &&
                clientInfo.InternalId != null)
            {
                actorId =
                    clientInfo.InternalId.CombinedString;
            }

            // Local player / host
            if (string.IsNullOrWhiteSpace(actorId) &&
                GameManager.Instance != null)
            {
                PlatformUserIdentifierAbs localId =
                    GameManager.Instance.getPersistentPlayerID(
                        null
                    );

                if (localId != null)
                {
                    actorId =
                        localId.CombinedString;
                }
            }

            // =====================================================
            // RESOLUTION LOG
            // =====================================================

            if (string.IsNullOrWhiteSpace(actorId))
            {
                Logger.Warning(
                    $"Could not resolve actor ID for chat command '{message}'. " +
                    $"Player='{actorName}', EntityId={data.SenderEntityId}."
                );
            }
            else
            {
                Logger.Info(
                    $"Resolved chat actor: '{actorName}' -> '{actorId}' " +
                    $"(EntityId={data.SenderEntityId})."
                );
            }

            // =====================================================
            // COMMAND RECEIVED
            // =====================================================

            Logger.Info(
                $"Chat command received from '{actorName}' " +
                $"({actorId}): {message}"
            );

            // =====================================================
            // EXECUTE COMMAND
            // =====================================================

            CommandResult result =
                ChatCommandBridge.Handle(
                    actorId,
                    actorName,
                    message
                );

            if (result == null)
            {
                Logger.Warning(
                    $"Chat command '{message}' returned no result."
                );

                return ModEvents.EModEventResult
                    .StopHandlersAndVanilla;
            }

            // =====================================================
            // SEND RESPONSE
            // =====================================================

            if (result.ShouldReply &&
                !string.IsNullOrWhiteSpace(result.Message))
            {
                bool responseSent =
                    CommandResponseSender.SendToActor(
                        data.SenderEntityId,
                        result.Message
                    );

                if (!responseSent)
                {
                    Logger.Warning(
                        $"Failed to send command response to " +
                        $"'{actorName}' (EntityId={data.SenderEntityId})."
                    );
                }
            }

            // cHub fully handled this command.
            return ModEvents.EModEventResult
                .StopHandlersAndVanilla;
        }
    }
}
