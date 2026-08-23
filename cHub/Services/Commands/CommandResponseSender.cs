using System.Collections.Generic;
using cHub.Shared.Utils;

namespace cHub.Services.Commands
{
    public static class CommandResponseSender
    {
        // =========================================================
        // SEND TO COMMAND ACTOR
        // =========================================================

        public static bool SendToActor(
            int actorEntityId,
            string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return false;
            }

            if (GameManager.Instance == null)
            {
                Logger.Warning(
                    "Cannot send command response because GameManager is unavailable."
                );

                return false;
            }

            // =====================================================
            // LOCAL PLAYER / HOST
            // =====================================================

            if (!GameManager.IsDedicatedServer &&
                GameManager.Instance.World != null)
            {
                foreach (
                    EntityPlayerLocal localPlayer in
                    GameManager.Instance.World.GetLocalPlayers())
                {
                    if (localPlayer == null)
                    {
                        continue;
                    }

                    if (localPlayer.entityId != actorEntityId)
                    {
                        continue;
                    }

                    GameManager.Instance.ChatMessageClient(
                        EChatType.Global,
                        -1,
                        FormatMessage(message),
                        new List<int>
                        {
                            actorEntityId
                        },
                        EMessageSender.Server,
                        GeneratedTextManager.BbCodeSupportMode.Supported
                    );

                    Logger.Info(
                        $"Sent command response to local player EntityId={actorEntityId}."
                    );

                    return true;
                }
            }

            // =====================================================
            // REMOTE PLAYER
            // =====================================================

            if (ConnectionManager.Instance == null ||
                ConnectionManager.Instance.Clients == null)
            {
                Logger.Warning(
                    "Cannot send command response because ConnectionManager is unavailable."
                );

                return false;
            }

            ClientInfo clientInfo =
                ConnectionManager.Instance.Clients.ForEntityId(
                    actorEntityId
                );

            if (clientInfo == null)
            {
                Logger.Warning(
                    $"Cannot send command response. No ClientInfo found for EntityId={actorEntityId}."
                );

                return false;
            }

            clientInfo.SendPackage(
                NetPackageManager
                    .GetPackage<NetPackageChat>()
                    .Setup(
                        EChatType.Global,
                        -1,
                        FormatMessage(message),
                        null,
                        EMessageSender.Server,
                        GeneratedTextManager.BbCodeSupportMode.Supported
                    )
            );

            Logger.Info(
                $"Sent command response to remote player EntityId={actorEntityId}."
            );

            return true;
        }

        // =========================================================
        // FORMAT MESSAGE
        // =========================================================

        private static string FormatMessage(
            string message)
        {
            return
                "[FF2A2A][cHub][-] " +
                message;
        }
    }
}