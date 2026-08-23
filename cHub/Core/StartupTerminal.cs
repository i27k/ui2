using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using cHub.Services.Permissions;
using cHub.Services.Roles;
using UnityEngine;
using HarmonyLib;
using PermissionIds = cHub.Shared.Constants.Permissions;

namespace cHub.Core
{
    internal sealed class StartupTerminal : MonoBehaviour
    {
        private const int MaxLines = 5000;
        private const float LogLineHeight = 21f;
        private const string DiscordUrl = "https://discord.gg/s9DAmRgsAg";
        private static StartupTerminal _instance;
        private readonly List<LogLine> _lines = new List<LogLine>();
        private readonly Dictionary<string, string> _featureStates =
    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static int _auditOk, _auditMismatch, _auditFailed, _auditSkipped;
        private static string _lastAuditProblem = "none";
        private static float _fpsMin = float.MaxValue, _fpsMax, _fpsTotal;
        private static int _fpsSamples;
        private static float _lastFpsSampleTime;
        private Vector2 _scroll;
        private Texture2D _wallpaper;
        private GUIStyle _terminalStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _logStyle;
        private GUIStyle _statusStyle;
        private GUIStyle _brandStyle;
        private GUIStyle _brandButtonStyle;
        private GUIStyle _filterButtonStyle;
        private GUIStyle _filterActiveStyle;
        private GUIStyle _panelHeaderStyle;
        private bool _visible = true;
        private bool _stylesReady;
        private string _eacStatus;
        private string _toastText;
        private float _toastUntil;
        private Vector2 _toastPosition;
        private string _gameLogPath;
        private long _gameLogPosition;
        private float _gameLogPollTimer;
        private const float GameLogPollInterval = 0.25f;
        private static bool _consumeClosePointer;
        private static int _consumeCloseUntilFrame;

        internal static bool IsEacActive => DetectEac();

        internal static void OpenDiscord()
        {
            if (_instance != null)
            {
                _instance.OpenExternalUrl(DiscordUrl);
                return;
            }

            // The main-menu XUi may become interactive before the persistent
            // terminal component has completed Awake. Keep the public action
            // functional in that short window as well.
            try { Application.OpenURL(DiscordUrl); }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[cHub] Discord open failed: " + ex.Message);
            }
        }

        internal static bool IsVisible
        {
            get { return _instance != null && _instance._visible; }
        }

        internal static void ToggleConsole()
        {
            if (_instance == null)
            {
                Create();
                return;
            }

            _instance._visible = !_instance._visible;

            if (_instance._visible)
                ReportRuntimeSnapshot();

            _instance.Add(
                _instance._visible
                    ? "[CONSOLE] Runtime console opened."
                    : "[CONSOLE] Runtime console hidden.",
                LogType.Log);
        }

        internal static void ShowConsole()
        {
            if (_instance == null)
                Create();

            if (_instance != null)
            {
                _instance._visible = true;
                ReportRuntimeSnapshot();
            }
        }

        internal static void RecordFps(float fps)
        {
            if (fps < 1f || fps > 2000f || Time.unscaledTime - _lastFpsSampleTime < 0.25f) return;
            _lastFpsSampleTime = Time.unscaledTime;
            _fpsMin = Mathf.Min(_fpsMin, fps);
            _fpsMax = Mathf.Max(_fpsMax, fps);
            _fpsTotal += fps;
            _fpsSamples++;
        }

        private static string FpsSnapshotText()
        {
            if (_fpsSamples == 0) return "MIN -- | AVG -- | MAX --";
            return "MIN " + Mathf.RoundToInt(_fpsMin) + " | AVG " +
                   Mathf.RoundToInt(_fpsTotal / _fpsSamples) + " | MAX " + Mathf.RoundToInt(_fpsMax);
        }

        private static void ReportRuntimeSnapshot()
        {
            if (_instance == null) return;
            _instance.Add("[cHub] [FPS] " + FpsSnapshotText(), LogType.Warning);
            _instance.Add("[cHub] [ACTIVE_STATE] " + XUiC_cHubPauseTools.GetPerformanceStateSummary(), LogType.Warning);
        }
        internal static void ShowFloatingConsole()
        {
            if (_instance == null)
                Create();

            if (_instance == null)
                return;

            // Use the exact same terminal layout as the startup/menu terminal.
            _instance._floatingMode = false;
            _instance._visible = true;

            _instance.Add(
                "[CONSOLE] Runtime terminal opened.",
                LogType.Log);
        }

        internal static void ShowStartupConsole()
        {
            if (_instance == null)
                Create();

            if (_instance == null)
                return;

            _instance._floatingMode = false;
            _instance._visible = true;
        }

        internal static void HideConsole()
        {
            if (_instance != null)
                _instance._visible = false;
        }

        internal static void CopyDiscord()
        {
            GUIUtility.systemCopyBuffer = DiscordUrl;
            bool copied = string.Equals(GUIUtility.systemCopyBuffer, DiscordUrl,
                StringComparison.Ordinal);
            if (_instance != null)
            {
                _instance.ShowCursorToast(copied ? "COPY SUCCESSFUL" : "COPY FAILED");
                _instance.Add(copied
                    ? "[CLIPBOARD] Discord invite copied."
                    : "[CLIPBOARD] Discord invite copy failed.",
                    copied ? LogType.Log : LogType.Error);
            }
            else
            {
                UnityEngine.Debug.Log(copied
                    ? "[cHub] Discord invite copied."
                    : "[cHub] Discord invite copy failed.");
            }
        }

        private void ClampFloatingRect()
        {
            float maxWidth = Mathf.Max(
                FloatingMinWidth,
                Screen.width - 20f);

            float maxHeight = Mathf.Max(
                FloatingMinHeight,
                Screen.height - 20f);

            _floatingRect.width = Mathf.Clamp(
                _floatingRect.width,
                FloatingMinWidth,
                maxWidth);

            _floatingRect.height = Mathf.Clamp(
                _floatingRect.height,
                FloatingMinHeight,
                maxHeight);

            _floatingRect.x = Mathf.Clamp(
                _floatingRect.x,
                0f,
                Mathf.Max(0f, Screen.width - _floatingRect.width));

            _floatingRect.y = Mathf.Clamp(
                _floatingRect.y,
                0f,
                Mathf.Max(0f, Screen.height - _floatingRect.height));
        }

        public static void Create()
        {
            if (_instance != null) return;
            GameObject host = new GameObject("cHub.StartupTerminal");
            DontDestroyOnLoad(host);
            _instance = host.AddComponent<StartupTerminal>();
        }

        private void Awake()
        {
            _eacStatus = DetectEac() ? "EAC ON" : "EAC OFF";
            LoadWallpaper();
            Application.logMessageReceived += OnLog;

            AttachToGameLog();

            Add("[BOOT] c/Hub terminal attached", LogType.Log);
            Add("[SECURITY] " + _eacStatus, LogType.Log);
            Add("[DISCORD] " + DiscordUrl, LogType.Log);
        }
        private void Update()
        {
            if (_consumeClosePointer && !Input.GetMouseButton(0) &&
                Time.frameCount > _consumeCloseUntilFrame)
                _consumeClosePointer = false;
            // Any visible c/Hub console follows the vanilla ESC lifecycle.
            // This prevents the overlay from remaining over gameplay after
            // the player closes the pause menu with Escape.
            if (_visible && Input.GetKeyDown(KeyCode.Escape))
            {
                HideConsole();
                return;
            }

            _gameLogPollTimer += Time.unscaledDeltaTime;

            if (_gameLogPollTimer >= GameLogPollInterval)
            {
                _gameLogPollTimer = 0f;
                PollGameLog();
            }
        }


        private void OnDestroy()
        {
            Application.logMessageReceived -= OnLog;
            if (_wallpaper != null) Destroy(_wallpaper);
            if (_instance == this) _instance = null;
        }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            Add(condition, type);
            if ((type == LogType.Error || type == LogType.Exception) &&
                !string.IsNullOrWhiteSpace(stackTrace)) Add(stackTrace, type);
        }

        private void Add(string message, LogType type)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            string[] split = message.Replace("\r", string.Empty).Split('\n');

            foreach (string line in split)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                _lines.Add(new LogLine(
                    DateTime.Now.ToString("HH:mm:ss.fff"),
                    line,
                    type));
            }

            if (_lines.Count > MaxLines)
                _lines.RemoveRange(0, _lines.Count - MaxLines);

            _scroll.y = float.MaxValue;
        }

        //====== Modificari i27k ========


        private bool _showBoot = true;
        private bool _showCHub = true;
        private bool _showCommands = true;
        private bool _showPermissions = true;
        private bool _showNetwork = true;
        private bool _showHarmony = true;
        private bool _showXui = true;
        private bool _showUi = true;
        private bool _showWarnings = true;
        private bool _showErrors = true;
        private bool _showGeneral = true;

        private enum LogCategory
        {
            General,
            cHub,
            Boot,
            Commands,
            Permissions,
            Network,
            Harmony,
            Xui,
            Ui,
            Warning,
            Error
        }

        private bool ShouldDisplay(string message, LogType type)
        {
            // Consola c/Hub nu afișează zgomotul general al jocului.
            if (!IsCHubRelevant(message))
                return false;

            LogCategory category = DetectCategory(message, type);

            switch (category)
            {
                case LogCategory.cHub:
                    return _showCHub;

                case LogCategory.Boot:
                    return _showBoot;

                case LogCategory.Commands:
                    return _showCommands;

                case LogCategory.Permissions:
                    return _showPermissions;

                case LogCategory.Network:
                    return _showNetwork;

                case LogCategory.Harmony:
                    return _showHarmony;

                case LogCategory.Xui:
                    return _showXui;

                case LogCategory.Ui:
                    return _showUi;

                case LogCategory.Warning:
                    return _showWarnings;

                case LogCategory.Error:
                    return _showErrors;

                default:
                    return _showGeneral;
            }
        }

        private LogCategory DetectCategory(string message, LogType type)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                if (type == LogType.Error || type == LogType.Exception)
                    return LogCategory.Error;

                if (type == LogType.Warning)
                    return LogCategory.Warning;

                return LogCategory.General;
            }

            string text = message.ToLowerInvariant();

            // ============================================================
            // ERRORS / EXCEPTIONS
            // Highest priority
            // ============================================================

            if (type == LogType.Error ||
                type == LogType.Exception)
            {
                return LogCategory.Error;
            }

            // ============================================================
            // XUI / XML
            // ============================================================

            if (text.Contains("xml patch") ||
                text.Contains("[xui]") ||
                text.Contains("xui_menu") ||
                text.Contains("xui_ingame") ||
                text.Contains("windows.xml") ||
                text.Contains("xui.xml") ||
                text.Contains("failed initializing window") ||
                text.Contains("window group") ||
                text.Contains("windowgroup") ||
                text.Contains("specified window anchor") ||
                text.Contains("could not find window"))
            {
                return LogCategory.Xui;
            }

            // ============================================================
            // HARMONY
            // ============================================================

            if (text.Contains("harmony") ||
                text.Contains("harmony patch") ||
                text.Contains("prefix patch") ||
                text.Contains("postfix patch") ||
                text.Contains("transpiler"))
            {
                return LogCategory.Harmony;
            }

            // ============================================================
            // COMMANDS
            // ============================================================

            if (text.Contains("registered command") ||
                text.Contains("commandservice") ||
                text.Contains("chatcommandlistener") ||
                text.Contains("/perms") ||
                text.Contains("/perm") ||
                text.Contains("/role") ||
                text.Contains("/rot") ||
                text.Contains("/toprot"))
            {
                return LogCategory.Commands;
            }

            // ============================================================
            // PERMISSIONS / ROLES
            // ============================================================

            if (text.Contains("permission") ||
                text.Contains("roleservice") ||
                text.Contains("rolemanagement") ||
                text.Contains("registered role") ||
                text.Contains("role data"))
            {
                return LogCategory.Permissions;
            }

            // ============================================================
            // NETWORK
            // ============================================================

            if (text.Contains("net:") ||
                text.Contains("netpackage") ||
                text.Contains("netpackagemanager") ||
                text.Contains("network") ||
                text.Contains("steamnetworking") ||
                text.Contains("litenetlib") ||
                text.Contains("[eos-p2ps]") ||
                text.Contains("socket") ||
                text.Contains("connectionmanager") ||
                text.Contains("sendtoserver") ||
                text.Contains("sendpackage"))
            {
                return LogCategory.Network;
            }

            // ============================================================
            // UI / ADMIN PANEL
            // ============================================================

            if (text.Contains("[adminpanel") ||
                text.Contains("adminpanel.ui") ||
                text.Contains("button") ||
                text.Contains("panel") ||
                text.Contains("startupterminal"))
            {
                return LogCategory.Ui;
            }

            // ============================================================
            // BOOT / STARTUP
            // ============================================================

            if (text.Contains("[boot]") ||
                text.Contains("bootstrap") ||
                text.Contains("startup") ||
                text.Contains("initialized") ||
                text.Contains("initializing") ||
                text.Contains("feature manifest") ||
                text.Contains("loading configuration") ||
                text.Contains("service initialization"))
            {
                return LogCategory.Boot;
            }

            // ============================================================
            // WARNINGS
            // ============================================================

            if (type == LogType.Warning)
            {
                return LogCategory.Warning;
            }

            // ============================================================
            // c/HUB GENERAL
            // Important: this stays AFTER the specialized categories.
            // ============================================================

            if (IsCHubRelevant(message))
            {
                return LogCategory.cHub;
            }

            // ============================================================
            // EVERYTHING ELSE
            // ============================================================

            return LogCategory.General;
        }

        private void ShowAllLogCategories()
        {
            _showBoot = true;
            _showCHub = true;
            _showCommands = true;
            _showPermissions = true;
            _showNetwork = true;
            _showHarmony = true;
            _showXui = true;
            _showUi = true;
            _showWarnings = true;
            _showErrors = true;
            _showGeneral = true;

            _scroll.y = float.MaxValue;
        }

        private void ShowOnlyLogCategory(LogCategory category)
        {
            _showBoot = category == LogCategory.Boot;
            _showCHub = category == LogCategory.cHub;
            _showCommands = category == LogCategory.Commands;
            _showPermissions = category == LogCategory.Permissions;
            _showNetwork = category == LogCategory.Network;
            _showHarmony = category == LogCategory.Harmony;
            _showXui = category == LogCategory.Xui;
            _showUi = category == LogCategory.Ui;
            _showWarnings = category == LogCategory.Warning;
            _showErrors = category == LogCategory.Error;
            _showGeneral = category == LogCategory.General;

            _scroll.y = float.MaxValue;
        }

        //===============================
        private bool IsCHubRelevant(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return false;

            string text = message.ToLowerInvariant();

            // ============================================================
            // DIRECT c/HUB OUTPUT
            // ============================================================

            if (text.Contains("[chub]") ||
                text.Contains("c/hub") ||
                text.Contains("chub.dll") ||
                text.Contains("assembly chub") ||
                text.Contains("mod 'chub'") ||
                text.Contains("mod \"chub\"") ||
                text.Contains("from mod \"chub\""))
            {
                return true;
            }

            // ============================================================
            // c/HUB FILES / PATHS
            // ============================================================

            if (text.Contains("\\mods\\chub\\") ||
                text.Contains("/mods/chub/") ||
                text.Contains("mods\\chub") ||
                text.Contains("mods/chub"))
            {
                return true;
            }

            // ============================================================
            // c/HUB C# TYPES / CONTROLLERS
            // ============================================================

            if (text.Contains("xuic_chub") ||
                text.Contains("chubpausetools") ||
                text.Contains("chub.core") ||
                text.Contains("chub.modules") ||
                text.Contains("chub.services") ||
                text.Contains("chub.networking"))
            {
                return true;
            }

            // ============================================================
            // WINDOWS / UI CREATED BY c/HUB
            // ============================================================

            if (text.Contains("serverinfowindow") ||
                text.Contains("adminpanel") ||
                text.Contains("chubmenu") ||
                text.Contains("chubwindow"))
            {
                return true;
            }

            // ============================================================
            // STARTUP TERMINAL INTERNAL EVENTS
            // ============================================================

            if (text.Contains("[boot] c/hub") ||
                text.Contains("[gamelog]") ||
                text.Contains("[export]") ||
                text.Contains("[security]") ||
                text.Contains("[discord] https://discord.gg/s9damrgsag"))
            {
                return true;
            }

            return false;
        }

        //====== Modificari i27k ========

        internal static void ReportFeature(
    string id,
    string displayName,
    bool working,
    string details = null)
        {
            if (_instance == null)
                return;

            _instance.ReportFeatureInternal(
                id,
                displayName,
                working,
                details);
        }

        internal static void Trace(string area, string action, string details)
        {
            string message = "[cHub] [TRACE] [" + (area ?? "GENERAL") + "] [" +
                (action ?? "EVENT") + "] " + (details ?? string.Empty);
            if (_instance != null) _instance.Add(message, LogType.Log);
        }

        internal static void Audit(string component, string operation, string status, string details)
        {
            string normalized = string.IsNullOrWhiteSpace(status) ? "FAILED" : status.ToUpperInvariant();
            if (normalized == "OK") _auditOk++;
            else if (normalized == "MISMATCH") { _auditMismatch++; _lastAuditProblem = component + "/" + operation + ": " + details; }
            else if (normalized == "SKIPPED") _auditSkipped++;
            else { _auditFailed++; _lastAuditProblem = component + "/" + operation + ": " + details; }
            string message = "[cHub] [AUDIT] [" + (component ?? "GENERAL") + "] [" +
                (operation ?? "EVENT") + "] [" + normalized + "] " + (details ?? string.Empty);
            if (_instance != null) _instance.Add(message,
                normalized == "FAILED" || normalized == "MISMATCH" ? LogType.Warning : LogType.Log);
        }

        private void ReportFeatureInternal(
            string id,
            string displayName,
            bool working,
            string details)
        {
            if (string.IsNullOrWhiteSpace(id))
                return;

            string state = working ? "READY" : "FAILED";

            string previousState;

            // Aceeași componentă + aceeași stare = nu o mai logăm.
            if (_featureStates.TryGetValue(id, out previousState) &&
                string.Equals(
                    previousState,
                    state,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _featureStates[id] = state;

            string message =
                "[cHub][FEATURE] " +
                displayName +
                " [" + state + "]";

            if (!string.IsNullOrWhiteSpace(details))
                message += " - " + details;

            Add(
                message,
                working ? LogType.Log : LogType.Error);
        }

        private void AttachToGameLog()
        {
            try
            {
                string logsDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "7DaysToDie",
                    "logs");

                if (!Directory.Exists(logsDirectory))
                    return;

                FileInfo newest = new DirectoryInfo(logsDirectory)
                    .GetFiles("output_log_client__*.txt")
                    .OrderByDescending(file => file.LastWriteTimeUtc)
                    .FirstOrDefault();

                if (newest == null)
                    return;

                _gameLogPath = newest.FullName;

                // Citim și istoricul deja scris al sesiunii curente.
                _gameLogPosition = 0;

                Add(
                    "[GAMELOG] Attached to " + newest.Name,
                    LogType.Log);
            }
            catch (Exception ex)
            {
                Add(
                    "[GAMELOG] Attach failed: " + ex.Message,
                    LogType.Warning);
            }
        }

        private void PollGameLog()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_gameLogPath) ||
                    !File.Exists(_gameLogPath))
                {
                    AttachToGameLog();
                    return;
                }

                FileInfo info = new FileInfo(_gameLogPath);

                // Logul a fost recreat/trunchiat.
                if (info.Length < _gameLogPosition)
                    _gameLogPosition = 0;

                if (info.Length == _gameLogPosition)
                    return;

                using (FileStream stream = new FileStream(
                    _gameLogPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete))
                {
                    stream.Seek(_gameLogPosition, SeekOrigin.Begin);

                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string line;

                        while ((line = reader.ReadLine()) != null)
                        {
                            ProcessGameLogLine(line);
                        }

                        _gameLogPosition = stream.Position;
                    }
                }
            }
            catch (IOException)
            {
                // Jocul scrie în fișier exact în momentul citirii.
                // Ignorăm și încercăm din nou la următorul poll.
            }
            catch (Exception ex)
            {
                Add(
                    "[GAMELOG] Read failed: " + ex.Message,
                    LogType.Warning);
            }
        }

        private void ProcessGameLogLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return;

            // Mesajele cHub sunt deja primite prin Application.logMessageReceived.
            // Evităm dublurile.
            if (line.IndexOf("[cHub]", StringComparison.OrdinalIgnoreCase) >= 0)
                return;

            LogType type = DetectGameLogType(line);

            Add(line, type);
        }

        private static LogType DetectGameLogType(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return LogType.Log;

            if (line.IndexOf(" EXC ", StringComparison.OrdinalIgnoreCase) >= 0 ||
                line.StartsWith("EXC ", StringComparison.OrdinalIgnoreCase))
            {
                return LogType.Exception;
            }

            if (line.IndexOf(" ERR ", StringComparison.OrdinalIgnoreCase) >= 0 ||
                line.StartsWith("ERR ", StringComparison.OrdinalIgnoreCase))
            {
                return LogType.Error;
            }

            if (line.IndexOf(" WRN ", StringComparison.OrdinalIgnoreCase) >= 0 ||
                line.StartsWith("WRN ", StringComparison.OrdinalIgnoreCase))
            {
                return LogType.Warning;
            }

            return LogType.Log;
        }
        private void ExportConsoleLog()
        {
            try
            {
                string modPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Mods",
                    "cHub");

                string logsPath = Path.Combine(modPath, "Logs");

                if (!Directory.Exists(logsPath))
                {
                    Directory.CreateDirectory(logsPath);
                }

                string fileName =
                    "cHub-Console-" +
                    DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") +
                    ".txt";

                string filePath = Path.Combine(logsPath, fileName);

                List<string> exportLines = new List<string>();

                exportLines.Add("============================================================");
                exportLines.Add("c/Hub // STARTUP TERMINAL EXPORT");
                exportLines.Add("============================================================");
                exportLines.Add("Exported: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                exportLines.Add("c/Hub Version: " + cHubVersion.Current);
                exportLines.Add("Security: " + _eacStatus);

                int relevantCount = _lines.Count(
                    line => IsCHubRelevant(line.Text));

                exportLines.Add("Raw lines captured: " + _lines.Count);
                exportLines.Add("c/Hub relevant lines: " + relevantCount);
                exportLines.Add("Filter: ALL (forced for export)");
                exportLines.Add("Audit OK: " + _auditOk);
                exportLines.Add("Audit MISMATCH: " + _auditMismatch);
                exportLines.Add("Audit FAILED: " + _auditFailed);
                exportLines.Add("Audit SKIPPED: " + _auditSkipped);
                exportLines.Add("Last audit problem: " + _lastAuditProblem);
                exportLines.Add("FPS Session: " + FpsSnapshotText());
                exportLines.Add("============================================================");
                exportLines.Add(string.Empty);

                foreach (LogLine line in _lines)
                {
                    // EXPORT = ALL din consola c/Hub,
                    // nu întregul output brut al jocului.
                    if (!IsCHubRelevant(line.Text))
                        continue;

                    LogCategory category = DetectCategory(
                        line.Text,
                        line.Type);

                    exportLines.Add(
                        "[" + line.Time + "] " +
                        "[" + category.ToString().ToUpperInvariant() + "] " +
                        line.Text);
                }

                File.WriteAllLines(
                    filePath,
                    exportLines.ToArray());

                ShowCursorToast("CONSOLE EXPORTED");

                Add(
                    "[EXPORT] Console exported: " + filePath,
                    LogType.Log);
            }
            catch (Exception ex)
            {
                ShowCursorToast("EXPORT FAILED");

                Add(
                    "[EXPORT] Failed: " + ex.Message,
                    LogType.Error);
            }
        }


        //====== Modificari i27k ========

        private bool _floatingMode = false;

        private Rect _floatingRect =
            new Rect(180f, 100f, 1180f, 720f);

        private bool _draggingTerminal;
        private bool _resizingTerminal;

        private Vector2 _terminalDragOffset;
        private Vector2 _terminalResizeStartMouse;
        private Vector2 _terminalResizeStartSize;

        private const float FloatingMinWidth = 760f;
        private const float FloatingMinHeight = 500f;
        private const float FloatingResizeHandle = 22f;

        //===============================
        private void DrawFloatingResizeHandle(Rect panel)
        {
            if (!_floatingMode)
                return;

            Color previousColor = GUI.color;

            GUI.color = new Color(
                1f,
                0.30f,
                0.34f,
                0.80f);

            Texture2D texture = Texture2D.whiteTexture;

            // Mic handle diagonal în colțul dreapta-jos.
            GUI.DrawTexture(
                new Rect(
                    panel.xMax - 8f,
                    panel.yMax - 5f,
                    5f,
                    2f),
                texture);

            GUI.DrawTexture(
                new Rect(
                    panel.xMax - 13f,
                    panel.yMax - 10f,
                    10f,
                    2f),
                texture);

            GUI.DrawTexture(
                new Rect(
                    panel.xMax - 18f,
                    panel.yMax - 15f,
                    15f,
                    2f),
                texture);

            GUI.color = previousColor;
        }

        private void HandleFloatingWindowInput(ref Rect panel)
        {
            Event evt = Event.current;

            if (evt == null)
                return;

            Vector2 mouse = evt.mousePosition;

            Rect dragArea =
                new Rect(
                    panel.x,
                    panel.y,
                    Mathf.Max(100f, panel.width - 230f),
                    52f);

            Rect resizeArea =
                new Rect(
                    panel.xMax - FloatingResizeHandle,
                    panel.yMax - FloatingResizeHandle,
                    FloatingResizeHandle,
                    FloatingResizeHandle);

            if (evt.type == EventType.MouseDown &&
                evt.button == 0)
            {
                if (resizeArea.Contains(mouse))
                {
                    _resizingTerminal = true;

                    _terminalResizeStartMouse = mouse;

                    _terminalResizeStartSize =
                        new Vector2(
                            panel.width,
                            panel.height);

                    evt.Use();
                }
                else if (dragArea.Contains(mouse))
                {
                    _draggingTerminal = true;

                    _terminalDragOffset =
                        mouse -
                        new Vector2(
                            panel.x,
                            panel.y);

                    evt.Use();
                }
            }

            if (evt.type == EventType.MouseDrag &&
                evt.button == 0)
            {
                if (_resizingTerminal)
                {
                    Vector2 delta =
                        mouse -
                        _terminalResizeStartMouse;

                    _floatingRect.width =
                        Mathf.Clamp(
                            _terminalResizeStartSize.x + delta.x,
                            FloatingMinWidth,
                            Mathf.Max(
                                FloatingMinWidth,
                                Screen.width - _floatingRect.x));

                    _floatingRect.height =
                        Mathf.Clamp(
                            _terminalResizeStartSize.y + delta.y,
                            FloatingMinHeight,
                            Mathf.Max(
                                FloatingMinHeight,
                                Screen.height - _floatingRect.y));

                    panel = _floatingRect;

                    evt.Use();
                }
                else if (_draggingTerminal)
                {
                    Vector2 next =
                        mouse -
                        _terminalDragOffset;

                    _floatingRect.x =
                        Mathf.Clamp(
                            next.x,
                            0f,
                            Mathf.Max(
                                0f,
                                Screen.width - _floatingRect.width));

                    _floatingRect.y =
                        Mathf.Clamp(
                            next.y,
                            0f,
                            Mathf.Max(
                                0f,
                                Screen.height - _floatingRect.height));

                    panel = _floatingRect;

                    evt.Use();
                }
            }

            if (evt.type == EventType.MouseUp &&
                evt.button == 0)
            {
                _draggingTerminal = false;
                _resizingTerminal = false;
            }

            _floatingRect = panel;
        }
        //===============================
        private void DrawFilterButton(
        ref float x,
        ref float y,
        Rect panel,
        float width,
        string caption,
        Action action)
        {
            const float gap = 6f;
            const float height = 28f;

            float rightLimit = panel.xMax - 22f;

            // Dacă următorul buton nu mai încape,
            // trecem automat pe rândul următor.
            if (x + width > rightLimit)
            {
                x = panel.x + 22f;
                y += height + 6f;
            }

            if (GUI.Button(
                new Rect(x, y, width, height),
                caption,
                _filterButtonStyle))
            {
                action();
            }

            x += width + gap;
        }

        private void OnGUI()
        {
            EnsureStyles();

            if (!_visible)
            {
                DrawCursorToast();
                return;
            }

            // ============================================================
            // BACKGROUND / WINDOW MODE
            // ============================================================

            Rect panel;

            if (_floatingMode)
            {
                ClampFloatingRect();
                panel = _floatingRect;
            }
            else
            {
                // ============================================================
                // STARTUP MODE
                // Wallpaper + fullscreen overlay
                // ============================================================

                if (_wallpaper != null)
                {
                    GUI.DrawTexture(
                        new Rect(
                            0f,
                            0f,
                            Screen.width,
                            Screen.height),
                        _wallpaper,
                        ScaleMode.ScaleAndCrop,
                        true);
                }

                GUI.Box(
                    new Rect(
                        0f,
                        0f,
                        Screen.width,
                        Screen.height),
                    GUIContent.none,
                    _terminalStyle);

                float safeWidth =
                    Mathf.Max(
                        760f,
                        Screen.width - 40f);

                float safeHeight =
                    Mathf.Max(
                        520f,
                        Screen.height - 40f);

                float width =
                    Mathf.Min(
                        Mathf.Clamp(
                            Screen.width * 0.82f,
                            860f,
                            1480f),
                        safeWidth);

                float height =
                    Mathf.Min(
                        Mathf.Clamp(
                            Screen.height * 0.82f,
                            560f,
                            920f),
                        safeHeight);

                panel =
                    new Rect(
                        (Screen.width - width) * 0.5f,
                        (Screen.height - height) * 0.5f,
                        width,
                        height);
            }


            // ============================================================
            // FLOATING WINDOW INPUT
            // ============================================================

            if (_floatingMode)
            {
                HandleFloatingWindowInput(ref panel);
                _floatingRect = panel;
            }


            // ============================================================
            // TERMINAL PANEL
            // Floating mode = transparent background
            // Startup mode = keeps normal terminal panel
            // ============================================================

            if (!_floatingMode)
            {
                GUI.Box(
                    panel,
                    GUIContent.none,
                    _terminalStyle);
            }

            // ============================================================
            // HEADER
            // ============================================================

            float headerY = panel.y + 14f;

            GUI.Label(
                new Rect(
                    panel.x + 22f,
                    headerY,
                    280f,
                    38f),
                "c/Hub // TERMINAL",
                _titleStyle);


            // Security indicator

            GUI.Label(
                new Rect(
                    panel.x + panel.width - 178f,
                    headerY + 3f,
                    100f,
                    30f),
                _eacStatus,
                _statusStyle);


            // Close

            if (GUI.Button(
                new Rect(
                    panel.x + panel.width - 54f,
                    panel.y + 12f,
                    34f,
                    34f),
                "X"))
            {
                BeginClosePointerShield();
                _visible = false;
                Event.current?.Use();
            }


            // ============================================================
            // TOOLBAR
            // ============================================================

            float toolsY = panel.y + 58f;
            float toolsX = panel.x + 22f;

            if (IsLocalOwner())
            {
                if (GUI.Button(
                    new Rect(
                        toolsX,
                        toolsY,
                        86f,
                        28f),
                    "OPEN DIR",
                    _brandButtonStyle))
                {
                    OpenModDirectory();
                }

                toolsX += 94f;
            }


            if (GUI.Button(
                new Rect(
                    toolsX,
                    toolsY,
                    92f,
                    28f),
                "OPEN LOGS",
                _brandButtonStyle))
            {
                OpenLogDirectory();
            }

            toolsX += 100f;


            if (GUI.Button(
                new Rect(
                    toolsX,
                    toolsY,
                    82f,
                    28f),
                "EXPORT",
                _brandButtonStyle))
            {
                ExportConsoleLog();
            }


            // ============================================================
            // FILTER BAR - RESPONSIVE
            // ============================================================

            float filterY = panel.y + 98f;
            float filterX = panel.x + 22f;

            DrawFilterButton(
                ref filterX,
                ref filterY,
                panel,
                54f,
                "ALL",
                ShowAllLogCategories);

            DrawFilterButton(
                ref filterX,
                ref filterY,
                panel,
                68f,
                "c/HUB",
                () => ShowOnlyLogCategory(LogCategory.cHub));

            DrawFilterButton(
                ref filterX,
                ref filterY,
                panel,
                62f,
                "BOOT",
                () => ShowOnlyLogCategory(LogCategory.Boot));

            DrawFilterButton(
                ref filterX,
                ref filterY,
                panel,
                124f,
                "COMMANDS",
                () => ShowOnlyLogCategory(LogCategory.Commands));

            DrawFilterButton(
                ref filterX,
                ref filterY,
                panel,
                72f,
                "PERMS",
                () => ShowOnlyLogCategory(LogCategory.Permissions));

            DrawFilterButton(
                ref filterX,
                ref filterY,
                panel,
                54f,
                "XUI",
                () => ShowOnlyLogCategory(LogCategory.Xui));

            DrawFilterButton(
                ref filterX,
                ref filterY,
                panel,
                46f,
                "UI",
                () => ShowOnlyLogCategory(LogCategory.Ui));

            DrawFilterButton(
                ref filterX,
                ref filterY,
                panel,
                54f,
                "NET",
                () => ShowOnlyLogCategory(LogCategory.Network));

            DrawFilterButton(
                ref filterX,
                ref filterY,
                panel,
                116f,
                "HARMONY",
                () => ShowOnlyLogCategory(LogCategory.Harmony));

            DrawFilterButton(
                ref filterX,
                ref filterY,
                panel,
                66f,
                "WARN",
                () => ShowOnlyLogCategory(LogCategory.Warning));

            DrawFilterButton(
                ref filterX,
                ref filterY,
                panel,
                54f,
                "ERR",
                () => ShowOnlyLogCategory(LogCategory.Error));


            // ============================================================
            // LOG VIEW
            // ============================================================

            float logTop = filterY + 42f;

            Rect logArea = new Rect(
                panel.x + 20f,
                logTop,
                panel.width - 40f,
                panel.yMax - logTop - 68f);

            GUI.Box(
                logArea,
                GUIContent.none,
                _terminalStyle);


            List<LogLine> visibleLines = _lines
                .Where(line =>
                    ShouldDisplay(
                        line.Text,
                        line.Type))
                .ToList();


            float contentHeight = Mathf.Max(
                logArea.height - 12f,
                visibleLines.Count * LogLineHeight + 14f);


            _scroll = GUI.BeginScrollView(
                logArea,
                _scroll,
                new Rect(
                    0f,
                    0f,
                    logArea.width - 22f,
                    contentHeight));


            int firstVisible = Mathf.Clamp(
                Mathf.FloorToInt(
                    _scroll.y / LogLineHeight) - 1,
                0,
                Mathf.Max(
                    0,
                    visibleLines.Count - 1));


            int visibleCount =
                Mathf.CeilToInt(
                    logArea.height / LogLineHeight) + 3;


            int lastVisible = Mathf.Min(
                visibleLines.Count,
                firstVisible + visibleCount);


            for (int i = firstVisible;
                 i < lastVisible;
                 i++)
            {
                LogLine line = visibleLines[i];

                LogCategory category =
                    DetectCategory(
                        line.Text,
                        line.Type);

                _logStyle.normal.textColor =
                    ColorFor(line.Type);


                string prefix =
                    "[" +
                    category.ToString().ToUpperInvariant() +
                    "]";


                GUI.Label(
                    new Rect(
                        12f,
                        7f + i * LogLineHeight,
                        logArea.width - 48f,
                        LogLineHeight),
                    "[" + line.Time + "] " +
                    prefix + " " +
                    line.Text,
                    _logStyle);
            }


            GUI.EndScrollView();


            // ============================================================
            // FOOTER
            // ============================================================

            float footerY =
                panel.y + panel.height - 50f;


            GUI.Label(
                new Rect(
                    panel.x + 22f,
                    footerY + 6f,
                    170f,
                    28f),
                "c/Hub v" + cHubVersion.Current,
                _statusStyle);


            GUI.Label(
                new Rect(
                    panel.x + 190f,
                    footerY + 6f,
                    300f,
                    28f),
                _lines.Count.ToString("N0") +
                " LOG EVENTS",
                _brandStyle);


            if (GUI.Button(
                new Rect(
                    panel.x + panel.width - 292f,
                    footerY,
                    128f,
                    34f),
                "DISCORD",
                _brandButtonStyle))
            {
                OpenDiscord();
            }



            if (GUI.Button(
                new Rect(
                    panel.x + panel.width - 152f,
                    footerY,
                    124f,
                    34f),
                "COPY LINK",
                _brandButtonStyle))
            {
                CopyDiscord();
            }



            // ============================================================
            // RESIZE HANDLE
            // ============================================================

            DrawFloatingResizeHandle(panel);


            DrawCursorToast();
        }

        private static void BeginClosePointerShield()
        {
            _consumeClosePointer = true;
            _consumeCloseUntilFrame = Time.frameCount + 2;
            Trace("UI", "CONSOLE_CLOSE_SHIELD", "active=true frame=" + Time.frameCount);
        }

        private static bool ShouldBlockUnderlyingPointer()
        {
            return _consumeClosePointer || Time.frameCount <= _consumeCloseUntilFrame;
        }

        [HarmonyPatch(typeof(XUiController), "Pressed")]
        private static class BlockXuiPressAfterTerminalClosePatch
        {
            private static bool Prefix()
            {
                return !ShouldBlockUnderlyingPointer();
            }
        }

        [HarmonyPatch(typeof(XUiController), "MouseUpDown")]
        private static class BlockXuiMouseAfterTerminalClosePatch
        {
            private static bool Prefix()
            {
                return !ShouldBlockUnderlyingPointer();
            }
        }

        private void OpenExternalUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
                ShowCursorToast("OPENING DISCORD");
                Add("[DISCORD] Opening invite in the default browser.", LogType.Log);
            }
            catch (Exception shellException)
            {
                try
                {
                    Application.OpenURL(url);
                    ShowCursorToast("OPENING DISCORD");
                    Add("[DISCORD] Opened through Unity fallback.", LogType.Log);
                }
                catch (Exception unityException)
                {
                    ShowCursorToast("OPEN FAILED");
                    Add("[DISCORD] Open failed: " + shellException.Message + " / " +
                        unityException.Message, LogType.Error);
                }
            }
        }

        private void ShowCursorToast(string message)
        {
            Vector3 mouse = Input.mousePosition;
            _toastPosition = new Vector2(mouse.x, Screen.height - mouse.y);
            _toastText = message;
            _toastUntil = Time.realtimeSinceStartup + 2.2f;
        }

        private void DrawCursorToast()
        {
            if (string.IsNullOrEmpty(_toastText) || Time.realtimeSinceStartup > _toastUntil)
                return;
            float width = 172f;
            float height = 34f;
            float x = Mathf.Clamp(_toastPosition.x + 16f, 8f, Screen.width - width - 8f);
            float y = Mathf.Clamp(_toastPosition.y + 14f, 8f, Screen.height - height - 8f);
            Rect toast = new Rect(x, y, width, height);
            GUI.Box(toast, GUIContent.none, _terminalStyle);
            GUI.Label(toast, _toastText, new GUIStyle(_brandButtonStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.55f, 1f, 0.65f, 1f) }
            });
        }

        private void EnsureStyles()
        {
            if (_stylesReady)
                return;

            // ============================================
            // MAIN PANEL
            // ============================================

            _terminalStyle = new GUIStyle(GUI.skin.box);

            _terminalStyle.normal.background =
                MakeSolid(new Color(0.012f, 0.012f, 0.016f, 0.94f));

            _terminalStyle.border =
                new RectOffset(1, 1, 1, 1);

            _terminalStyle.padding =
                new RectOffset(12, 12, 10, 10);


            // ============================================
            // TITLE
            // ============================================

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,

                normal =
        {
            textColor = new Color(
                1f,
                0.30f,
                0.34f,
                1f)
        }
            };


            // ============================================
            // PANEL SECTION TITLE
            // ============================================

            _panelHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,

                normal =
        {
            textColor = new Color(
                0.62f,
                0.65f,
                0.70f,
                1f)
        }
            };


            // ============================================
            // LOG TEXT
            // ============================================

            _logStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                wordWrap = false,
                alignment = TextAnchor.MiddleLeft,

                normal =
        {
            textColor = new Color(
                0.82f,
                0.84f,
                0.86f,
                1f)
        }
            };


            // ============================================
            // STATUS TEXT
            // ============================================

            _statusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,

                normal =
        {
            textColor = new Color(
                1f,
                0.72f,
                0.25f,
                1f)
        }
            };


            // ============================================
            // FOOTER / BRAND
            // ============================================

            _brandStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,

                normal =
        {
            textColor = new Color(
                0.72f,
                0.74f,
                0.78f,
                1f)
        }
            };


            // ============================================
            // PRIMARY BUTTONS
            // OPEN DIR / OPEN LOGS / DISCORD
            // ============================================

            _brandButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,

                padding =
                    new RectOffset(8, 8, 4, 4),

                normal =
        {
            textColor = new Color(
                0.90f,
                0.90f,
                0.92f,
                1f)
        },

                hover =
        {
            textColor = Color.white
        }
            };


            // ============================================
            // FILTER BUTTONS
            // ============================================

            _filterButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,

                padding =
                    new RectOffset(8, 8, 3, 3),

                normal =
        {
            textColor = new Color(
                0.72f,
                0.74f,
                0.78f,
                1f)
        },

                hover =
        {
            textColor = Color.white
        }
            };


            // ============================================
            // ACTIVE FILTER
            // ============================================

            _filterActiveStyle = new GUIStyle(_filterButtonStyle);

            _filterActiveStyle.normal.textColor =
                new Color(
                    1f,
                    0.34f,
                    0.38f,
                    1f);

            _filterActiveStyle.hover.textColor =
                Color.white;


            _stylesReady = true;
        }

        private void DrawMainMenuBrand()
        {
            // XUi_Menu mod patches are not consistently applied by all 3.x
            // clients. This lightweight runtime fallback is intentionally
            // limited to the menu (no active World) and remains clickable.
            if (GameManager.Instance != null && GameManager.Instance.World != null) return;

            float width = Mathf.Min(690f, Screen.width - 32f);
            Rect bar = new Rect((Screen.width - width) * 0.5f,
                Screen.height - 82f, width, 58f);
            GUI.Box(bar, GUIContent.none, _terminalStyle);
            GUI.Label(new Rect(bar.x + 18f, bar.y + 10f, 235f, 38f),
                "c/Hub  •  v" + cHubVersion.Current, _brandStyle);

            Color previous = _statusStyle.normal.textColor;
            _statusStyle.normal.textColor = IsEacActive
                ? new Color(1f, 0.72f, 0.25f, 1f)
                : new Color(0.45f, 0.95f, 0.58f, 1f);
            GUI.Label(new Rect(bar.x + 245f, bar.y + 12f, 90f, 34f),
                IsEacActive ? "EAC ON" : "EAC OFF", _statusStyle);
            _statusStyle.normal.textColor = previous;

            if (GUI.Button(new Rect(bar.x + width - 330f, bar.y + 9f, 150f, 40f),
                "OPEN DISCORD", _brandButtonStyle)) OpenDiscord();
            if (GUI.Button(new Rect(bar.x + width - 168f, bar.y + 9f, 150f, 40f),
                "COPY LINK", _brandButtonStyle)) CopyDiscord();
        }

        private static Texture2D MakeSolid(Color color)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private void LoadWallpaper()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    "Mods", "cHub", "UI", "chub-startup-wallpaper-v1.png");
                if (!File.Exists(path)) return;
                byte[] bytes = File.ReadAllBytes(path);
                _wallpaper = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                _wallpaper.LoadImage(bytes);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[cHub] Startup wallpaper failed: " + ex.Message);
            }
        }

        private void OpenModDirectory()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    "Mods", "cHub");
                if (!Directory.Exists(path))
                {
                    Add("[OPEN DIR] c/Hub mod directory was not found: " + path,
                        LogType.Warning);
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
                Add("[OPEN DIR] " + path, LogType.Log);
            }
            catch (Exception ex)
            {
                Add("[OPEN DIR] Failed: " + ex.Message, LogType.Error);
            }
        }

        private void OpenLogDirectory()
        {
            try
            {
                string path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "7DaysToDie", "logs");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    Add("[OPEN LOGS] Created log directory: " + path, LogType.Log);
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
                Add("[OPEN LOGS] " + path, LogType.Log);
            }
            catch (Exception ex)
            {
                Add("[OPEN LOGS] Failed: " + ex.Message, LogType.Error);
            }
        }

        private static bool IsLocalOwner()
        {
            try
            {
                string playerId = GameManager.Instance?.getPersistentPlayerID(null)?.CombinedString;
                return !string.IsNullOrWhiteSpace(playerId) &&
                       ServiceRegistry.Get<RoleService>()?.HasPermission(
                           playerId, PermissionIds.Owner) == true;
            }
            catch
            {
                return false;
            }
        }

        private static bool DetectEac()
        {
            try
            {
                string process = Process.GetCurrentProcess().ProcessName ?? string.Empty;
                string command = Environment.CommandLine ?? string.Empty;
                if (command.IndexOf("-noeac", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    command.IndexOf("-disableeac", StringComparison.OrdinalIgnoreCase) >= 0)
                    return false;
                // EAC assemblies can be present in both launch modes. Only an
                // explicit launcher/process signal is treated as active.
                return process.IndexOf("eac", StringComparison.OrdinalIgnoreCase) >= 0 ||
                       command.IndexOf("-eac", StringComparison.OrdinalIgnoreCase) >= 0 ||
                       command.IndexOf("eac_launcher", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch { return false; }
        }

        private static Color ColorFor(LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception)
                return new Color(1f, 0.25f, 0.28f, 1f);
            if (type == LogType.Warning)
                return new Color(1f, 0.72f, 0.25f, 1f);
            return new Color(0.78f, 0.82f, 0.80f, 1f);
        }

        private sealed class LogLine
        {
            public readonly string Time;
            public readonly string Text;
            public readonly LogType Type;
            public LogLine(string time, string text, LogType type)
            { Time = time; Text = text; Type = type; }
        }
    }
}
