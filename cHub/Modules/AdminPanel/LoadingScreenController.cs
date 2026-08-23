using cHub.Core;
using System;
using System.Linq;
using System.Reflection;

/// <summary>
/// Keeps the vanilla LoadingScreen behaviour and only supplies a c/Hub
/// background chosen once whenever the loading window opens.
/// </summary>
public class XUiC_cHubLoadingScreen : XUiC_LoadingScreen
{
    public static bool IsLoadingActive { get; private set; }
    public static event Action<bool> LoadingStateChanged;
    private static readonly string[] Backgrounds =
    {
        "@modfolder:UI/chub-loading-forest-v2.png",
        "@modfolder:UI/chub-loading-eclipse-v2.png",
        "@modfolder:UI/chub-loading-bunker-v2.png"
    };

    private static int _lastIndex = -1;
    private int _currentIndex;
    private string _currentBackground = Backgrounds[0];
    private string[] _connectionLines =
    {
        "Preparing connection",
        "Contacting server",
        "Waiting for server response"
    };
    private float _typeTimer;
    private int _typeLine;
    private int _typeCharacter;
    private string _connectionText = string.Empty;
    private string _serverName = "c/Hub Public Server";
    private string _serverIp = "LOCAL HOST";
    private string _serverPort = "N/A";
    private string _hostedBy = "bluefangs.com";
    private bool _singlePlayer;
    private bool _connectionComplete;

    public override void OnOpen()
    {
        SetLoadingState(true);
        int next;
        if (Backgrounds.Length <= 1) next = 0;
        else
        {
            // Avoid showing the same artwork twice consecutively while still
            // keeping the sequence unpredictable between game sessions.
            next = UnityEngine.Random.Range(0, Backgrounds.Length - 1);
            if (next >= _lastIndex) next++;
            if (next >= Backgrounds.Length) next = 0;
        }
        _lastIndex = next;
        _currentIndex = next;
        _currentBackground = Backgrounds[next];
        StartupTerminal.ReportFeature(
            "loading.wallpapers",
            "Loading Screen Wallpapers",
            Backgrounds != null && Backgrounds.Length > 0,
            Backgrounds != null
                ? Backgrounds.Length + " wallpapers registered, active: " + _currentBackground
                : "Wallpaper list is null");
        _typeTimer = 0f;
        _typeLine = 0;
        _typeCharacter = 0;
        _connectionText = string.Empty;
        _connectionComplete = false;
        _serverName = "c/Hub Public Server";
        _serverIp = "LOCAL HOST";
        _serverPort = "N/A";
        _hostedBy = "bluefangs.com";
        StartupTerminal.ReportFeature(
    "loading.status",
    "Loading Screen / cHub Status",
    true,
    "Status controller initialized and server details refresh requested");
        RefreshServerDetails();
        base.OnOpen();
        SetAllChildrenDirty();
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        if (_typeLine >= _connectionLines.Length)
        {
            RefreshConnectionOutcome();
            return;
        }
        _typeTimer += deltaTime;
        if (_typeTimer < 0.075f) return;
        _typeTimer = 0f;

        string current = _connectionLines[_typeLine];
        if (_typeCharacter < current.Length)
        {
            _typeCharacter++;
        }
        else
        {
            _typeLine++;
            _typeCharacter = 0;
            // Hold the completed step briefly, then replace it in-place with
            // the next one. The status panel intentionally stays one line tall.
            _typeTimer = -0.70f;
        }
        RebuildConnectionText();
        SetAllChildrenDirty();
    }

    private void RebuildConnectionText()
    {
        if (_typeLine < _connectionLines.Length)
            _connectionText = _connectionLines[_typeLine].Substring(0,
                UnityEngine.Mathf.Clamp(_typeCharacter, 0, _connectionLines[_typeLine].Length));
        else if (_singlePlayer)
            _connectionText = _connectionComplete ? "Single-player world ready" : "Starting local session...";
        else
            _connectionText = _connectionComplete ? "Connected to c/Hub server" : "Waiting for server response...";
    }

    private void RefreshServerDetails()
    {
        string name = ReadGamePreference("ServerName");
        string ip = ReadGamePreference("ServerIP");
        string port = ReadGamePreference("ServerPort");
        if (!string.IsNullOrWhiteSpace(name)) _serverName = name;
        if (!string.IsNullOrWhiteSpace(ip) && ip != "0.0.0.0") _serverIp = ip;
        if (!string.IsNullOrWhiteSpace(port) && port != "0") _serverPort = port;
        _singlePlayer = IsLocalAddress(_serverIp);
        if (_singlePlayer)
        {
            _connectionLines = new[]
            {
                "Preparing single-player world",
                "Loading local world data",
                "Starting local session"
            };
            _serverName = "SINGLE-PLAYER WORLD";
            _serverIp = "LOCAL WORLD";
            _serverPort = "N/A";
            _hostedBy = "LOCAL CLIENT";
        }
        else
        {
            _connectionLines = new[]
            {
                "Preparing connection",
                "Contacting c/Hub server",
                "Authenticating session"
            };
            _hostedBy = "bluefangs.com";
        }
    }

    private void RefreshConnectionOutcome()
    {
        bool ready = false;
        try
        {
            ready = GameManager.Instance?.World != null &&
                    GameManager.Instance.World.GetLocalPlayers()?.Any(player => player != null) == true;
        }
        catch { }
        if (ready == _connectionComplete) return;
        _connectionComplete = ready;
        RebuildConnectionText();
        SetAllChildrenDirty();
    }

    private static bool IsLocalAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address)) return true;
        string value = address.Trim().ToLowerInvariant();
        return value == "local host" || value == "localhost" || value == "local world" ||
               value == "127.0.0.1" || value == "0.0.0.0" || value == "::1" || value == "n/a";
    }

    private static string ReadGamePreference(string preferenceName)
    {
        try
        {
            object key = Enum.Parse(typeof(EnumGamePrefs), preferenceName, true);
            MethodInfo getter = typeof(GamePrefs).GetMethods(BindingFlags.Public |
                BindingFlags.Static).FirstOrDefault(method =>
                    (method.Name == "GetString" || method.Name == "GetInt") &&
                    method.GetParameters().Length == 1 &&
                    method.GetParameters()[0].ParameterType == typeof(EnumGamePrefs) &&
                    (preferenceName == "ServerPort" ? method.Name == "GetInt" :
                                                       method.Name == "GetString"));
            return getter?.Invoke(null, new[] { key })?.ToString() ?? string.Empty;
        }
        catch { return string.Empty; }
    }

    public override void OnClose()
    {
        SetLoadingState(false);
        base.OnClose();
    }

    private static void SetLoadingState(bool active)
    {
        IsLoadingActive = active;
        try { LoadingStateChanged?.Invoke(active); }
        catch { }
    }

    public override bool GetBindingValueInternal(ref string value, string bindingName)
    {
        if (string.Equals(bindingName, "chub_loading_texture",
            StringComparison.OrdinalIgnoreCase))
        {
            value = _currentBackground;
            return true;
        }
        if (string.Equals(bindingName, "chub_connection_typewriter", StringComparison.OrdinalIgnoreCase))
        { value = _connectionText; return true; }
        if (string.Equals(bindingName, "chub_server_name", StringComparison.OrdinalIgnoreCase))
        { value = _serverName; return true; }
        if (string.Equals(bindingName, "chub_server_ip", StringComparison.OrdinalIgnoreCase))
        { value = _serverIp; return true; }
        if (string.Equals(bindingName, "chub_server_port", StringComparison.OrdinalIgnoreCase))
        { value = _serverPort; return true; }
        if (string.Equals(bindingName, "chub_hosted_by", StringComparison.OrdinalIgnoreCase))
        { value = _hostedBy; return true; }
        if (string.Equals(bindingName, "chub_loading_forest", StringComparison.OrdinalIgnoreCase))
        { value = (_currentIndex == 0).ToString().ToLowerInvariant(); return true; }
        if (string.Equals(bindingName, "chub_loading_eclipse", StringComparison.OrdinalIgnoreCase))
        { value = (_currentIndex == 1).ToString().ToLowerInvariant(); return true; }
        if (string.Equals(bindingName, "chub_loading_bunker", StringComparison.OrdinalIgnoreCase))
        { value = (_currentIndex == 2).ToString().ToLowerInvariant(); return true; }
        return base.GetBindingValueInternal(ref value, bindingName);
    }
}
