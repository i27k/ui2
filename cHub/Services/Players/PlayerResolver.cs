using System;
using cHub.Core;
using cHub.Shared.Utils;

namespace cHub.Services.Players
{
    public static class PlayerResolver
    {
        // =========================================================
        // RESOLVE PLAYER ID
        // =========================================================

        public static bool TryResolvePlayerId(
            string input,
            out string playerId,
            out string playerName)
        {
            playerId =
                string.Empty;

            playerName =
                string.Empty;

            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            string value =
                input.Trim();

            // =====================================================
            // EOS / INTERNAL ID DIRECT INPUT
            // =====================================================

            if (value.StartsWith(
                "EOS_",
                StringComparison.OrdinalIgnoreCase))
            {
                playerId =
                    value;

                // Try to recover the known player name from the
                // persistent identity store.
                PlayerIdentityService identityService =
                    ServiceRegistry.Get<PlayerIdentityService>();

                if (identityService != null &&
                    identityService.TryGetById(
                        value,
                        out PlayerIdentityRecord record) &&
                    record != null &&
                    !string.IsNullOrWhiteSpace(record.Name))
                {
                    playerName =
                        record.Name;
                }
                else
                {
                    playerName =
                        value;
                }

                return true;
            }

            // =====================================================
            // ONLINE PLAYERS
            // =====================================================

            if (ConnectionManager.Instance != null &&
                ConnectionManager.Instance.Clients != null)
            {
                foreach (
                    ClientInfo clientInfo in
                    ConnectionManager.Instance.Clients.List)
                {
                    if (clientInfo == null)
                    {
                        continue;
                    }

                    if (!string.Equals(
                        clientInfo.playerName,
                        value,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (clientInfo.InternalId == null)
                    {
                        continue;
                    }

                    string resolvedId =
                        clientInfo.InternalId.CombinedString;

                    if (string.IsNullOrWhiteSpace(
                        resolvedId))
                    {
                        continue;
                    }

                    playerId =
                        resolvedId;

                    playerName =
                        clientInfo.playerName;

                    return true;
                }
            }

            // =====================================================
            // LOCAL PLAYER / HOST
            // =====================================================

            if (!GameManager.IsDedicatedServer &&
                GameManager.Instance != null &&
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

                    if (!string.Equals(
                        localPlayer.EntityName,
                        value,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    PlatformUserIdentifierAbs localId =
                        GameManager.Instance.getPersistentPlayerID(
                            null
                        );

                    if (localId == null)
                    {
                        continue;
                    }

                    string resolvedId =
                        localId.CombinedString;

                    if (string.IsNullOrWhiteSpace(
                        resolvedId))
                    {
                        continue;
                    }

                    playerId =
                        resolvedId;

                    playerName =
                        localPlayer.EntityName;

                    return true;
                }
            }

            // =====================================================
            // OFFLINE PLAYER
            //
            // Fall back to PlayerIdentityService.
            //
            // This allows commands such as:
            //
            // /rolelist SMikiS
            //
            // even when the player is no longer connected.
            // =====================================================

            PlayerIdentityService offlineIdentityService =
                ServiceRegistry.Get<PlayerIdentityService>();

            if (offlineIdentityService == null)
            {
                Logger.Warning(
                    $"PlayerResolver could not resolve '{value}' because PlayerIdentityService is unavailable."
                );

                return false;
            }

            if (offlineIdentityService.TryGetByName(
                value,
                out PlayerIdentityRecord offlineRecord))
            {
                if (offlineRecord == null ||
                    string.IsNullOrWhiteSpace(
                        offlineRecord.PlayerId))
                {
                    return false;
                }

                playerId =
                    offlineRecord.PlayerId;

                playerName =
                    !string.IsNullOrWhiteSpace(
                        offlineRecord.Name)
                        ? offlineRecord.Name
                        : value;

                Logger.Info(
                    $"Resolved offline player '{playerName}' -> '{playerId}'."
                );

                return true;
            }

            // =====================================================
            // NOT FOUND
            // =====================================================

            return false;
        }
    }
}