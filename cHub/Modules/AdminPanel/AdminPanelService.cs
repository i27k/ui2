using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using cHub.Networking;
using cHub.Core;
using cHub.Services.Config;
using cHub.Services.Players;
using cHub.Services.Roles;
using cHub.Shared.Base;
using cHub.Shared.Constants;
using cHub.Shared.Utils;
using Newtonsoft.Json;
using Vector3 = UnityEngine.Vector3;

namespace cHub.Modules.AdminPanel
{
    public class AdminPanelService : Service
    {
        public string CurrentPage { get; private set; } = "menu";
        private readonly object _homesSync = new object();
        private readonly Dictionary<string, List<HomeEntry>> _homes =
            new Dictionary<string, List<HomeEntry>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DateTime> _lastHomeTeleport =
            new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        private string _homesPath;
        public static string PendingMagicRequesterName { get; private set; }
        public static string PendingMagicRequestId { get; private set; }
        public static string RequestedPage { get; private set; }
        public static RotSnapshot ClientRotSnapshot { get; private set; } = new RotSnapshot();

        public override void Initialize()
        {
            try
            {
                _homesPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Mods",
                    "cHub",
                    "Data",
                    "homes.json");

                LoadHomes();

                ConfigService config =
                    ServiceRegistry.Get<ConfigService>();

                if (config?.Current?.UI?.Keybinds != null)
                {
                    bool missing =
                        !config.Current.UI.Keybinds.TryGetValue(
                            "OpenMenu",
                            out string openMenu) ||
                        string.IsNullOrWhiteSpace(openMenu);

                    bool oldDefault =
                        string.Equals(
                            openMenu,
                            "U",
                            StringComparison.OrdinalIgnoreCase);

                    if (missing || oldDefault)
                    {
                        config.Current.UI.Keybinds["OpenMenu"] = "RightBracket";
                        config.Save();

                        Logger.Info(
                            "[AdminPanel] OpenMenu keybind migrated from U to RightBracket (]).");
                    }
                }

                IsInitialized = true;

                Logger.Info(
                    "[AdminPanel] Initialized.");

                //====== Modificari i27k ========

                StartupTerminal.ReportFeature(
                    "admin.panel.service",
                    "Admin Panel Service",
                    true,
                    "Homes, keybinds and AdminPanel runtime service initialized");

                //===============================
            }
            catch (Exception ex)
            {
                IsInitialized = false;

                StartupTerminal.ReportFeature(
                    "admin.panel.service",
                    "Admin Panel Service",
                    false,
                    ex.Message);

                Logger.Error(
                    "[AdminPanel] Initialization failed: " + ex);

                throw;
            }
        }

        public void SetCurrentPage(string page)
        {
            CurrentPage = string.IsNullOrWhiteSpace(page) ? "menu" : page.Trim();
            Logger.Info($"[AdminPanel] Opening {CurrentPage}.");
        }

        public void ResetNavigation()
        {
            CurrentPage = "menu";
        }

        public static string ConsumeRequestedPage()
        {
            string page = RequestedPage;
            RequestedPage = null;
            return page;
        }

        public static bool HasConfiguredKeybind(string action)
        {
            Dictionary<string, string> bindings =
                ServiceRegistry.Get<ConfigService>()?.Current?.UI?.Keybinds;
            bool setupCompleted =
                ServiceRegistry.Get<ConfigService>()?.Current?.UI?.KeybindSetupCompleted ?? false;
            return setupCompleted && bindings != null &&
                   bindings.TryGetValue(action, out string binding) &&
                   !string.IsNullOrWhiteSpace(binding);
        }

        public static bool OpenKeybindSetup(out string error)
        {
            RequestedPage = "options";
            return TryOpenLocal(out error);
        }

        public static void RequestRotSnapshot(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId)) return;
            if (ConnectionManager.Instance != null && ConnectionManager.Instance.IsServer)
            {
                ClientRotSnapshot = RotService.GetSnapshot(playerId);
                return;
            }
            ConnectionManager.Instance?.SendToServer(NetPackageManager.GetPackage<NetPackageRotData>()
                .SetupRequest(playerId));
        }

        public static void ReceiveRotSnapshot(string json)
        {
            try { ClientRotSnapshot = JsonConvert.DeserializeObject<RotSnapshot>(json) ?? new RotSnapshot(); }
            catch (Exception ex) { Logger.Error($"[Rot] Invalid server snapshot: {ex}"); }
        }

        public static void ProcessGlobalKeybinds()
        {
            if (GameManager.Instance?.World == null) return;
            if (!GameManager.Instance.World.GetLocalPlayers().Any()) return;
            ConfigService config = ServiceRegistry.Get<ConfigService>();
            Dictionary<string, string> bindings = config?.Current?.UI?.Keybinds;
            if (bindings == null) return;
            ProcessBinding(bindings, "OpenMenu", "menu", false, true);
            ProcessBinding(bindings, "MyHomes", "homes", false);
            ProcessBinding(bindings, "PlayerTeleport", "playerlist", false);
            ProcessBinding(bindings, "Options", "options", false);
            ProcessBinding(bindings, "ZombieDirector", "zombies", true);
        }

        private static void ProcessBinding(Dictionary<string, string> bindings,
            string action, string page, bool administratorOnly, bool toggle = false)
        {
            if (!bindings.TryGetValue(action, out string binding) ||
                string.IsNullOrWhiteSpace(binding) || !IsBindingPressed(binding)) return;
            if (administratorOnly)
            {
                try
                {
                    string id = GameManager.Instance.getPersistentPlayerID(null)?.CombinedString;
                    RoleService roles = ServiceRegistry.Get<RoleService>();
                    if (roles == null || !roles.HasPermission(id, cHub.Shared.Constants.Permissions.AdminPanelManage)) return;
                }
                catch { return; }
            }
            if (toggle && TryToggleLocal()) return;
            RequestedPage = page;
            TryOpenLocal(out string error);
        }

        private static bool TryToggleLocal()
        {
            try
            {
                EntityPlayerLocal player = GameManager.Instance?.World?
                    .GetLocalPlayers()?.FirstOrDefault();
                LocalPlayerUI playerUi = player == null ? null : LocalPlayerUI.GetUIForPlayer(player);
                if (playerUi?.windowManager == null) return false;
                if (playerUi.windowManager.IsWindowOpen(WindowIds.AdminPanel))
                {
                    playerUi.windowManager.Close(WindowIds.AdminPanel);
                    RequestedPage = null;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Warning("[AdminPanel] Menu toggle failed: " + ex.Message);
            }
            return false;
        }

        private static bool IsBindingPressed(string binding)
        {
            string value = binding.Trim();
            if (value.StartsWith("Mouse", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(value.Substring(5), out int mouseButton))
                return UnityEngine.Input.GetMouseButtonDown(mouseButton);
            return Enum.TryParse(value, true, out UnityEngine.KeyCode key) &&
                   UnityEngine.Input.GetKeyDown(key);
        }

        public bool SetKeybind(string action, string binding)
        {
            ConfigService config = ServiceRegistry.Get<ConfigService>();
            if (config?.Current?.UI?.Keybinds == null || string.IsNullOrWhiteSpace(action)) return false;
            config.Current.UI.Keybinds[action] = binding ?? string.Empty;
            config.Current.UI.KeybindSetupCompleted = true;
            return config.Save();
        }

        public bool SetTheme(string theme)
        {
            ConfigService config = ServiceRegistry.Get<ConfigService>();
            if (config?.Current?.UI == null || !cHub.Config.UIConfig.Themes.Contains(theme)) return false;
            config.Current.UI.Theme = theme;
            int index = Array.IndexOf(cHub.Config.UIConfig.Themes, theme);
            if (index >= 0 && index < cHub.Config.UIConfig.ThemeAccentColors.Length)
                config.Current.UI.AccentColor = cHub.Config.UIConfig.ThemeAccentColors[index];
            return config.Save();
        }

        public bool SaveUiPreferences(float opacity, float density, bool glow, bool scanlines, bool animations)
        {
            ConfigService config = ServiceRegistry.Get<ConfigService>();
            if (config?.Current?.UI == null) return false;
            config.Current.UI.BackgroundOpacity = UnityEngine.Mathf.Clamp(opacity, 0.08f, 1f);
            config.Current.UI.BackgroundDensity = UnityEngine.Mathf.Clamp(density, 1.1f, 6f);
            config.Current.UI.GlowEnabled = glow;
            config.Current.UI.ScanlineEffect = scanlines;
            config.Current.UI.WaterdropAnimations = animations;
            return config.Save();
        }

        public bool ApplyCustomTheme(string css, out string status)
        {
            ConfigService config = ServiceRegistry.Get<ConfigService>();
            if (config?.Current?.UI == null) { status = "Configuration service unavailable."; return false; }
            string source = css ?? string.Empty;
            try
            {
                string accent = ReadCssValue(source, "accent");
                string opacity = ReadCssValue(source, "background-opacity");
                string glow = ReadCssValue(source, "glow");
                string scanlines = ReadCssValue(source, "scanlines");
                string animations = ReadCssValue(source, "animations");
                string name = ReadCssValue(source, "name");
                if (!string.IsNullOrWhiteSpace(accent))
                {
                    string normalized = accent.Trim();
                    if (!System.Text.RegularExpressions.Regex.IsMatch(normalized, "^#[0-9a-fA-F]{6}$"))
                        throw new FormatException("--accent must use #RRGGBB.");
                    config.Current.UI.AccentColor = normalized.ToUpperInvariant();
                }
                if (!string.IsNullOrWhiteSpace(opacity) && float.TryParse(opacity,
                    System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture,
                    out float parsedOpacity))
                {
                    config.Current.UI.BackgroundOpacity = UnityEngine.Mathf.Clamp(parsedOpacity, 0.08f, 1f);
                    config.Current.UI.BackgroundDensity = UnityEngine.Mathf.Clamp(1.1f +
                        ((config.Current.UI.BackgroundOpacity - 0.12f) / 0.88f) * 4.9f, 1.1f, 6f);
                }
                if (bool.TryParse(glow, out bool parsedGlow)) config.Current.UI.GlowEnabled = parsedGlow;
                if (bool.TryParse(scanlines, out bool parsedScanlines)) config.Current.UI.ScanlineEffect = parsedScanlines;
                if (bool.TryParse(animations, out bool parsedAnimations)) config.Current.UI.WaterdropAnimations = parsedAnimations;
                config.Current.UI.CustomThemeCss = source;
                config.Current.UI.Theme = string.IsNullOrWhiteSpace(name) ? "Custom CSS" : name.Trim();
                bool saved = config.Save();
                status = saved ? "Theme applied live and saved." : "Theme applied but could not be saved.";
                return saved;
            }
            catch (Exception ex) { status = "Theme error: " + ex.Message; return false; }
        }

        public bool DeleteCustomTheme(out string status)
        {
            ConfigService config = ServiceRegistry.Get<ConfigService>();
            if (config?.Current?.UI == null) { status = "Configuration service unavailable."; return false; }
            config.Current.UI.CustomThemeCss = string.Empty;
            config.Current.UI.Theme = "Blood Moon";
            config.Current.UI.AccentColor = "#EF3943";
            config.Current.UI.BackgroundOpacity = 0.12f;
            config.Current.UI.BackgroundDensity = 1.1f;
            config.Current.UI.GlowEnabled = true;
            config.Current.UI.ScanlineEffect = false;
            config.Current.UI.WaterdropAnimations = true;
            bool saved = config.Save(); status = saved ? "Custom theme deleted. Blood Moon restored." : "Could not save defaults."; return saved;
        }

        private static string ReadCssValue(string css, string key)
        {
            System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(css ?? string.Empty,
                "--" + System.Text.RegularExpressions.Regex.Escape(key) + @"\s*:\s*([^;\r\n}]+)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
        }

        public IReadOnlyList<HomeEntry> GetHomes(string playerId)
        {
            lock (_homesSync)
            {
                if (string.IsNullOrWhiteSpace(playerId) ||
                    !_homes.TryGetValue(playerId.Trim(), out List<HomeEntry> homes))
                {
                    return Array.Empty<HomeEntry>();
                }

                return homes.Select(home => home.Clone()).ToList();
            }
        }

        public bool SaveHome(string playerId, string name, Vector3 position, out string error)
        {
            error = string.Empty;
            name = name?.Trim();

            if (string.IsNullOrWhiteSpace(playerId) || string.IsNullOrWhiteSpace(name))
            {
                error = "Enter a home name first.";
                return false;
            }

            if (name.Length > 24)
            {
                error = "Home names may contain at most 24 characters.";
                return false;
            }

            lock (_homesSync)
            {
                if (!_homes.TryGetValue(playerId.Trim(), out List<HomeEntry> homes))
                {
                    homes = new List<HomeEntry>();
                    _homes[playerId.Trim()] = homes;
                }

                HomeEntry existing = homes.FirstOrDefault(home =>
                    string.Equals(home.Name, name, StringComparison.OrdinalIgnoreCase));

                if (existing == null && homes.Count >= 8)
                {
                    error = "You can save at most 8 homes.";
                    return false;
                }

                if (existing == null)
                {
                    homes.Add(new HomeEntry { Name = name, X = position.x, Y = position.y, Z = position.z });
                }
                else
                {
                    existing.X = position.x;
                    existing.Y = position.y;
                    existing.Z = position.z;
                }

                SaveHomes();
                return true;
            }
        }

        public bool TryTeleportHome(string playerId, int index, EntityPlayerLocal player, out string error)
        {
            error = string.Empty;

            if (player == null)
            {
                error = "Local player is unavailable.";
                return false;
            }

            HomeEntry home;
            lock (_homesSync)
            {
                if (!_homes.TryGetValue(playerId ?? string.Empty, out List<HomeEntry> homes) ||
                    index < 0 || index >= homes.Count)
                {
                    error = "That home no longer exists.";
                    return false;
                }

                int cooldown = GetHomeCooldownSeconds();
                if (_lastHomeTeleport.TryGetValue(playerId, out DateTime last))
                {
                    int remaining = cooldown - (int)(DateTime.UtcNow - last).TotalSeconds;
                    if (remaining > 0)
                    {
                        error = $"Home teleport is available in {remaining}s.";
                        return false;
                    }
                }

                home = homes[index].Clone();
                _lastHomeTeleport[playerId] = DateTime.UtcNow;
            }

            player.SetPosition(new Vector3(home.X, home.Y, home.Z));
            return true;
        }

        public int GetHomeCooldownSeconds()
        {
            return Math.Max(0, ServiceRegistry.Get<ConfigService>()?.Current?.UI?
                .HomeTeleportCooldownSeconds ?? 60);
        }

        public bool SetHomeCooldownSeconds(int seconds)
        {
            ConfigService config = ServiceRegistry.Get<ConfigService>();
            if (config?.Current?.UI == null || seconds < 0 || seconds > 86400)
            {
                return false;
            }

            config.Current.UI.HomeTeleportCooldownSeconds = seconds;
            return config.Save();
        }

        private void LoadHomes()
        {
            try
            {
                if (!File.Exists(_homesPath)) return;
                Dictionary<string, List<HomeEntry>> loaded = JsonConvert.DeserializeObject<Dictionary<string, List<HomeEntry>>>(File.ReadAllText(_homesPath));
                if (loaded == null) return;
                foreach (KeyValuePair<string, List<HomeEntry>> entry in loaded)
                {
                    if (!string.IsNullOrWhiteSpace(entry.Key) && entry.Value != null)
                        _homes[entry.Key] = entry.Value.Take(8).ToList();
                }
            }
            catch (Exception ex) { Logger.Error($"Failed to load homes: {ex}"); }
        }

        private void SaveHomes()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_homesPath));
                File.WriteAllText(_homesPath, JsonConvert.SerializeObject(_homes, Formatting.Indented));
            }
            catch (Exception ex) { Logger.Error($"Failed to save homes: {ex}"); }
        }

        public bool TryOpen(string actorId, out string error)
        {
            error = string.Empty;

            if (!IsInitialized)
            {
                error = "Admin Panel service is unavailable.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(actorId))
            {
                error = "The player identity could not be resolved.";
                return false;
            }

            LocalPlayerUI playerUi = FindLocalPlayerUi(actorId);

            if (playerUi != null)
            {
                return TryOpen(playerUi, out error);
            }

            ClientInfo remoteClient = FindRemoteClient(actorId);

            if (remoteClient == null)
            {
                error = "The command actor is not connected to this server.";
                return false;
            }

            remoteClient.SendPackage(
                NetPackageManager.GetPackage<NetPackageOpenAdminPanel>());

            Logger.Info(
                $"Sent Admin Panel open request to '{actorId.Trim()}'.");

            return true;
        }

        public static bool TryOpenLocal(out string error)
        {
            error = string.Empty;

            if (GameManager.Instance == null ||
                GameManager.Instance.World == null)
            {
                error = "The local game world is unavailable.";
                return false;
            }

            foreach (EntityPlayerLocal player in
                GameManager.Instance.World.GetLocalPlayers())
            {
                if (player != null)
                {
                    return TryOpen(
                        LocalPlayerUI.GetUIForPlayer(player),
                        out error);
                }
            }

            error = "No local player UI is available.";
            return false;
        }

        public bool RequestMagicTeleport(string targetPlayerId, out string error)
        {
            error = string.Empty;
            string requesterId = GetLocalPlayerId();
            EntityPlayerLocal player = GameManager.Instance?.World?.GetLocalPlayers()?.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(requesterId) || string.IsNullOrWhiteSpace(targetPlayerId) || player == null)
            {
                error = "Player identity is unavailable.";
                return false;
            }
            ConnectionManager.Instance.SendToServer(NetPackageManager
                .GetPackage<cHub.Networking.NetPackageMagicTeleport>()
                .SetupRequest(requesterId, player.EntityName, targetPlayerId), false);
            return true;
        }

        public bool RequestAdminTeleport(string sourcePlayerId, string targetPlayerId, out string error)
        {
            error = string.Empty;
            string administratorId = GetLocalPlayerId();
            if (string.IsNullOrWhiteSpace(administratorId) ||
                string.IsNullOrWhiteSpace(sourcePlayerId) ||
                string.IsNullOrWhiteSpace(targetPlayerId) ||
                string.Equals(sourcePlayerId, targetPlayerId, StringComparison.OrdinalIgnoreCase))
            {
                error = "Select two different online players.";
                return false;
            }
            ConnectionManager.Instance.SendToServer(NetPackageManager
                .GetPackage<cHub.Networking.NetPackageMagicTeleport>()
                .SetupAdminTeleport(administratorId, sourcePlayerId, targetPlayerId), false);
            return true;
        }

        public bool RequestZombieSpawn(string entityClassName, int count, out string error)
        {
            error = string.Empty;
            string administratorId = GetLocalPlayerId();
            if (string.IsNullOrWhiteSpace(administratorId) ||
                string.IsNullOrWhiteSpace(entityClassName) || count < 1 || count > 25)
            {
                error = "Invalid zombie spawn request.";
                return false;
            }
            ConnectionManager.Instance.SendToServer(NetPackageManager
                .GetPackage<cHub.Networking.NetPackageMagicTeleport>()
                .SetupZombieSpawn(administratorId, entityClassName, count), false);
            return true;
        }

        public static void ShowMagicTeleportRequest(string requestId, string requesterName)
        {
            PendingMagicRequestId = requestId;
            PendingMagicRequesterName = requesterName;
            if (GameManager.Instance?.World == null) return;
            EntityPlayerLocal player = GameManager.Instance.World.GetLocalPlayers().FirstOrDefault();
            LocalPlayerUI ui = player == null ? null : LocalPlayerUI.GetUIForPlayer(player);
            ui?.windowManager?.Open(WindowIds.MagicTeleportRequest, true);
        }

        public static void AnswerMagicTeleportRequest(bool accepted)
        {
            if (string.IsNullOrWhiteSpace(PendingMagicRequestId)) return;
            ConnectionManager.Instance.SendToServer(NetPackageManager
                .GetPackage<cHub.Networking.NetPackageMagicTeleport>()
                .SetupResponse(PendingMagicRequestId, accepted), false);
            PendingMagicRequestId = null;
            PendingMagicRequesterName = null;
        }

        private static bool TryOpen(
            LocalPlayerUI playerUi,
            out string error)
        {
            error = string.Empty;

            if (playerUi == null || playerUi.windowManager == null)
            {
                error = "The local player UI is unavailable.";
                return false;
            }

            if (playerUi.windowManager.GetWindow(WindowIds.AdminPanel) == null)
            {
                error = "Admin Panel XUi is not loaded.";
                return false;
            }

            // The launcher lives in the crafting/inventory selector. Close the
            // currently open modal group first, so cHub is displayed alone.
            playerUi.windowManager.CloseAllOpenModalWindows(
                WindowIds.AdminPanel);
            // c/Hub manages its own CapsLock interaction/cursor mode while the
            // panel stays visible, so it must not permanently monopolize input.
            playerUi.windowManager.Open(WindowIds.AdminPanel, false);
            return true;
        }

        private static ClientInfo FindRemoteClient(string actorId)
        {
            if (ConnectionManager.Instance == null ||
                ConnectionManager.Instance.Clients == null)
            {
                return null;
            }

            string normalizedActorId = actorId.Trim();

            foreach (ClientInfo clientInfo in
                ConnectionManager.Instance.Clients.List)
            {
                if (clientInfo == null ||
                    !PlayerIdentityResolver.TryResolve(
                        clientInfo,
                        out string playerId))
                {
                    continue;
                }

                if (string.Equals(
                    normalizedActorId,
                    playerId,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return clientInfo;
                }
            }

            return null;
        }

        private static LocalPlayerUI FindLocalPlayerUi(string actorId)
        {
            if (GameManager.Instance == null || GameManager.Instance.World == null)
            {
                return null;
            }

            string normalizedActorId = actorId.Trim();
            string localPlayerId = GetLocalPlayerId();

            foreach (EntityPlayerLocal player in GameManager.Instance.World.GetLocalPlayers())
            {
                if (player == null)
                {
                    continue;
                }

                bool isActor =
                    !string.IsNullOrWhiteSpace(localPlayerId) &&
                    string.Equals(
                        normalizedActorId,
                        localPlayerId,
                        StringComparison.OrdinalIgnoreCase);

                if (!isActor &&
                    PlayerIdentityResolver.TryResolve(player.entityId, out string playerId))
                {
                    isActor = string.Equals(
                        normalizedActorId,
                        playerId,
                        StringComparison.OrdinalIgnoreCase);
                }

                if (!isActor)
                {
                    continue;
                }

                return LocalPlayerUI.GetUIForPlayer(player);
            }

            return null;
        }

        private static string GetLocalPlayerId()
        {
            try
            {
                PlatformUserIdentifierAbs persistentId =
                    GameManager.Instance.getPersistentPlayerID(null);

                return persistentId?.CombinedString;
            }
            catch (Exception ex)
            {
                Logger.Warning(
                    $"Failed to resolve the local player identity: {ex.Message}");

                return null;
            }
        }

        public override void Shutdown()
        {
            ResetNavigation();
            IsInitialized = false;
            Logger.Info("[AdminPanel] Shutdown.");
        }
    }

    public class HomeEntry
    {
        public string Name { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public HomeEntry Clone() => new HomeEntry { Name = Name, X = X, Y = Y, Z = Z };
    }
}
