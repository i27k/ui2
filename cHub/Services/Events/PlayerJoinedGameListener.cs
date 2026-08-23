using cHub.Core;
using cHub.Services.Players;
using cHub.Services.Roles;
using cHub.Shared.Utils;

namespace cHub.Services.Events
{
    public static class PlayerJoinedGameListener
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
                    "PlayerJoinedGameListener is already registered."
                );

                return;
            }

            ModEvents.PlayerJoinedGame.RegisterHandler(
                OnPlayerJoinedGame
            );

            ModEvents.PlayerSpawnedInWorld.RegisterHandler(
                OnPlayerSpawnedInWorld
            );

            _registered = true;

            Logger.Info(
                "PlayerJoinedGameListener registered for PlayerJoinedGame and PlayerSpawnedInWorld."
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

            ModEvents.PlayerJoinedGame.UnregisterHandler(
                OnPlayerJoinedGame
            );

            ModEvents.PlayerSpawnedInWorld.UnregisterHandler(
                OnPlayerSpawnedInWorld
            );

            _registered = false;

            Logger.Info(
                "PlayerJoinedGameListener unregistered."
            );
        }

        // =========================================================
        // PLAYER JOINED GAME
        // =========================================================

        private static void OnPlayerJoinedGame(
            ref ModEvents.SPlayerJoinedGameData data)
        {
            Logger.Info(
                "PlayerJoinedGame event fired."
            );

            HandlePlayer(
                data.ClientInfo,
                0,
                "PlayerJoinedGame"
            );
        }

        // =========================================================
        // PLAYER SPAWNED IN WORLD
        // =========================================================

        private static void OnPlayerSpawnedInWorld(
            ref ModEvents.SPlayerSpawnedInWorldData data)
        {
            Logger.Info(
                $"PlayerSpawnedInWorld event fired. EntityId={data.EntityId}, RespawnType={data.RespawnType}, IsLocalPlayer={data.IsLocalPlayer}."
            );

            HandlePlayer(
                data.ClientInfo,
                data.EntityId,
                "PlayerSpawnedInWorld"
            );
        }

        // =========================================================
        // COMMON PLAYER HANDLER
        // =========================================================

        private static void HandlePlayer(
            ClientInfo clientInfo,
            int entityId,
            string sourceEvent)
        {
            string playerName =
                PlayerIdentityResolver.GetPlayerName(
                    clientInfo,
                    entityId
                );

            Logger.Info(
                $"{sourceEvent}: Resolving identity for '{playerName}' (EntityId={entityId})."
            );

            // =====================================================
            // RESOLVE CANONICAL PLAYER ID
            // =====================================================

            if (!PlayerIdentityResolver.TryResolve(
                clientInfo,
                entityId,
                out string playerId))
            {
                Logger.Warning(
                    $"{sourceEvent}: Could not resolve persistent player identity for '{playerName}' (EntityId={entityId})."
                );

                return;
            }

            Logger.Info(
                $"{sourceEvent}: Resolved '{playerName}' -> '{playerId}'."
            );

            // =====================================================
            // PLAYER IDENTITY SERVICE
            //
            // Stores EOS <-> name so players can later be resolved
            // even when they are offline.
            // =====================================================

            PlayerIdentityService identityService =
                ServiceRegistry.Get<PlayerIdentityService>();

            if (identityService == null)
            {
                Logger.Warning(
                    $"{sourceEvent}: PlayerIdentityService is unavailable."
                );
            }
            else
            {
                bool identityUpdated =
                    identityService.UpsertPlayer(
                        playerId,
                        playerName
                    );

                if (!identityUpdated)
                {
                    Logger.Warning(
                        $"{sourceEvent}: Failed to update player identity for '{playerName}' ({playerId})."
                    );
                }
            }

            // =====================================================
            // ROLE SERVICE
            // =====================================================

            RoleService roleService =
                ServiceRegistry.Get<RoleService>();

            if (roleService == null)
            {
                Logger.Warning(
                    $"{sourceEvent}: RoleService is unavailable."
                );

                return;
            }

            // =====================================================
            // DEFAULT PLAYER ROLE
            // =====================================================

            bool assigned =
                roleService.EnsurePlayerRole(
                    playerId
                );

            if (assigned)
            {
                Logger.Info(
                    $"{sourceEvent}: Default Player role assigned to '{playerName}' ({playerId})."
                );
            }
            else
            {
                Logger.Info(
                    $"{sourceEvent}: No default Player role assignment required for '{playerName}' ({playerId})."
                );
            }
        }
    }
}