using cHub.Core;
using UnityEngine;

public class XUiC_cHubInGameMenuWindow : XUiC_InGameMenuWindow
{
    private XUiC_SimpleButton _pauseButton;
    private XUiC_SimpleButton _saveButton;
    private bool _pausedByUser;
    private string _saveCaption = "SAVE WORLD";
    private XUiController _bottomActions;
    private XUiController _moveHandle;
    private XUiController _resizeHandle;
    private float _actionsScale = 1f;

    public override void Init()
    {
        base.Init();
        _pauseButton = GetChildById("btnChubPause") as XUiC_SimpleButton;
        _saveButton = GetChildById("btnChubSaveWorld") as XUiC_SimpleButton;
        _bottomActions = GetChildById("chubBottomActions");
        _moveHandle = GetChildById("chubBottomActionsMove");
        _resizeHandle = GetChildById("chubBottomActionsResize");
        BindBottomActionsLayout();
        if (_pauseButton != null)
        {
            _pauseButton.OnPressed += (sender, mouseButton) =>
            {
                if (mouseButton != -1 && mouseButton != 0) return;
                _pausedByUser = !_pausedByUser;
                ApplyPause(_pausedByUser);
                SetAllChildrenDirty();
            };
            StartupTerminal.Trace("UI", "PAUSE_BUTTON_BIND", "ready=true");
        }
        else StartupTerminal.Trace("UI", "PAUSE_BUTTON_BIND", "ready=false");
        if (_saveButton != null)
        {
            _saveButton.OnPressed += (sender, mouseButton) =>
            {
                if (mouseButton != -1 && mouseButton != 0) return;
                SaveWorldNow();
            };
            StartupTerminal.Trace("UI", "SAVE_WORLD_BUTTON_BIND", "ready=true");
        }
        else StartupTerminal.Trace("UI", "SAVE_WORLD_BUTTON_BIND", "ready=false");
    }

    private void BindBottomActionsLayout()
    {
        Transform root = _bottomActions?.ViewComponent?.UiTransform;
        if (root == null) return;
        Vector3 position = root.localPosition;
        position.x = PlayerPrefs.GetFloat("cHub.BottomActionsPositionX", position.x);
        position.y = PlayerPrefs.GetFloat("cHub.BottomActionsPositionY", position.y);
        root.localPosition = position;
        _actionsScale = Mathf.Clamp(PlayerPrefs.GetFloat("cHub.BottomActionsScale", 1f), 0.65f, 1.50f);
        root.localScale = new Vector3(_actionsScale, _actionsScale, 1f);
        if (_moveHandle != null) _moveHandle.OnDrag += (sender, type, delta) =>
        {
            if (type != EDragType.Dragging) return;
            Vector3 moved = root.localPosition;
            moved.x += delta.x; moved.y += delta.y;
            root.localPosition = moved;
            PlayerPrefs.SetFloat("cHub.BottomActionsPositionX", moved.x);
            PlayerPrefs.SetFloat("cHub.BottomActionsPositionY", moved.y);
        };
        if (_resizeHandle != null) _resizeHandle.OnDrag += (sender, type, delta) =>
        {
            if (type != EDragType.Dragging) return;
            _actionsScale = Mathf.Clamp(_actionsScale + (delta.x - delta.y) / 500f, 0.65f, 1.50f);
            root.localScale = new Vector3(_actionsScale, _actionsScale, 1f);
            PlayerPrefs.SetFloat("cHub.BottomActionsScale", _actionsScale);
        };
        if (_moveHandle != null) _moveHandle.OnMouseUpDown += (sender, down) => { if (!down) PlayerPrefs.Save(); };
        if (_resizeHandle != null) _resizeHandle.OnMouseUpDown += (sender, down) => { if (!down) PlayerPrefs.Save(); };
        StartupTerminal.Trace("UI", "BOTTOM_ACTIONS_LAYOUT", "drag=true resize=true grouped=true");
    }

    public override void OnOpen()
    {
        base.OnOpen();
        _pausedByUser = false;
        ApplyPause(false);
        SetAllChildrenDirty();
    }

    public override void OnClose()
    {
        ApplyPause(false);
        _pausedByUser = false;
        base.OnClose();
    }

    public override bool GetBindingValueInternal(ref string value, string bindingName)
    {
        if (bindingName == "chub_pause_game")
        {
            value = _pausedByUser ? "RESUME" : "PAUSE";
            return true;
        }
        if (bindingName == "chub_save_world")
        {
            value = _saveCaption;
            return true;
        }
        return base.GetBindingValueInternal(ref value, bindingName);
    }

    private void ApplyPause(bool paused)
    {
        try
        {
            GameManager.Instance?.Pause(paused);
            bool effective = GameManager.Instance != null && GameManager.Instance.IsPaused();
            StartupTerminal.Trace("GAME", "PAUSE", "requested=" + paused + " effective=" + effective);
        }
        catch (System.Exception ex)
        {
            StartupTerminal.Trace("GAME", "PAUSE_FAILED", ex.GetType().Name + ": " + ex.Message);
        }
    }

    private void SaveWorldNow()
    {
        try
        {
            _saveCaption = "SAVING...";
            SetAllChildrenDirty();
            var timer = System.Diagnostics.Stopwatch.StartNew();
            GameManager.Instance?.SaveWorld();
            GameManager.Instance?.SaveLocalPlayerData();
            timer.Stop();
            _saveCaption = "WORLD SAVED";
            StartupTerminal.Trace("GAME", "SAVE_WORLD", "success=true elapsedMs=" + timer.ElapsedMilliseconds);
            SetAllChildrenDirty();
        }
        catch (System.Exception ex)
        {
            _saveCaption = "SAVE FAILED";
            StartupTerminal.Trace("GAME", "SAVE_WORLD", "success=false " + ex.GetType().Name + ": " + ex.Message);
            SetAllChildrenDirty();
        }
    }
}
