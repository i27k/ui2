using System;
using cHub.Shared.Utils;

namespace cHub.Services.Players
{
    /// <summary>
    /// Resolves the canonical persistent identity used by cHub.
    ///
    /// Preferred identity:
    /// ClientInfo.InternalId.CombinedString
    ///
    /// Example:
    /// EOS_00028ef93e494b4ead6248437af730e9
    ///
    /// Resolution order:
    ///
    /// 1. ClientInfo directly
    /// 2. EntityId -> ConnectionManager -> ClientInfo
    ///
    /// The resolver never falls back to player name or EntityId
    /// because neither is a safe persistent identity.
    /// </summary>
    public static class PlayerIdentityResolver
    {
        // =========================================================
        // CLIENT INFO
        // =========================================================

        public static bool TryResolve(
            ClientInfo clientInfo,
            out string playerId)
        {
            playerId = null;

            if (clientInfo == null)
            {
                return false;
            }

            if (clientInfo.InternalId == null)
            {
                return false;
            }

            string resolvedId =
                clientInfo.InternalId.CombinedString;

            if (string.IsNullOrWhiteSpace(
                resolvedId))
            {
                return false;
            }

            playerId =
                NormalizePlayerId(
                    resolvedId
                );

            return !string.IsNullOrWhiteSpace(
                playerId
            );
        }

        // =========================================================
        // ENTITY ID
        // =========================================================

        public static bool TryResolve(
            int entityId,
            out string playerId)
        {
            playerId = null;

            if (entityId <= 0)
            {
                return false;
            }

            try
            {
                // Single-player/listen-server spawn events may not expose a
                // ClientInfo yet. The entity's persistent platform identity is
                // canonical and safe; never fall back to name or EntityId.
                if (GameManager.Instance?.World?.Players?.dict != null &&
                    GameManager.Instance.World.Players.dict.TryGetValue(
                        entityId, out EntityPlayer entityPlayer) &&
                    entityPlayer?.PersistentPlayerData?.PrimaryId != null)
                {
                    string persistentId = entityPlayer.PersistentPlayerData
                        .PrimaryId.CombinedString;
                    if (!string.IsNullOrWhiteSpace(persistentId))
                    {
                        playerId = NormalizePlayerId(persistentId);
                        if (!string.IsNullOrWhiteSpace(playerId)) return true;
                    }
                }

                if (ConnectionManager.Instance == null)
                {
                    return false;
                }

                ClientInfo clientInfo =
                    ConnectionManager.Instance
                        .Clients
                        .ForEntityId(
                            entityId
                        );

                if (clientInfo == null)
                {
                    return false;
                }

                return TryResolve(
                    clientInfo,
                    out playerId
                );
            }
            catch (Exception ex)
            {
                Logger.Warning(
                    $"Failed to resolve player identity for EntityId={entityId}: {ex.Message}"
                );

                return false;
            }
        }

        // =========================================================
        // CLIENT INFO + ENTITY FALLBACK
        // =========================================================

        public static bool TryResolve(
            ClientInfo clientInfo,
            int entityId,
            out string playerId)
        {
            playerId = null;

            // =====================================================
            // PRIMARY:
            // ClientInfo supplied by the event.
            // =====================================================

            if (TryResolve(
                clientInfo,
                out playerId))
            {
                return true;
            }

            // =====================================================
            // FALLBACK:
            // Resolve ClientInfo through EntityId.
            // =====================================================

            if (entityId > 0 &&
                TryResolve(
                    entityId,
                    out playerId))
            {
                return true;
            }

            return false;
        }

        // =========================================================
        // CLIENT INFO FROM ENTITY ID
        // =========================================================

        public static ClientInfo GetClientInfo(
            int entityId)
        {
            if (entityId <= 0)
            {
                return null;
            }

            try
            {
                if (ConnectionManager.Instance == null)
                {
                    return null;
                }

                return ConnectionManager.Instance
                    .Clients
                    .ForEntityId(
                        entityId
                    );
            }
            catch
            {
                return null;
            }
        }

        // =========================================================
        // PLAYER NAME
        // =========================================================

        public static string GetPlayerName(
            ClientInfo clientInfo,
            int entityId)
        {
            // =====================================================
            // CLIENT INFO
            // =====================================================

            if (clientInfo != null &&
                !string.IsNullOrWhiteSpace(
                    clientInfo.playerName))
            {
                return clientInfo.playerName;
            }

            // =====================================================
            // CONNECTION MANAGER
            // =====================================================

            ClientInfo resolvedClientInfo =
                GetClientInfo(
                    entityId
                );

            if (resolvedClientInfo != null &&
                !string.IsNullOrWhiteSpace(
                    resolvedClientInfo.playerName))
            {
                return resolvedClientInfo.playerName;
            }

            // =====================================================
            // ENTITY PLAYER
            //
            // Name only.
            // Never use this as persistent identity.
            // =====================================================

            try
            {
                if (GameManager.Instance != null &&
                    GameManager.Instance.World != null)
                {
                    EntityPlayer entityPlayer;

                    if (GameManager.Instance
                        .World
                        .Players
                        .dict
                        .TryGetValue(
                            entityId,
                            out entityPlayer))
                    {
                        if (entityPlayer != null &&
                            !string.IsNullOrWhiteSpace(
                                entityPlayer.EntityName))
                        {
                            return entityPlayer.EntityName;
                        }
                    }
                }
            }
            catch
            {
                // Player name is informational only.
            }

            // =====================================================
            // FINAL FALLBACK
            // =====================================================

            if (entityId > 0)
            {
                return $"Entity_{entityId}";
            }

            return "Unknown";
        }

        // =========================================================
        // NORMALIZE
        // =========================================================

        private static string NormalizePlayerId(
            string value)
        {
            if (string.IsNullOrWhiteSpace(
                value))
            {
                return null;
            }

            return value.Trim();
        }
    }
}
