using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using cHub.Core;
using cHub.Services.Permissions;
using cHub.Services.Config;
using cHub.Services.Players;
using cHub.Services.Roles;
using cHub.Services.Commands;
using cHub.Modules.AdminPanel;
using cHub.Networking;
using cHub.Modules.Party;
using UnityEngine;
using System.Diagnostics;

using PermissionIds = cHub.Shared.Constants.Permissions;

public class XUiC_cHubAdminPanel : XUiController
{
    private string _activePage = "menu";
    private float _windowScale = 1f;
    private float _targetWindowScale = 1f;
    private float _popupProgress = 1f;
    private bool _sidePanelShifted;
    private int _playerListPage;
    private XUiC_TextInput _searchInput;
    private XUiController _searchPlaceholder;
    private XUiController _playerCommandsScrollView;
    private Vector3 _playerCommandsScrollBasePosition;
    private bool _searchSelected;
    private int _searchSelectedFrame = -1;
    private string _searchText = string.Empty;
    private XUiC_TextInput _adminPlayerSearchInput;
    private string _adminPlayerSearchText = string.Empty;
    private List<PlayerListEntry> _cachedPlayerEntries = new List<PlayerListEntry>();
    private float _playerRefreshTimer;
    private string _partySelectedPlayerId;
    private string _partySelectedMemberId;
    private string _partyStatus = "Create a party or select a survivor.";
    private bool _partyContextVisible;
    private string _capturingKeybindAction;
    private string _optionsStatus = "Select a binding, then press a keyboard or mouse button.";
    private string _selectedZombieClass = "zombieMoe";
    private string _zombieDirectorStatus = "Selected: Walker";
    private XUiC_TextInput _homeNameInput;
    private XUiC_TextInput _homeCooldownInput;
    private string _homeStatus = string.Empty;
    private XUiC_TextInput _rolePlayerInput;
    private XUiC_TextInput _roleIdInput;
    private string _roleManagerStatus = "Enter a player name/ID and a role.";
    private string _playerTeleportStatus = "Select an online player to send a magic request.";
    private string _adminTeleportSourceId;
    private string _adminTeleportTargetId;
    private bool _selectingAdminSource = true;
    private string _adminTeleportStatus = "Choose Source and Target.";
    private string _economyTargetId;
    private XUiC_TextInput _economyAmountInput;
    private string _economyStatus = "Select an online player and enter an amount.";
    private float _liveFooterRefreshTimer;
    private XUiController _localXpProgressFill;
    private XUiController _localLevelStageLabel;
    private XUiController _localXpLabel;
    private XUiController _xpGainPopup;
    private int _lastObservedXp;
    private int _lastObservedXpRequired = 1;
    private int _lastObservedLevel = 1;
    private bool _xpObservationReady;
    private int _xpGainAmount;
    private float _xpGainTimer;
    private XUiC_TextInput _commandCenterInput;
    private string _commandCenterStatus = "Enter a registered command without using game chat.";
    private string _devToolsCategory = "project";
    private string _devToolsStatus = "OWNER DEVTOOLS READY // select a category or action";
    private XUiController _animatedTitle;
    private XUiController _animatedTitleGlow;
    private XUiController _animatedEmblem;
    private float _titleAnimationTime;
    private Vector3 _titleBasePosition;
    private Vector3 _titleGlowBasePosition;
    private readonly Dictionary<string, float> _floatingPanelScales =
        new Dictionary<string, float>(System.StringComparer.OrdinalIgnoreCase);
    private XUiController _activeFloatingPanel;
    private float _floatingPopupProgress = 1f;
    private Vector2 _mainDragRemainder = Vector2.zero;
    private float _mainResizeRemainder;
    private const int PlayersPerPage = 6;
    private const float MinimumPanelScale = 0.38f;
    private const float MaximumPanelScale = 1.40f;
    private const string DiscordInviteUrl = "https://discord.gg/s9DAmRgsAg";
    private bool _discordPopupVisible;
    private string _discordStatus = "Open Discord or copy the invite.";
    private string _placeholderTitle = "c/Hub Module";
    private string _placeholderMessage = "This module is registered and awaiting its backend implementation.";
    private XUiC_TextInput _assistantInput;
    private string _assistantHistory = "c/Hub AI online. Ask me anything about 7 Days to Die or c/Hub gameplay.";
    private string _assistantStatus = "READY";
    private string _assistantRequestId;
    private bool _menuInteractionEnabled = true;
    private XUiC_TextInput _themeCssInput;
    private XUiC_SimpleButton _footerDiscordButton;
    private XUiC_SimpleButton _footerCloseButton;
    private float _footerBindRetry;
    private string _themeCss = string.Empty;
    private string _themeEditorStatus = "Paste a c/Hub CSS theme, then press APPLY LIVE.";
    private float _backgroundOpacity = 0.72f;
    private float _backgroundDensity = 1.1f;
    private XUiC_TextInput _backgroundValueInput;
    private XUiController _backgroundSliderKnob;
    private bool _appearanceSavePending;
    private float _appearanceSaveDelay;
    private bool _glowEnabled = true;
    private bool _scanlinesEnabled;
    private bool _animationsEnabled = true;
    private bool _themeSelectAll;
    private const string NightglassTheme = "chub-theme {\n  --name: Nightglass;\n  --accent: #FF3048;\n  --background-opacity: 0.38;\n  --glow: true;\n  --scanlines: true;\n  --animations: true;\n}";

    public override void Init()
    {
        base.Init();

        BindCloseButton("btnCloseMenu");
        BindPanelCloseButton("btnCloseStaff", "menu");
        BindPanelCloseButton("btnCloseModerator", "staff");
        BindPanelCloseButton("btnCloseAdministrator", "staff");
        BindPanelCloseButton("btnCloseDev", "menu");
        BindCloseButton("btnCloseTeleport");
        BindPageButton("btnBackTeleport", "menu");
        BindPanelCloseButton("btnClosePlayerList", "menu");
        BindPanelCloseButton("btnCloseHomes", "menu");
        BindPanelCloseButton("btnCloseRoleManager", "dev");
        BindPanelCloseButton("btnCloseSettingsHub", "menu");
        BindPanelCloseButton("btnCloseOptions", "settings");
        BindPanelCloseButton("btnCloseThemeEditor", "settings");
        BindPanelCloseButton("btnCloseZombies", "administrator");
        BindPanelCloseButton("btnCloseRot", "menu");
        BindPanelCloseButton("btnCloseTopRot", "menu");
        BindFooterControls();
        BindDiscordControls();

        XUiController dragHandle =
            FindDescendant<XUiController>(this, "dragHandle");

        if (dragHandle != null)
        {
            dragHandle.OnDrag += DragHandle_OnDrag;
        }

        BindResizeHandle("resizeLeft");
        BindResizeHandle("resizeRight");
        BindResizeHandle("resizeTop");
        BindResizeHandle("resizeBottom");
        BindResizeHandle("resizeTopLeft");
        BindResizeHandle("resizeTopRight");
        BindResizeHandle("resizeBottomLeft");
        BindResizeHandle("resizeBottomRight");
        BindFloatingPanel("homesPage", "homesDrag", "homesResize");
        BindFloatingPanel("playerListPage", "playerListDrag", "playerListResize");
        BindFloatingPanel("teleportPage", "teleportDrag", "teleportResize");
        BindFloatingPanel("roleManagerPage", "roleManagerDrag", "roleManagerResize");
        BindFloatingPanel("assistantPage", "assistantDrag", "assistantResize");
        BindFloatingPanel("placeholderPage", "placeholderDrag", "placeholderResize");
        BindFloatingPanel("discordPopup", "discordDrag", "discordResize");
        BindFloatingPanel("commandCenterPage", "commandCenterDrag", "commandCenterResize");
        BindFloatingPanel("devToolsPage", "devToolsDrag", "devToolsResize");

        BindPlayerMenuTab();
        BindPageButton("tabStaff", "staff");
        BindPageButton("tabDev", "dev");
        BindPageButton("tabParty", "party");
        BindPageButton("staffTabPlayer", "menu");
        BindPageButton("staffTabStaff", "staff");
        BindPageButton("staffTabDev", "dev");
        BindPageButton("devTabPlayer", "menu");
        BindPageButton("devTabStaff", "staff");
        BindPageButton("devTabDev", "dev");
        BindPageButton("btnStaffPlayers", "playerlist");
        BindPageButton("btnStaffTeleport", "teleport");
        BindPageButton("btnStaffZombies", "zombies");
        BindPageButton("btnDevTeleport", "teleport");
        BindPageButton("btnDevZombies", "zombies");
        BindPageButton("btnDevOptions", "settings");
        BindPageButton("btnDevCommands", "commandcenter");
        BindPageButton("btnDevToolbar", "devtools");
        BindPanelCloseButton("btnCloseDevTools", "dev");
        BindDevToolsControls();
        BindEconomyAdminControls();
        BindPageButton("btnModeratorCommands", "playerlist");
        BindPageButton("btnAdminCommands", "teleport");
        BindPageButton("btnTeleport", "teleport");
        BindPageButton("btnPlayerList", "teleport");
        BindPageButton("btnModeratorPlayers", "playerlist");
        BindPageButton("btnPlayerTeleport", "playerlist");
        BindPageButton("btnHomes", "homes");
        BindPageButton("btnRoleManager", "rolemanager");
        BindPageButton("btnSettings", "settings");
        BindPageButton("btnOpenKeybinds", "options");
        BindPageButton("btnOpenThemeEditor", "themeeditor");
        BindPageButton("btnRot", "rot");
        BindPageButton("btnTopRot", "toprot");
        BindPageButton("btnZombieDirector", "zombies");
        BindPageButton("btnRoles", "staff");
        BindPageButton("btnPermissions", "staff");
        BindPageButton("btnServer", "dev");
        BindPageButton("btnModules", "dev");
        BindPanelCloseButton("btnCloseParty", "menu");
        BindPageButton("btnPartyBack", "menu");
        BindPartyControls();
        BindPlaceholderButton("btnShop", "Shop");
        BindPlaceholderButton("btnCityTeleport", "City Teleport");
        BindPlaceholderButton("btnDailyRewards", "Daily Rewards");
        BindPlaceholderButton("btnEconomy", "Economy");
        BindPlaceholderButton("btnVehicles", "Vehicle Spawner");
        BindPlaceholderButton("btnQuests", "Quests");
        BindPlaceholderButton("btnMarketplace", "Marketplace");
        BindPlaceholderButton("btnAltSystems", "Alt Systems");
        BindPageButton("btnAssistant", "assistant");
        BindPanelCloseButton("btnCloseAssistant", "menu");
        BindPageButton("btnPlaceholderBack", "menu");
        BindCloseButton("btnPlaceholderClose");
        BindPageButton("btnBack", "menu");
        BindPageButton("btnPlayerBack", "menu");
        BindPageButton("btnBackStaff", "menu");
        BindPageButton("btnBackModerator", "staff");
        BindPageButton("btnBackAdministrator", "staff");
        BindPageButton("btnBackDev", "menu");
        BindListPageButton("btnPlayerPrev", -1);
        BindListPageButton("btnPlayerNext", 1);
        BindTeleportRequestButton("btnRequestPlayer1", 0);
        BindTeleportRequestButton("btnRequestPlayer2", 1);
        BindTeleportRequestButton("btnRequestPlayer3", 2);
        BindTeleportRequestButton("btnRequestPlayer4", 3);
        BindTeleportRequestButton("btnRequestPlayer5", 4);
        BindTeleportRequestButton("btnRequestPlayer6", 5);
        BindAdminSelector("btnAdminSource", true);
        BindAdminSelector("btnAdminTarget", false);
        BindAdminCandidate("btnAdminCandidate1", 0);
        BindAdminCandidate("btnAdminCandidate2", 1);
        BindAdminCandidate("btnAdminCandidate3", 2);
        BindAdminCandidate("btnAdminCandidate4", 3);
        XUiC_SimpleButton swapAdmin = FindDescendant<XUiC_SimpleButton>(this, "btnAdminSwap");
        if (swapAdmin != null) swapAdmin.OnPressed += SwapAdminTeleport_OnPressed;
        XUiC_SimpleButton confirmAdmin = FindDescendant<XUiC_SimpleButton>(this, "btnTeleportConfirm");
        if (confirmAdmin != null) confirmAdmin.OnPressed += ConfirmAdminTeleport_OnPressed;
        BindHomeButton("btnHome1", 0);
        BindHomeButton("btnHome2", 1);
        BindHomeButton("btnHome3", 2);
        BindHomeButton("btnHome4", 3);
        BindHomeButton("btnHome5", 4);
        BindHomeButton("btnHome6", 5);
        BindHomeButton("btnHome7", 6);
        BindHomeButton("btnHome8", 7);

        XUiC_SimpleButton saveHome = FindDescendant<XUiC_SimpleButton>(this, "btnSaveHome");
        if (saveHome != null) saveHome.OnPressed += SaveHome_OnPressed;
        XUiC_SimpleButton saveCooldown = FindDescendant<XUiC_SimpleButton>(this, "btnSaveHomeCooldown");
        if (saveCooldown != null) saveCooldown.OnPressed += SaveHomeCooldown_OnPressed;
        _homeNameInput = FindDescendant<XUiC_TextInput>(this, "homeName");
        _homeCooldownInput = FindDescendant<XUiC_TextInput>(this, "homeCooldown");
        _rolePlayerInput = FindDescendant<XUiC_TextInput>(this, "rolePlayer");
        _roleIdInput = FindDescendant<XUiC_TextInput>(this, "roleId");
        _economyAmountInput = FindDescendant<XUiC_TextInput>(this, "economyAmount");
        _localXpProgressFill = FindDescendant<XUiController>(this, "localXpProgressFill");
        _localLevelStageLabel = FindDescendant<XUiController>(this, "localLevelStageLabel");
        _localXpLabel = FindDescendant<XUiController>(this, "localXpLabel");
        _xpGainPopup = FindDescendant<XUiController>(this, "xpGainPopup");
        _commandCenterInput = FindDescendant<XUiC_TextInput>(this, "commandCenterInput");
        XUiC_SimpleButton runCommand = FindDescendant<XUiC_SimpleButton>(this, "btnCommandCenterRun");
        if (runCommand != null) runCommand.OnPressed += CommandCenterRun_OnPressed;
        XUiC_SimpleButton clearCommand = FindDescendant<XUiC_SimpleButton>(this, "btnCommandCenterClear");
        if (clearCommand != null) clearCommand.OnPressed += (sender, mouseButton) =>
        { if (mouseButton == 0) { if (_commandCenterInput != null) _commandCenterInput.Text = string.Empty; _commandCenterStatus = "Cleared."; SetAllChildrenDirty(); } };
        BindPanelCloseButton("btnCloseCommandCenter", "dev");
        BindRoleAction("btnRoleAssign", "assign");
        BindRoleAction("btnRoleRemove", "remove");
        BindRoleAction("btnRoleInspect", "inspect");
        BindKeyCapture("btnBindMenu", "OpenMenu");
        BindKeyCapture("btnBindHomes", "MyHomes");
        BindKeyCapture("btnBindTeleport", "PlayerTeleport");
        BindKeyCapture("btnBindZombies", "ZombieDirector");
        BindKeyCapture("btnBindOptions", "Options");
        BindKeyClear("btnClearMenu", "OpenMenu");
        BindKeyClear("btnClearHomes", "MyHomes");
        BindKeyClear("btnClearTeleport", "PlayerTeleport");
        BindKeyClear("btnClearZombies", "ZombieDirector");
        BindKeyClear("btnClearOptions", "Options");
        BindAppearanceControls();
        BindBackgroundSlider();
        for (int themeIndex = 0; themeIndex < cHub.Config.UIConfig.Themes.Length; themeIndex++)
            BindThemeButton("btnTheme" + (themeIndex + 1), cHub.Config.UIConfig.Themes[themeIndex]);
        BindZombiePreset("btnZombieWalker", "zombieMoe", "Walker");
        BindZombiePreset("btnZombieFeral", "zombieMoeFeral", "Feral");
        BindZombiePreset("btnZombieRadiated", "zombieMoeRadiated", "Radiated");
        BindZombiePreset("btnZombieCop", "zombieFatCop", "Cop");
        BindZombiePreset("btnZombieIon", "zombieZombiIon", "ZombiIon // c/Hub Original");
        BindZombieSpawn("btnSpawnZombie1", 1);
        BindZombieSpawn("btnSpawnZombie5", 5);
        BindZombieSpawn("btnSpawnZombie10", 10);
        _animatedTitle = FindDescendant<XUiController>(this, "chubAnimatedTitle");
        _animatedTitleGlow = FindDescendant<XUiController>(this, "chubTitleGlow");
        _animatedEmblem = FindDescendant<XUiController>(this, "chubAnimatedEmblem");
        if (_animatedTitle?.ViewComponent?.UiTransform != null)
            _titleBasePosition = _animatedTitle.ViewComponent.UiTransform.localPosition;
        if (_animatedTitleGlow?.ViewComponent?.UiTransform != null)
            _titleGlowBasePosition = _animatedTitleGlow.ViewComponent.UiTransform.localPosition;

        _searchInput = FindDescendant<XUiC_TextInput>(this, "searchCommands");
        _searchPlaceholder = FindDescendant<XUiController>(this, "searchPlaceholder");
        _playerCommandsScrollView = FindDescendant<XUiController>(this, "playerCommandsScrollView");
        if (_playerCommandsScrollView?.ViewComponent?.UiTransform != null)
        {
            _playerCommandsScrollBasePosition = Vector3.zero;
            _playerCommandsScrollView.ViewComponent.UiTransform.localPosition = Vector3.zero;
        }

        if (_searchInput != null)
        {
            _searchInput.ActiveTextColor = Color.white;
            _searchInput.CaretColor = new Color(1f, 0.22f, 0.26f, 1f);
            _searchInput.SelectionColor = new Color(0.72f, 0.04f, 0.08f, 0.62f);
            _searchInput.OnInputSelectedHandler += SearchInput_OnSelected;
            _searchInput.OnChangeHandler += SearchInput_OnChanged;
        }
        UpdateSearchVisualState();
        _adminPlayerSearchInput = FindDescendant<XUiC_TextInput>(this, "adminPlayerSearch");
        if (_adminPlayerSearchInput != null)
            _adminPlayerSearchInput.OnChangeHandler += AdminPlayerSearch_OnChanged;
        _assistantInput = FindDescendant<XUiC_TextInput>(this, "assistantInput");
        _themeCssInput = FindDescendant<XUiC_TextInput>(this, "themeCssInput");
        _backgroundValueInput = FindDescendant<XUiC_TextInput>(this, "backgroundValueInput");
        if (_backgroundValueInput != null) _backgroundValueInput.OnChangeHandler += BackgroundValue_OnChanged;
        _backgroundSliderKnob = FindDescendant<XUiController>(this, "backgroundSliderKnob");
        if (_themeCssInput != null) _themeCssInput.OnChangeHandler += (sender, text, finished) =>
        { _themeCss = text ?? string.Empty; _themeSelectAll = false; };
        XUiC_SimpleButton assistantSend = FindDescendant<XUiC_SimpleButton>(this, "btnAssistantSend");
        XUiC_SimpleButton assistantClear = FindDescendant<XUiC_SimpleButton>(this, "btnAssistantClear");
        if (assistantSend != null) assistantSend.OnPressed += AssistantSend_OnPressed;
        if (assistantClear != null) assistantClear.OnPressed += (sender, mouseButton) =>
        {
            if (mouseButton != 0) return;
            _assistantHistory = "c/Hub AI terminal cleared.";
            _assistantStatus = "READY";
            SetAllChildrenDirty();
        };
        //====== Modificari i27k ========

        bool progressionFooterReady =
            _localXpProgressFill != null &&
            _localLevelStageLabel != null &&
            _localXpLabel != null;

        StartupTerminal.ReportFeature(
            "progression.footer",
            "Progression Footer",
            progressionFooterReady,
            progressionFooterReady
                ? "Player, level, gamestage, XP and casinoCoin bindings initialized"
                : "One or more progression footer controls were not found");

        //===============================
    }

    public override void OnOpen()
    {
        base.OnOpen();
        // Footer children can be materialized after Init by XUi. Retry safely
        // on every open; stored references prevent duplicate subscriptions.
        BindFooterControls();
        SetMenuInteraction(true);
        _activePage = "menu";
        cHub.Config.UIConfig uiConfig = ServiceRegistry.Get<ConfigService>()?.Current?.UI;
        if (uiConfig != null)
        {
            _backgroundOpacity = uiConfig.BackgroundOpacity;
            _backgroundDensity = Mathf.Clamp(uiConfig.BackgroundDensity, 1.1f, 6f);
            _backgroundOpacity = Mathf.Lerp(0.12f, 1f, (_backgroundDensity - 1.1f) / 4.9f);
            _glowEnabled = uiConfig.GlowEnabled;
            _scanlinesEnabled = uiConfig.ScanlineEffect;
            _animationsEnabled = uiConfig.WaterdropAnimations;
            _themeCss = uiConfig.CustomThemeCss ?? string.Empty;
            if (_themeCssInput != null) _themeCssInput.Text = _themeCss;
            if (_backgroundValueInput != null) _backgroundValueInput.Text = _backgroundDensity.ToString("0.0", CultureInfo.InvariantCulture);
        }
        string requestedPage = AdminPanelService.ConsumeRequestedPage();
        if (!string.IsNullOrWhiteSpace(requestedPage)) _activePage = requestedPage;
        _popupProgress = 0f;
        PrimeXpObservation();
        _playerListPage = 0;
        RefreshPlayerEntries();
        _searchSelected = false;
        _searchText = _searchInput?.Text ?? string.Empty;
        if (_playerCommandsScrollView?.ViewComponent?.UiTransform != null)
            _playerCommandsScrollView.ViewComponent.UiTransform.localPosition =
                _playerCommandsScrollBasePosition;
        RefreshSearchTargetVisibility();
        UpdateSearchVisualState();
        RestoreMainPanelPosition();
        SetAllChildrenDirty();
    }

    public override void OnClose()
    {
        //====== Modificari i27k ========

        _menuInteractionEnabled = false;

        _searchSelected = false;
        _searchSelectedFrame = -1;

        if (_searchInput != null)
            _searchInput.Selected(false);

        if (_assistantInput != null)
            _assistantInput.Selected(false);

        // XUi trebuie să-și închidă singur action set-ul.
        // Nu modificăm manual hasActionSetThisOpen / bActionSetEnabled.
        base.OnClose();

        // Nu forțăm Cursor.lockState aici.
        // Următoarea fereastră vanilla poate fi deschisă în același frame.

        //===============================
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseCurrentPanel();
            return;
        }
        if (Input.GetKeyDown(KeyCode.CapsLock))
            SetMenuInteraction(!_menuInteractionEnabled);
        // UI mode owns the cursor. GAME mode deliberately leaves it alone so
        // TAB/inventory/crafting can make the cursor visible and interactive.
        if (_menuInteractionEnabled) ApplyCursorMode();
        ProcessThemeEditorShortcuts();
        UpdateBackgroundSliderVisual();
        if (_appearanceSavePending)
        {
            _appearanceSaveDelay -= deltaTime;
            if (_appearanceSaveDelay <= 0f) { _appearanceSavePending = false; SaveAppearanceNow(); }
        }
        while (GameAssistantService.TryDequeueReply(out AssistantReply reply))
        {
            if (!string.IsNullOrEmpty(_assistantRequestId) &&
                !string.Equals(reply.RequestId, _assistantRequestId, System.StringComparison.Ordinal)) continue;
            _assistantHistory += "\n\n[c/Hub AI]\n" + reply.Answer;
            _assistantStatus = "READY";
            _assistantRequestId = null;
            SetAllChildrenDirty();
        }
        if (_popupProgress < 1f && ViewComponent?.UiTransform != null)
        {
            _popupProgress = Mathf.Min(1f, _popupProgress + deltaTime * 3.2f);
            float t = _popupProgress;
            float dropEase = 1f + 2.7f * Mathf.Pow(t - 1f, 3f) +
                             1.7f * Mathf.Pow(t - 1f, 2f);
            float popupScale = Mathf.Lerp(0.70f, _targetWindowScale, dropEase);
            ViewComponent.UiTransform.localScale = new Vector3(popupScale, popupScale, 1f);
        }
        else if (ViewComponent?.UiTransform != null)
        {
            if (Mathf.Abs(_windowScale - _targetWindowScale) > 0.001f)
                _windowScale = Mathf.Lerp(_windowScale, _targetWindowScale,
                    1f - Mathf.Exp(-deltaTime * 18f));
            else
                _windowScale = _targetWindowScale;

            // XUi can rebuild a dirty view and restore the XML scale to 1.
            // Reapply the persisted window scale every frame, not only while
            // the easing value is changing.
            ViewComponent.UiTransform.localScale =
                new Vector3(_windowScale, _windowScale, 1f);
        }

        if (_activeFloatingPanel?.ViewComponent?.UiTransform != null)
        {
            string panelId = _activeFloatingPanel.ViewComponent.ID;
            float target = _floatingPanelScales.TryGetValue(panelId, out float savedScale)
                ? savedScale : 1f;
            float nextScale;
            if (_floatingPopupProgress < 1f)
            {
                _floatingPopupProgress = Mathf.Min(1f,
                    _floatingPopupProgress + deltaTime * 4.2f);
                float t = _floatingPopupProgress;
                float dropEase = 1f + 2.7f * Mathf.Pow(t - 1f, 3f) +
                                 1.7f * Mathf.Pow(t - 1f, 2f);
                nextScale = Mathf.Lerp(0.76f, target, dropEase);
            }
            else
            {
                float current = _activeFloatingPanel.ViewComponent.UiTransform.localScale.x;
                nextScale = Mathf.Lerp(current, target, 1f - Mathf.Exp(-deltaTime * 18f));
            }
            _activeFloatingPanel.ViewComponent.UiTransform.localScale =
                new Vector3(nextScale, nextScale, 1f);
        }
        _titleAnimationTime += _animationsEnabled ? deltaTime : 0f;
        ReleaseSearchFocusWhenClickingPanel();
        if (_searchInput != null)
        {
            string liveSearchText = _searchInput.Text ?? string.Empty;
            if (!string.Equals(_searchText, liveSearchText, System.StringComparison.Ordinal))
            {
                _searchText = liveSearchText;
                RefreshSearchTargetVisibility();
                SetAllChildrenDirty();
            }
            // Keep this after the XUi binding pass: the placeholder is a purely
            // local visual and must react before editing is committed with ESC.
            UpdateSearchVisualState();
        }
        CapturePendingKeybind();
        _footerBindRetry += deltaTime;
        if (_footerBindRetry >= 0.5f)
        {
            _footerBindRetry = 0f;
            BindFooterControls();
        }
        _liveFooterRefreshTimer += deltaTime;
        if (_liveFooterRefreshTimer >= 0.25f)
        {
            _liveFooterRefreshTimer = 0f;
            UpdateLiveXpBar();
            DetectXpGain();
            SetAllChildrenDirty();
        }
        UpdateXpGainPopup(deltaTime);
        _playerRefreshTimer += deltaTime;
        if (_playerRefreshTimer >= 1f &&
            (IsPage("playerlist") || IsPage("teleport") || IsPage("party")))
        {
            _playerRefreshTimer = 0f;
            RefreshPlayerEntries();
            if (IsPage("party")) PartyService.SynchronizeAcceptedVanillaMembers(GetLocalPlayerId());
            SetAllChildrenDirty();
        }

        float emblemPulse = (Mathf.Sin(_titleAnimationTime * 2.15f + 0.8f) + 1f) * 0.5f;

        if (_animatedTitle?.ViewComponent?.UiTransform != null)
        {
            // Independent cinematic motion: a slow, asymmetric float with a
            // restrained horizontal stretch. It deliberately does not mirror
            // the emblem pulse.
            float driftX = Mathf.Sin(_titleAnimationTime * 0.58f) * 2.2f +
                           Mathf.Sin(_titleAnimationTime * 0.19f + 1.4f) * 0.9f;
            float driftY = Mathf.Sin(_titleAnimationTime * 0.73f + 0.6f) * 0.65f;
            float breathe = (Mathf.Sin(_titleAnimationTime * 0.82f - 0.4f) + 1f) * 0.5f;
            _animatedTitle.ViewComponent.UiTransform.localPosition =
                _titleBasePosition + new Vector3(driftX, driftY, 0f);
            _animatedTitle.ViewComponent.UiTransform.localScale =
                new Vector3(1f + breathe * 0.009f, 1f - breathe * 0.003f, 1f);
            XUiV_Label titleView = _animatedTitle.ViewComponent as XUiV_Label;
            if (titleView != null)
            {
                float shimmer = Mathf.Pow((Mathf.Sin(_titleAnimationTime * 0.46f) + 1f) * 0.5f, 3f);
                titleView.Color = new Color(
                    1f,
                    0.80f + shimmer * 0.16f,
                    0.80f + shimmer * 0.13f,
                    1f);
            }
        }

        if (_animatedTitleGlow?.ViewComponent?.UiTransform != null)
        {
            float wave = (Mathf.Sin(_titleAnimationTime * 0.67f + 2.1f) + 1f) * 0.5f;
            float sweep = Mathf.Sin(_titleAnimationTime * 0.31f) * 3.5f;
            _animatedTitleGlow.ViewComponent.UiTransform.localPosition =
                _titleGlowBasePosition + new Vector3(sweep, 0f, 0f);
            float glowScale = 1.012f + wave * 0.026f;
            _animatedTitleGlow.ViewComponent.UiTransform.localScale =
                new Vector3(glowScale, 1f + wave * 0.014f, 1f);
            XUiV_Label glowView = _animatedTitleGlow.ViewComponent as XUiV_Label;
            if (glowView != null)
                glowView.Color = _glowEnabled
                    ? new Color(1f, 0.08f, 0.12f, 0.07f + wave * 0.14f)
                    : new Color(1f, 0.08f, 0.12f, 0f);
        }

        if (_animatedEmblem?.ViewComponent?.UiTransform != null)
        {
            float scale = 0.98f + emblemPulse * 0.07f;
            _animatedEmblem.ViewComponent.UiTransform.localScale =
                new Vector3(scale, scale, 1f);
        }
    }

    public override bool GetBindingValueInternal(
        ref string value,
        string bindingName)
    {
        switch (bindingName)
        {
            case "chub_search_placeholder":
                value = (!_searchSelected && string.IsNullOrEmpty(_searchText))
                    .ToString().ToLowerInvariant();
                return true;

            case "chub_search_active":
                value = _searchSelected.ToString().ToLowerInvariant();
                return true;

            case "chub_tab_player_active":
                value = (IsPage("menu") || IsPage("teleport") || IsPage("playerlist") ||
                         IsPage("homes") || IsPage("options") || IsPage("rot") ||
                         IsPage("settings") || IsPage("themeeditor") || IsPage("toprot") || IsPage("placeholder"))
                    .ToString().ToLowerInvariant();
                return true;
            case "chub_tab_staff_active":
                value = (IsPage("staff") || IsPage("moderator") || IsPage("administrator") ||
                         (IsPage("teleport") && HasPermission(PermissionIds.TeleportPlayers)) ||
                         IsPage("zombies")).ToString().ToLowerInvariant();
                return true;
            case "chub_tab_dev_active":
                value = (IsPage("dev") || IsPage("rolemanager")).ToString().ToLowerInvariant();
                return true;

            case "chub_page_menu":
                value = (IsPage("menu") || IsPage("teleport") || IsPage("homes") ||
                         IsPage("playerlist") || IsPage("assistant"))
                    .ToString().ToLowerInvariant();
                return true;

            case "chub_page_staff":
                value = IsPage("staff").ToString().ToLowerInvariant();
                return true;

            case "chub_page_moderator":
                value = IsPage("moderator").ToString().ToLowerInvariant();
                return true;

            case "chub_page_administrator":
                value = IsPage("administrator").ToString().ToLowerInvariant();
                return true;

            case "chub_page_dev":
                value = (IsPage("dev") || IsPage("rolemanager")).ToString().ToLowerInvariant();
                return true;

            case "chub_page_rolemanager":
                value = IsPage("rolemanager").ToString().ToLowerInvariant();
                return true;
            case "chub_page_options":
                value = IsPage("options").ToString().ToLowerInvariant(); return true;
            case "chub_page_settings": value = IsPage("settings").ToString().ToLowerInvariant(); return true;
            case "chub_page_themeeditor": value = IsPage("themeeditor").ToString().ToLowerInvariant(); return true;
            case "chub_page_zombies":
                value = IsPage("zombies").ToString().ToLowerInvariant(); return true;
            case "chub_page_rot": value = IsPage("rot").ToString().ToLowerInvariant(); return true;
            case "chub_page_toprot": value = IsPage("toprot").ToString().ToLowerInvariant(); return true;
            case "chub_page_placeholder": value = IsPage("placeholder").ToString().ToLowerInvariant(); return true;
            case "chub_page_assistant": value = IsPage("assistant").ToString().ToLowerInvariant(); return true;
            case "chub_page_commandcenter": value = IsPage("commandcenter").ToString().ToLowerInvariant(); return true;
            case "chub_page_devtools": value = IsPage("devtools").ToString().ToLowerInvariant(); return true;
            case "chub_page_party": value = IsPage("party").ToString().ToLowerInvariant(); return true;
            case "chub_party_status": value = _partyStatus; return true;
            case "chub_party_selected": value = GetPartySelectedName(); return true;
            case "chub_devtools_project": value = (_devToolsCategory == "project").ToString().ToLowerInvariant(); return true;
            case "chub_devtools_xui": value = (_devToolsCategory == "xui").ToString().ToLowerInvariant(); return true;
            case "chub_devtools_data": value = (_devToolsCategory == "data").ToString().ToLowerInvariant(); return true;
            case "chub_devtools_build": value = (_devToolsCategory == "build").ToString().ToLowerInvariant(); return true;
            case "chub_devtools_logs": value = (_devToolsCategory == "logs").ToString().ToLowerInvariant(); return true;
            case "chub_devtools_ai": value = (_devToolsCategory == "ai").ToString().ToLowerInvariant(); return true;
            case "chub_devtools_status": value = _devToolsStatus; return true;
            case "chub_command_center_status": value = _commandCenterStatus; return true;
            case "chub_command_center_list": value = GetCommandCenterList(); return true;
            case "chub_assistant_history": value = _assistantHistory; return true;
            case "chub_assistant_status": value = _assistantStatus; return true;
            case "chub_interaction_mode": value = _menuInteractionEnabled ? "UI" : "GAME"; return true;
            case "chub_placeholder_title": value = _placeholderTitle; return true;
            case "chub_placeholder_message": value = _placeholderMessage; return true;
            case "chub_discord_popup": value = _discordPopupVisible.ToString().ToLowerInvariant(); return true;
            case "chub_discord_url": value = DiscordInviteUrl; return true;
            case "chub_discord_status": value = _discordStatus; return true;
            case "chub_rot_score": value = (AdminPanelService.ClientRotSnapshot?.Own?.Rot ?? 0).ToString("N0"); return true;
            case "chub_rot_kills": value = (AdminPanelService.ClientRotSnapshot?.Own?.ZombieKills ?? 0).ToString("N0"); return true;
            case "chub_rot_drops": value = (AdminPanelService.ClientRotSnapshot?.Own?.FleshDrops ?? 0).ToString("N0"); return true;
            case "chub_rot_next":
                long kills = AdminPanelService.ClientRotSnapshot?.Own?.ZombieKills ?? 0;
                value = (10 - kills % 10).ToString(); return true;
            case "chub_toprot_list":
                var top = AdminPanelService.ClientRotSnapshot?.Top ?? new List<RotRecord>();
                value = top.Count == 0 ? "No Rot scores recorded yet." : string.Join("\n", top.Take(12).Select((r, i) =>
                    $"#{i + 1:00}  {r.Name,-18}  {r.Rot:N0} ROT")); return true;
            case "chub_options_status":
                value = _optionsStatus; return true;
            case "chub_current_theme":
                value = ServiceRegistry.Get<ConfigService>()?.Current?.UI?.Theme ?? "Blood Moon"; return true;
            case "chub_background_alpha": value = _backgroundDensity.ToString("0.0", CultureInfo.InvariantCulture); return true;
            case "chub_background_color": value = "4,0,1," + Mathf.RoundToInt(_backgroundOpacity * 255f); return true;
            case "chub_surface_color": value = "255,255,255," + Mathf.RoundToInt(_backgroundOpacity * 255f); return true;
            case "chub_glow_checkbox": value = _glowEnabled ? "[X]" : "[ ]"; return true;
            case "chub_scanlines_checkbox": value = _scanlinesEnabled ? "[X]" : "[ ]"; return true;
            case "chub_animations_checkbox": value = _animationsEnabled ? "[X]" : "[ ]"; return true;
            case "chub_scanlines_enabled": value = _scanlinesEnabled.ToString().ToLowerInvariant(); return true;
            case "chub_theme_editor_status": value = _themeEditorStatus; return true;
            case "chub_accent":
                value = HexToXuiColor(ServiceRegistry.Get<ConfigService>()?.Current?.UI?.AccentColor); return true;
            case "chub_zombie_status":
                value = _zombieDirectorStatus; return true;

            case "chub_page_teleport":
                value = IsPage("teleport").ToString().ToLowerInvariant();
                return true;

            case "chub_page_playerlist":
                value = IsPage("playerlist").ToString().ToLowerInvariant();
                return true;

            case "chub_page_homes":
                value = IsPage("homes").ToString().ToLowerInvariant();
                return true;

            case "chub_home_status":
                value = _homeStatus;
                return true;

            case "chub_home_cooldown":
                value = (ServiceRegistry.Get<AdminPanelService>()?.GetHomeCooldownSeconds() ?? 60).ToString();
                return true;

            case "chub_page_sidepanel":
                value = (IsPage("teleport") || IsPage("playerlist") || IsPage("rolemanager") || IsPage("assistant"))
                    .ToString().ToLowerInvariant();
                return true;

            case "chub_role_manager_status":
                value = _roleManagerStatus;
                return true;

            case "chub_is_staff":
                value = HasStaffAccess().ToString().ToLowerInvariant();
                return true;

            case "chub_is_admin":
                value = HasPermission(PermissionIds.AdminPanelManage)
                    .ToString().ToLowerInvariant();
                return true;

            case "chub_is_owner":
                value = HasPermission(PermissionIds.Owner)
                    .ToString().ToLowerInvariant();
                return true;

            case "chub_can_player_teleport":
                value = HasPermission(PermissionIds.TeleportToPlayer)
                    .ToString().ToLowerInvariant();
                return true;

            case "chub_can_marketplace":
                value = HasPermission(PermissionIds.MarketplaceUse)
                    .ToString().ToLowerInvariant();
                return true;

            case "chub_can_city_teleport":
                value = HasPermission(PermissionIds.TeleportLocations)
                    .ToString().ToLowerInvariant();
                return true;

            case "chub_can_economy":
                value = HasPermission(PermissionIds.EconomyView)
                    .ToString().ToLowerInvariant();
                return true;

            case "chub_can_daily_rewards":
                value = HasPermission(PermissionIds.DailyRewardsView)
                    .ToString().ToLowerInvariant();
                return true;

            case "chub_can_leaderboards":
                value = HasPermission(PermissionIds.LeaderboardsView)
                    .ToString().ToLowerInvariant();
                return true;

            case "chub_can_players_view":
                value = HasPermission(PermissionIds.PlayersView)
                    .ToString().ToLowerInvariant();
                return true;

            case "chub_show_player_list":
                value = (HasPermission(PermissionIds.TeleportToPlayer) && MatchesCommandSearch("Player List Teleport Player tp teleportare jucator jucător"))
                    .ToString().ToLowerInvariant(); return true;
            case "chub_show_shop":
                value = (HasPermission(PermissionIds.MarketplaceUse) && MatchesCommandSearch("Shop Marketplace"))
                    .ToString().ToLowerInvariant(); return true;
            case "chub_show_city_teleport":
                value = (HasPermission(PermissionIds.TeleportLocations) && MatchesCommandSearch("City Teleport"))
                    .ToString().ToLowerInvariant(); return true;
            case "chub_show_player_teleport":
                value = (HasPermission(PermissionIds.TeleportToPlayer) && MatchesCommandSearch("Player Teleport tp teleportare jucator jucător"))
                    .ToString().ToLowerInvariant(); return true;
            case "chub_show_daily_rewards":
                value = (HasPermission(PermissionIds.DailyRewardsView) && MatchesCommandSearch("Daily Rewards"))
                    .ToString().ToLowerInvariant(); return true;
            case "chub_show_economy":
                value = (HasPermission(PermissionIds.EconomyView) && MatchesCommandSearch("Economy Balance"))
                    .ToString().ToLowerInvariant(); return true;
            case "chub_show_homes":
                value = (HasPermission(PermissionIds.TeleportSelf) && MatchesCommandSearch("My Homes casa casă case casele mele acasa acasă locuinta locuință"))
                    .ToString().ToLowerInvariant(); return true;
            case "chub_show_marketplace":
                value = (HasPermission(PermissionIds.MarketplaceUse) && MatchesCommandSearch("Marketplace"))
                    .ToString().ToLowerInvariant(); return true;
            case "chub_show_vehicles":
                value = MatchesCommandSearch("Vehicle Spawner").ToString().ToLowerInvariant(); return true;
            case "chub_show_quests":
                value = MatchesCommandSearch("Quests misiune misiuni obiective sarcini").ToString().ToLowerInvariant(); return true;
            case "chub_show_settings":
                value = MatchesCommandSearch("Settings Options setari setări optiuni opțiuni configurare configurari configurări").ToString().ToLowerInvariant(); return true;
            case "chub_show_alt_systems":
                value = MatchesCommandSearch("Alt Systems").ToString().ToLowerInvariant(); return true;

            case "chub_player_list_page":
                int pageCount = GetPlayerListPageCount();
                value = $"{_playerListPage + 1}/{pageCount}";
                return true;

            case "chub_player_teleport_status":
                value = _playerTeleportStatus;
                return true;

            case "chub_admin_source":
                value = GetOnlinePlayerName(_adminTeleportSourceId, "Select source player");
                return true;

            case "chub_admin_target":
                value = GetOnlinePlayerName(_adminTeleportTargetId, "Select target player");
                return true;

            case "chub_admin_selecting":
                value = _selectingAdminSource ? "Selecting SOURCE" : "Selecting TARGET";
                return true;

            case "chub_admin_teleport_status":
                value = _adminTeleportStatus;
                return true;

            case "chub_known_players":
                PlayerIdentityService playerService =
                    ServiceRegistry.Get<PlayerIdentityService>();

                value = playerService == null
                    ? "0"
                    : playerService.GetPlayers().Count.ToString();
                return true;

            case "chub_local_player":
                value = xui?.playerUI?.entityPlayer?.EntityName ?? "Player";
                return true;

            case "chub_local_level":
                EntityPlayerLocal localPlayer = xui?.playerUI?.entityPlayer;
                value = localPlayer?.Progression == null
                    ? "1"
                    : localPlayer.Progression.Level.ToString();
                return true;

            case "chub_local_gamestage":
                value = GetLocalGameStage().ToString(CultureInfo.InvariantCulture);
                return true;

            case "chub_local_xp":
                GetLocalXp(out int currentXp, out int requiredXp, out _);
                value = currentXp.ToString("N0", CultureInfo.InvariantCulture) + "/" +
                        requiredXp.ToString("N0", CultureInfo.InvariantCulture) + " XP";
                return true;

            case "chub_local_xp_width":
                GetLocalXp(out _, out _, out float xpProgress);
                value = Mathf.RoundToInt(188f * xpProgress).ToString(CultureInfo.InvariantCulture);
                return true;

            case "chub_xp_gain_visible":
                value = (_xpGainTimer > 0f && IsPage("menu")).ToString().ToLowerInvariant();
                return true;

            case "chub_xp_gain_text":
                value = "+" + _xpGainAmount.ToString("N0", CultureInfo.InvariantCulture) + " XP";
                return true;

            case "chub_economy_target":
                value = GetOnlinePlayerName(_economyTargetId, "SELECT ONLINE PLAYER");
                return true;

            case "chub_economy_target_balance":
                EntityPlayer economyPlayer = ResolveOnlineEntity(_economyTargetId);
                value = economyPlayer == null ? "--" : GetCurrencyBalance(economyPlayer).ToString("N0", CultureInfo.InvariantCulture);
                return true;

            case "chub_economy_status":
                value = _economyStatus;
                return true;

            case "chub_currency":
                value = GetCurrencyBalance().ToString(
                    "N0", CultureInfo.InvariantCulture);
                return true;

            case "chub_player_1_name":
                value = GetKnownPlayerName(0);
                return true;

            case "chub_player_2_name":
                value = GetKnownPlayerName(1);
                return true;

            case "chub_player_3_name":
                value = GetKnownPlayerName(2);
                return true;

            case "chub_player_4_name":
                value = GetKnownPlayerName(3);
                return true;

            case "chub_roles":
                RoleService roleService =
                    ServiceRegistry.Get<RoleService>();

                value = roleService == null
                    ? "0"
                    : roleService.GetRoles().Count().ToString();
                return true;

            case "chub_permissions":
                PermissionService permissionService =
                    ServiceRegistry.Get<PermissionService>();

                value = permissionService == null
                    ? "0"
                    : permissionService.GetRegisteredPermissions().Count().ToString();
                return true;
        }

        if (TryGetPlayerListBinding(bindingName, out string listValue))
        {
            value = listValue;
            return true;
        }

        if (TryGetPartyBinding(bindingName, out string partyValue))
        {
            value = partyValue;
            return true;
        }

        if (TryGetHomeBinding(bindingName, out string homeValue))
        {
            value = homeValue;
            return true;
        }

        if (TryGetAdminCandidateBinding(bindingName, out string candidateValue))
        {
            value = candidateValue;
            return true;
        }

        if (bindingName.StartsWith("chub_keybind_", System.StringComparison.OrdinalIgnoreCase))
        {
            string action = bindingName.Substring("chub_keybind_".Length);
            Dictionary<string, string> keys = ServiceRegistry.Get<ConfigService>()?.Current?.UI?.Keybinds;
            value = keys != null && keys.TryGetValue(action, out string binding) && !string.IsNullOrWhiteSpace(binding)
                ? binding : "Unbound";
            return true;
        }

        return base.GetBindingValueInternal(
            ref value,
            bindingName
        );
    }

    private void SearchInput_OnSelected(XUiController sender, bool selected)
    {
        _searchSelected = selected;
        _searchSelectedFrame = selected ? Time.frameCount : -1;
        UpdateSearchVisualState();
        SetAllChildrenDirty();
    }

    private void SearchInput_OnChanged(
        XUiController sender,
        string text,
        bool finished)
    {
        _searchText = text ?? string.Empty;
        RefreshSearchTargetVisibility();
        UpdateSearchVisualState();
        SetAllChildrenDirty();
    }

    private void UpdateSearchVisualState()
    {
        if (_searchPlaceholder?.ViewComponent != null)
            _searchPlaceholder.ViewComponent.IsVisible =
                !_searchSelected && string.IsNullOrEmpty(_searchText);
    }

    private void ReleaseSearchFocusWhenClickingPanel()
    {
        if (!_searchSelected || !Input.GetMouseButtonDown(0) ||
            Time.frameCount <= _searchSelectedFrame) return;

        _searchInput.Selected(false);
        _searchSelected = false;
        _searchSelectedFrame = -1;
        UpdateSearchVisualState();
        SetAllChildrenDirty();
    }

    private void AdminPlayerSearch_OnChanged(XUiController sender, string text, bool finished)
    {
        _adminPlayerSearchText = text?.Trim() ?? string.Empty;
        SetAllChildrenDirty();
    }

    private bool MatchesCommandSearch(string command)
    {
        if (string.IsNullOrWhiteSpace(_searchText)) return true;
        string[] terms = _searchText.Split(
            new[] { ';', ',', '|', '\t', '\r', '\n', ' ' },
            System.StringSplitOptions.RemoveEmptyEntries);
        return terms.Length == 0 || terms.Any(term =>
            command.IndexOf(term.Trim(), System.StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private void RefreshSearchTargetVisibility()
    {
        SetSearchTargetVisibility("btnPlayerList",
            HasPermission(PermissionIds.TeleportToPlayer),
            "Teleport Player Player List tp teleportare jucator jucător");
        SetSearchTargetVisibility("btnShop",
            HasPermission(PermissionIds.MarketplaceUse), "Shop Marketplace");
        SetSearchTargetVisibility("btnCityTeleport",
            HasPermission(PermissionIds.TeleportLocations), "City Teleport");
        SetSearchTargetVisibility("btnPlayerTeleport",
            HasPermission(PermissionIds.TeleportToPlayer),
            "Player Teleport tp teleportare jucator jucător");
        SetSearchTargetVisibility("btnDailyRewards",
            HasPermission(PermissionIds.DailyRewardsView), "Daily Rewards");
        SetSearchTargetVisibility("btnEconomy",
            HasPermission(PermissionIds.EconomyView), "Economy Balance");
        SetSearchTargetVisibility("btnHomes",
            HasPermission(PermissionIds.TeleportSelf),
            "Home Homes My Homes casa casă case casele mele acasa acasă locuinta locuință");
        SetSearchTargetVisibility("btnVehicles", true, "Vehicle Spawner Vehicles");
        SetSearchTargetVisibility("btnQuests", true,
            "Quest Quests misiune misiuni obiective sarcini");
        SetSearchTargetVisibility("btnMarketplace",
            HasPermission(PermissionIds.MarketplaceUse), "Marketplace Shop");
        SetSearchTargetVisibility("btnSettings", true,
            "Setting Settings Options setari setări optiuni opțiuni configurare configurari configurări");
        SetSearchTargetVisibility("btnAltSystems", true, "Alt Systems");

        bool showOther = string.IsNullOrWhiteSpace(_searchText) ||
                         MatchesCommandSearch("Alt Systems More Coming Soon");
        SetControllerVisibility("otherSystemsLabel", showOther);
        SetControllerVisibility("otherSystemsLine", showOther);
        SetControllerVisibility("btnTopRot", showOther);
        ReflowPlayerCommandButtons();
    }

    private void ReflowPlayerCommandButtons()
    {
        string[] ids =
        {
            "btnPlayerList", "btnShop", "btnCityTeleport", "btnPlayerTeleport",
            "btnDailyRewards", "btnEconomy", "btnHomes", "btnVehicles",
            "btnQuests", "btnMarketplace", "btnSettings"
        };
        int visibleIndex = 0;
        foreach (string id in ids)
        {
            XUiController controller = FindDescendant<XUiController>(this, id);
            if (controller?.ViewComponent == null || !controller.ViewComponent.IsVisible) continue;
            Transform transform = controller.ViewComponent.UiTransform;
            if (transform == null) continue;
            Vector3 position = transform.localPosition;
            position.x = visibleIndex % 2 == 0 ? 14f : 266f;
            position.y = -52f - (visibleIndex / 2) * 48f;
            transform.localPosition = position;
            visibleIndex++;
        }
        if (_playerCommandsScrollView?.ViewComponent?.UiTransform != null)
            _playerCommandsScrollView.ViewComponent.UiTransform.localPosition =
                _playerCommandsScrollBasePosition;
    }

    private void SetSearchTargetVisibility(string id, bool allowed, string keywords)
    {
        SetControllerVisibility(id, allowed && MatchesCommandSearch(keywords));
    }

    private void SetControllerVisibility(string id, bool visible)
    {
        XUiController controller = FindDescendant<XUiController>(this, id);
        if (controller?.ViewComponent != null)
            controller.ViewComponent.IsVisible = visible;
    }

    private static string HexToXuiColor(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return "239,57,67,255";
        string value = hex.Trim().TrimStart('#');
        if (value.Length != 6 || !int.TryParse(value, System.Globalization.NumberStyles.HexNumber,
            CultureInfo.InvariantCulture, out int rgb)) return "239,57,67,255";
        return $"{(rgb >> 16) & 255},{(rgb >> 8) & 255},{rgb & 255},255";
    }

    private void BindPageButton(string id, string page)
    {
        XUiC_SimpleButton button = FindDescendant<XUiC_SimpleButton>(this, id);

        if (button != null)
        {
            cHub.Shared.Utils.Logger.Info($"[AdminPanel.UI] Bound '{id}' -> '{page}'.");
            button.OnPressed += (sender, mouseButton) =>
            {
                if (mouseButton != 0)
                {
                    return;
                }

                cHub.Shared.Utils.Logger.Info($"[AdminPanel.UI] Pressed '{id}' -> '{page}'.");
                SetActivePage(page);
            };
        }
        else
        {
            cHub.Shared.Utils.Logger.Warning($"[AdminPanel.UI] Button '{id}' was not found.");
        }

    }

    private void BindPlayerMenuTab()
    {
        XUiC_SimpleButton button = FindDescendant<XUiC_SimpleButton>(this, "tabPlayer");
        if (button == null)
        {
            cHub.Shared.Utils.Logger.Warning("[AdminPanel.UI] Player Menu tab was not found.");
            return;
        }

        button.OnPressed += (sender, mouseButton) =>
        {
            if (mouseButton != 0) return;
            if (_searchInput != null) _searchInput.Text = string.Empty;
            _searchText = string.Empty;
            _searchSelected = false;
            _searchSelectedFrame = -1;
            RefreshSearchTargetVisibility();
            UpdateSearchVisualState();
            SetActivePage("menu");
        };
    }

    private void BindPlaceholderButton(string id, string title)
    {
        XUiC_SimpleButton button = FindDescendant<XUiC_SimpleButton>(this, id);
        if (button == null)
        {
            cHub.Shared.Utils.Logger.Warning($"[AdminPanel.UI] Button '{id}' was not found.");
            return;
        }
        button.OnPressed += (sender, mouseButton) =>
        {
            if (mouseButton != 0) return;
            _placeholderTitle = title;
            _placeholderMessage = title + " is registered in c/Hub. Its gameplay backend is coming next.";
            SetActivePage("placeholder");
        };
    }

    private void SetMenuInteraction(bool enabled)
    {
        _menuInteractionEnabled = enabled;

        if (!enabled)
        {
            if (_searchInput != null)
                _searchInput.Selected(false);

            if (_assistantInput != null)
                _assistantInput.Selected(false);

            if (_adminPlayerSearchInput != null)
                _adminPlayerSearchInput.Selected(false);

            if (_homeNameInput != null)
                _homeNameInput.Selected(false);

            if (_homeCooldownInput != null)
                _homeCooldownInput.Selected(false);

            if (_rolePlayerInput != null)
                _rolePlayerInput.Selected(false);

            if (_roleIdInput != null)
                _roleIdInput.Selected(false);

            if (_economyAmountInput != null)
                _economyAmountInput.Selected(false);

            if (_commandCenterInput != null)
                _commandCenterInput.Selected(false);

            if (_themeCssInput != null)
                _themeCssInput.Selected(false);

            if (_backgroundValueInput != null)
                _backgroundValueInput.Selected(false);

            _searchSelected = false;
            _searchSelectedFrame = -1;
        }

        //====== Modificari i27k ========

        try
        {
            object group = WindowGroup;

            if (group != null)
            {
                System.Type type = group.GetType();

                SetBooleanMember(
                    type,
                    group,
                    "isModal",
                    enabled);

                SetBooleanMember(
                    type,
                    group,
                    "isInputActive",
                    enabled);

                // Never write bActionSetEnabled/hasActionSetThisOpen. They are
                // stack bookkeeping owned by GUIWindowManager. Use its native
                // Push/Pop path so opening from Crafting behaves exactly like
                // opening from a vanilla XUi window.
                SwitchNativeActionSet(enabled);
            }
        }
        catch (System.Exception ex)
        {
            cHub.Shared.Utils.Logger.Warning(
                "[AdminPanel.UI] Could not switch interaction state: " +
                ex.Message);
        }

        //===============================

        ApplyCursorMode();
        SetAllChildrenDirty();
    }

    private void SwitchNativeActionSet(bool enabled)
    {
        try
        {
            object group = WindowGroup;
            object manager = xui?.playerUI?.windowManager;
            if (group == null || manager == null) return;

            const System.Reflection.BindingFlags flags =
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance;

            System.Reflection.FieldInfo stateField =
                group.GetType().BaseType?.GetField("bActionSetEnabled", flags) ??
                group.GetType().GetField("bActionSetEnabled", flags);
            bool currentlyEnabled = stateField != null &&
                stateField.FieldType == typeof(bool) &&
                (bool)stateField.GetValue(group);
            if (currentlyEnabled == enabled) return;

            string methodName = enabled
                ? "EnableWindowActionSet"
                : "DisableWindowActionSet";
            System.Reflection.MethodInfo method = manager.GetType().GetMethod(
                methodName, flags, null, new[] { group.GetType().BaseType }, null);
            if (method == null)
            {
                foreach (System.Reflection.MethodInfo candidate in
                    manager.GetType().GetMethods(flags))
                {
                    System.Reflection.ParameterInfo[] parameters = candidate.GetParameters();
                    if (candidate.Name == methodName && parameters.Length == 1 &&
                        parameters[0].ParameterType.IsInstanceOfType(group))
                    {
                        method = candidate;
                        break;
                    }
                }
            }
            if (method == null)
                throw new System.MissingMethodException(manager.GetType().FullName, methodName);

            method.Invoke(manager, new[] { group });
            StartupTerminal.Trace("UI", "ACTION_SET",
                "window=chubAdminPanel enabled=" + enabled + " native=true");
        }
        catch (System.Exception ex)
        {
            cHub.Shared.Utils.Logger.Warning(
                "[AdminPanel.UI] Native action-set switch failed: " + ex.Message);
        }
    }

    private static void SetBooleanMember(System.Type type, object instance, string name, bool value)
    {
        const System.Reflection.BindingFlags flags =
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.IgnoreCase;

        System.Reflection.PropertyInfo property = type.GetProperty(name, flags);
        if (property != null && property.CanWrite && property.PropertyType == typeof(bool))
        {
            property.SetValue(instance, value, null);
            return;
        }

        System.Reflection.FieldInfo field = type.GetField(name, flags);
        if (field != null && field.FieldType == typeof(bool)) field.SetValue(instance, value);
    }

    private void ApplyCursorMode()
    {
        Cursor.visible = _menuInteractionEnabled;
        Cursor.lockState = _menuInteractionEnabled ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private void CloseCurrentPanel()
    {
        if (xui?.playerUI?.windowManager == null || WindowGroup == null) return;
        xui.playerUI.windowManager.Close(WindowGroup.Id);
    }

    private bool IsPointerOverWindow()
    {
        try
        {
            object view = ViewComponent;
            if (view == null) return false;
            System.Type type = view.GetType();
            foreach (string memberName in new[] { "MouseOver", "IsMouseOver" })
            {
                System.Reflection.PropertyInfo property = type.GetProperty(memberName,
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);
                if (property != null && property.PropertyType == typeof(bool))
                    return (bool)property.GetValue(view, null);
                System.Reflection.FieldInfo field = type.GetField(memberName,
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);
                if (field != null && field.FieldType == typeof(bool)) return (bool)field.GetValue(view);
            }
        }
        catch { }
        // If a particular XUi build does not expose hover state, do not
        // accidentally disable interaction on clicks inside the window.
        return true;
    }

    private void AssistantSend_OnPressed(XUiController sender, int mouseButton)
    {
        if (mouseButton != 0 || _assistantStatus == "THINKING") return;
        string question = (_assistantInput?.Text ?? string.Empty).Trim();
        if (question.Length == 0)
        {
            _assistantStatus = "TYPE A QUESTION";
            SetAllChildrenDirty();
            return;
        }
        if (question.Length > GameAssistantService.MaxQuestionLength)
            question = question.Substring(0, GameAssistantService.MaxQuestionLength);
        _assistantRequestId = System.Guid.NewGuid().ToString("N");
        _assistantHistory += "\n\n[YOU]\n" + question;
        _assistantStatus = "THINKING";
        if (_assistantInput != null) _assistantInput.Text = string.Empty;
        string playerId = GetLocalPlayerId();
        string playerName = "Survivor";
        try { playerName = GameManager.Instance.World.GetPrimaryPlayer()?.EntityName ?? playerName; } catch { }
        if (ConnectionManager.Instance != null && ConnectionManager.Instance.IsServer)
            AskAssistantLocally(playerId, playerName, question, _assistantRequestId);
        else
            ConnectionManager.Instance?.SendToServer(NetPackageManager.GetPackage<NetPackageGameAssistant>()
                .SetupRequest(_assistantRequestId, playerId, playerName, question));
        SetAllChildrenDirty();
    }

    private static async void AskAssistantLocally(string playerId, string playerName,
        string question, string requestId)
    {
        if (!GameAssistantService.TryBeginRequest(playerId, out string error))
        {
            GameAssistantService.EnqueueClientReply(requestId, error);
            return;
        }
        string answer = await GameAssistantService.AskAsync(playerId, playerName, question);
        GameAssistantService.EnqueueClientReply(requestId, answer);
    }

    private void BindHomeButton(string id, int index)
    {
        XUiC_SimpleButton button = FindDescendant<XUiC_SimpleButton>(this, id);
        if (button == null) return;
        button.OnPressed += (sender, mouseButton) =>
        {
            if (mouseButton != 0) return;
            AdminPanelService service = ServiceRegistry.Get<AdminPanelService>();
            string playerId = GetLocalPlayerId();
            if (service == null || !service.TryTeleportHome(playerId, index,
                xui?.playerUI?.entityPlayer, out _homeStatus))
            {
                SetAllChildrenDirty();
                return;
            }
            _homeStatus = "Teleported home.";
            SetAllChildrenDirty();
        };
    }

    private void BindRoleAction(string id, string action)
    {
        XUiC_SimpleButton button = FindDescendant<XUiC_SimpleButton>(this, id);
        if (button == null) return;
        button.OnPressed += (sender, mouseButton) =>
        {
            if (mouseButton != 0 || !HasPermission(PermissionIds.Owner)) return;
            RoleService roles = ServiceRegistry.Get<RoleService>();
            PlayerIdentityService players = ServiceRegistry.Get<PlayerIdentityService>();
            string query = _rolePlayerInput?.Text?.Trim();
            PlayerIdentityRecord player = players?.GetPlayers().FirstOrDefault(item =>
                string.Equals(item.PlayerId, query, System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(item.Name, query, System.StringComparison.OrdinalIgnoreCase));
            if (roles == null || player == null)
            {
                _roleManagerStatus = "Player not found. Use the exact name or persistent ID.";
            }
            else if (action == "assign")
            {
                string roleId = _roleIdInput?.Text?.Trim();
                _roleManagerStatus = roles.AssignRole(player.PlayerId, roleId)
                    ? $"Assigned '{roleId}' to {player.Name}."
                    : "Role assignment failed. Check the role ID.";
            }
            else if (action == "remove")
            {
                string roleId = _roleIdInput?.Text?.Trim();
                _roleManagerStatus = roles.RemoveRole(player.PlayerId, roleId)
                    ? $"Removed '{roleId}' from {player.Name}."
                    : "Role removal failed or is protected.";
            }
            else
            {
                string roleList = string.Join(", ", roles.GetPlayerRoleIds(player.PlayerId));
                string permissionList = string.Join(", ", roles.GetEffectivePermissions(player.PlayerId));
                _roleManagerStatus = $"{player.Name}\nRoles: {roleList}\nPermissions: {permissionList}";
            }
            SetAllChildrenDirty();
        };
    }

    private void BindKeyCapture(string id, string action)
    {
        XUiC_SimpleButton button = FindDescendant<XUiC_SimpleButton>(this, id);
        if (button != null) button.OnPressed += (sender, mouseButton) =>
        {
            if (mouseButton != 0) return;
            _capturingKeybindAction = action;
            _optionsStatus = $"Press a key or mouse button for {action}. ESC cancels.";
            SetAllChildrenDirty();
        };
    }

    private void BindKeyClear(string id, string action)
    {
        XUiC_SimpleButton button = FindDescendant<XUiC_SimpleButton>(this, id);
        if (button != null) button.OnPressed += (sender, mouseButton) =>
        {
            if (mouseButton != 0) return;
            ServiceRegistry.Get<AdminPanelService>()?.SetKeybind(action, string.Empty);
            _optionsStatus = $"{action} keybind removed.";
            SetAllChildrenDirty();
        };
    }

    private void CapturePendingKeybind()
    {
        if (string.IsNullOrWhiteSpace(_capturingKeybindAction)) return;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            _capturingKeybindAction = null;
            _optionsStatus = "Keybind capture cancelled.";
            SetAllChildrenDirty();
            return;
        }
        string captured = null;
        for (int button = 0; button <= 6 && captured == null; button++)
            if (Input.GetMouseButtonDown(button)) captured = "Mouse" + button;
        if (captured == null)
        {
            foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
                if (key != KeyCode.None && Input.GetKeyDown(key)) { captured = key.ToString(); break; }
        }
        if (captured == null) return;
        ServiceRegistry.Get<AdminPanelService>()?.SetKeybind(_capturingKeybindAction, captured);
        _optionsStatus = $"{_capturingKeybindAction} assigned to {captured}.";
        _capturingKeybindAction = null;
        SetAllChildrenDirty();
    }

    private void BindThemeButton(string id, string theme)
    {
        XUiC_SimpleButton button = FindDescendant<XUiC_SimpleButton>(this, id);
        if (button != null) button.OnPressed += (sender, mouseButton) =>
        {
            if (mouseButton != 0) return;
            bool saved = ServiceRegistry.Get<AdminPanelService>()?.SetTheme(theme) ?? false;
            _optionsStatus = saved ? $"Theme '{theme}' saved." : "Theme could not be saved.";
            SetAllChildrenDirty();
        };
    }

    private void BindAppearanceControls()
    {
        BindAction("btnToggleGlow", () => { _glowEnabled = !_glowEnabled; SaveAppearance(); });
        BindAction("btnToggleScanlines", () => { _scanlinesEnabled = !_scanlinesEnabled; SaveAppearance(); });
        BindAction("btnToggleAnimations", () => { _animationsEnabled = !_animationsEnabled; SaveAppearance(); });
        BindAction("btnLoadNightglass", () =>
        {
            _themeCss = NightglassTheme; if (_themeCssInput != null) _themeCssInput.Text = _themeCss;
            _themeEditorStatus = "Nightglass loaded. Press APPLY LIVE."; SetAllChildrenDirty();
        });
        BindAction("btnApplyThemeCss", () =>
        {
            _themeCss = _themeCssInput?.Text ?? _themeCss;
            ServiceRegistry.Get<AdminPanelService>()?.ApplyCustomTheme(_themeCss, out _themeEditorStatus);
            ReloadAppearanceFromConfig(); SetAllChildrenDirty();
        });
        BindAction("btnDeleteThemeCss", () =>
        {
            ServiceRegistry.Get<AdminPanelService>()?.DeleteCustomTheme(out _themeEditorStatus);
            _themeCss = string.Empty; if (_themeCssInput != null) _themeCssInput.Text = string.Empty;
            ReloadAppearanceFromConfig(); SetAllChildrenDirty();
        });
    }

    private void BindAction(string id, System.Action action)
    {
        XUiC_SimpleButton button = FindDescendant<XUiC_SimpleButton>(this, id);
        if (button != null) button.OnPressed += (sender, mouseButton) => { if (mouseButton == 0) action(); };
    }

    private void SaveAppearance()
    {
        _appearanceSavePending = true;
        _appearanceSaveDelay = 0.45f;
        SetAllChildrenDirty();
    }

    private void SaveAppearanceNow()
    {
        ServiceRegistry.Get<AdminPanelService>()?.SaveUiPreferences(_backgroundOpacity,
            _backgroundDensity, _glowEnabled, _scanlinesEnabled, _animationsEnabled);
    }

    private void BindBackgroundSlider()
    {
        XUiController slider = FindDescendant<XUiController>(this, "backgroundSlider");
        if (slider == null) return;
        slider.OnDrag += (sender, dragType, delta) =>
        {
            if (dragType != EDragType.Dragging) return;
            SetBackgroundDensity(_backgroundDensity + delta.x / 300f * 4.9f, true);
        };
    }

    private void BackgroundValue_OnChanged(XUiController sender, string text, bool finished)
    {
        string normalized = (text ?? string.Empty).Trim().Replace(',', '.');
        if (!float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out float value)) return;
        if (value < 1.1f || value > 6f) return;
        SetBackgroundDensity(value, false);
    }

    private void SetBackgroundDensity(float value, bool updateInput)
    {
        _backgroundDensity = Mathf.Round(Mathf.Clamp(value, 1.1f, 6f) * 10f) / 10f;
        float normalized = (_backgroundDensity - 1.1f) / 4.9f;
        _backgroundOpacity = Mathf.Lerp(0.12f, 1f, normalized);
        if (updateInput && _backgroundValueInput != null)
            _backgroundValueInput.Text = _backgroundDensity.ToString("0.0", CultureInfo.InvariantCulture);
        UpdateBackgroundSliderVisual(); SaveAppearance();
    }

    private void UpdateBackgroundSliderVisual()
    {
        if (_backgroundSliderKnob?.ViewComponent == null) return;
        int x = Mathf.RoundToInt(((_backgroundDensity - 1.1f) / 4.9f) * 286f);
        _backgroundSliderKnob.ViewComponent.Position = new Vector2i(x, -5);
    }

    private void ReloadAppearanceFromConfig()
    {
        cHub.Config.UIConfig ui = ServiceRegistry.Get<ConfigService>()?.Current?.UI;
        if (ui == null) return;
        _backgroundOpacity = ui.BackgroundOpacity; _glowEnabled = ui.GlowEnabled;
        _backgroundDensity = Mathf.Clamp(ui.BackgroundDensity, 1.1f, 6f);
        _backgroundOpacity = Mathf.Lerp(0.12f, 1f, (_backgroundDensity - 1.1f) / 4.9f);
        _scanlinesEnabled = ui.ScanlineEffect; _animationsEnabled = ui.WaterdropAnimations;
    }

    private void ProcessThemeEditorShortcuts()
    {
        if (!IsPage("themeeditor") || _themeCssInput == null) return;
        bool control = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        if (control && Input.GetKeyDown(KeyCode.A)) { _themeSelectAll = true; _themeEditorStatus = "All theme text selected."; }
        if (control && Input.GetKeyDown(KeyCode.C))
        { GUIUtility.systemCopyBuffer = _themeCssInput.Text ?? string.Empty; _themeEditorStatus = "Theme copied to clipboard."; }
        if (control && Input.GetKeyDown(KeyCode.V))
        {
            string pasted = GUIUtility.systemCopyBuffer ?? string.Empty;
            _themeCssInput.Text = _themeSelectAll ? pasted : (_themeCssInput.Text ?? string.Empty) + pasted;
            _themeCss = _themeCssInput.Text; _themeSelectAll = false; _themeEditorStatus = "Clipboard pasted.";
        }
        if (_themeSelectAll && (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete)))
        { _themeCssInput.Text = string.Empty; _themeCss = string.Empty; _themeSelectAll = false; _themeEditorStatus = "Editor cleared. APPLY or DELETE to restore defaults."; }
    }

    private void BindZombiePreset(string id, string className, string displayName)
    {
        XUiC_SimpleButton button = FindDescendant<XUiC_SimpleButton>(this, id);
        if (button != null) button.OnPressed += (sender, mouseButton) =>
        {
            if (mouseButton != 0 || !HasPermission(PermissionIds.AdminPanelManage)) return;
            _selectedZombieClass = className;
            _zombieDirectorStatus = "Selected: " + displayName;
            SetAllChildrenDirty();
        };
    }

    private void BindZombieSpawn(string id, int count)
    {
        XUiC_SimpleButton button = FindDescendant<XUiC_SimpleButton>(this, id);
        if (button != null) button.OnPressed += (sender, mouseButton) =>
        {
            if (mouseButton != 0 || !HasPermission(PermissionIds.AdminPanelManage)) return;
            AdminPanelService service = ServiceRegistry.Get<AdminPanelService>();
            string error = "Zombie Director service is unavailable.";
            _zombieDirectorStatus = service != null &&
                service.RequestZombieSpawn(_selectedZombieClass, count, out error)
                ? $"Server spawn requested: {count} × {_selectedZombieClass}." : error;
            SetAllChildrenDirty();
        };
    }

    private void SaveHome_OnPressed(XUiController sender, int mouseButton)
    {
        if (mouseButton != 0) return;
        AdminPanelService service = ServiceRegistry.Get<AdminPanelService>();
        EntityPlayerLocal player = xui?.playerUI?.entityPlayer;
        if (service == null || player == null ||
            !service.SaveHome(GetLocalPlayerId(), _homeNameInput?.Text,
                player.position, out _homeStatus))
        {
            SetAllChildrenDirty();
            return;
        }
        _homeStatus = "Home saved.";
        if (_homeNameInput != null) _homeNameInput.Text = string.Empty;
        SetAllChildrenDirty();
    }

    private void SaveHomeCooldown_OnPressed(XUiController sender, int mouseButton)
    {
        if (mouseButton != 0 || !HasPermission(PermissionIds.Owner)) return;
        if (!int.TryParse(_homeCooldownInput?.Text, out int seconds) ||
            !(ServiceRegistry.Get<AdminPanelService>()?.SetHomeCooldownSeconds(seconds) ?? false))
            _homeStatus = "Cooldown must be between 0 and 86400 seconds.";
        else
            _homeStatus = "Home cooldown saved.";
        SetAllChildrenDirty();
    }

    private bool TryGetHomeBinding(string bindingName, out string value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(bindingName) ||
            !bindingName.StartsWith("chub_home_", System.StringComparison.OrdinalIgnoreCase)) return false;
        string[] parts = bindingName.Split('_');
        if (parts.Length != 4 || !int.TryParse(parts[2], out int slot) || slot < 1 || slot > 8) return false;
        IReadOnlyList<HomeEntry> homes = ServiceRegistry.Get<AdminPanelService>()?.GetHomes(GetLocalPlayerId());
        HomeEntry home = homes != null && slot <= homes.Count ? homes[slot - 1] : null;
        if (parts[3].Equals("visible", System.StringComparison.OrdinalIgnoreCase))
            value = (home != null).ToString().ToLowerInvariant();
        else if (parts[3].Equals("name", System.StringComparison.OrdinalIgnoreCase))
            value = home?.Name ?? string.Empty;
        else return false;
        return true;
    }

    private static string GetLocalPlayerId()
    {
        try { return GameManager.Instance?.getPersistentPlayerID(null)?.CombinedString ?? string.Empty; }
        catch { return string.Empty; }
    }

    private void BindCloseButton(string id)
    {
        XUiC_SimpleButton button = FindDescendant<XUiC_SimpleButton>(this, id);

        if (button != null)
        {
            button.OnPressed += CloseButton_OnPressed;
        }
    }

    private void BindDiscordControls()
    {
        XUiC_SimpleButton close = FindDescendant<XUiC_SimpleButton>(this, "btnCloseDiscord");
        XUiC_SimpleButton open = FindDescendant<XUiC_SimpleButton>(this, "btnOpenDiscord");
        XUiC_SimpleButton copy = FindDescendant<XUiC_SimpleButton>(this, "btnCopyDiscord");
        if (close != null) close.OnPressed += (sender, mouseButton) =>
        {
            if (mouseButton != 0) return;
            _discordPopupVisible = false;
            SetAllChildrenDirty();
        };
        if (open != null) open.OnPressed += (sender, mouseButton) =>
        {
            if (mouseButton != 0) return;
            Application.OpenURL(DiscordInviteUrl);
            _discordStatus = "Discord invite opened in your browser.";
            SetAllChildrenDirty();
        };
        if (copy != null) copy.OnPressed += (sender, mouseButton) =>
        {
            if (mouseButton != 0) return;
            GUIUtility.systemCopyBuffer = DiscordInviteUrl;
            _discordStatus = GUIUtility.systemCopyBuffer == DiscordInviteUrl
                ? "Copied. Paste it with Ctrl+V." : "Clipboard copy could not be verified.";
            SetAllChildrenDirty();
        };
    }

    private void BindFooterControls()
    {
        XUiC_SimpleButton discord = FindDescendant<XUiC_SimpleButton>(this, "btnDiscord");
        if (!object.ReferenceEquals(discord, _footerDiscordButton))
        {
            if (_footerDiscordButton != null) _footerDiscordButton.OnPressed -= FooterDiscord_OnPressed;
            _footerDiscordButton = discord;
            if (_footerDiscordButton != null)
                _footerDiscordButton.OnPressed += FooterDiscord_OnPressed;
        }

        XUiC_SimpleButton close = FindDescendant<XUiC_SimpleButton>(this, "btnCloseFooter");
        if (!object.ReferenceEquals(close, _footerCloseButton))
        {
            if (_footerCloseButton != null) _footerCloseButton.OnPressed -= FooterClose_OnPressed;
            _footerCloseButton = close;
            if (_footerCloseButton != null)
                _footerCloseButton.OnPressed += FooterClose_OnPressed;
        }
    }

    private void FooterDiscord_OnPressed(XUiController sender, int mouseButton)
    {
        if (mouseButton != 0) return;
        OpenDiscordInvite();
        _discordStatus = "Discord invite opened.";
        SetAllChildrenDirty();
    }

    private void FooterClose_OnPressed(XUiController sender, int mouseButton)
    {
        if (mouseButton != 0) return;
        CloseCurrentPanel();
    }

    private static void OpenDiscordInvite()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = DiscordInviteUrl,
                UseShellExecute = true
            });
        }
        catch
        {
            Application.OpenURL(DiscordInviteUrl);
        }
    }

    private void BindPanelCloseButton(string id, string returnPage)
    {
        XUiC_SimpleButton button = FindDescendant<XUiC_SimpleButton>(this, id);
        if (button == null) return;
        button.OnPressed += (sender, mouseButton) =>
        {
            if (mouseButton == 0) SetActivePage(returnPage);
        };
    }

    private void BindListPageButton(string id, int direction)
    {
        XUiC_SimpleButton button = FindDescendant<XUiC_SimpleButton>(this, id);

        if (button == null)
        {
            return;
        }

        button.OnPressed += (sender, mouseButton) =>
        {
            if (mouseButton != 0)
            {
                return;
            }

            int pageCount = GetPlayerListPageCount();
            _playerListPage = Mathf.Clamp(
                _playerListPage + direction, 0, pageCount - 1);
            SetAllChildrenDirty();
        };
    }

    private void BindTeleportRequestButton(string id, int slot)
    {
        XUiC_SimpleButton button = FindDescendant<XUiC_SimpleButton>(this, id);
        if (button == null) return;
        button.OnPressed += (sender, mouseButton) =>
        {
            if (mouseButton != 0 || !HasPermission(PermissionIds.TeleportToPlayer)) return;
            int index = _playerListPage * PlayersPerPage + slot;
            List<PlayerListEntry> entries = GetPlayerListEntries();
            if (index < 0 || index >= entries.Count || !entries[index].IsOnline)
            {
                _playerTeleportStatus = "That player is offline.";
            }
            else
            {
                AdminPanelService service = ServiceRegistry.Get<AdminPanelService>();
                string error = "Teleport service is unavailable.";
                _playerTeleportStatus = service != null &&
                    service.RequestMagicTeleport(entries[index].PlayerId, out error)
                    ? $"Magic request sent to {entries[index].Name}."
                    : error;
            }
            SetAllChildrenDirty();
        };
    }

    private void BindAdminSelector(string id, bool source)
    {
        XUiC_SimpleButton button = FindDescendant<XUiC_SimpleButton>(this, id);
        if (button != null) button.OnPressed += (sender, mouseButton) =>
        {
            if (mouseButton != 0) return;
            _selectingAdminSource = source;
            SetAllChildrenDirty();
        };
    }

    private void BindAdminCandidate(string id, int slot)
    {
        XUiC_SimpleButton button = FindDescendant<XUiC_SimpleButton>(this, id);
        if (button == null) return;
        button.OnPressed += (sender, mouseButton) =>
        {
            if (mouseButton != 0) return;
            List<PlayerListEntry> online = GetFilteredAdminOnlinePlayers();
            if (slot >= online.Count) return;
            bool canMoveOthers = HasPermission(PermissionIds.TeleportPlayers);
            if (canMoveOthers && _selectingAdminSource) _adminTeleportSourceId = online[slot].PlayerId;
            else _adminTeleportTargetId = online[slot].PlayerId;
            SetAllChildrenDirty();
        };
    }

    private void SwapAdminTeleport_OnPressed(XUiController sender, int mouseButton)
    {
        if (mouseButton != 0 || !HasPermission(PermissionIds.TeleportPlayers)) return;
        string temporary = _adminTeleportSourceId;
        _adminTeleportSourceId = _adminTeleportTargetId;
        _adminTeleportTargetId = temporary;
        SetAllChildrenDirty();
    }

    private void ConfirmAdminTeleport_OnPressed(XUiController sender, int mouseButton)
    {
        if (mouseButton != 0) return;
        AdminPanelService service = ServiceRegistry.Get<AdminPanelService>();
        string error = "Teleport service is unavailable.";
        if (HasPermission(PermissionIds.TeleportPlayers))
            _adminTeleportStatus = service != null &&
                service.RequestAdminTeleport(_adminTeleportSourceId, _adminTeleportTargetId, out error)
                ? "Administrator teleport sent to the server." : error;
        else
            _adminTeleportStatus = service != null &&
                service.RequestMagicTeleport(_adminTeleportTargetId, out error)
                ? "Magic teleport request sent." : error;
        SetAllChildrenDirty();
    }

    private string GetOnlinePlayerName(string playerId, string fallback)
    {
        PlayerListEntry entry = GetPlayerListEntries().FirstOrDefault(item =>
            item.IsOnline && string.Equals(item.PlayerId, playerId,
                System.StringComparison.OrdinalIgnoreCase));
        return entry?.Name ?? fallback;
    }

    private bool TryGetAdminCandidateBinding(string bindingName, out string value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(bindingName) ||
            !bindingName.StartsWith("chub_admin_candidate_", System.StringComparison.OrdinalIgnoreCase)) return false;
        if (!int.TryParse(bindingName.Substring("chub_admin_candidate_".Length), out int slot) || slot < 1 || slot > 4) return false;
        List<PlayerListEntry> online = GetFilteredAdminOnlinePlayers();
        value = slot <= online.Count ? online[slot - 1].Name : "Empty";
        return true;
    }

    private List<PlayerListEntry> GetFilteredAdminOnlinePlayers()
    {
        IEnumerable<PlayerListEntry> players = _cachedPlayerEntries.Where(item => item.IsOnline);
        if (!string.IsNullOrWhiteSpace(_adminPlayerSearchText))
        {
            string search = _adminPlayerSearchText;
            players = players.Where(item =>
                (item.Name?.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                (item.PlayerId?.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) ?? -1) >= 0);
        }
        return players.ToList();
    }

    private void BindResizeHandle(string id)
    {
        XUiController handle = FindDescendant<XUiController>(this, id);

        if (handle != null)
        {
            handle.OnDrag += ResizeHandle_OnDrag;
        }
    }

    private void BindFloatingPanel(string panelId, string dragId, string resizeId)
    {
        XUiController panel = FindDescendant<XUiController>(this, panelId);
        XUiController drag = FindDescendant<XUiController>(this, dragId);
        XUiController resize = FindDescendant<XUiController>(this, resizeId);
        if (panel == null) return;
        _floatingPanelScales[panelId] = 1f;
        if (drag != null)
        {
            drag.OnDrag += (sender, dragType, delta) =>
            {
                if (dragType != EDragType.Dragging || panel.ViewComponent == null) return;
                Vector2i position = panel.ViewComponent.Position;
                panel.ViewComponent.Position = new Vector2i(position.x + Mathf.RoundToInt(delta.x),
                    position.y + Mathf.RoundToInt(delta.y));
            };
        }
        if (resize != null)
        {
            resize.OnDrag += (sender, dragType, delta) =>
            {
                if (dragType != EDragType.Dragging || panel.ViewComponent?.UiTransform == null) return;
                float target = Mathf.Clamp(_floatingPanelScales[panelId] +
                    (delta.x - delta.y) / 420f, MinimumPanelScale, MaximumPanelScale);
                _floatingPanelScales[panelId] = target;
                float current = panel.ViewComponent.UiTransform.localScale.x;
                float animated = Mathf.Lerp(current, target, 0.42f);
                panel.ViewComponent.UiTransform.localScale = new Vector3(animated, animated, 1f);
            };
        }
    }

    private static T FindDescendant<T>(XUiController root, string id)
        where T : XUiController
    {
        if (root == null)
        {
            return null;
        }

        foreach (XUiController child in root.Children)
        {
            if (child == null)
            {
                continue;
            }

            if (child is T typed &&
                child.ViewComponent != null &&
                string.Equals(child.ViewComponent.ID, id,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                return typed;
            }

            T nested = FindDescendant<T>(child, id);

            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    private bool IsPage(string page)
    {
        return string.Equals(_activePage, page,
            System.StringComparison.OrdinalIgnoreCase);
    }

    private void SetActivePage(string page)
    {
        if (string.Equals(page, "homes", System.StringComparison.OrdinalIgnoreCase) &&
            !HasPermission(PermissionIds.TeleportSelf))
        {
            return;
        }

        if ((string.Equals(page, "staff", System.StringComparison.OrdinalIgnoreCase) ||
             string.Equals(page, "moderator", System.StringComparison.OrdinalIgnoreCase)) &&
            !HasStaffAccess())
        {
            return;
        }

        if (string.Equals(page, "administrator", System.StringComparison.OrdinalIgnoreCase) &&
            !HasPermission(PermissionIds.AdminPanelManage))
        {
            return;
        }

        if ((string.Equals(page, "dev", System.StringComparison.OrdinalIgnoreCase) ||
             string.Equals(page, "rolemanager", System.StringComparison.OrdinalIgnoreCase) ||
             string.Equals(page, "commandcenter", System.StringComparison.OrdinalIgnoreCase) ||
             string.Equals(page, "devtools", System.StringComparison.OrdinalIgnoreCase)) &&
            !HasPermission(PermissionIds.Owner))
        {
            return;
        }

        if (string.Equals(page, "zombies", System.StringComparison.OrdinalIgnoreCase) &&
            !HasPermission(PermissionIds.AdminPanelManage)) return;

        if (string.Equals(page, "teleport",
                System.StringComparison.OrdinalIgnoreCase) &&
            !HasPermission(PermissionIds.TeleportSelf) &&
            !HasPermission(PermissionIds.TeleportPlayers))
        {
            return;
        }

        _activePage = page;
        if (string.Equals(page, "rot", System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(page, "toprot", System.StringComparison.OrdinalIgnoreCase))
            AdminPanelService.RequestRotSnapshot(GetLocalPlayerId());
        ServiceRegistry.Get<AdminPanelService>()?.SetCurrentPage(page);
        if (IsPage("teleport")) EnsureAdminTeleportSelection();
        _activeFloatingPanel = GetFloatingPanelForPage(page);
        _floatingPopupProgress = _activeFloatingPanel == null ? 1f : 0f;

        if (IsPage("teleport") || IsPage("playerlist") || IsPage("rolemanager") || IsPage("assistant"))
        {
            ShowSidePanelPosition();
        }
        else
        {
            RestoreMainPanelPosition();
        }

        SetAllChildrenDirty();
    }

    private void EnsureAdminTeleportSelection()
    {
        List<PlayerListEntry> online = GetFilteredAdminOnlinePlayers();
        if (online.Count == 0) return;
        string localId = GetLocalPlayerId();
        if (!HasPermission(PermissionIds.TeleportPlayers))
            _adminTeleportSourceId = localId;
        else if (string.IsNullOrWhiteSpace(_adminTeleportSourceId))
            _adminTeleportSourceId = online[0].PlayerId;
        if (string.IsNullOrWhiteSpace(_adminTeleportTargetId))
            _adminTeleportTargetId = online.FirstOrDefault(item =>
                !string.Equals(item.PlayerId, _adminTeleportSourceId,
                    System.StringComparison.OrdinalIgnoreCase))?.PlayerId;
    }

    private XUiController GetFloatingPanelForPage(string page)
    {
        string panelId = null;
        if (string.Equals(page, "homes", System.StringComparison.OrdinalIgnoreCase)) panelId = "homesPage";
        else if (string.Equals(page, "playerlist", System.StringComparison.OrdinalIgnoreCase)) panelId = "playerListPage";
        else if (string.Equals(page, "teleport", System.StringComparison.OrdinalIgnoreCase)) panelId = "teleportPage";
        else if (string.Equals(page, "rolemanager", System.StringComparison.OrdinalIgnoreCase)) panelId = "roleManagerPage";
        else if (string.Equals(page, "assistant", System.StringComparison.OrdinalIgnoreCase)) panelId = "assistantPage";
        else if (string.Equals(page, "placeholder", System.StringComparison.OrdinalIgnoreCase)) panelId = "placeholderPage";
        else if (string.Equals(page, "commandcenter", System.StringComparison.OrdinalIgnoreCase)) panelId = "commandCenterPage";
        else if (string.Equals(page, "devtools", System.StringComparison.OrdinalIgnoreCase)) panelId = "devToolsPage";
        return panelId == null ? null : FindDescendant<XUiController>(this, panelId);
    }

    private void BindDevToolsControls()
    {
        BindDevToolsCategory("btnDevToolsProject", "project");
        BindDevToolsCategory("btnDevToolsXui", "xui");
        BindDevToolsCategory("btnDevToolsData", "data");
        BindDevToolsCategory("btnDevToolsBuild", "build");
        BindDevToolsCategory("btnDevToolsLogs", "logs");
        BindDevToolsCategory("btnDevToolsAi", "ai");
        BindDevToolsAction("btnDevToolsReloadConfig", () =>
        {
            ConfigService service = ServiceRegistry.Get<ConfigService>();
            bool ok = service != null && service.Reload();
            _devToolsStatus = ok ? "OK // CONFIG RELOADED LIVE" : "FAILED // CONFIG RELOAD - CHECK LOG";
        });
        BindDevToolsAction("btnDevToolsCommandCenter", () => SetActivePage("commandcenter"));
        BindDevToolsAction("btnDevToolsThemes", () => SetActivePage("themeeditor"));
        BindDevToolsAction("btnDevToolsAssistant", () => SetActivePage("assistant"));
        BindDevToolsAction("btnDevToolsCopyProject", () =>
        {
            GUIUtility.systemCopyBuffer = @"C:\Users\zeqlz\source\repos\cHub";
            _devToolsStatus = "COPIED // PROJECT PATH";
        });
        BindDevToolsAction("btnDevToolsCopyLogs", () =>
        {
            GUIUtility.systemCopyBuffer = @"C:\Users\zeqlz\AppData\Roaming\7DaysToDie\logs";
            _devToolsStatus = "COPIED // LOG PATH";
        });
        BindDevToolsAction("btnDevToolsOpenLogs", () =>
        {
            Application.OpenURL("file:///C:/Users/zeqlz/AppData/Roaming/7DaysToDie/logs");
            _devToolsStatus = "OPEN REQUEST // 7 DAYS TO DIE LOGS";
        });
        BindDevToolsAction("btnDevToolsBuildNow", () =>
            _devToolsStatus = "BRIDGE REQUIRED // BUILD IS DISABLED IN-GAME");
        BindDevToolsAction("btnDevToolsDeployNow", () =>
            _devToolsStatus = "BRIDGE REQUIRED // DEPLOY IS DISABLED IN-GAME");
    }

    private void BindDevToolsCategory(string id, string category)
    {
        XUiC_SimpleButton button = FindDescendant<XUiC_SimpleButton>(this, id);
        if (button == null) return;
        button.OnPressed += (sender, mouseButton) =>
        {
            if (mouseButton != 0 || !HasPermission(PermissionIds.Owner)) return;
            _devToolsCategory = category;
            _devToolsStatus = category.ToUpperInvariant() + " // TOOL GROUP ACTIVE";
            SetAllChildrenDirty();
        };
    }

    private void BindDevToolsAction(string id, System.Action action)
    {
        XUiC_SimpleButton button = FindDescendant<XUiC_SimpleButton>(this, id);
        if (button == null) return;
        button.OnPressed += (sender, mouseButton) =>
        {
            if (mouseButton != 0 || !HasPermission(PermissionIds.Owner)) return;
            action?.Invoke();
            SetAllChildrenDirty();
        };
    }

    private string GetCommandCenterList()
    {
        CommandService service = ServiceRegistry.Get<CommandService>();
        if (service == null) return "Command service unavailable.";
        string playerId = GetLocalPlayerId();
        return string.Join("\n", service.GetAvailableCommands(playerId)
            .Select(command => command.Usage + "  //  " + command.Description));
    }

    private void CommandCenterRun_OnPressed(XUiController sender, int mouseButton)
    {
        if (mouseButton != 0 || !HasPermission(PermissionIds.Owner)) return;
        string input = (_commandCenterInput?.Text ?? string.Empty).Trim();
        if (input.Length == 0) { _commandCenterStatus = "Enter a command first."; SetAllChildrenDirty(); return; }
        CommandService service = ServiceRegistry.Get<CommandService>();
        if (service == null) { _commandCenterStatus = "Command service unavailable."; SetAllChildrenDirty(); return; }
        EntityPlayerLocal player = xui?.playerUI?.entityPlayer;
        cHub.Services.Commands.CommandResult result = service.ExecuteRaw(
            GetLocalPlayerId(), player?.EntityName ?? "Owner", input);
        _commandCenterStatus = (result.Success ? "OK // " : "DENIED // ") + result.Message;
        SetAllChildrenDirty();
    }

    private bool HasStaffAccess()
    {
        return HasPermission(PermissionIds.AdminPanelAccess) ||
               HasPermission(PermissionIds.AdminPanelView);
    }

    private bool HasPermission(string permissionId)
    {
        try
        {
            string playerId = GameManager.Instance?
                .getPersistentPlayerID(null)?.CombinedString;
            RoleService roleService = ServiceRegistry.Get<RoleService>();

            return roleService != null &&
                   !string.IsNullOrWhiteSpace(playerId) &&
                   roleService.HasPermission(playerId, permissionId);
        }
        catch
        {
            return false;
        }
    }

    private int GetPlayerListPageCount()
    {
        int count = GetPlayerListEntries().Count;
        return Mathf.Max(1, Mathf.CeilToInt(count / (float)PlayersPerPage));
    }

    private void BindPartyControls()
    {
        BindPartyAction("btnPartyCreate", () => PartyService.Create(GetLocalPlayerId(),
            xui?.playerUI?.entityPlayer?.EntityName));
        BindPartyAction("btnPartyDelete", () => PartyService.Delete(GetLocalPlayerId()));
        BindPartyAction("btnPartyInvite", InviteSelectedPartyPlayer);
        BindPartyAction("btnPartyContextInvite", InviteSelectedPartyPlayer);
        BindPartyAction("btnPartyRemove", () => PartyService.Remove(GetLocalPlayerId(), _partySelectedMemberId));
        BindPartyAction("btnPartyPromote", () => PartyService.SetRank(GetLocalPlayerId(), _partySelectedMemberId, cHubPartyRank.Officer));
        BindPartyAction("btnPartyDemote", () => PartyService.SetRank(GetLocalPlayerId(), _partySelectedMemberId, cHubPartyRank.Member));
        BindPartyAction("btnPartyContextClose", () => { _partyContextVisible = false; return "Actions closed."; });
        for (int i = 0; i < 16; i++) BindPartyCandidate("btnPartyCandidate" + (i + 1), i);
        for (int i = 0; i < PartyService.MaximumMembers; i++) BindPartyMember("btnPartyMember" + (i + 1), i);
    }

    private void BindPartyAction(string id, System.Func<string> action)
    {
        XUiC_SimpleButton button = FindDescendant<XUiC_SimpleButton>(this, id);
        if (button == null) return;
        button.OnPressed += (sender, mouseButton) =>
        {
            if (mouseButton != 0) return;
            _partyStatus = action();
            RefreshPlayerEntries();
            SetAllChildrenDirty();
        };
    }

    private void BindPartyCandidate(string id, int index)
    {
        XUiC_SimpleButton button = FindDescendant<XUiC_SimpleButton>(this, id);
        if (button == null) return;
        button.OnPressed += (sender, mouseButton) =>
        {
            PlayerListEntry entry = GetPartyCandidates().ElementAtOrDefault(index);
            if (entry == null) return;
            _partySelectedPlayerId = entry.PlayerId;
            _partyContextVisible = mouseButton == 1;
            _partyStatus = entry.Name + (entry.IsOnline ? " selected." : " is offline; selection retained.");
            SetAllChildrenDirty();
        };
    }

    private void BindPartyMember(string id, int index)
    {
        XUiC_SimpleButton button = FindDescendant<XUiC_SimpleButton>(this, id);
        if (button == null) return;
        button.OnPressed += (sender, mouseButton) =>
        {
            cHubPartyMember member = GetPartyMembers().ElementAtOrDefault(index);
            if (member == null) return;
            _partySelectedMemberId = member.PlayerId;
            _partyContextVisible = mouseButton == 1;
            _partyStatus = member.Name + " // " + member.Rank;
            SetAllChildrenDirty();
        };
    }

    private string InviteSelectedPartyPlayer()
    {
        PlayerListEntry target = GetPartyCandidates().FirstOrDefault(p =>
            string.Equals(p.PlayerId, _partySelectedPlayerId, System.StringComparison.OrdinalIgnoreCase));
        return target == null ? "Select a survivor first." :
            PartyService.Invite(GetLocalPlayerId(), target.PlayerId, target.Name, target.EntityId);
    }

    private string GetPartySelectedName()
    {
        PlayerListEntry player = GetPartyCandidates().FirstOrDefault(p =>
            string.Equals(p.PlayerId, _partySelectedPlayerId, System.StringComparison.OrdinalIgnoreCase));
        return player == null ? "NO SURVIVOR SELECTED" : player.Name;
    }

    private List<PlayerListEntry> GetPartyCandidates()
    {
        cHubPartyRecord party = PartyService.GetFor(GetLocalPlayerId());
        return _cachedPlayerEntries.Where(p => party == null ||
            !party.Members.Any(m => string.Equals(m.PlayerId, p.PlayerId, System.StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    private List<cHubPartyMember> GetPartyMembers() =>
        PartyService.GetFor(GetLocalPlayerId())?.Members
            .OrderBy(m => m.Rank == cHubPartyRank.Leader ? 0 : m.Rank == cHubPartyRank.Officer ? 1 : 2)
            .ThenBy(m => m.Name).ToList() ?? new List<cHubPartyMember>();

    private bool TryGetPartyBinding(string bindingName, out string value)
    {
        value = null;
        if (bindingName == "chub_party_context")
        { value = _partyContextVisible.ToString().ToLowerInvariant(); return true; }
        string[] parts = (bindingName ?? string.Empty).Split('_');
        if (parts.Length != 5 || parts[0] != "chub" || parts[1] != "party" || !int.TryParse(parts[3], out int slot)) return false;
        if (parts[2] == "candidate")
        {
            PlayerListEntry item = GetPartyCandidates().ElementAtOrDefault(slot - 1);
            if (parts[4] == "visible") value = (item != null).ToString().ToLowerInvariant();
            else if (parts[4] == "name") value = item?.Name ?? string.Empty;
            else if (parts[4] == "meta") value = item?.Meta ?? string.Empty;
            else if (parts[4] == "color") value = item != null && item.IsOnline ? "110,225,145,255" : "125,118,120,255";
            else return false;
            return true;
        }
        if (parts[2] == "member")
        {
            cHubPartyMember item = GetPartyMembers().ElementAtOrDefault(slot - 1);
            bool online = item != null && _cachedPlayerEntries.Any(p => p.IsOnline && string.Equals(p.PlayerId, item.PlayerId, System.StringComparison.OrdinalIgnoreCase));
            if (parts[4] == "visible") value = (item != null).ToString().ToLowerInvariant();
            else if (parts[4] == "name") value = item?.Name ?? string.Empty;
            else if (parts[4] == "meta") value = item == null ? string.Empty : (item.Rank == cHubPartyRank.Leader ? "★ LEADER" : item.Rank.ToString().ToUpperInvariant()) + (online ? " • ONLINE" : " • OFFLINE");
            else if (parts[4] == "color") value = online ? "255,215,65,255" : "145,132,132,255";
            else return false;
            return true;
        }
        return false;
    }

    private bool TryGetPlayerListBinding(string bindingName, out string value)
    {
        value = null;

        if (string.IsNullOrWhiteSpace(bindingName) ||
            !bindingName.StartsWith("chub_list_",
                System.StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string[] parts = bindingName.Split('_');

        if (parts.Length != 4 ||
            !int.TryParse(parts[2], out int slot) ||
            slot < 1 || slot > PlayersPerPage)
        {
            return false;
        }

        int index = _playerListPage * PlayersPerPage + slot - 1;
        List<PlayerListEntry> entries = GetPlayerListEntries();
        PlayerListEntry entry = index < entries.Count ? entries[index] : null;

        switch (parts[3].ToLowerInvariant())
        {
            case "visible":
                value = (entry != null).ToString().ToLowerInvariant();
                break;
            case "name":
                value = entry?.Name ?? string.Empty;
                break;
            case "meta":
                value = entry?.Meta ?? string.Empty;
                break;
            case "color":
                value = entry != null && entry.IsOnline
                    ? "135,210,150,255"
                    : "135,125,125,255";
                break;
            default:
                return false;
        }

        return true;
    }

    private List<PlayerListEntry> GetPlayerListEntries()
    {
        return _cachedPlayerEntries;
    }

    private void RefreshPlayerEntries()
    {
        PlayerIdentityService service =
            ServiceRegistry.Get<PlayerIdentityService>();
        List<PlayerListEntry> result = new List<PlayerListEntry>();

        if (service == null)
        {
            _cachedPlayerEntries = result;
            return;
        }

        Dictionary<string, ClientInfo> online =
            new Dictionary<string, ClientInfo>(
                System.StringComparer.OrdinalIgnoreCase);

        if (ConnectionManager.Instance?.Clients?.List != null)
        {
            foreach (ClientInfo client in ConnectionManager.Instance.Clients.List)
            {
                if (cHub.Services.Players.PlayerIdentityResolver.TryResolve(
                    client, out string id))
                {
                    online[id] = client;
                }
            }
        }

        EntityPlayerLocal localPlayer = xui?.playerUI?.entityPlayer;

        foreach (PlayerIdentityRecord record in service.GetPlayers())
        {
            PlayerListEntry entry = new PlayerListEntry
            {
                PlayerId = record.PlayerId,
                Name = record.Name,
                IsOnline = online.TryGetValue(record.PlayerId, out ClientInfo client),
                EntityId = client?.entityId ?? -1
            };

            if (entry.IsOnline &&
                GameManager.Instance?.World?.Players?.dict != null &&
                GameManager.Instance.World.Players.dict.TryGetValue(
                    client.entityId, out EntityPlayer player))
            {
                int level = player?.Progression?.Level ?? 1;
                int gameStage = GetPlayerGameStage(player);
                int distance = localPlayer == null || player == null
                    ? 0
                    : Mathf.RoundToInt(Vector3.Distance(
                        localPlayer.position, player.position));
                entry.Meta = $"Level {level} • GS {gameStage} • {distance}m • {client.ping}ms";
            }
            else
            {
                entry.Meta = "Offline";
            }

            result.Add(entry);
        }

        _cachedPlayerEntries = result
            .OrderByDescending(item => item.IsOnline)
            .ThenBy(item => item.Name)
            .ToList();
    }

    private sealed class PlayerListEntry
    {
        public string Name { get; set; }
        public string PlayerId { get; set; }
        public string Meta { get; set; }
        public bool IsOnline { get; set; }
        public int EntityId { get; set; } = -1;
    }

    private void ShowSidePanelPosition()
    {
        if (_sidePanelShifted || ViewComponent == null)
        {
            return;
        }

        Vector2i position = ViewComponent.Position;
        ViewComponent.Position = new Vector2i(position.x - 240, position.y);
        _sidePanelShifted = true;
    }

    private void RestoreMainPanelPosition()
    {
        if (!_sidePanelShifted || ViewComponent == null)
        {
            return;
        }

        Vector2i position = ViewComponent.Position;
        ViewComponent.Position = new Vector2i(position.x + 240, position.y);
        _sidePanelShifted = false;
    }

    private static string GetKnownPlayerName(int index)
    {
        PlayerIdentityService service =
            ServiceRegistry.Get<PlayerIdentityService>();

        if (service == null)
        {
            return "No player";
        }

        PlayerIdentityRecord player = service.GetPlayers()
            .OrderByDescending(item => item.LastSeenUtc)
            .ElementAtOrDefault(index);

        return player?.Name ?? "Empty slot";
    }

    private int GetLocalGameStage()
    {
        return GetPlayerGameStage(xui?.playerUI?.entityPlayer);
    }

    private static int GetPlayerGameStage(EntityPlayer player)
    {
        if (player == null) return 1;
        try
        {
            System.Type type = player.GetType();
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.IgnoreCase;
            System.Reflection.PropertyInfo property = type.GetProperty("gameStage", flags);
            if (property != null) return System.Convert.ToInt32(property.GetValue(player, null));
            System.Reflection.FieldInfo field = type.GetField("gameStage", flags);
            if (field != null) return System.Convert.ToInt32(field.GetValue(player));
            System.Reflection.MethodInfo method = type.GetMethod("GetGameStage", flags,
                null, System.Type.EmptyTypes, null);
            if (method != null) return System.Convert.ToInt32(method.Invoke(player, null));
        }
        catch { }
        return 1;
    }

    private void GetLocalXp(out int current, out int required, out float progress)
    {
        current = 0;
        required = 1;
        progress = 0f;
        Progression progression = xui?.playerUI?.entityPlayer?.Progression;
        if (progression == null) return;
        try
        {
            required = Mathf.Max(1, progression.GetExpForNextLevel());
            progress = Mathf.Clamp01(progression.GetLevelProgressPercentage());
            current = Mathf.Clamp(Mathf.RoundToInt(required * progress), 0, required);
        }
        catch { }
    }

    private void UpdateLiveXpBar()
    {
        EntityPlayerLocal player = xui?.playerUI?.entityPlayer;
        GetLocalXp(out int current, out int required, out float progress);
        if (_localXpProgressFill?.ViewComponent?.UiTransform != null)
            _localXpProgressFill.ViewComponent.UiTransform.localScale =
                new Vector3(Mathf.Max(0.001f, progress), 1f, 1f);

        XUiV_Label levelView = _localLevelStageLabel?.ViewComponent as XUiV_Label;
        if (levelView != null)
        {
            int level = player?.Progression?.Level ?? 1;
            levelView.Text = $"LVL {level}  •  GS {GetPlayerGameStage(player)}";
        }

        XUiV_Label xpView = _localXpLabel?.ViewComponent as XUiV_Label;
        if (xpView != null)
            xpView.Text = current.ToString("N0", CultureInfo.InvariantCulture) + "/" +
                          required.ToString("N0", CultureInfo.InvariantCulture) + " XP";
    }

    private void PrimeXpObservation()
    {
        GetLocalXp(out _lastObservedXp, out _lastObservedXpRequired, out _);
        _lastObservedLevel = xui?.playerUI?.entityPlayer?.Progression?.Level ?? 1;
        _xpObservationReady = true;
        _xpGainTimer = 0f;
    }

    private void DetectXpGain()
    {
        GetLocalXp(out int current, out int required, out _);
        int level = xui?.playerUI?.entityPlayer?.Progression?.Level ?? 1;
        if (!_xpObservationReady)
        {
            _lastObservedXp = current;
            _lastObservedXpRequired = required;
            _lastObservedLevel = level;
            _xpObservationReady = true;
            return;
        }

        int gained = level == _lastObservedLevel
            ? current - _lastObservedXp
            : Mathf.Max(0, _lastObservedXpRequired - _lastObservedXp) + current;
        _lastObservedXp = current;
        _lastObservedXpRequired = required;
        _lastObservedLevel = level;
        if (gained <= 0) return;

        _xpGainAmount = gained;
        _xpGainTimer = 2.4f;
        if (_xpGainPopup?.ViewComponent?.UiTransform != null)
            _xpGainPopup.ViewComponent.UiTransform.localScale = new Vector3(0.72f, 0.72f, 1f);
    }

    private void UpdateXpGainPopup(float deltaTime)
    {
        if (_xpGainTimer <= 0f) return;
        _xpGainTimer = Mathf.Max(0f, _xpGainTimer - deltaTime);
        if (_xpGainPopup?.ViewComponent?.UiTransform != null)
        {
            float elapsed = 2.4f - _xpGainTimer;
            float scale = Mathf.Lerp(0.72f, 1f, 1f - Mathf.Exp(-elapsed * 9f));
            _xpGainPopup.ViewComponent.UiTransform.localScale = new Vector3(scale, scale, 1f);
        }
        if (_xpGainTimer <= 0f) SetAllChildrenDirty();
    }

    private void BindEconomyAdminControls()
    {
        XUiC_SimpleButton select = FindDescendant<XUiC_SimpleButton>(this, "btnEconomySelect");
        if (select != null) select.OnPressed += (sender, mouseButton) =>
        {
            if (mouseButton != 0 || !CanManageEconomySelection()) return;
            List<PlayerListEntry> online = GetPlayerListEntries().Where(item => item.IsOnline).ToList();
            if (online.Count == 0) { _economyStatus = "No online players available."; SetAllChildrenDirty(); return; }
            int index = online.FindIndex(item => string.Equals(item.PlayerId, _economyTargetId,
                System.StringComparison.OrdinalIgnoreCase));
            _economyTargetId = online[(index + 1) % online.Count].PlayerId;
            _economyStatus = "Selected " + GetOnlinePlayerName(_economyTargetId, "player") + ".";
            SetAllChildrenDirty();
        };
        BindEconomyChangeButton("btnEconomyAdd", true);
        BindEconomyChangeButton("btnEconomyRemove", false);
    }

    private void BindEconomyChangeButton(string id, bool add)
    {
        XUiC_SimpleButton button = FindDescendant<XUiC_SimpleButton>(this, id);
        if (button == null) return;
        button.OnPressed += (sender, mouseButton) =>
        {
            if (mouseButton != 0 || !CanManageEconomy(add)) return;
            if (!int.TryParse(_economyAmountInput?.Text, out int amount) || amount <= 0 || amount > 1000000)
            { _economyStatus = "Amount must be between 1 and 1,000,000."; SetAllChildrenDirty(); return; }
            EntityPlayer player = ResolveOnlineEntity(_economyTargetId);
            if (player == null) { _economyStatus = "Selected player is no longer online."; SetAllChildrenDirty(); return; }
            int before = GetCurrencyBalance(player);
            ItemValue coin = ItemClass.GetItem("casinoCoin", false);
            if (add)
            {
                ItemStack stack = new ItemStack(coin, amount);
                if (player.inventory == null || !player.inventory.AddItem(stack))
                {
                    if (player.bag == null || !player.bag.AddItem(stack))
                    { _economyStatus = "Inventory is full; no coins were added."; SetAllChildrenDirty(); return; }
                }
            }
            else
            {
                int remaining = amount;
                if (player.inventory != null)
                {
                    int available = player.inventory.GetItemCount(coin, false, -1, -1, true);
                    int take = Mathf.Min(available, remaining);
                    if (take > 0) { player.inventory.DecItem(coin, take, false, null); remaining -= take; }
                }
                if (remaining > 0 && player.bag != null)
                {
                    int available = player.bag.GetItemCount(coin, -1, -1, true);
                    int take = Mathf.Min(available, remaining);
                    if (take > 0) player.bag.DecItem(coin, take, false, null);
                }
            }
            int after = GetCurrencyBalance(player);
            _economyStatus = $"{player.EntityName}: {before:N0} → {after:N0} ({(add ? "+" : "-")}{amount:N0})";
            SetAllChildrenDirty();
        };
    }

    private bool CanManageEconomySelection()
    {
        return HasPermission(PermissionIds.Owner) ||
               HasPermission(PermissionIds.EconomyManage) ||
               HasPermission(PermissionIds.EconomyBalanceAdd) ||
               HasPermission(PermissionIds.EconomyBalanceRemove);
    }

    private bool CanManageEconomy(bool add)
    {
        return HasPermission(PermissionIds.Owner) ||
               HasPermission(PermissionIds.EconomyManage) ||
               HasPermission(add ? PermissionIds.EconomyBalanceAdd :
                                   PermissionIds.EconomyBalanceRemove);
    }

    private EntityPlayer ResolveOnlineEntity(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId) || ConnectionManager.Instance?.Clients?.List == null ||
            GameManager.Instance?.World?.Players?.dict == null) return null;
        foreach (ClientInfo client in ConnectionManager.Instance.Clients.List)
        {
            if (!PlayerIdentityResolver.TryResolve(client, out string id) ||
                !string.Equals(id, playerId, System.StringComparison.OrdinalIgnoreCase)) continue;
            if (GameManager.Instance.World.Players.dict.TryGetValue(client.entityId, out EntityPlayer player))
                return player;
        }
        return null;
    }

    private int GetCurrencyBalance()
    {
        EntityPlayerLocal player = xui?.playerUI?.entityPlayer;

        if (player == null)
        {
            return 0;
        }

        ItemValue currency = ItemClass.GetItem("casinoCoin", false);
        int balance = 0;

        if (player.inventory != null)
        {
            balance += player.inventory.GetItemCount(
                currency, false, -1, -1, true);
        }

        if (player.bag != null)
        {
            balance += player.bag.GetItemCount(
                currency, -1, -1, true);
        }

        return balance;
    }

    private static int GetCurrencyBalance(EntityPlayer player)
    {
        if (player == null) return 0;
        ItemValue currency = ItemClass.GetItem("casinoCoin", false);
        int balance = player.inventory == null ? 0 :
            player.inventory.GetItemCount(currency, false, -1, -1, true);
        if (player.bag != null) balance += player.bag.GetItemCount(currency, -1, -1, true);
        return balance;
    }

    private void DragHandle_OnDrag(
        XUiController sender,
        EDragType dragType,
        Vector2 delta)
    {
        if (dragType != EDragType.Dragging || ViewComponent == null)
        {
            _mainDragRemainder = Vector2.zero;
            return;
        }

        // Never let the popup/resize animation compete with a pointer gesture.
        _popupProgress = 1f;
        _windowScale = _targetWindowScale;
        if (ViewComponent.UiTransform != null)
            ViewComponent.UiTransform.localScale =
                new Vector3(_windowScale, _windowScale, 1f);

        _mainDragRemainder += delta;
        int moveX = Mathf.RoundToInt(_mainDragRemainder.x);
        int moveY = Mathf.RoundToInt(_mainDragRemainder.y);
        _mainDragRemainder -= new Vector2(moveX, moveY);
        Vector2i position = ViewComponent.Position;
        ViewComponent.Position = new Vector2i(
            position.x + moveX,
            position.y + moveY);
    }

    private void ResizeHandle_OnDrag(
        XUiController sender,
        EDragType dragType,
        Vector2 delta)
    {
        if (dragType != EDragType.Dragging || ViewComponent == null)
        {
            _mainResizeRemainder = 0f;
            return;
        }

        string handle = sender?.ViewComponent?.ID ?? string.Empty;
        bool left = handle.IndexOf("Left", System.StringComparison.OrdinalIgnoreCase) >= 0;
        bool right = handle.IndexOf("Right", System.StringComparison.OrdinalIgnoreCase) >= 0;
        bool top = handle.IndexOf("Top", System.StringComparison.OrdinalIgnoreCase) >= 0;
        bool bottom = handle.IndexOf("Bottom", System.StringComparison.OrdinalIgnoreCase) >= 0;
        float horizontal = left ? -delta.x : right ? delta.x : 0f;
        float vertical = top ? delta.y : bottom ? -delta.y : 0f;
        float pixelDelta = horizontal != 0f && vertical != 0f
            ? (horizontal + vertical) * 0.5f
            : horizontal != 0f ? horizontal : vertical;

        _mainResizeRemainder += pixelDelta;
        pixelDelta = _mainResizeRemainder;
        _mainResizeRemainder = 0f;

        float oldScale = _targetWindowScale;
        _targetWindowScale = Mathf.Clamp(
            _targetWindowScale + pixelDelta / 560f,
            MinimumPanelScale,
            MaximumPanelScale);

        float scaleChange = _targetWindowScale - oldScale;

        if (Mathf.Abs(scaleChange) < 0.0001f)
        {
            return;
        }

        // Apply the scale synchronously while the pointer is down. Smoothing the
        // same transform during a drag causes the visible window to oscillate.
        _popupProgress = 1f;
        _windowScale = _targetWindowScale;
        if (ViewComponent.UiTransform != null)
            ViewComponent.UiTransform.localScale =
                new Vector3(_windowScale, _windowScale, 1f);
    }

    private void CloseButton_OnPressed(
        XUiController sender,
        int mouseButton)
    {
        if (mouseButton != 0 ||
            xui == null ||
            xui.playerUI == null ||
            xui.playerUI.windowManager == null ||
            WindowGroup == null)
        {
            return;
        }

        xui.playerUI.windowManager.Close(
            WindowGroup.Id
        );
    }
}

public class XUiC_cHubMagicTeleportRequest : XUiController
{
    private float _popup;

    public override void Init()
    {
        base.Init();
        Bind("btnMagicYes", true);
        Bind("btnMagicNo", false);
    }

    public override void OnOpen()
    {
        base.OnOpen();
        _popup = 0f;
        if (ViewComponent?.UiTransform != null)
            ViewComponent.UiTransform.localScale = new Vector3(0.68f, 0.68f, 1f);
        SetAllChildrenDirty();
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        if (_popup >= 1f || ViewComponent?.UiTransform == null) return;
        _popup = Mathf.Min(1f, _popup + deltaTime * 4.2f);
        float t = _popup;
        float ease = 1f + 2.7f * Mathf.Pow(t - 1f, 3f) + 1.7f * Mathf.Pow(t - 1f, 2f);
        float scale = Mathf.Lerp(0.68f, 1f, ease);
        ViewComponent.UiTransform.localScale = new Vector3(scale, scale, 1f);
    }

    public override bool GetBindingValueInternal(ref string value, string bindingName)
    {
        if (bindingName == "chub_magic_requester")
        {
            value = AdminPanelService.PendingMagicRequesterName ?? "Un jucator";
            return true;
        }
        return base.GetBindingValueInternal(ref value, bindingName);
    }

    private void Bind(string id, bool accepted)
    {
        XUiC_SimpleButton button = FindDescendant<XUiC_SimpleButton>(this, id);
        if (button == null) return;
        button.OnPressed += (sender, mouseButton) =>
        {
            if (mouseButton != 0) return;
            AdminPanelService.AnswerMagicTeleportRequest(accepted);
            xui?.playerUI?.windowManager?.Close(WindowGroup.Id);
        };
    }

    private static T FindDescendant<T>(XUiController root, string id) where T : XUiController
    {
        if (root == null) return null;
        foreach (XUiController child in root.Children)
        {
            if (child is T typed && child.ViewComponent != null &&
                string.Equals(child.ViewComponent.ID, id, System.StringComparison.OrdinalIgnoreCase)) return typed;
            T nested = FindDescendant<T>(child, id);
            if (nested != null) return nested;
        }
        return null;
    }
}
