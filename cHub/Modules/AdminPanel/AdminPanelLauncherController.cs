using UnityEngine;
using cHub.Modules.AdminPanel;

public class XUiC_cHubAdminPanelLauncher : XUiController
{
    private XUiC_SimpleButton _openButton;
    private XUiController _image;
    private bool _hovered;
    private bool _pressed;
    private float _visualTime;

    public override void Init()
    {
        base.Init();

        _openButton = GetChildById("btnOpenChub") as XUiC_SimpleButton;

        if (_openButton != null)
        {
            _openButton.OnPressed += OpenButton_OnPressed;
            _openButton.OnHovered += (sender, isOver) =>
            {
                _hovered = isOver;
                cHub.Core.StartupTerminal.Trace("UI", isOver ? "CRAFTING_HOVER_ENTER" : "CRAFTING_HOVER_EXIT", "launcher");
            };
            _openButton.OnMouseUpDown += (sender, down) =>
            {
                _pressed = down;
                cHub.Core.StartupTerminal.Trace("UI", down ? "CRAFTING_PRESS" : "CRAFTING_RELEASE", "launcher");
            };

            cHub.Shared.Utils.Logger.Warning(
                "[cHub] Header btnOpenChub bound successfully.");
        }
        else
        {
            cHub.Shared.Utils.Logger.Warning(
                "[cHub] Header btnOpenChub NOT FOUND.");
        }

        _image = GetChildById("chubMenuImage");
    }

    public override void OnOpen()
    {
        base.OnOpen();

        if (ViewComponent?.UiTransform != null)
            ViewComponent.UiTransform.localScale = Vector3.one;
        _hovered = false;
        _pressed = false;
        _visualTime = 0f;
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        // Intentionally static while idle. Hover/pressed feedback is handled
        // by the XUi button itself and does not consume an update animation.
        _visualTime += deltaTime;
        float targetScale = _pressed ? 0.965f : _hovered ? 1.035f : 1f;
        if (_image?.ViewComponent?.UiTransform != null)
        {
            Vector3 current = _image.ViewComponent.UiTransform.localScale;
            _image.ViewComponent.UiTransform.localScale = Vector3.Lerp(
                current, new Vector3(targetScale, targetScale, 1f),
                1f - Mathf.Exp(-deltaTime * 15f));
        }

    }

    private void OpenButton_OnPressed(
        XUiController sender,
        int mouseButton)
    {
        cHub.Shared.Utils.Logger.Warning(
            $"[cHub] Header c/Hub PRESSED // mouseButton={mouseButton}");
        cHub.Core.StartupTerminal.Trace(
            "UI", "CRAFTING_LAUNCHER_CLICK", "mouse=" + mouseButton);

        // XUi SimpleButton poate trimite -1 pentru click normal.
        // Acceptam atat -1, cat si 0.
        if (mouseButton != -1 && mouseButton != 0)
        {
            return;
        }

        if (xui?.playerUI?.windowManager == null)
        {
            cHub.Shared.Utils.Logger.Warning(
                "[cHub] Header c/Hub failed: WindowManager is null.");

            return;
        }

        string error;

        bool opened =
            AdminPanelService.TryOpenLocal(out error);

        if (!opened)
        {
            cHub.Shared.Utils.Logger.Warning(
                $"[cHub] Admin Panel launcher failed: {error}");

            return;
        }

        cHub.Shared.Utils.Logger.Warning(
            "[cHub] Header c/Hub menu opened successfully.");
    }
}
