using UnityEngine;
using cHub.Core;

public class XUiC_cHubMainMenuBrand : XUiController
{
    private const string DiscordUrl = "https://discord.gg/s9DAmRgsAg";
    private XUiC_SimpleButton _consoleButton;
    private XUiC_SimpleButton _openButton;
    private XUiC_SimpleButton _copyButton;
    private bool _buttonsBound;
    private int _lastHandledFrame = -1;

    public override void Init()
    {
        base.Init();
        XUiC_cHubLoadingScreen.LoadingStateChanged += LoadingStateChanged;
        LoadingStateChanged(XUiC_cHubLoadingScreen.IsLoadingActive);
        BindButtons();
    }

    public override void OnOpen()
    {
        base.OnOpen();
        BindButtons();
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        if (ViewComponent == null || !ViewComponent.IsVisible ||
            !Input.GetMouseButtonDown(0) || _lastHandledFrame == Time.frameCount) return;

        // mainMenu contains a full-screen vanilla collider which can consume
        // XUiC_SimpleButton.OnPressed before a mod-appended child receives it.
        // The brand is pinned to the physical top-left corner, so use the same
        // screen rectangles as a deterministic fallback for this window only.
        Vector3 mouse = Input.mousePosition;
        Vector2 point = new Vector2(mouse.x, Screen.height - mouse.y);
        if (new Rect(252f, 6f, 112f, 36f).Contains(point))
        {
            _lastHandledFrame = Time.frameCount;
            StartupTerminal.ShowFloatingConsole();
        }
        else if (new Rect(372f, 6f, 128f, 36f).Contains(point))
        {
            _lastHandledFrame = Time.frameCount;
            StartupTerminal.OpenDiscord();
        }
        else if (new Rect(508f, 6f, 128f, 36f).Contains(point))
        {
            _lastHandledFrame = Time.frameCount;
            StartupTerminal.CopyDiscord();
        }
    }

    private void LoadingStateChanged(bool loading)
    {
        if (ViewComponent != null) ViewComponent.IsVisible = !loading;
    }

    public override bool GetBindingValueInternal(ref string value,
        string bindingName)
    {
        if (bindingName == "chub_menu_version")
        {
            value = "c/Hub  •  v" + cHubVersion.Current;
            return true;
        }
        if (bindingName == "chub_menu_eac")
        {
            value = StartupTerminal.IsEacActive ? "EAC ON" : "EAC OFF";
            return true;
        }
        return base.GetBindingValueInternal(ref value, bindingName);
    }

    private void BindButtons()
    {
        if (_buttonsBound) return;

        _consoleButton =
            GetChildById("btnChubConsole") as XUiC_SimpleButton ??
            FindDescendant<XUiC_SimpleButton>(this, "btnChubConsole");

        _openButton =
            GetChildById("btnChubDiscordOpen") as XUiC_SimpleButton ??
            FindDescendant<XUiC_SimpleButton>(this, "btnChubDiscordOpen");

        _copyButton =
            GetChildById("btnChubDiscordCopy") as XUiC_SimpleButton ??
            FindDescendant<XUiC_SimpleButton>(this, "btnChubDiscordCopy");

        if (_consoleButton == null ||
            _openButton == null ||
            _copyButton == null)
        {
            return;
        }

        _consoleButton.OnPressed += Console_OnPressed;
        _openButton.OnPressed += Open_OnPressed;
        _copyButton.OnPressed += Copy_OnPressed;

        _buttonsBound = true;
    }
    private void Console_OnPressed(XUiController sender, int mouseButton)
    {
        if ((mouseButton != 0 && mouseButton != -1) ||
            _lastHandledFrame == Time.frameCount)
        {
            return;
        }

        _lastHandledFrame = Time.frameCount;

        StartupTerminal.ShowFloatingConsole();
    }

    private void Open_OnPressed(XUiController sender, int mouseButton)
    {
        if ((mouseButton != 0 && mouseButton != -1) ||
            _lastHandledFrame == Time.frameCount)
        {
            return;
        }

        _lastHandledFrame = Time.frameCount;
        StartupTerminal.OpenDiscord();
    }

    private void Copy_OnPressed(XUiController sender, int mouseButton)
    {
        if ((mouseButton != 0 && mouseButton != -1) ||
            _lastHandledFrame == Time.frameCount)
        {
            return;
        }

        _lastHandledFrame = Time.frameCount;
        StartupTerminal.CopyDiscord();
    }

    private static T FindDescendant<T>(XUiController root, string id) where T : XUiController
    {
        if (root == null) return null;
        foreach (XUiController child in root.Children)
        {
            if (child is T typed && child.ViewComponent != null &&
                string.Equals(child.ViewComponent.ID, id, System.StringComparison.OrdinalIgnoreCase))
                return typed;
            T nested = FindDescendant<T>(child, id);
            if (nested != null) return nested;
        }
        return null;
    }
}
