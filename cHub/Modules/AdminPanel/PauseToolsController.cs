using cHub.Core;
using System.IO;
using cHub.Modules.AdminPanel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;



public class XUiC_cHubPauseTools : XUiController
{
    private void DebugGfxPresetPanelState(string stage)
    {
        XUiController gfxScroll =
            FindDescendant<XUiController>(this, "gfxPresetScroll");

        if (!(gfxScroll?.ViewComponent is XUiV_ScrollView scrollView))
            return;

        BindingFlags flags =
            BindingFlags.Instance |
            BindingFlags.NonPublic |
            BindingFlags.Public;

        FieldInfo scrollViewField =
            typeof(XUiV_ScrollView).GetField(
                "scrollView",
                flags);

        object nguiScroll =
            scrollViewField?.GetValue(scrollView);

        if (nguiScroll == null)
        {
            UnityEngine.Debug.Log(
                "[cHub] [GFXPANEL] " + stage +
                " // UIScrollView missing");

            return;
        }

        Type scrollType =
            nguiScroll.GetType();

        FieldInfo panelField =
            scrollType.GetField(
                "mPanel",
                flags);

        object panel =
            panelField?.GetValue(nguiScroll);

        // Unele build-uri NGUI pot folosi "panel" în loc de "mPanel".
        if (panel == null)
        {
            panelField =
                scrollType.GetField(
                    "panel",
                    flags);

            panel =
                panelField?.GetValue(nguiScroll);
        }

        if (panel == null)
        {
            UnityEngine.Debug.Log(
                "[cHub] [GFXPANEL] " + stage +
                " // UIPanel missing");

            return;
        }

        Type panelType =
            panel.GetType();

        string result =
            "[cHub] [GFXPANEL] " +
            stage;

        string[] propertyNames =
        {
        "clipOffset",
        "baseClipRegion",
        "finalClipRegion",
        "clipping",
        "alpha",
        "isVisible"
    };

        foreach (string propertyName in propertyNames)
        {
            PropertyInfo property =
                panelType.GetProperty(
                    propertyName,
                    flags);

            if (property == null)
                continue;

            if (property.GetIndexParameters().Length != 0)
                continue;

            try
            {
                object value =
                    property.GetValue(
                        panel,
                        null);

                result +=
                    " // " +
                    propertyName +
                    "=" +
                    (value == null
                        ? "null"
                        : value.ToString());
            }
            catch
            {
            }
        }

        FieldInfo clipOffsetField =
            panelType.GetField(
                "mClipOffset",
                flags);

        if (clipOffsetField != null)
        {
            object value =
                clipOffsetField.GetValue(panel);

            result +=
                " // mClipOffset=" +
                (value == null
                    ? "null"
                    : value.ToString());
        }

        FieldInfo clipRangeField =
            panelType.GetField(
                "mClipRange",
                flags);

        if (clipRangeField != null)
        {
            object value =
                clipRangeField.GetValue(panel);

            result +=
                " // mClipRange=" +
                (value == null
                    ? "null"
                    : value.ToString());
        }

        Component panelComponent =
            panel as Component;

        if (panelComponent != null)
        {
            Transform t =
                panelComponent.transform;

            result +=
                " // panelLocal=" +
                t.localPosition +
                " // panelWorld=" +
                t.position +
                " // panelActive=" +
                t.gameObject.activeInHierarchy;
        }

        result +=
            " // scrollLocal=" +
            scrollView.UiTransform.localPosition +
            " // scrollClip=" +
            scrollView.ClipOffset;

        UnityEngine.Debug.Log(
            result);
    }
    private void BackToGfxPresets()
    {
        _gfxPresetsVisible = true;
        _gfxPresetsMinimized = false;
        _gfxDevGraphicsVisible = false;

        RefreshGfxPresetFilter();

        SyncGfxFlyoutVisibility();

        ConfigureGfxPresetNativeClip();

        ResetGfxPresetScroll();

        DebugGfxPresetScrollState("BACK");

        StartupTerminal.Audit(
            "DEV_GRAPHICS",
            "BACK",
            "OK",
            "presetVisible=true devVisible=false");
    }
    private void DebugGfxPresetNguiMotion(string stage)
    {
        XUiController gfxScroll =
            FindDescendant<XUiController>(this, "gfxPresetScroll");

        if (!(gfxScroll?.ViewComponent is XUiV_ScrollView scrollView))
            return;

        BindingFlags flags =
            BindingFlags.Instance |
            BindingFlags.NonPublic |
            BindingFlags.Public;

        FieldInfo scrollViewField =
            typeof(XUiV_ScrollView).GetField(
                "scrollView",
                flags);

        object nguiScroll =
            scrollViewField?.GetValue(scrollView);

        if (nguiScroll == null)
        {
            UnityEngine.Debug.Log(
                "[cHub] [GFXMOTION] " + stage +
                " // UIScrollView missing");
            return;
        }

        Type type =
            nguiScroll.GetType();

        string[] fieldNames =
        {
        "mMomentum",
        "momentum",
        "mScroll",
        "mPressed",
        "mDragID",
        "mShouldMove",
        "mStarted",
        "mIgnoreCallbacks"
    };

        string result =
            "[cHub] [GFXMOTION] " +
            stage;

        foreach (string fieldName in fieldNames)
        {
            FieldInfo field =
                type.GetField(
                    fieldName,
                    flags);

            if (field == null)
                continue;

            object value =
                field.GetValue(nguiScroll);

            result +=
                " // " +
                fieldName +
                "=" +
                (value == null
                    ? "null"
                    : value.ToString());
        }

        PropertyInfo shouldMoveVertically =
            type.GetProperty(
                "shouldMoveVertically",
                flags);

        PropertyInfo canMoveVertically =
            type.GetProperty(
                "canMoveVertically",
                flags);

        if (shouldMoveVertically != null)
        {
            result +=
                " // shouldMoveVertically=" +
                shouldMoveVertically.GetValue(
                    nguiScroll,
                    null);
        }

        if (canMoveVertically != null)
        {
            result +=
                " // canMoveVertically=" +
                canMoveVertically.GetValue(
                    nguiScroll,
                    null);
        }

        result +=
            " // local=" +
            scrollView.UiTransform.localPosition +
            " // clip=" +
            scrollView.ClipOffset;

        UnityEngine.Debug.Log(
            result);
    }


    private string _status = string.Empty;
    private bool _contextVisible;
    private bool _displaySubmenuVisible;
    private bool _gfxPanelVisible;
    private string _gfxStatus = "READY • Changes apply locally in real time";
    private int _gfxAf = 1, _gfxDynamic, _gfxDt = 1, _gfxDti = 1, _gfxLod = 2;
    private int _gfxPixel = 40, _gfxView = 8, _gfxSkin = 1, _gfxStream = 1;
    private int _gfxBias, _gfxLimit;
    private float _interfaceScale = 1f;
    private float _taskbarScale = 1f;
    private float _gfxUiScale = 1f;
    private readonly Dictionary<XUiController, Vector3> _taskbarBasePositions =
        new Dictionary<XUiController, Vector3>();
    private readonly Dictionary<XUiController, Vector3> _taskbarBaseScales =
        new Dictionary<XUiController, Vector3>();
    private float _interfaceScaleRefresh;
    private float _baseHudSize = 1f;
    private float _baseActiveUiScale = 1f;
    private int _gfxPresetVisibleCount;

    private const int GfxCustomPresetSlots = 10;

    private readonly HashSet<int> _gfxBoundCustomPresetSlots =
        new HashSet<int>();
    private int _gfxAdvAa=4, _gfxAdvGrass=3, _gfxAdvObjects=3, _gfxAdvReflections=2, _gfxAdvShadows=3;
    private int _gfxDevUpscaler, _gfxDevFsr, _gfxDevWater=2, _gfxDevWaterParticles=1;
    private int _gfxDevOcclusion=1, _gfxDevReflectShadows=1, _gfxDevSigns=2, _gfxDevUma=1;
    private int _gfxDevTexFilter=2, _gfxDevLod=2, _gfxDevDynamicMinFps=60, _gfxDevInternalLimit;
    private readonly Dictionary<string, int> _devOriginalIntPrefs = new Dictionary<string, int>();
    private readonly Dictionary<string, bool> _devOriginalBoolPrefs = new Dictionary<string, bool>();
    private readonly Dictionary<string, float> _devOriginalFloatPrefs = new Dictionary<string, float>();
    private bool _gfxAo = true, _gfxBloom = true, _gfxExposure = true, _gfxColor = true;
    private bool _gfxSsao = true, _gfxSunshafts = true, _gfxDof = true, _gfxMotionBlur;
    private bool _gfxVanillaCaptured;
    private bool _vanillaSsao, _vanillaBloom, _vanillaSunshafts, _vanillaDof, _vanillaMotionBlur;
    private int _gfxResolutionIndex;
    private int _gfxWindowMode;
    private bool _gfxSlidersMinimized;
    private bool _gfxPresetsVisible;
    private bool _gfxPresetsMinimized;
    private bool _gfxDevGraphicsVisible;
    private string _gfxSelectedPreset = "maxfps";
    private string _gfxPresetName = "Client save preset";
    private XUiC_TextInput _gfxPresetNameInput;
    private XUiC_TextInput _gfxPresetSearchInput;
    private string _gfxPresetSearch = string.Empty;
    private readonly Queue<string> _gfxCommandQueue = new Queue<string>();
    private float _gfxCommandQueueDelay;

    private float _gfxPresetManualY;
    private float _gfxPresetScrollTopY = float.NaN;
    private string _pendingSliderCommand;
    private string _pendingSliderDescription;
    private bool _gfxVisualDirty = true;
    private float _lastAppliedPanelOpacity = -1f;
    private Vector2 _gfxDragRemainder;
    private readonly List<string> _gfxResolutions = new List<string>();
    internal static bool ShowFpsOverlay { get; private set; } = true;
    private float _smoothedFps;
    private float _contextAnimation;
    private float _fpsBindingRefresh;

    private float _gfxScrollDebugTimer;
    private float _liveTextRefresh;
    private float _statusTimer;
    private string _status2 = string.Empty, _status3 = string.Empty;
    private float _statusTimer2, _statusTimer3;
    private float _bindingRefreshBurst;
    private float _fogStartDistance;
    private float _fogEndDistance;
    private static bool _fogOverride;
    private static bool _fogEnabled;
    private static float _fogDensity;
    private static bool _frameLimitOverride;
    private static int _frameLimit;
    private static bool _shadowOverride;
    private static ShadowQuality _shadowQuality;
    private static float _shadowDistance;
    private static bool _ambientOverride;
    private static float _ambientIntensity;
    private static bool _defaultsCaptured;
    private static bool _fullVanillaSnapshotCaptured;
    private static readonly Dictionary<string, int> _vanillaIntPrefs = new Dictionary<string, int>();
    private static readonly Dictionary<string, bool> _vanillaBoolPrefs = new Dictionary<string, bool>();
    private static readonly Dictionary<string, float> _vanillaFloatPrefs = new Dictionary<string, float>();
    private static bool _defaultFogEnabled;
    private static float _defaultFogDensity, _defaultFogStart, _defaultFogEnd;
    private static FogMode _defaultFogMode;
    private static int _defaultScreenWidth, _defaultScreenHeight;
    private static FullScreenMode _defaultFullScreenMode;
    private static int _vanillaGfxAf, _vanillaGfxDynamic, _vanillaGfxDt, _vanillaGfxDti, _vanillaGfxLod;
    private static int _vanillaGfxPixel, _vanillaGfxView, _vanillaGfxSkin, _vanillaGfxStream;
    private static int _vanillaGfxBias, _vanillaGfxLimit;
    private static bool _vanillaGfxAo, _vanillaGfxExposure, _vanillaGfxColor;
    private static bool _fpsBoostActive;
    private static bool _qualityBoostActive;
    private static bool _fpsBoostSnapshotCaptured;
    private static readonly Dictionary<string, int> _fpsBoostIntPrefs = new Dictionary<string, int>();
    private static readonly Dictionary<string, bool> _fpsBoostBoolPrefs = new Dictionary<string, bool>();
    private static readonly Dictionary<string, float> _fpsBoostFloatPrefs = new Dictionary<string, float>();
    private static int _boostAa, _boostPixelLights, _boostMaxLod, _boostParticleBudget;
    private static int _boostAsyncSlice, _boostAsyncBuffer, _boostVsync, _boostTargetFps;
    private static float _boostLodBias, _boostShadowDistance;
    private static ShadowQuality _boostShadows;
    private static AnisotropicFiltering _boostAniso;
    private static bool _boostRealtimeReflections, _boostSoftVegetation, _boostStreaming;
    private static int _boostGfxAf, _boostGfxDynamic, _boostGfxDt, _boostGfxDti, _boostGfxLod;
    private static int _boostGfxPixel, _boostGfxView, _boostGfxSkin, _boostGfxStream, _boostGfxBias, _boostGfxLimit;
    private static bool _boostGfxAo, _boostGfxBloom, _boostGfxExposure, _boostGfxColor;
    private static bool _boostGfxSsao, _boostGfxSunshafts, _boostGfxDof, _boostGfxMotionBlur;
    private static int _defaultShadowQuality;
    private static int _defaultShadowDistance;
    private static float _defaultBrightness;
    private static int _defaultAntiAliasing;
    private static ShadowQuality _defaultUnityShadows;
    private static float _defaultUnityShadowDistance;
    private static float _defaultUnityAmbient;
    private static int _defaultTargetFrameRate;
    private static bool _defaultShowFpsOverlay;
    private static bool _showingDefaults;
    private XUiController _contextPanel;
    private XUiController _displaySubmenu;
    private XUiController _notificationPanel;
    private XUiController _notificationPanel2, _notificationPanel3;
    private XUiController _gfxPanel;
    private bool _gfxSliderDragging;
    private float _gfxPanelOpacity = 1f;
    private float _gfxSliderApplyCooldown;
    private Vector3 _notificationVisiblePosition;
    private readonly Dictionary<string, XUiC_SimpleButton> _liveButtons =
        new Dictionary<string, XUiC_SimpleButton>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, XUiV_Label> _liveLabels =
        new Dictionary<string, XUiV_Label>(StringComparer.OrdinalIgnoreCase);
    private static bool _cameraFogHooked;

    public override void Init()
    {
        try
        {
            base.Init();
            RepairLegacyGfxPrefTypes();

            //====== Modificari i27k ========

            _controlCache.Clear();
            CacheDescendants(this);

            //===============================

            Bind("btnPauseFog", ToggleFog);
            Bind("btnPauseFogDown", () => SetFogDensity((_fogOverride ? _fogDensity : RenderSettings.fogDensity) - 0.01f));
            Bind("btnPauseFogUp", () => SetFogDensity((_fogOverride ? _fogDensity : RenderSettings.fogDensity) + 0.01f));
            Bind("btnPauseFogRangeDown", () => SetFogDistance(RenderSettings.fogEndDistance - 100f));
            Bind("btnPauseFogRangeUp", () => SetFogDistance(RenderSettings.fogEndDistance + 100f));
            Bind("btnPauseLightDown", () => SetBrightness((_ambientOverride ? _ambientIntensity : RenderSettings.ambientIntensity) - 0.10f));
            Bind("btnPauseLightUp", () => SetBrightness((_ambientOverride ? _ambientIntensity : RenderSettings.ambientIntensity) + 0.10f));
            Bind("btnPauseShadowDown", () => SetShadowDistance((_shadowOverride ? _shadowDistance : QualitySettings.shadowDistance) - 20f));
            Bind("btnPauseShadowUp", () => SetShadowDistance((_shadowOverride ? _shadowDistance : QualitySettings.shadowDistance) + 20f));
            Bind("btnPauseVisualReset", ResetVisuals);
            Bind("btnPauseOpenHub", OpenHub);
            Bind("btnPauseVsync", ToggleFpsDisplay);
            Bind("btnPauseConsole", ToggleConsole);
            Bind("btnPauseQualityBoost", ActivateQualityBoost);
            Bind("btnPauseGfxShadows", ToggleShadows);
            Bind("btnPauseFps", ActivateFpsBoost);
            Bind("btnPauseFpsLock", CycleFrameLimit);
            Bind("btnPauseBookTracker", ToggleBookTracker);

            if (!_cameraFogHooked)
            {
                Camera.onPreRender += ApplyFogToMainCamera;
                _cameraFogHooked = true;
            }

            Bind("btnTaskbarSettings", OpenGfxSettingsDirect);

            Bind("btnTaskbarContextClose", CloseContextMenu);
            Bind("btnTaskbarContextDisplay", ToggleDisplaySubmenu);
            Bind("btnTaskbarContextHub", OpenHub);
            Bind("btnTaskbarContextFog", ToggleFog);
            Bind("btnTaskbarContextReset", ResetVisuals);
            Bind("btnTaskbarContextFps", ToggleFpsDisplay);
            Bind("btnTaskbarContextPersonalise", OpenHub);

            Bind("btnContextVsync", ToggleFpsDisplay);
            Bind("btnContextAa", CycleAntiAliasing);
            Bind("btnContextShadows", ToggleShadows);
            Bind("btnContextFpsLimit", CycleFrameLimit);
            Bind("btnContextFogDown", () => SetFogDensity((_fogOverride ? _fogDensity : RenderSettings.fogDensity) - 0.01f));
            Bind("btnContextFogUp", () => SetFogDensity((_fogOverride ? _fogDensity : RenderSettings.fogDensity) + 0.01f));
            Bind("btnContextLightDown", () => SetBrightness((_ambientOverride ? _ambientIntensity : RenderSettings.ambientIntensity) - 0.10f));
            Bind("btnContextLightUp", () => SetBrightness((_ambientOverride ? _ambientIntensity : RenderSettings.ambientIntensity) + 0.10f));
            Bind("btnContextShadowDown", () => SetShadowDistance((_shadowOverride ? _shadowDistance : QualitySettings.shadowDistance) - 20f));
            Bind("btnContextShadowUp", () => SetShadowDistance((_shadowOverride ? _shadowDistance : QualitySettings.shadowDistance) + 20f));

            Bind("btnTaskbarContextGfx", ToggleGfxPanel);
            Bind("btnGfxClose", CloseGfxPanelDirect);
            Bind("btnGfxMinimize", ToggleGfxSliders);
            Bind("btnGfxRestoreSliders", ToggleGfxSliders);
            Bind("btnGfxWindowMode", CycleGfxWindowMode);

            Bind("btnGfxAf", () =>
            {
                _gfxAf = (_gfxAf + 1) % 3;
                ApplyGfx("af " + _gfxAf, "ANISOTROPIC " + _gfxAf);
            });

            Bind("btnGfxDynamic", () =>
            {
                _gfxDynamic = _gfxDynamic == 0 ? 1 : 0;
                ApplyGfx("dr " + _gfxDynamic + " 0.5 1", "DYNAMIC RESOLUTION " + OnOff(_gfxDynamic));
            });

            Bind("btnGfxResolution", CycleGfxResolution);

            Bind("btnGfxDt", () =>
            {
                _gfxDt = 1 - _gfxDt;
                ApplyGfx("dt " + _gfxDt, "DISTANT TERRAIN " + OnOff(_gfxDt));
            });

            Bind("btnGfxDti", () =>
            {
                _gfxDti = 1 - _gfxDti;
                ApplyGfx("dti " + _gfxDti, "TERRAIN INSTANCING " + OnOff(_gfxDti));
            });

            Bind("btnGfxLod", () =>
            {
                _gfxLod = (_gfxLod + 1) % 6;
                ApplyGfx("dtmaxlod " + _gfxLod, "TERRAIN MAX LOD " + _gfxLod);
            });

            Bind("btnGfxPixelDown", () => ChangeGfxInt(ref _gfxPixel, -10, 1, 200, "dtpix", "TERRAIN PIXEL ERROR"));
            Bind("btnGfxPixelUp", () => ChangeGfxInt(ref _gfxPixel, 10, 1, 200, "dtpix", "TERRAIN PIXEL ERROR"));
            Bind("btnGfxViewDown", () => ChangeGfxInt(ref _gfxView, -1, 1, 20, "viewdist", "VIEW DISTANCE"));
            Bind("btnGfxViewUp", () => ChangeGfxInt(ref _gfxView, 1, 1, 20, "viewdist", "VIEW DISTANCE"));

            Bind("btnGfxAo", () => TogglePostProcess(ref _gfxAo, "ao", "AMBIENT OCCLUSION"));
            Bind("btnGfxBloom", () => TogglePostProcess(ref _gfxBloom, "bloom", "BLOOM"));
            Bind("btnGfxExposure", () => TogglePostProcess(ref _gfxExposure, "ae", "AUTO EXPOSURE"));
            Bind("btnGfxColor", () => TogglePostProcess(ref _gfxColor, "cg", "COLOR GRADING"));
            Bind("btnGfxSsao", () => TogglePostProcess(ref _gfxSsao, "ssao", "SSAO"));
            Bind("btnGfxSunshafts", () => TogglePostProcess(ref _gfxSunshafts, "sunshafts", "SUN SHAFTS"));
            Bind("btnGfxDof", () => TogglePostProcess(ref _gfxDof, "dof", "DEPTH OF FIELD"));
            Bind("btnGfxMotionBlur", () => TogglePostProcess(ref _gfxMotionBlur, "motionblur", "MOTION BLUR"));

            Bind("btnGfxCinematicAll", ToggleAllCinematicEffects);

            Bind("btnGfxSkin", () =>
            {
                _gfxSkin =
                    _gfxSkin == 1 ? 2 :
                    _gfxSkin == 2 ? 4 :
                    _gfxSkin == 4 ? 5 : 1;

                ApplyGfx(
                    "skin " + _gfxSkin,
                    "SKIN BONES " + _gfxSkin);
            });

            Bind("btnGfxStream", () =>
            {
                _gfxStream = 1 - _gfxStream;

                ApplyGfx(
                    "st budget " + (_gfxStream == 1 ? "1" : "0"),
                    "MIP STREAMING " + OnOff(_gfxStream));
            });

            Bind("btnGfxBiasDown", () => ChangeGfxInt(ref _gfxBias, -1, -10, 10, "texbias", "TEXTURE BIAS"));
            Bind("btnGfxBiasUp", () => ChangeGfxInt(ref _gfxBias, 1, -10, 10, "texbias", "TEXTURE BIAS"));
            Bind("btnGfxLimitDown", () => ChangeGfxInt(ref _gfxLimit, -1, 0, 8, "texlimit", "TEXTURE LIMIT"));
            Bind("btnGfxLimitUp", () => ChangeGfxInt(ref _gfxLimit, 1, 0, 8, "texlimit", "TEXTURE LIMIT"));

            Bind("btnGfxSave", ToggleGfxPresets);
            Bind("btnGfxLoad", OpenDevGraphicsDirect);

            Bind("btnGfxPresetClose", CloseGfxPresets);
            Bind("btnGfxPresetMinimize", ToggleGfxPresetMinimized);
            Bind("btnGfxRestorePresets", ToggleGfxPresetMinimized);

            Bind("btnGfxPresetMaxFps", () => SelectGfxPreset("maxfps"));
            Bind("btnGfxPresetMaxQuality", () => SelectGfxPreset("maxquality"));
            Bind("btnGfxPresetUltra", () => SelectGfxPreset("ultra"));
            Bind("btnGfxPresetCinema", () => SelectGfxPreset("cinema"));
            Bind("btnGfxPresetClient", () => SelectGfxPreset("client"));

            Bind("btnGfxPresetSave", SaveGfxCustom);
            Bind("btnGfxPresetLoad", LoadSelectedGfxPreset);

            // =========================================================
            // CUSTOM PRESET FILE ACTIONS
            // =========================================================

            Bind(
                "btnGfxPresetOpenDir",
                OpenGfxPresetDirectory);

            Bind(
                "btnGfxPresetDelete",
                DeleteSelectedGfxPreset);

            Bind("btnGfxPresetVanilla", LoadExactVanillaDefaults);
            Bind("btnGfxPresetReset", ResetGfxPanel);

            Bind("btnGfxDevBack", BackToGfxPresets);

            Bind("btnGfxDevUpscaler", () => CycleDev(ref _gfxDevUpscaler, 0, 4, "OptionsGfxUpscalerMode", "UPSCALER"));
            Bind("btnGfxDevFsr", () => CycleDev(ref _gfxDevFsr, 0, 4, "OptionsGfxFSRPreset", "FSR PRESET"));
            Bind("btnGfxDevWater", () => CycleDev(ref _gfxDevWater, 0, 3, "OptionsGfxWaterQuality", "WATER QUALITY"));
            Bind("btnGfxDevWaterParticles", CycleWaterParticleLimiter);
            Bind("btnGfxDevOcclusion", () => ToggleDevBool(ref _gfxDevOcclusion, "OptionsGfxOcclusion", "OCCLUSION"));
            Bind("btnGfxDevReflectShadows", () => ToggleDevBool(ref _gfxDevReflectShadows, "OptionsGfxReflectShadows", "REFLECTION SHADOWS"));
            Bind("btnGfxDevSigns", () => CycleDev(ref _gfxDevSigns, 0, 3, "OptionsGfxSignQuality", "SIGN QUALITY"));
            Bind("btnGfxDevUma", () => CycleDev(ref _gfxDevUma, 0, 3, "OptionsGfxUMATexQuality", "UMA TEXTURES"));
            Bind("btnGfxDevTexFilter", () => CycleDev(ref _gfxDevTexFilter, 0, 3, "OptionsGfxTexFilter", "TEXTURE FILTER"));
            Bind("btnGfxDevLod", CycleDevLodDistance);

            Bind("btnGfxDevMinFps", () =>
            {
                _gfxDevDynamicMinFps =
                    _gfxDevDynamicMinFps >= 240
                        ? 30
                        : _gfxDevDynamicMinFps + 30;

                ApplyAdvancedControl(
                    "OptionsGfxDynamicMinFPS",
                    _gfxDevDynamicMinFps,
                    "DYNAMIC MIN FPS");
            });

            Bind("btnGfxDevInternalLimit", () =>
            {
                _gfxDevInternalLimit =
                    _gfxDevInternalLimit == 0
                        ? 240
                        : _gfxDevInternalLimit == 240
                            ? 360
                            : 0;

                ApplyAdvancedControl(
                    "OptionsGfxLimitFpsInGame",
                    _gfxDevInternalLimit,
                    "INTERNAL FPS LIMIT");
            });

            Bind("btnGfxAdvAa", () =>
            {
                _gfxAdvAa = (_gfxAdvAa + 1) % 4;
                ApplyAdvancedControl("OptionsGfxAA", _gfxAdvAa, "AA QUALITY");
            });

            Bind("btnGfxAdvGrass", () =>
            {
                _gfxAdvGrass = (_gfxAdvGrass + 1) % 5;
                ApplyAdvancedControl("OptionsGfxGrassDistance", _gfxAdvGrass, "GRASS");
            });

            Bind("btnGfxAdvObjects", () =>
            {
                _gfxAdvObjects = (_gfxAdvObjects + 1) % 5;
                ApplyAdvancedControl("OptionsGfxObjQuality", _gfxAdvObjects, "OBJECTS");
            });

            Bind("btnGfxAdvReflections", () =>
            {
                _gfxAdvReflections = (_gfxAdvReflections + 1) % 4;
                ApplyAdvancedControl("OptionsGfxReflectQuality", _gfxAdvReflections, "REFLECTIONS");
            });

            Bind("btnGfxAdvShadows", () =>
            {
                _gfxAdvShadows = (_gfxAdvShadows + 1) % 4;
                ApplyAdvancedControl("OptionsGfxShadowQuality", _gfxAdvShadows, "SHADERS/SHADOWS");
            });

            BindGfxSliders();
            BindSlider("taskbarScaleSlider", SlideTaskbarScale);
            BindSlider("gfxPanelScaleSlider", SlideGfxPanelScale);
            _interfaceScale = Mathf.Clamp(PlayerPrefs.GetFloat("cHub.InterfaceScale", 1f), 0.65f, 1.25f);
            float currentHudSize = Mathf.Max(0.01f, GamePrefs.GetFloat(EnumGamePrefs.OptionsHudSize));
            // The current preference may already contain c/Hub's persisted
            // multiplier. Divide it back out so reopening gfxHub never compounds
            // the scale (for example 80% becoming 64%).
            _baseHudSize = Mathf.Max(0.01f, currentHudSize / Mathf.Max(0.01f, _interfaceScale));
            _baseActiveUiScale = Mathf.Max(0.01f,
                GameOptionsManager.GetActiveUiScale() / Mathf.Max(0.01f, _interfaceScale));
            ApplyInterfaceScale(false);

            _contextPanel =
                FindDescendant<XUiController>(
                    this,
                    "taskbarContextMenu");

            _displaySubmenu =
                FindDescendant<XUiController>(
                    this,
                    "taskbarDisplaySubmenu");

            _notificationPanel =
                FindDescendant<XUiController>(
                    this,
                    "taskbarNotification");

            _notificationPanel2 =
                FindDescendant<XUiController>(
                    this,
                    "taskbarNotification2");

            _notificationPanel3 =
                FindDescendant<XUiController>(
                    this,
                    "taskbarNotification3");

            _gfxPanel =
                FindDescendant<XUiController>(
                    this,
                    "gfxControlCenter");

            _taskbarScale = Mathf.Clamp(PlayerPrefs.GetFloat("cHub.TaskbarScale", 1f), 0.65f, 1.35f);
            _gfxUiScale = Mathf.Clamp(PlayerPrefs.GetFloat("cHub.GfxHubPanelScale", 1f), 0.65f, 1.35f);
            CaptureTaskbarTransforms();
            ApplyTaskbarAndGfxScale(false);

            BuildSupportedResolutionList();

            _gfxPresetNameInput =
                FindDescendant<XUiC_TextInput>(
                    this,
                    "gfxPresetName");

            _gfxPresetSearchInput =
                FindDescendant<XUiC_TextInput>(
                    this,
                    "gfxPresetSearch");

            _gfxPresetName =
                PlayerPrefs.GetString(
                    GfxPresetKey + "name",
                    "Client save preset");

            if (_gfxPresetNameInput != null)
            {
                _gfxPresetNameInput.Text =
                    _gfxPresetName;

                _gfxPresetNameInput.OnChangeHandler +=
                    (sender, text, finished) =>
                        _gfxPresetName =
                            string.IsNullOrEmpty(text)
                                ? "Client save preset"
                                : text.Trim();
            }

            if (_gfxPresetSearchInput != null)
            {
                _gfxPresetSearchInput.ActiveTextColor =
                    Color.white;

                _gfxPresetSearchInput.CaretColor =
                    new Color(
                        1f,
                        0.22f,
                        0.26f,
                        1f);

                _gfxPresetSearchInput.SelectionColor =
                    new Color(
                        0.72f,
                        0.04f,
                        0.08f,
                        0.62f);

                _gfxPresetSearchInput.OnChangeHandler +=
                    (sender, search, finished) =>
                    {
                        _gfxPresetSearch =
                            (search ?? string.Empty)
                            .Trim();

                        RefreshGfxPresetFilter();
                    };
            }

            RefreshGfxPresetFilter();

            BindGfxWindowManipulation();

            if (_notificationPanel?.ViewComponent?.UiTransform != null)
            {
                _notificationVisiblePosition =
                    _notificationPanel
                        .ViewComponent
                        .UiTransform
                        .localPosition;
            }

            CacheLiveTextControls();
            RefreshLiveText();

            _fogStartDistance =
                RenderSettings.fogStartDistance;

            _fogEndDistance =
                RenderSettings.fogEndDistance;

            CaptureVanillaDefaults();
            CaptureVanillaCinematicEffects();
            CaptureFullVanillaSnapshot();

            StartupTerminal.ReportFeature(
                "ui.ingame-menu",
                "ESC / ingameMenu Integration",
                true,
                "Pause tools controller initialized and bindings completed");

            StartupTerminal.ReportFeature(
                "ui.chub-menu",
                "In-Game c/Hub Menu",
                true,
                "OpenHub bindings initialized");
        }
        catch (Exception ex)
        {
            StartupTerminal.ReportFeature(
                "ui.ingame-menu",
                "ESC / ingameMenu Integration",
                false,
                ex.Message);

            StartupTerminal.ReportFeature(
                "ui.chub-menu",
                "In-Game c/Hub Menu",
                false,
                ex.Message);

            throw;
        }
    }

    private string SanitizeGfxPresetFileName(
    string name)
    {
        string safeName =
            string.IsNullOrWhiteSpace(name)
                ? "ClientPreset"
                : name.Trim();

        foreach (char invalid in
            Path.GetInvalidFileNameChars())
        {
            safeName =
                safeName.Replace(
                    invalid,
                    '_');
        }

        if (safeName.Length > 80)
        {
            safeName =
                safeName.Substring(
                    0,
                    80);
        }

        return safeName;
    }

    private void SyncGfxPresetFilesToDirectory()
    {
        MigrateLegacyGfxPresetIfNeeded();

        string directory =
            GetGfxPresetDirectory();

        try
        {
            // =========================================================
            // REMOVE OLD EXPORTED PRESET FILES
            // =========================================================

            string[] oldFiles =
                Directory.GetFiles(
                    directory,
                    "*.chubgfx");

            foreach (string file in oldFiles)
            {
                try
                {
                    File.Delete(
                        file);
                }
                catch
                {
                }
            }


            // =========================================================
            // EXPORT CURRENT PLAYERPREF PRESETS
            // =========================================================

            for (int slot = 1;
                 slot <= GfxCustomPresetSlots;
                 slot++)
            {
                if (!TryGetGfxCustomPreset(
                    slot,
                    out string presetName,
                    out string presetData))
                {
                    continue;
                }


                string safeName =
                    SanitizeGfxPresetFileName(
                        presetName);


                string fileName =
                    slot.ToString("00") +
                    "_" +
                    safeName +
                    ".chubgfx";


                string filePath =
                    Path.Combine(
                        directory,
                        fileName);


                string contents =
                    "cHub GFX PRESET" +
                    Environment.NewLine +
                    "version=1" +
                    Environment.NewLine +
                    "slot=" +
                    slot +
                    Environment.NewLine +
                    "name=" +
                    presetName +
                    Environment.NewLine +
                    "data=" +
                    presetData +
                    Environment.NewLine;


                File.WriteAllText(
                    filePath,
                    contents);
            }


            UnityEngine.Debug.Log(
                "[cHub] [GFXPRESET] DIRECTORY SYNC // " +
                "path=" +
                directory);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.Log(
                "[cHub] [GFXPRESET] DIRECTORY SYNC FAILED // " +
                ex.GetType().Name +
                ": " +
                ex.Message);

            throw;
        }
    }

    private void OpenGfxPresetDirectory()
    {
        try
        {
            SyncGfxPresetFilesToDirectory();


            string directory =
                GetGfxPresetDirectory();


            System.Diagnostics.ProcessStartInfo startInfo =
                new System.Diagnostics.ProcessStartInfo();

            startInfo.FileName =
                "explorer.exe";

            startInfo.Arguments =
                "\"" +
                directory +
                "\"";

            startInfo.UseShellExecute =
                true;


            System.Diagnostics.Process.Start(
                startInfo);


            _gfxStatus =
                "PRESET DIRECTORY OPENED";


            Notify(
                "GFX PRESETS • Folder deschis");


            StartupTerminal.Audit(
                "GFX_PRESET",
                "OPEN_DIRECTORY",
                "OK",
                "path=" +
                directory);


            UnityEngine.Debug.Log(
                "[cHub] [GFXPRESET] OPEN DIRECTORY // " +
                directory);
        }
        catch (Exception ex)
        {
            _gfxStatus =
                "PRESET DIRECTORY OPEN FAILED";


            Notify(
                "GFX PRESETS • Nu am putut deschide folderul");


            StartupTerminal.Audit(
                "GFX_PRESET",
                "OPEN_DIRECTORY",
                "FAILED",
                ex.GetType().Name +
                ": " +
                ex.Message);


            UnityEngine.Debug.Log(
                "[cHub] [GFXPRESET] OPEN DIRECTORY FAILED // " +
                ex.GetType().Name +
                ": " +
                ex.Message);
        }
    }
    private void DeleteSelectedGfxPreset()
    {
        MigrateLegacyGfxPresetIfNeeded();


        // =========================================================
        // ONLY CUSTOM PRESETS CAN BE DELETED
        // =========================================================

        int slot =
            GetSelectedGfxCustomPresetSlot();


        if (slot < 1)
        {
            _gfxStatus =
                "DELETE FAILED • SELECT A CUSTOM PRESET";


            Notify(
                "GFX DELETE • Selectează un preset CLIENT");


            StartupTerminal.Audit(
                "GFX_PRESET",
                "DELETE",
                "SKIPPED",
                "reason=no_custom_preset_selected selected=" +
                _gfxSelectedPreset);


            return;
        }


        if (!TryGetGfxCustomPreset(
            slot,
            out string presetName,
            out string presetData))
        {
            _gfxStatus =
                "DELETE FAILED • PRESET NOT FOUND";


            Notify(
                "GFX DELETE • Presetul selectat nu mai există");


            RefreshGfxPresetFilter();

            return;
        }


        // =========================================================
        // DELETE SLOT DATA
        // =========================================================

        string key =
            GfxPresetSlotKey(
                slot);


        PlayerPrefs.DeleteKey(
            key + "name");

        PlayerPrefs.DeleteKey(
            key + "data");


        // =========================================================
        // SLOT 1 LEGACY PROTECTION
        //
        // MigrateLegacyGfxPresetIfNeeded() would recreate slot 1
        // if old single-preset keys remained.
        // =========================================================

        if (slot == 1)
        {
            PlayerPrefs.DeleteKey(
                GfxPresetKey + "name");

            PlayerPrefs.DeleteKey(
                GfxPresetKey + "data");
        }


        PlayerPrefs.Save();


        // =========================================================
        // FALL BACK TO SAFE BUILT-IN SELECTION
        // =========================================================

        _gfxSelectedPreset =
            "maxfps";


        _gfxPresetName =
            "Client save preset";


        if (_gfxPresetNameInput != null)
        {
            _gfxPresetNameInput.Text =
                _gfxPresetName;
        }


        // Search may remain exactly as the user typed it.
        // Only list geometry / scroll is refreshed.
        RefreshGfxPresetFilter();


        // Start filtered list from the top after removal.
        _gfxPresetManualY =
            0f;


        ResetGfxPresetScroll();


        // Keep exported directory synchronized.
        try
        {
            SyncGfxPresetFilesToDirectory();
        }
        catch
        {
            // PlayerPrefs deletion succeeded.
            // Export sync failure must not resurrect the preset.
        }


        _gfxStatus =
            "CUSTOM PRESET DELETED • SLOT " +
            slot;


        Notify(
            "GFX DELETE • " +
            presetName +
            " șters");


        StartupTerminal.Audit(
            "GFX_PRESET",
            "DELETE",
            "OK",
            "slot=" +
            slot +
            " name=" +
            presetName);


        UnityEngine.Debug.Log(
            "[cHub] [GFXPRESET] DELETE // " +
            "slot=" +
            slot +
            " // name=" +
            presetName);


        SetAllChildrenDirty();
    }
    private void ClampGfxPresetScroll()
    {
        XUiController gfxScroll =
            FindDescendant<XUiController>(
                this,
                "gfxPresetScroll");

        if (!(gfxScroll?.ViewComponent is XUiV_ScrollView scrollView))
            return;

        if (scrollView.UiTransform == null)
            return;


        // =========================================================
        // REAL CONTENT SIZE
        // =========================================================

        const float rowStep = 52f;
        const float rowHeight = 48f;
        const float viewportHeight = 210f;


        float contentHeight =
            _gfxPresetVisibleCount > 0
                ? ((_gfxPresetVisibleCount - 1) * rowStep) +
                  rowHeight
                : 0f;


        float maxTravel =
            Mathf.Max(
                0f,
                contentHeight - viewportHeight);


        // ResetGfxPresetScroll() stabilește top-ul absolut la Y = 0.
        const float topY = 0f;

        float bottomY =
            topY + maxTravel;


        float currentY =
            scrollView.UiTransform.localPosition.y;


        float targetY =
            Mathf.Clamp(
                currentY,
                topY,
                bottomY);


        // =========================================================
        // INSIDE VALID RANGE
        //
        // Nu atingem nimic.
        // NGUI + scrollbar rămân singurii master.
        // =========================================================

        if (Mathf.Abs(
            currentY - targetY) <= 0.05f)
        {
            return;
        }


        BindingFlags flags =
            BindingFlags.Instance |
            BindingFlags.NonPublic |
            BindingFlags.Public;


        FieldInfo scrollViewField =
            typeof(XUiV_ScrollView).GetField(
                "scrollView",
                flags);


        object nguiScroll =
            scrollViewField?.GetValue(
                scrollView);


        if (nguiScroll == null)
            return;


        try
        {
            Type type =
                nguiScroll.GetType();


            // =====================================================
            // STOP NGUI MOTION BEFORE CLAMP
            // =====================================================

            FieldInfo momentumField =
                type.GetField(
                    "mMomentum",
                    flags);


            if (momentumField != null &&
                momentumField.FieldType == typeof(Vector3))
            {
                momentumField.SetValue(
                    nguiScroll,
                    Vector3.zero);
            }


            FieldInfo scrollField =
                type.GetField(
                    "mScroll",
                    flags);


            if (scrollField != null)
            {
                if (scrollField.FieldType ==
                    typeof(float))
                {
                    scrollField.SetValue(
                        nguiScroll,
                        0f);
                }
                else if (scrollField.FieldType ==
                         typeof(Vector2))
                {
                    scrollField.SetValue(
                        nguiScroll,
                        Vector2.zero);
                }
                else if (scrollField.FieldType ==
                         typeof(Vector3))
                {
                    scrollField.SetValue(
                        nguiScroll,
                        Vector3.zero);
                }
            }


            MethodInfo disableSpring =
                type.GetMethod(
                    "DisableSpring",
                    flags,
                    null,
                    Type.EmptyTypes,
                    null);


            disableSpring?.Invoke(
                nguiScroll,
                null);


            // =====================================================
            // CONVERT PIXEL POSITION -> NGUI NORMALIZED POSITION
            // =====================================================

            float normalizedY =
                maxTravel > 0.001f
                    ? Mathf.Clamp01(
                        (targetY - topY) /
                        maxTravel)
                    : 0f;


            // =====================================================
            // LET NGUI APPLY POSITION
            //
            // IMPORTANT:
            // Nu modificăm manual Transform-ul.
            // SetDragAmount sincronizează și scrollbar-ul.
            // =====================================================

            MethodInfo setDragAmount =
                type.GetMethod(
                    "SetDragAmount",
                    flags,
                    null,
                    new Type[]
                    {
                    typeof(float),
                    typeof(float),
                    typeof(bool)
                    },
                    null);


            if (setDragAmount == null)
            {
                UnityEngine.Debug.Log(
                    "[cHub] [GFXSCROLL] NATIVE CLAMP FAILED // " +
                    "SetDragAmount missing");

                return;
            }


            setDragAmount.Invoke(
                nguiScroll,
                new object[]
                {
                0f,
                normalizedY,
                true
                });


            // =====================================================
            // CRITICAL:
            // SetDragAmount poate declanșa intern callbacks.
            //
            // Oprim DIN NOU orice momentum / scroll rămas.
            // =====================================================

            if (momentumField != null &&
                momentumField.FieldType == typeof(Vector3))
            {
                momentumField.SetValue(
                    nguiScroll,
                    Vector3.zero);
            }


            if (scrollField != null)
            {
                if (scrollField.FieldType ==
                    typeof(float))
                {
                    scrollField.SetValue(
                        nguiScroll,
                        0f);
                }
                else if (scrollField.FieldType ==
                         typeof(Vector2))
                {
                    scrollField.SetValue(
                        nguiScroll,
                        Vector2.zero);
                }
                else if (scrollField.FieldType ==
                         typeof(Vector3))
                {
                    scrollField.SetValue(
                        nguiScroll,
                        Vector3.zero);
                }
            }


            disableSpring?.Invoke(
                nguiScroll,
                null);


            // =====================================================
            // FORCE SCROLLBAR REFRESH
            // =====================================================

            MethodInfo updateScrollbars =
                type.GetMethod(
                    "UpdateScrollbars",
                    flags,
                    null,
                    new Type[]
                    {
                    typeof(bool)
                    },
                    null);


            updateScrollbars?.Invoke(
                nguiScroll,
                new object[]
                {
                true
                });


            float afterY =
                scrollView.UiTransform.localPosition.y;


            UnityEngine.Debug.Log(
                "[cHub] [GFXSCROLL] NATIVE HARD STOP // " +
                "oldY=" +
                currentY.ToString("0.00") +
                " // targetY=" +
                targetY.ToString("0.00") +
                " // afterY=" +
                afterY.ToString("0.00") +
                " // maxTravel=" +
                maxTravel.ToString("0.00") +
                " // normalized=" +
                normalizedY.ToString("0.000") +
                " // visible=" +
                _gfxPresetVisibleCount);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.Log(
                "[cHub] [GFXSCROLL] NATIVE HARD STOP FAILED // " +
                ex.GetType().Name +
                ": " +
                ex.Message);
        }
    }


    private void DebugGfxPresetScrollState(string stage)
    {
        XUiController gfxScroll =
            FindDescendant<XUiController>(this, "gfxPresetScroll");

        if (gfxScroll?.ViewComponent == null)
        {
            UnityEngine.Debug.Log(
                "[cHub] [GFXSCROLL] " + stage + " // scrollview missing");
            return;
        }

        Vector3 scrollPos =
            gfxScroll.ViewComponent.UiTransform != null
                ? gfxScroll.ViewComponent.UiTransform.localPosition
                : Vector3.zero;

        UnityEngine.Debug.Log(
            "[cHub] [GFXSCROLL] " +
            stage +
            " // viewType=" +
            gfxScroll.ViewComponent.GetType().Name +
            " // scrollLocal=" +
            scrollPos);

        foreach (string id in new[]
        {
        "btnGfxPresetMaxFps",
        "btnGfxPresetMaxQuality",
        "btnGfxPresetUltra",
        "btnGfxPresetCinema",
        "btnGfxPresetClient"
    })
        {
            XUiController row =
                FindDescendant<XUiController>(this, id);

            if (row?.ViewComponent == null)
                continue;

            Vector3 local =
                row.ViewComponent.UiTransform != null
                    ? row.ViewComponent.UiTransform.localPosition
                    : Vector3.zero;

            UnityEngine.Debug.Log(
                "[cHub] [GFXSCROLL] " +
                stage +
                " // " +
                id +
                " // Position=" +
                row.ViewComponent.Position +
                " // local=" +
                local);
        }
    }
    private void DebugGfxPresetScrollInternals(string stage)
    {
        XUiController gfxScroll =
            FindDescendant<XUiController>(this, "gfxPresetScroll");

        if (gfxScroll?.ViewComponent == null)
        {
            UnityEngine.Debug.Log(
                "[cHub] [GFXSCROLL_INTERNAL] " + stage + " // missing");
            return;
        }

        object view = gfxScroll.ViewComponent;
        Type type = view.GetType();

        UnityEngine.Debug.Log(
            "[cHub] [GFXSCROLL_INTERNAL] " +
            stage +
            " // viewType=" +
            type.FullName);

        BindingFlags flags =
            BindingFlags.Instance |
            BindingFlags.NonPublic |
            BindingFlags.Public;

        foreach (FieldInfo field in type.GetFields(flags))
        {
            try
            {
                object value = field.GetValue(view);

                UnityEngine.Debug.Log(
                    "[cHub] [GFXSCROLL_INTERNAL] FIELD // " +
                    field.FieldType.FullName +
                    " " +
                    field.Name +
                    " = " +
                    (value == null ? "null" : value.ToString()));
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.Log(
                    "[cHub] [GFXSCROLL_INTERNAL] FIELD FAILED // " +
                    field.Name +
                    " // " +
                    ex.Message);
            }
        }

        foreach (PropertyInfo property in type.GetProperties(flags))
        {
            if (property.GetIndexParameters().Length != 0)
                continue;

            try
            {
                object value = property.GetValue(view, null);

                UnityEngine.Debug.Log(
                    "[cHub] [GFXSCROLL_INTERNAL] PROP // " +
                    property.PropertyType.FullName +
                    " " +
                    property.Name +
                    " = " +
                    (value == null ? "null" : value.ToString()));
            }
            catch
            {
            }
        }

        Transform t = gfxScroll.ViewComponent.UiTransform;

        if (t != null)
        {
            UnityEngine.Debug.Log(
                "[cHub] [GFXSCROLL_INTERNAL] TRANSFORM // " +
                "name=" + t.name +
                " local=" + t.localPosition +
                " world=" + t.position +
                " parent=" + (t.parent == null ? "null" : t.parent.name));

            foreach (Component component in t.gameObject.GetComponents<Component>())
            {
                if (component == null)
                    continue;

                UnityEngine.Debug.Log(
                    "[cHub] [GFXSCROLL_INTERNAL] COMPONENT SELF // " +
                    component.GetType().FullName);
            }

            if (t.parent != null)
            {
                foreach (Component component in t.parent.gameObject.GetComponents<Component>())
                {
                    if (component == null)
                        continue;

                    UnityEngine.Debug.Log(
                        "[cHub] [GFXSCROLL_INTERNAL] COMPONENT PARENT // " +
                        component.GetType().FullName);
                }
            }
        }
    }
    private void ToggleConsole()
    {
        _status = "CONSOLE CLICKED";

        UnityEngine.Debug.Log(
            "[cHub] [PauseTools] CONSOLE BUTTON CLICKED");

        StartupTerminal.ShowConsole();

        _status = StartupTerminal.IsVisible
            ? "CONSOLE OPEN"
            : "CONSOLE FAILED";
        XUiController gfxScroll =
    FindDescendant<XUiController>(
        this,
        "gfxPresetScroll");

        if (gfxScroll?.ViewComponent is XUiV_ScrollView scrollView)
        {
            BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.NonPublic |
                BindingFlags.Public;

            FieldInfo scrollViewField =
                typeof(XUiV_ScrollView).GetField(
                    "scrollView",
                    flags);

            object nguiScroll =
                scrollViewField?.GetValue(scrollView);

            if (nguiScroll != null)
            {
                MethodInfo invalidateMethod =
                    nguiScroll.GetType().GetMethod(
                        "InvalidateBounds",
                        flags);

                invalidateMethod?.Invoke(
                    nguiScroll,
                    null);
            }
        }

        SetAllChildrenDirty();
    }
    private void CacheDescendants(XUiController root)
    {
        if (root == null)
            return;

        foreach (XUiController child in root.Children)
        {
            if (child == null)
                continue;

            if (child.ViewComponent != null &&
                !string.IsNullOrWhiteSpace(child.ViewComponent.ID))
            {
                _controlCache[child.ViewComponent.ID] = child;
            }

            CacheDescendants(child);
        }
    }
    //====== Modificari i27k ========

    private readonly Dictionary<string, XUiController> _controlCache =
        new Dictionary<string, XUiController>(
            StringComparer.OrdinalIgnoreCase);

    //===============================
    private void SetGfxPresetScrollbarNormalized(
    XUiV_ScrollView scrollView,
    float normalized)
    {
        if (scrollView == null)
            return;

        normalized =
            Mathf.Clamp01(
                normalized);

        BindingFlags flags =
            BindingFlags.Instance |
            BindingFlags.NonPublic |
            BindingFlags.Public;

        try
        {
            // =========================================================
            // GET XUiV_ScrollBar WRAPPER
            // =========================================================

            FieldInfo wrapperBarField =
                typeof(XUiV_ScrollView).GetField(
                    "scrollBar",
                    flags);

            object wrapperBar =
                wrapperBarField?.GetValue(
                    scrollView);

            if (wrapperBar == null)
            {
                UnityEngine.Debug.Log(
                    "[cHub] [GFXSCROLL] BAR SYNC FAILED // " +
                    "XUiV_ScrollBar missing");

                return;
            }


            Type wrapperBarType =
                wrapperBar.GetType();


            // =========================================================
            // FIRST TRY:
            // SET WRAPPER VALUE DIRECTLY
            // =========================================================

            bool wrapperValueSet =
                false;


            foreach (string propertyName in new[]
            {
            "Value",
            "value"
        })
            {
                PropertyInfo property =
                    wrapperBarType.GetProperty(
                        propertyName,
                        flags);

                if (property == null ||
                    !property.CanWrite ||
                    property.PropertyType != typeof(float))
                {
                    continue;
                }

                property.SetValue(
                    wrapperBar,
                    normalized,
                    null);

                wrapperValueSet =
                    true;

                break;
            }


            // =========================================================
            // GET REAL NGUI UIScrollBar FROM WRAPPER
            // =========================================================

            object nativeBar =
                null;


            foreach (string fieldName in new[]
            {
            "scrollBar",
            "scrollbar",
            "uiScrollBar",
            "mScrollBar"
        })
            {
                FieldInfo field =
                    wrapperBarType.GetField(
                        fieldName,
                        flags);

                if (field == null)
                    continue;


                object candidate =
                    field.GetValue(
                        wrapperBar);


                if (candidate == null)
                    continue;


                nativeBar =
                    candidate;

                break;
            }


            // =========================================================
            // FALLBACK:
            // SEARCH COMPONENTS ON WRAPPER TRANSFORM
            // =========================================================

            if (nativeBar == null)
            {
                XUiView wrapperView =
                    wrapperBar as XUiView;

                Transform transform =
                    wrapperView?.UiTransform;

                if (transform != null)
                {
                    foreach (Component component in
                        transform.gameObject.GetComponents<Component>())
                    {
                        if (component == null)
                            continue;

                        Type componentType =
                            component.GetType();

                        if (componentType.Name ==
                            "UIScrollBar")
                        {
                            nativeBar =
                                component;

                            break;
                        }
                    }
                }
            }


            if (nativeBar == null)
            {
                UnityEngine.Debug.Log(
                    "[cHub] [GFXSCROLL] BAR SYNC PARTIAL // " +
                    "wrapperValueSet=" +
                    wrapperValueSet +
                    " // native UIScrollBar not found");

                return;
            }


            Type nativeBarType =
                nativeBar.GetType();


            // =========================================================
            // SET REAL NGUI SCROLLBAR VALUE
            // =========================================================

            PropertyInfo nativeValueProperty =
                nativeBarType.GetProperty(
                    "value",
                    flags);


            if (nativeValueProperty == null)
            {
                nativeValueProperty =
                    nativeBarType.GetProperty(
                        "Value",
                        flags);
            }


            if (nativeValueProperty != null &&
                nativeValueProperty.CanWrite &&
                nativeValueProperty.PropertyType == typeof(float))
            {
                nativeValueProperty.SetValue(
                    nativeBar,
                    normalized,
                    null);
            }
            else
            {
                // Some NGUI builds keep the value in mValue.
                FieldInfo nativeValueField =
                    nativeBarType.GetField(
                        "mValue",
                        flags);

                if (nativeValueField != null &&
                    nativeValueField.FieldType == typeof(float))
                {
                    nativeValueField.SetValue(
                        nativeBar,
                        normalized);
                }
            }


            // =========================================================
            // FORCE VISUAL REFRESH
            // =========================================================

            foreach (string methodName in new[]
            {
            "ForceUpdate",
            "UpdateVisuals"
        })
            {
                MethodInfo method =
                    nativeBarType.GetMethod(
                        methodName,
                        flags,
                        null,
                        Type.EmptyTypes,
                        null);

                if (method != null)
                {
                    method.Invoke(
                        nativeBar,
                        null);

                    break;
                }
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.Log(
                "[cHub] [GFXSCROLL] BAR SYNC FAILED // " +
                ex.GetType().Name +
                ": " +
                ex.Message);
        }
    }

    private void UpdateGfxPresetManualScroll()
    {
        if (!_gfxPresetsVisible ||
            _gfxPresetsMinimized ||
            _gfxDevGraphicsVisible)
        {
            return;
        }


        XUiController gfxScroll =
            FindDescendant<XUiController>(
                this,
                "gfxPresetScroll");

        if (!(gfxScroll?.ViewComponent is XUiV_ScrollView scrollView))
            return;

        if (scrollView.UiTransform == null)
            return;


        const float rowStep =
            52f;

        const float rowHeight =
            48f;

        const float viewportHeight =
            210f;

        const float wheelStep =
            52f;


        // =========================================================
        // CURRENT FILTERED CONTENT SIZE
        // =========================================================

        float contentHeight =
            _gfxPresetVisibleCount > 0
                ? ((_gfxPresetVisibleCount - 1) * rowStep) +
                  rowHeight
                : 0f;


        float maxTravel =
            Mathf.Max(
                0f,
                contentHeight - viewportHeight);


        // =========================================================
        // IMPORTANT:
        // CLAMP EVERY FRAME
        //
        // Daca search-ul schimba visible rows:
        //
        // 8 rows -> maxTravel 202
        // 1 row  -> maxTravel 0
        //
        // manualY trebuie corectat IMEDIAT, chiar daca userul
        // nu mai misca rotita.
        // =========================================================

        _gfxPresetManualY =
            Mathf.Clamp(
                _gfxPresetManualY,
                0f,
                maxTravel);


        // =========================================================
        // READ WHEEL
        // =========================================================

        float wheel =
            Input.mouseScrollDelta.y;


        if (Mathf.Abs(wheel) >= 0.01f)
        {
            float oldY =
                _gfxPresetManualY;


            _gfxPresetManualY -=
                wheel * wheelStep;


            _gfxPresetManualY =
                Mathf.Clamp(
                    _gfxPresetManualY,
                    0f,
                    maxTravel);


            UnityEngine.Debug.Log(
                "[cHub] [GFXSCROLL] MANUAL STEP // " +
                "wheel=" +
                wheel.ToString("0.00") +
                " // oldY=" +
                oldY.ToString("0.00") +
                " // newY=" +
                _gfxPresetManualY.ToString("0.00") +
                " // maxTravel=" +
                maxTravel.ToString("0.00") +
                " // visible=" +
                _gfxPresetVisibleCount);
        }


        // =========================================================
        // OUR POSITION IS AUTHORITATIVE
        // =========================================================

        Vector3 position =
            scrollView.UiTransform.localPosition;


        if (Mathf.Abs(
            position.y -
            _gfxPresetManualY) > 0.01f)
        {
            UnityEngine.Debug.Log(
                "[cHub] [GFXSCROLL] EXTERNAL MOVE BLOCKED // " +
                "externalY=" +
                position.y.ToString("0.00") +
                " // restoredY=" +
                _gfxPresetManualY.ToString("0.00"));
        }


        position.x =
            0f;

        position.y =
            _gfxPresetManualY;


        scrollView.UiTransform.localPosition =
            position;


        // =========================================================
        // GET NATIVE UIScrollView
        // =========================================================

        BindingFlags flags =
            BindingFlags.Instance |
            BindingFlags.NonPublic |
            BindingFlags.Public;


        FieldInfo scrollViewField =
            typeof(XUiV_ScrollView).GetField(
                "scrollView",
                flags);


        object nguiScroll =
            scrollViewField?.GetValue(
                scrollView);


        if (nguiScroll != null)
        {
            try
            {
                Type scrollType =
                    nguiScroll.GetType();


                // =====================================================
                // KILL NGUI MOTION
                // =====================================================

                FieldInfo momentumField =
                    scrollType.GetField(
                        "mMomentum",
                        flags);


                if (momentumField != null &&
                    momentumField.FieldType == typeof(Vector3))
                {
                    momentumField.SetValue(
                        nguiScroll,
                        Vector3.zero);
                }


                FieldInfo scrollField =
                    scrollType.GetField(
                        "mScroll",
                        flags);


                if (scrollField != null)
                {
                    if (scrollField.FieldType == typeof(float))
                    {
                        scrollField.SetValue(
                            nguiScroll,
                            0f);
                    }
                    else if (scrollField.FieldType == typeof(Vector2))
                    {
                        scrollField.SetValue(
                            nguiScroll,
                            Vector2.zero);
                    }
                    else if (scrollField.FieldType == typeof(Vector3))
                    {
                        scrollField.SetValue(
                            nguiScroll,
                            Vector3.zero);
                    }
                }


                MethodInfo disableSpring =
                    scrollType.GetMethod(
                        "DisableSpring",
                        flags,
                        null,
                        Type.EmptyTypes,
                        null);


                disableSpring?.Invoke(
                    nguiScroll,
                    null);


                // =====================================================
                // PANEL CLIP FOLLOWS MANUAL POSITION
                // =====================================================

                object panel =
                    null;


                PropertyInfo panelProperty =
                    scrollType.GetProperty(
                        "panel",
                        flags);


                if (panelProperty != null &&
                    panelProperty.GetIndexParameters().Length == 0)
                {
                    try
                    {
                        panel =
                            panelProperty.GetValue(
                                nguiScroll,
                                null);
                    }
                    catch
                    {
                    }
                }


                if (panel == null)
                {
                    FieldInfo panelField =
                        scrollType.GetField(
                            "mPanel",
                            flags);


                    panel =
                        panelField?.GetValue(
                            nguiScroll);
                }


                if (panel != null)
                {
                    Type panelType =
                        panel.GetType();


                    Vector2 clipOffset =
                        new Vector2(
                            0f,
                            -_gfxPresetManualY);


                    PropertyInfo clipOffsetProperty =
                        panelType.GetProperty(
                            "clipOffset",
                            flags);


                    if (clipOffsetProperty != null &&
                        clipOffsetProperty.CanWrite)
                    {
                        clipOffsetProperty.SetValue(
                            panel,
                            clipOffset,
                            null);
                    }
                    else
                    {
                        FieldInfo clipOffsetField =
                            panelType.GetField(
                                "mClipOffset",
                                flags);


                        if (clipOffsetField != null)
                        {
                            clipOffsetField.SetValue(
                                panel,
                                clipOffset);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.Log(
                    "[cHub] [GFXSCROLL] MANUAL ENFORCE FAILED // " +
                    ex.GetType().Name +
                    ": " +
                    ex.Message);
            }
        }


        // =========================================================
        // SCROLLBAR SYNC
        // =========================================================

        float normalized =
            maxTravel > 0.001f
                ? Mathf.Clamp01(
                    _gfxPresetManualY /
                    maxTravel)
                : 0f;


        SetGfxPresetScrollbarNormalized(
            scrollView,
            normalized);
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        _interfaceScaleRefresh += deltaTime;
        if (_interfaceScaleRefresh >= 0.5f)
        {
            _interfaceScaleRefresh = 0f;
            ApplyInterfaceScale(false);
        }


        // =========================================================
        // GFX PRESET MANUAL SCROLL
        // =========================================================

        UpdateGfxPresetManualScroll();


        // =========================================================
        // GFX PRESET SCROLL DEBUG
        // =========================================================

        if (_gfxPresetsVisible &&
            !_gfxPresetsMinimized &&
            !_gfxDevGraphicsVisible)
        {
            _gfxScrollDebugTimer +=
                deltaTime;

            if (_gfxScrollDebugTimer >= 1f)
            {
                _gfxScrollDebugTimer =
                    0f;

                DebugGfxPresetScrollState(
                    "RUNTIME");
            }
        }
        else
        {
            _gfxScrollDebugTimer =
                0f;
        }


        // =========================================================
        // FPS CALCULATION
        // =========================================================

        float instantaneous =
            deltaTime > 0.0001f
                ? 1f / deltaTime
                : 0f;


        _smoothedFps =
            _smoothedFps <= 0f
                ? instantaneous
                : Mathf.Lerp(
                    _smoothedFps,
                    instantaneous,
                    1f - Mathf.Exp(
                        -deltaTime * 5f));


        // =========================================================
        // CONTEXT PANEL ANIMATION
        // =========================================================

        if (_contextVisible &&
            _contextAnimation < 1f)
        {
            _contextAnimation =
                Mathf.Min(
                    1f,
                    _contextAnimation +
                    deltaTime * 6f);


            float eased =
                1f -
                Mathf.Pow(
                    1f - _contextAnimation,
                    3f);


            if (_contextPanel?.ViewComponent?.UiTransform != null)
            {
                _contextPanel
                    .ViewComponent
                    .UiTransform
                    .localScale =
                        new Vector3(
                            0.88f + eased * 0.12f,
                            0.88f + eased * 0.12f,
                            1f);
            }
        }


        // =========================================================
        // FPS BINDING REFRESH
        // =========================================================

        _fpsBindingRefresh +=
            deltaTime;


        // =========================================================
        // STATUS TIMER 1
        // =========================================================

        if (_statusTimer > 0f)
        {
            _statusTimer =
                Mathf.Max(
                    0f,
                    _statusTimer -
                    deltaTime);


            if (_statusTimer <= 0f)
            {
                SetAllChildrenDirty();
            }
        }


        // =========================================================
        // STATUS TIMERS 2 / 3
        // =========================================================

        _statusTimer2 =
            Mathf.Max(
                0f,
                _statusTimer2 -
                deltaTime);


        _statusTimer3 =
            Mathf.Max(
                0f,
                _statusTimer3 -
                deltaTime);


        // =========================================================
        // NOTIFICATION PANELS
        // =========================================================

        if (_notificationPanel2?.ViewComponent != null)
        {
            _notificationPanel2
                .ViewComponent
                .IsVisible =
                    _statusTimer2 > 0f;
        }


        if (_notificationPanel3?.ViewComponent != null)
        {
            _notificationPanel3
                .ViewComponent
                .IsVisible =
                    _statusTimer3 > 0f;
        }


        UpdateNotificationVisual(
            deltaTime);


        // =========================================================
        // BINDING REFRESH BURST
        // =========================================================

        if (_bindingRefreshBurst > 0f)
        {
            _bindingRefreshBurst -=
                deltaTime;

            SetAllChildrenDirty();
        }


        // =========================================================
        // FPS OVERLAY REFRESH
        // =========================================================

        if (ShowFpsOverlay &&
            _fpsBindingRefresh >= 0.5f)
        {
            _fpsBindingRefresh =
                0f;

            SetAllChildrenDirty();
        }


        // =========================================================
        // LIVE TEXT REFRESH
        // =========================================================

        _liveTextRefresh +=
            deltaTime;


        if (_liveTextRefresh >= 0.10f)
        {
            _liveTextRefresh =
                0f;

            RefreshLiveText();
        }


        // =========================================================
        // GRAPHICS COMMANDS / SLIDERS
        // =========================================================

        ProcessGfxCommands(
            deltaTime);


        UpdateGfxSliderPreview(
            deltaTime);


        // =========================================================
        // IMPORTANT
        //
        // NU:
        // ClampGfxPresetScroll();
        //
        // NU:
        // SetDragAmount() aici.
        //
        // UpdateGfxPresetManualScroll() este singurul master pentru
        // wheel-ul Preset Manager.
        // =========================================================
    }

    private void ConfigureGfxPresetNativeClip()
    {
        XUiController gfxScroll =
            FindDescendant<XUiController>(
                this,
                "gfxPresetScroll");

        if (!(gfxScroll?.ViewComponent is XUiV_ScrollView scrollView))
            return;

        BindingFlags flags =
            BindingFlags.Instance |
            BindingFlags.NonPublic |
            BindingFlags.Public;

        try
        {
            Type wrapperType =
                typeof(XUiV_ScrollView);


            // =========================================================
            // DISABLE XUI AUTOMATIC WHEEL
            //
            // De acum scroll-ul este controlat exclusiv de
            // UpdateGfxPresetManualScroll().
            // =========================================================

            FieldInfo wrapperScrollFactorField =
                wrapperType.GetField(
                    "scrollFactor",
                    flags);

            if (wrapperScrollFactorField != null &&
                wrapperScrollFactorField.FieldType == typeof(float))
            {
                wrapperScrollFactorField.SetValue(
                    scrollView,
                    0f);
            }


            PropertyInfo wrapperScrollFactorProperty =
                wrapperType.GetProperty(
                    "ScrollFactor",
                    flags);

            if (wrapperScrollFactorProperty != null &&
                wrapperScrollFactorProperty.CanWrite &&
                wrapperScrollFactorProperty.PropertyType == typeof(float))
            {
                wrapperScrollFactorProperty.SetValue(
                    scrollView,
                    0f,
                    null);
            }


            // =========================================================
            // GET REAL NGUI UIScrollView
            // =========================================================

            FieldInfo scrollViewField =
                wrapperType.GetField(
                    "scrollView",
                    flags);

            object nguiScroll =
                scrollViewField?.GetValue(
                    scrollView);

            if (nguiScroll == null)
            {
                UnityEngine.Debug.Log(
                    "[cHub] [GFXSCROLL] MANUAL CONFIG FAILED // " +
                    "UIScrollView missing");

                return;
            }


            Type type =
                nguiScroll.GetType();


            // =========================================================
            // DISABLE NATIVE NGUI WHEEL
            // =========================================================

            FieldInfo nativeWheelField =
                type.GetField(
                    "scrollWheelFactor",
                    flags);

            if (nativeWheelField != null &&
                nativeWheelField.FieldType == typeof(float))
            {
                nativeWheelField.SetValue(
                    nguiScroll,
                    0f);
            }


            PropertyInfo nativeWheelProperty =
                type.GetProperty(
                    "scrollWheelFactor",
                    flags);

            if (nativeWheelProperty != null &&
                nativeWheelProperty.CanWrite &&
                nativeWheelProperty.PropertyType == typeof(float))
            {
                nativeWheelProperty.SetValue(
                    nguiScroll,
                    0f,
                    null);
            }


            // =========================================================
            // DO NOT LET NGUI REPOSITION CONTENT AUTOMATICALLY
            //
            // Manual scroll-ul nostru face propriul clamp 0..maxTravel.
            // =========================================================

            FieldInfo restrictField =
                type.GetField(
                    "restrictWithinPanel",
                    flags);

            if (restrictField != null &&
                restrictField.FieldType == typeof(bool))
            {
                restrictField.SetValue(
                    nguiScroll,
                    false);
            }


            // =========================================================
            // KILL ANY OLD NGUI MOTION
            // =========================================================

            FieldInfo momentumField =
                type.GetField(
                    "mMomentum",
                    flags);

            if (momentumField != null &&
                momentumField.FieldType == typeof(Vector3))
            {
                momentumField.SetValue(
                    nguiScroll,
                    Vector3.zero);
            }


            FieldInfo scrollField =
                type.GetField(
                    "mScroll",
                    flags);

            if (scrollField != null)
            {
                if (scrollField.FieldType == typeof(float))
                {
                    scrollField.SetValue(
                        nguiScroll,
                        0f);
                }
                else if (scrollField.FieldType == typeof(Vector2))
                {
                    scrollField.SetValue(
                        nguiScroll,
                        Vector2.zero);
                }
                else if (scrollField.FieldType == typeof(Vector3))
                {
                    scrollField.SetValue(
                        nguiScroll,
                        Vector3.zero);
                }
            }


            MethodInfo disableSpring =
                type.GetMethod(
                    "DisableSpring",
                    flags,
                    null,
                    Type.EmptyTypes,
                    null);

            disableSpring?.Invoke(
                nguiScroll,
                null);


            // =========================================================
            // KEEP VERTICAL MODE
            // =========================================================

            FieldInfo movementField =
                type.GetField(
                    "movement",
                    flags);

            if (movementField != null &&
                movementField.FieldType.IsEnum)
            {
                object vertical =
                    Enum.Parse(
                        movementField.FieldType,
                        "Vertical");

                movementField.SetValue(
                    nguiScroll,
                    vertical);
            }


            // =========================================================
            // RECALCULATE BOUNDS
            // =========================================================

            MethodInfo invalidate =
                type.GetMethod(
                    "InvalidateBounds",
                    flags,
                    null,
                    Type.EmptyTypes,
                    null);

            invalidate?.Invoke(
                nguiScroll,
                null);


            MethodInfo updateScrollbars =
                type.GetMethod(
                    "UpdateScrollbars",
                    flags,
                    null,
                    new Type[]
                    {
                    typeof(bool)
                    },
                    null);

            updateScrollbars?.Invoke(
                nguiScroll,
                new object[]
                {
                true
                });


            UnityEngine.Debug.Log(
                "[cHub] [GFXSCROLL] MANUAL MODE // " +
                "wrapperWheel=0 // nativeWheel=0 // " +
                "restrictWithinPanel=false // manual controller active");
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.Log(
                "[cHub] [GFXSCROLL] MANUAL CONFIG FAILED // " +
                ex.GetType().Name +
                ": " +
                ex.Message);
        }
    }




    public override bool GetBindingValueInternal(ref string value, string bindingName)
    {
        switch (bindingName)
        {
            case "chub_pause_fog": value = (_fogOverride ? _fogEnabled : RenderSettings.fog) ? "ON" : "OFF"; return true;
            case "chub_pause_fog_density": value = (_fogOverride ? _fogDensity : RenderSettings.fogDensity).ToString("0.0000"); return true;
            case "chub_pause_brightness": value = (_ambientOverride ? _ambientIntensity : RenderSettings.ambientIntensity).ToString("0.0"); return true;
            case "chub_pause_shadows": value = Mathf.RoundToInt(_shadowOverride ? _shadowDistance : QualitySettings.shadowDistance) + "m"; return true;
            case "chub_pause_status": value = _status; return true;
            case "chub_pause_status_visible": value = (_statusTimer > 0f).ToString().ToLowerInvariant(); return true;
            case "chub_pause_status2": value = _status2; return true;
            case "chub_pause_status2_visible": value = (_statusTimer2 > 0f).ToString().ToLowerInvariant(); return true;
            case "chub_pause_status3": value = _status3; return true;
            case "chub_pause_status3_visible": value = (_statusTimer3 > 0f).ToString().ToLowerInvariant(); return true;
            case "chub_pause_fog_range": value = Mathf.RoundToInt(RenderSettings.fogEndDistance) + "m"; return true;
            case "chub_pause_vsync": value = ShowFpsOverlay ? "FPS HUD ON" : "FPS HUD OFF"; return true;
            case "chub_pause_aa": value = "AA " + QualitySettings.antiAliasing + "x"; return true;
            case "chub_pause_gfx_shadows": value = (_shadowOverride ? _shadowQuality : QualitySettings.shadows) == ShadowQuality.Disable ? "SHADOWS OFF" : "SHADOWS ON"; return true;
            case "chub_pause_fps": value = _fpsBoostActive ? "BOOST ACTIVE" : "FPS BOOST"; return true;
            case "chub_pause_quality_boost": value = _qualityBoostActive ? "ULTRA ACTIVE" : "GFX ULTRA"; return true;
            case "chub_pause_book_tracker": value = XUiC_cHubBookInfoWindow.TrackerEnabled ? "BOOK INFO ON" : "BOOK INFO OFF"; return true;
            case "chub_taskbar_live_fps": value = ShowFpsOverlay ? Mathf.RoundToInt(_smoothedFps) + " FPS" : "FPS HIDDEN"; return true;
            case "chub_taskbar_context": value = _contextVisible.ToString().ToLowerInvariant(); return true;
            case "chub_taskbar_display_submenu": value = (_contextVisible && _displaySubmenuVisible).ToString().ToLowerInvariant(); return true;
            case "chub_taskbar_fps_toggle": value = ShowFpsOverlay ? "HIDE FPS" : "SHOW FPS"; return true;
            case "chub_gfx_panel": value = _gfxPanelVisible.ToString().ToLowerInvariant(); return true;
            case "chub_gfx_presets_visible": value = _gfxPresetsVisible.ToString().ToLowerInvariant(); return true;
            case "chub_gfx_presets_body_visible": value = (_gfxPresetsVisible && !_gfxPresetsMinimized && !_gfxDevGraphicsVisible).ToString().ToLowerInvariant(); return true;
            case "chub_gfx_dev_visible": value = (_gfxPresetsVisible && !_gfxPresetsMinimized && _gfxDevGraphicsVisible).ToString().ToLowerInvariant(); return true;
            case "chub_gfx_presets_minimized": value = (_gfxPresetsVisible && _gfxPresetsMinimized).ToString().ToLowerInvariant(); return true;
            case "chub_gfx_preset_maxfps": value = (_gfxSelectedPreset == "maxfps" ? "★  " : "☆  ") + "MAX FPS PRESET by GPT"; return true;
            case "chub_gfx_preset_maxquality": value = (_gfxSelectedPreset == "maxquality" ? "★  " : "☆  ") + "MAX QUALITY PRESET by GPT"; return true;
            case "chub_gfx_preset_ultra": value = (_gfxSelectedPreset == "ultra" ? "★  " : "☆  ") + "ULTRA GRAPHICS"; return true;
            case "chub_gfx_preset_cinema": value = (_gfxSelectedPreset == "cinema" ? "★  " : "☆  ") + "CINEMA - REALITY"; return true;
            case "chub_gfx_preset_client": value = (_gfxSelectedPreset == "client" ? "★  " : "☆  ") + _gfxPresetName; return true;
            case "chub_gfx_adv_aa": value = "AA " + _gfxAdvAa + "x"; return true;
            case "chub_gfx_adv_grass": value = "GRASS " + _gfxAdvGrass; return true;
            case "chub_gfx_adv_objects": value = "OBJECTS " + _gfxAdvObjects; return true;
            case "chub_gfx_adv_reflections": value = "REFLECT " + _gfxAdvReflections; return true;
            case "chub_gfx_adv_shadows": value = "SHADER " + _gfxAdvShadows; return true;
            case "chub_gfx_dev_upscaler": value = "UPSCALER " + _gfxDevUpscaler; return true;
            case "chub_gfx_dev_fsr": value = "FSR " + _gfxDevFsr; return true;
            case "chub_gfx_dev_water": value = "WATER " + _gfxDevWater; return true;
            case "chub_gfx_dev_water_particles": value = "WATER FX " + OnOff(_gfxDevWaterParticles); return true;
            case "chub_gfx_dev_occlusion": value = "OCCLUSION " + OnOff(_gfxDevOcclusion); return true;
            case "chub_gfx_dev_reflect_shadows": value = "REFLECT SHADOWS " + OnOff(_gfxDevReflectShadows); return true;
            case "chub_gfx_dev_signs": value = "SIGNS " + _gfxDevSigns; return true;
            case "chub_gfx_dev_uma": value = "UMA TEX " + _gfxDevUma; return true;
            case "chub_gfx_dev_texfilter": value = "TEX FILTER " + _gfxDevTexFilter; return true;
            case "chub_gfx_dev_lod": value = "LOD DISTANCE " + _gfxDevLod; return true;
            case "chub_gfx_dev_minfps": value = "DYNAMIC MIN " + _gfxDevDynamicMinFps; return true;
            case "chub_gfx_dev_limit": value = _gfxDevInternalLimit <= 0 ? "FPS LIMIT OFF" : "FPS LIMIT " + _gfxDevInternalLimit; return true;
            case "chub_gfx_af": value = "ANISOTROPIC " + _gfxAf; return true;
            case "chub_gfx_dynamic": value = "DYNAMIC RES " + OnOff(_gfxDynamic); return true;
            case "chub_gfx_resolution": value = "RES " + (_gfxResolutions.Count == 0 ? Screen.width + "x" + Screen.height : _gfxResolutions[_gfxResolutionIndex]); return true;
            case "chub_pause_fps_lock": value = !_frameLimitOverride || _frameLimit <= 0 ? "FPS MAX" : "FPS " + _frameLimit; return true;
            case "chub_gfx_dt": value = "DISTANT TERRAIN " + OnOff(_gfxDt); return true;
            case "chub_gfx_dti": value = "INSTANCING " + OnOff(_gfxDti); return true;
            case "chub_gfx_lod": value = "MAX LOD " + _gfxLod; return true;
            case "chub_gfx_pixel": value = _gfxPixel.ToString(); return true;
            case "chub_gfx_view": value = _gfxView.ToString(); return true;
            case "chub_gfx_ao": value = "AO " + OnOff(_gfxAo); return true;
            case "chub_gfx_bloom": value = "BLOOM " + OnOff(_gfxBloom); return true;
            case "chub_gfx_exposure": value = "EXPOSURE " + OnOff(_gfxExposure); return true;
            case "chub_gfx_color": value = "COLOR " + OnOff(_gfxColor); return true;
            case "chub_gfx_ssao": value = "SSAO " + OnOff(_gfxSsao); return true;
            case "chub_gfx_sunshafts": value = "SUN " + OnOff(_gfxSunshafts); return true;
            case "chub_gfx_dof": value = "DOF " + OnOff(_gfxDof); return true;
            case "chub_gfx_motionblur": value = "MOTION " + OnOff(_gfxMotionBlur); return true;
            case "chub_gfx_cinematic_all": value = "ALL " + OnOff(_gfxSsao && _gfxBloom && _gfxSunshafts && _gfxDof && _gfxMotionBlur); return true;
            case "chub_gfx_skin": value = "SKIN " + _gfxSkin; return true;
            case "chub_gfx_stream": value = "STREAM " + OnOff(_gfxStream); return true;
            case "chub_gfx_bias": value = _gfxBias.ToString(); return true;
            case "chub_gfx_limit": value = _gfxLimit.ToString(); return true;
            case "chub_interface_scale": value = Mathf.RoundToInt(_interfaceScale * 100f) + "%"; return true;
            case "chub_taskbar_scale": value = "TASK " + Mathf.RoundToInt(_taskbarScale * 100f) + "%"; return true;
            case "chub_gfx_panel_scale": value = "GFX " + Mathf.RoundToInt(_gfxUiScale * 100f) + "%"; return true;
            case "chub_gfx_status": value = _gfxStatus; return true;
            case "chub_gfx_window_mode": value = "MODE " + (_gfxWindowMode == 0 ? "FULLSCREEN" : _gfxWindowMode == 1 ? "BORDERLESS" : "WINDOWED"); return true;
            case "chub_gfx_sliders_visible": value = (!_gfxSlidersMinimized && !_gfxPresetsVisible).ToString().ToLowerInvariant(); return true;
            case "chub_gfx_sliders_minimized": value = _gfxSlidersMinimized.ToString().ToLowerInvariant(); return true;
            case "chub_gfx_af_value": value = _gfxAf.ToString(); return true;
            case "chub_gfx_dynamic_value": value = OnOff(_gfxDynamic); return true;
            case "chub_gfx_dt_value": value = OnOff(_gfxDt); return true;
            case "chub_gfx_dti_value": value = OnOff(_gfxDti); return true;
            case "chub_gfx_lod_value": value = _gfxLod.ToString(); return true;
            case "chub_gfx_ao_value": value = OnOff(_gfxAo); return true;
            case "chub_gfx_bloom_value": value = OnOff(_gfxBloom); return true;
            case "chub_gfx_ssao_value": value = OnOff(_gfxSsao); return true;
            case "chub_gfx_sun_value": value = OnOff(_gfxSunshafts); return true;
            case "chub_gfx_dof_value": value = OnOff(_gfxDof); return true;
            case "chub_gfx_motion_value": value = OnOff(_gfxMotionBlur); return true;
            case "chub_gfx_exposure_value": value = OnOff(_gfxExposure); return true;
            case "chub_gfx_color_value": value = OnOff(_gfxColor); return true;
            case "chub_gfx_skin_value": value = _gfxSkin.ToString(); return true;
            case "chub_gfx_stream_value": value = OnOff(_gfxStream); return true;
        }
        return base.GetBindingValueInternal(ref value, bindingName);
    }

    private void ToggleFog()
    {
        if (!_fogOverride)
            _fogDensity = Mathf.Max(RenderSettings.fogDensity, 0.01f);
        _fogEnabled = _fogOverride ? !_fogEnabled : !RenderSettings.fog;
        _fogOverride = true;
        ApplyPersistentOverrides();
        ApplyStableFogOverride();
        Notify(_fogEnabled ? "FOG ON • Ceața locală este activă" : "FOG OFF • Ceața locală este dezactivată");
        TraceFogEffective("FOG_TOGGLE");
    }

    private void SetFogDensity(float value)
    {
        _fogDensity = Mathf.Clamp(value, 0f, 0.15f);
        _fogEnabled = true;
        _fogOverride = true;
        ApplyPersistentOverrides();
        ApplyStableFogOverride();
        Notify("FOG DENSITY " + _fogDensity.ToString("0.0000") + " • Densitatea ceții a fost aplicată");
        TraceFogEffective("FOG_DENSITY");
    }

    private void SetFogDistance(float value)
    {
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogEndDistance = Mathf.Clamp(value, 25f, 2000f);
        RenderSettings.fogStartDistance = Mathf.Clamp(
            RenderSettings.fogEndDistance * 0.15f, 0f, RenderSettings.fogEndDistance - 1f);
        Notify("FOG DISTANCE " + Mathf.RoundToInt(RenderSettings.fogEndDistance) + "m • Distanța vizibilă a fost schimbată");
    }

    private void SetBrightness(float value)
    {
        _ambientIntensity = Mathf.Clamp(value, 0.2f, 3f);
        _ambientOverride = true;
        _showingDefaults = false;
        SetGamePref(EnumGamePrefs.OptionsGfxBrightness, _ambientIntensity);
        ApplyVanillaGraphics();
        ApplyPersistentOverrides();
        Notify("AMBIENT LIGHT " + _ambientIntensity.ToString("0.0") + " • Luminozitatea mediului a fost aplicată");
        TraceGraphic("AMBIENT", "requested=" + _ambientIntensity.ToString("0.0") + " effective=" + RenderSettings.ambientIntensity.ToString("0.0"));
    }

    private void SetShadowDistance(float value)
    {
        _shadowDistance = Mathf.Clamp(_shadowOverride ? value : QualitySettings.shadowDistance + (value - QualitySettings.shadowDistance), 0f, 300f);
        _shadowQuality = QualitySettings.shadows == ShadowQuality.Disable ? ShadowQuality.All : QualitySettings.shadows;
        _shadowOverride = true;
        _showingDefaults = false;
        SetGamePref(EnumGamePrefs.OptionsGfxShadowDistance, ShadowDistanceToPref(_shadowDistance));
        ApplyVanillaGraphics();
        ApplyPersistentOverrides();
        Notify("SHADOW DISTANCE " + Mathf.RoundToInt(_shadowDistance) + "m • Raza umbrelor a fost aplicată");
        TraceGraphic("SHADOW_DISTANCE", "requested=" + _shadowDistance.ToString("0") + " effective=" + QualitySettings.shadowDistance.ToString("0"));
    }

    private void ResetVisuals()
    {
        CaptureVanillaDefaults();
        bool hadFpsBoost = _fpsBoostActive || _qualityBoostActive || _fpsBoostSnapshotCaptured;
        RestoreFpsBoostSnapshot();
        if (hadFpsBoost)
        {
            RestoreBoostGfxCommandState();
            ApplyVanillaGraphics();
            ApplyAllGfxImmediate("TASKBAR BOOST RESET");
        }
        _fogOverride = false;
        SkyManager.SetFogDebug(-1f, -1001f, -1001f);
        RenderSettings.fog = _defaultFogEnabled;
        RenderSettings.fogMode = _defaultFogMode;
        RenderSettings.fogDensity = _defaultFogDensity;
        RenderSettings.fogStartDistance = _defaultFogStart;
        RenderSettings.fogEndDistance = _defaultFogEnd;
        _ambientOverride = false;
        _shadowOverride = false;
        _frameLimitOverride = false;
        _showingDefaults = false;
        ShowFpsOverlay = _defaultShowFpsOverlay;
        XUiC_cHubBookInfoWindow.SetTrackerEnabled(true);
        RenderSettings.ambientIntensity = _defaultUnityAmbient;
        QualitySettings.shadows = _defaultUnityShadows;
        QualitySettings.shadowDistance = _defaultUnityShadowDistance;
        QualitySettings.antiAliasing = _defaultAntiAliasing;
        _qualityBoostActive = false;
        if (!hadFpsBoost) Application.targetFrameRate = _defaultTargetFrameRate;
        if (hadFpsBoost)
        {
            Notify("TASKBAR RESET - FPS BOOST și valorile taskbarului au fost restaurate");
            StartupTerminal.Audit("RESET", "FPS_BOOST_RESTORE", _fpsBoostSnapshotCaptured ? "FAILED" : "OK",
                "snapshotCleared=" + (!_fpsBoostSnapshotCaptured) + " boostActive=" + _fpsBoostActive);
            return;
        }
        Notify("TASKBAR RESET • Numai Fog, lumină, umbre, AA, FPS HUD/limită au fost restaurate");
        StartupTerminal.Audit("RESET", "TASKBAR_ONLY", "OK",
            "gfxPanelUntouched=true fog=" + RenderSettings.fog + " shadows=" + QualitySettings.shadows);
        RefreshLiveText();
        SetAllChildrenDirty();
    }

    private static void AuditFpsBoostEffective()
    {
        bool ok = QualitySettings.vSyncCount == 0 && Application.targetFrameRate == -1 &&
            QualitySettings.antiAliasing == 0 && QualitySettings.shadows == ShadowQuality.Disable &&
            QualitySettings.shadowDistance <= 0.01f && QualitySettings.pixelLightCount == 0 &&
            !QualitySettings.realtimeReflectionProbes && !QualitySettings.softVegetation;
        StartupTerminal.Audit("FPS_BOOST", "EFFECTIVE", ok ? "OK" : "MISMATCH",
            "vsync=" + QualitySettings.vSyncCount + " target=" + Application.targetFrameRate +
            " aa=" + QualitySettings.antiAliasing + " shadows=" + QualitySettings.shadows +
            " shadowDistance=" + QualitySettings.shadowDistance.ToString("0.0") +
            " pixelLights=" + QualitySettings.pixelLightCount + " reflections=" +
            QualitySettings.realtimeReflectionProbes + " softVegetation=" + QualitySettings.softVegetation);
    }

    private void ActivateFpsBoost()
    {
        if (_qualityBoostActive)
        {
            Notify("GFX ULTRA ACTIVE - Apasa RESET inainte de FPS BOOST");
            return;
        }
        if (_fpsBoostActive)
        {
            Notify("FPS BOOST ACTIVE - Foloseste RESET pentru restaurare");
            return;
        }
        CaptureFpsBoostSnapshot();

        // Keep the public-server experience playable: entities, physics and
        // server-owned simulation remain untouched. Only local rendering changes.
        SetBoostPref("OptionsGfxAA", 0);
        SetBoostFloatPref("OptionsGfxAASharpness", 0f);
        SetBoostPref("OptionsGfxGrassDistance", 0);
        SetBoostPref("OptionsGfxObjQuality", 0);
        SetBoostPref("OptionsGfxTerrainQuality", 0);
        SetBoostPref("OptionsGfxTreeDistance", 0);
        SetBoostPref("OptionsGfxReflectQuality", 0);
        SetBoostPref("OptionsGfxShadowQuality", 0);
        SetBoostPref("OptionsGfxShadowDistance", 0);
        SetBoostPref("OptionsGfxSignQuality", 0);
        SetBoostPref("OptionsGfxSSReflections", 0);
        SetBoostPref("OptionsGfxTexFilter", 0);
        SetBoostPref("OptionsGfxTexQuality", 2);
        SetBoostPref("OptionsGfxUMATexQuality", 2);
        SetBoostPref("OptionsGfxWaterQuality", 0);
        SetBoostPref("OptionsGfxDynamicMinFPS", 120);
        SetBoostPref("OptionsGfxLimitFpsInGame", 0);
        SetBoostPref("OptionsGfxVsync", 0);
        SetBoostBoolPref("OptionsGfxBloom", false);
        SetBoostBoolPref("OptionsGfxDOF", false);
        SetBoostBoolPref("OptionsGfxMotionBlurEnabled", false);
        SetBoostBoolPref("OptionsGfxOcclusion", true);
        SetBoostBoolPref("OptionsGfxReflectShadows", false);
        SetBoostBoolPref("OptionsGfxSSAO", false);
        SetBoostBoolPref("OptionsGfxSunShafts", false);
        SetBoostBoolPref("OptionsGfxStreamMipmaps", true);
        SetBoostFloatPref("OptionsGfxWaterPtlLimiter", 0.25f);
        SetBoostFloatPref("OptionsGfxLODDistance", 0.5f);
        ApplyVanillaGraphics();

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;
        QualitySettings.antiAliasing = 0;
        QualitySettings.shadows = ShadowQuality.Disable;
        QualitySettings.shadowDistance = 0f;
        QualitySettings.pixelLightCount = 0;
        QualitySettings.realtimeReflectionProbes = false;
        QualitySettings.softVegetation = false;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
        QualitySettings.lodBias = 0.45f;
        QualitySettings.maximumLODLevel = 1;
        QualitySettings.particleRaycastBudget = 16;
        QualitySettings.streamingMipmapsActive = true;
        QualitySettings.asyncUploadTimeSlice = 2;
        QualitySettings.asyncUploadBufferSize = 64;
        QualitySettings.maxQueuedFrames = 1;

        _gfxAf=0; _gfxDynamic=1; _gfxDt=0; _gfxDti=1; _gfxLod=0; _gfxPixel=200; _gfxView=6;
        _gfxAo=false; _gfxBloom=false; _gfxExposure=false; _gfxColor=false;
        _gfxSsao=false; _gfxSunshafts=false; _gfxDof=false; _gfxMotionBlur=false;
        _gfxSkin=1; _gfxStream=1; _gfxBias=2; _gfxLimit=2;
        ApplyAllGfx("FPS BOOST");
        _fpsBoostActive = true;
        _frameLimitOverride = false;
        Notify("FPS BOOST ACTIVE - Boost total client-side; RESET pentru restaurare");
        TraceGraphic("FPS_BOOST", "active=true playable=true serverSimulationUntouched=true");
        AuditFpsBoostEffective();
        SyncGfxFlyoutVisibility();
    }

    private void OpenHub()
    {
        string error;
        _status = AdminPanelService.TryOpenLocal(out error)
            ? "c/HUB MENU OPENED"
            : "OPEN FAILED // " + error;
    }

    private void ActivateQualityBoost()
    {
        if (_qualityBoostActive) { Notify("GFX ULTRA ACTIVE - Numai RESET restaureaza grafica anterioara"); return; }
        if (_fpsBoostActive) { Notify("FPS BOOST ACTIVE - Apasa RESET inainte de GFX ULTRA"); return; }
        CaptureFpsBoostSnapshot();
        SetBoostPref("OptionsGfxAA", 3); SetBoostFloatPref("OptionsGfxAASharpness", 0.85f);
        SetBoostPref("OptionsGfxGrassDistance", 4); SetBoostPref("OptionsGfxObjQuality", 4);
        SetBoostPref("OptionsGfxTerrainQuality", 4); SetBoostPref("OptionsGfxTreeDistance", 4);
        SetBoostPref("OptionsGfxReflectQuality", 3); SetBoostPref("OptionsGfxShadowQuality", 3);
        SetBoostPref("OptionsGfxShadowDistance", 3); SetBoostPref("OptionsGfxSignQuality", 4);
        SetBoostPref("OptionsGfxSSReflections", 3); SetBoostPref("OptionsGfxTexFilter", 3);
        SetBoostPref("OptionsGfxTexQuality", 0); SetBoostPref("OptionsGfxUMATexQuality", 0);
        SetBoostPref("OptionsGfxWaterQuality", 3);
        SetBoostBoolPref("OptionsGfxBloom", true); SetBoostBoolPref("OptionsGfxDOF", true);
        SetBoostBoolPref("OptionsGfxMotionBlurEnabled", false); SetBoostBoolPref("OptionsGfxOcclusion", true);
        SetBoostBoolPref("OptionsGfxReflectShadows", true); SetBoostBoolPref("OptionsGfxSSAO", true);
        SetBoostBoolPref("OptionsGfxSunShafts", true); SetBoostBoolPref("OptionsGfxStreamMipmaps", false);
        SetBoostFloatPref("OptionsGfxWaterPtlLimiter", 1f); SetBoostFloatPref("OptionsGfxLODDistance", 5f);
        ApplyVanillaGraphics();
        QualitySettings.antiAliasing=8; QualitySettings.anisotropicFiltering=AnisotropicFiltering.ForceEnable;
        QualitySettings.shadows=ShadowQuality.All; QualitySettings.shadowResolution=ShadowResolution.VeryHigh;
        QualitySettings.shadowProjection=ShadowProjection.StableFit; QualitySettings.shadowCascades=4;
        QualitySettings.shadowDistance=300f; QualitySettings.pixelLightCount=8;
        QualitySettings.realtimeReflectionProbes=true; QualitySettings.softVegetation=true;
        QualitySettings.lodBias=5f; QualitySettings.maximumLODLevel=0;
        QualitySettings.particleRaycastBudget=4096; QualitySettings.streamingMipmapsActive=false;
        QualitySettings.asyncUploadTimeSlice=8; QualitySettings.asyncUploadBufferSize=256;
        _gfxAf=2; _gfxDynamic=0; _gfxDt=1; _gfxDti=1; _gfxLod=5; _gfxPixel=1; _gfxView=20;
        _gfxAo=true; _gfxBloom=true; _gfxExposure=true; _gfxColor=true;
        _gfxSsao=true; _gfxSunshafts=true; _gfxDof=true; _gfxMotionBlur=false;
        _gfxSkin=5; _gfxStream=0; _gfxBias=-2; _gfxLimit=0;
        ApplyAllGfx("GFX ULTRA"); _qualityBoostActive=true;
        Notify("GFX ULTRA ACTIVE - Calitate maxima client-side; RESET pentru restaurare");
        StartupTerminal.Audit("GFX_ULTRA", "APPLY", "OK", "aa=8 af=force shadows=veryhigh lod=5 view=20 reflections=on cinematic=on");
    }

    private void CycleAntiAliasing()
    {
        int current = QualitySettings.antiAliasing;
        QualitySettings.antiAliasing = current <= 0 ? 2 : current == 2 ? 4 : current == 4 ? 8 : 0;
        SetGamePref(EnumGamePrefs.OptionsGfxAA, QualitySettings.antiAliasing);
        Notify("AA " + QualitySettings.antiAliasing + "x • Netezirea marginilor a fost aplicată");
    }

    private void ToggleShadows()
    {
        ShadowQuality current = _shadowOverride ? _shadowQuality : QualitySettings.shadows;
        _shadowQuality = current == ShadowQuality.Disable ? ShadowQuality.All : ShadowQuality.Disable;
        _shadowDistance = Mathf.Max(_shadowOverride ? _shadowDistance : QualitySettings.shadowDistance, 40f);
        _shadowOverride = true;
        _showingDefaults = false;
        SetGamePref(EnumGamePrefs.OptionsGfxShadowQuality,
            _shadowQuality == ShadowQuality.Disable ? 0 : 3);
        SetGamePref(EnumGamePrefs.OptionsGfxShadowDistance,
            ShadowDistanceToPref(_shadowDistance));
        ApplyVanillaGraphics();
        ApplyPersistentOverrides();
        Notify(_shadowQuality == ShadowQuality.Disable
            ? "SHADOWS OFF • Umbrele jocului sunt dezactivate"
            : "SHADOWS ON • Umbrele jocului sunt active");
        TraceGraphic("SHADOWS", "requested=" + _shadowQuality + " effective=" + QualitySettings.shadows + " distance=" + _shadowDistance.ToString("0"));
    }

    private void CycleFrameLimit()
    {
        int current = _frameLimitOverride ? _frameLimit : -1;
        _frameLimit = current <= 0 ? 30 : current == 30 ? 60 : current == 60 ? 120 : current == 120 ? 240 : -1;
        _frameLimitOverride = true;
        ApplyPersistentOverrides();
        Notify(_frameLimit <= 0 ? "FPS MAX • Limita de cadre este dezactivată"
            : "FPS " + _frameLimit + " • Limita de cadre a fost aplicată");
        TraceGraphic("FPS_LIMIT", "requested=" + _frameLimit + " effective=" + Application.targetFrameRate);
        StartupTerminal.Audit("FPS_LOCK", "APPLY",
            Application.targetFrameRate == _frameLimit ? "OK" : "MISMATCH",
            "requested=" + _frameLimit + " effective=" + Application.targetFrameRate);
    }

    private void Notify(string message)
    {
        if (_statusTimer2 > 0f) { _status3 = _status2; _statusTimer3 = Mathf.Min(_statusTimer2, 1.15f); }
        if (_statusTimer > 0f) { _status2 = _status; _statusTimer2 = Mathf.Min(_statusTimer, 1.35f); }
        _status = message;
        SetLabelText("lblTaskbarNotification", message);
        SetLabelText("lblTaskbarNotification2", _status2);
        SetLabelText("lblTaskbarNotification3", _status3);
        _statusTimer = 1.55f;
        _bindingRefreshBurst = 0.65f;
        SetAllChildrenDirty();
        RefreshLiveText();
    }

    private void UpdateNotificationVisual(float deltaTime)
    {
        if (_notificationPanel?.ViewComponent == null) return;
        bool visible = _statusTimer > 0f;
        _notificationPanel.ViewComponent.IsVisible = visible;
        Transform transform = _notificationPanel.ViewComponent.UiTransform;
        if (!visible || transform == null) return;
        float phase = Mathf.Clamp01((2.6f - _statusTimer) / 0.22f);
        float exit = Mathf.Clamp01((0.28f - _statusTimer) / 0.28f);
        float offset = Mathf.Lerp(-32f, 0f, 1f - Mathf.Pow(1f - phase, 3f));
        offset = Mathf.Lerp(offset, -32f, exit * exit);
        Vector3 target = _notificationVisiblePosition + new Vector3(0f, offset, 0f);
        transform.localPosition = Vector3.Lerp(transform.localPosition, target,
            1f - Mathf.Exp(-deltaTime * 24f));
    }

    private void CacheLiveTextControls()
    {
        foreach (string id in new[] { "btnPauseFog", "btnPauseVsync",
            "btnPauseGfxShadows", "btnPauseFps", "btnPauseFpsLock", "btnPauseBookTracker" })
        {
            XUiC_SimpleButton button = FindDescendant<XUiC_SimpleButton>(this, id);
            if (button != null) _liveButtons[id] = button;
        }
        foreach (string id in new[] { "lblPauseFogDensity", "lblPauseBrightness",
            "lblPauseShadowDistance", "lblTaskbarNotification", "lblTaskbarNotification2",
            "lblTaskbarNotification3" })
        {
            XUiController controller = FindDescendant<XUiController>(this, id);
            XUiV_Label label = controller?.ViewComponent as XUiV_Label;
            if (label != null) _liveLabels[id] = label;
        }
    }

    private void RefreshLiveText()
    {
        SetButtonText("btnPauseFog", "FOG " + ((_fogOverride ? _fogEnabled : RenderSettings.fog) ? "ON" : "OFF"));
        SetButtonText("btnPauseVsync", ShowFpsOverlay ? "FPS HUD ON" : "FPS HUD OFF");
        SetButtonText("btnPauseGfxShadows",
            (_shadowOverride ? _shadowQuality : QualitySettings.shadows) == ShadowQuality.Disable
                ? "SHADOWS OFF" : "SHADOWS ON");
        SetButtonText("btnPauseFps", _fpsBoostActive ? "BOOST ACTIVE" : "FPS BOOST");
        SetButtonText("btnPauseFpsLock", !_frameLimitOverride || _frameLimit <= 0 ? "FPS MAX" : "FPS " + _frameLimit);
        SetButtonText("btnPauseBookTracker", XUiC_cHubBookInfoWindow.TrackerEnabled ? "BOOK INFO ON" : "BOOK INFO OFF");
        SetLabelText("lblPauseFogDensity", (_fogOverride ? _fogDensity : RenderSettings.fogDensity).ToString("0.0000"));
        SetLabelText("lblPauseBrightness", "☀ " + (_ambientOverride ? _ambientIntensity : RenderSettings.ambientIntensity).ToString("0.0"));
        SetLabelText("lblPauseShadowDistance", Mathf.RoundToInt(_shadowOverride ? _shadowDistance : QualitySettings.shadowDistance) + "m");
    }

    private void SetButtonText(string id, string text)
    {
        XUiC_SimpleButton button;
        if (_liveButtons.TryGetValue(id, out button) && button.Text != text)
            button.Text = text;
    }

    private void SetLabelText(string id, string text)
    {
        XUiV_Label label;
        if (_liveLabels.TryGetValue(id, out label) && label.Text != text)
            label.Text = text;
    }

    internal static void ApplyPersistentOverrides()
    {
        if (_frameLimitOverride)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = _frameLimit;
        }
        if (_shadowOverride && !_showingDefaults)
        {
            QualitySettings.shadows = _shadowQuality;
            QualitySettings.shadowDistance = _shadowDistance;
            QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
            QualitySettings.shadowCascades = 4;
            QualitySettings.shadowProjection = ShadowProjection.StableFit;
        }
        if (_ambientOverride && !_showingDefaults)
            RenderSettings.ambientIntensity = _ambientIntensity;
        QualitySettings.maxQueuedFrames = -1;
    }

    internal static string GetPerformanceStateSummary()
    {
        string fpsLock = !_frameLimitOverride || _frameLimit <= 0 ? "MAX" : _frameLimit.ToString();
        string shadows = (_shadowOverride ? _shadowQuality : QualitySettings.shadows) == ShadowQuality.Disable ? "OFF" : "ON";
        return "FPS_BOOST=" + (_fpsBoostActive ? "ON" : "OFF") + " | GFX_ULTRA=" + (_qualityBoostActive ? "ON" : "OFF") +
               " | FPS_LOCK=" + fpsLock + " | FPS_HUD=" + (ShowFpsOverlay ? "ON" : "OFF") +
               " | VSYNC=" + QualitySettings.vSyncCount + " | AA=" + QualitySettings.antiAliasing + "x" +
               " | SHADOWS=" + shadows + " | FOG=" + ((_fogOverride ? _fogEnabled : RenderSettings.fog) ? "ON" : "OFF");
    }

    internal static void ApplyStableFogOverride()
    {
        if (!_fogOverride) return;

        RenderSettings.fog = _fogEnabled;
        if (!_fogEnabled)
        {
            // Negative debug values release the engine override and return
            // control to the active biome/weather package.
            SkyManager.SetFogDebug(-1f, -1001f, -1001f);
            return;
        }

        // 7DTD renders world fog from SkyManager.fogParams, not from Unity's
        // RenderSettings alone. Drive the native debug override so terrain,
        // atmosphere and post-processing all receive the same stable values.
        float strength = Mathf.Clamp01(_fogDensity / 0.15f);

        // A 7DTD block is approximately one world metre. At maximum the
        // opaque fade therefore completes three blocks from the local camera.
        // The eased distance makes the upper half of the control useful for
        // close-range fog instead of compressing it into the final click.
        float proximity = Mathf.Pow(strength, 0.72f);
        float endDistance = Mathf.Lerp(650f, 3f, proximity);

        // SkyManager squares this value into fogParams.w. Values above one are
        // intentionally supported by the native shader and are required to
        // make nearby geometry disappear, rather than merely tinting it.
        float nativeDensity = Mathf.Lerp(0.02f, 3.25f, strength * strength);

        SkyManager.SetFogDebug(nativeDensity, 0f, endDistance);
        SkyManager.SetFogDensity(nativeDensity);
        SkyManager.SetFogFade(0f, endDistance);

        if (SkyManager.skyManager != null)
        {
            SkyManager.skyManager.UpdateFogShader();
            SkyManager.skyManager.UpdateShaderGlobals();
        }

        // Keep standard Unity fog synchronized for particles and any shaders
        // that still consume the built-in fog state.
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogDensity = nativeDensity * 0.01f;
        RenderSettings.fogStartDistance = 0f;
        RenderSettings.fogEndDistance = endDistance;
    }

    private static void ApplyFogToMainCamera(Camera camera)
    {
        if (!_fogOverride || camera == null || camera != Camera.main)
            return;
        ApplyStableFogOverride();
    }

    private static void TraceFogEffective(string action)
    {
        TraceGraphic(action,
            "scope=CLIENT_LOCAL networkSent=false " +
            "enabled=" + _fogEnabled +
            " requested=" + _fogDensity.ToString("0.0000") +
            " effectiveEnabled=" + RenderSettings.fog +
            " mode=" + RenderSettings.fogMode +
            " start=" + RenderSettings.fogStartDistance.ToString("0.00") +
            " end=" + RenderSettings.fogEndDistance.ToString("0.00") +
            " skyDensity=" + SkyManager.GetFogDensity().ToString("0.000") +
            " skyStart=" + SkyManager.GetFogStart().ToString("0.00") +
            " skyEnd=" + SkyManager.GetFogEnd().ToString("0.00") +
            " camera=" + (Camera.main == null ? "NONE" : Camera.main.name));
    }

    [HarmonyPatch]
    private static class StableFogAfterSkyUpdatePatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(SkyManager), "Update");
        }

        private static void Postfix()
        {
            ApplyPersistentOverrides();
            ApplyStableFogOverride();
        }
    }

    private static void TraceGraphic(string action, string details)
    {
        StartupTerminal.Trace("GRAPHICS", action, details);
    }

    private static void CaptureVanillaDefaults()
    {
        if (_defaultsCaptured) return;
        try
        {
            _defaultShadowQuality = GamePrefs.GetInt(EnumGamePrefs.OptionsGfxShadowQuality);
            _defaultShadowDistance = GamePrefs.GetInt(EnumGamePrefs.OptionsGfxShadowDistance);
            _defaultBrightness = GamePrefs.GetFloat(EnumGamePrefs.OptionsGfxBrightness);
            _defaultAntiAliasing = QualitySettings.antiAliasing;
            _defaultUnityShadows = QualitySettings.shadows;
            _defaultUnityShadowDistance = QualitySettings.shadowDistance;
            _defaultUnityAmbient = RenderSettings.ambientIntensity;
            _defaultFogEnabled = RenderSettings.fog;
            _defaultFogMode = RenderSettings.fogMode;
            _defaultFogDensity = RenderSettings.fogDensity;
            _defaultFogStart = RenderSettings.fogStartDistance;
            _defaultFogEnd = RenderSettings.fogEndDistance;
            _defaultTargetFrameRate = Application.targetFrameRate;
            _defaultShowFpsOverlay = ShowFpsOverlay;
            _defaultsCaptured = true;
        }
        catch (Exception ex)
        {
            TraceGraphic("DEFAULT_CAPTURE_FAILED", ex.Message);
        }
    }

    private static int ShadowDistanceToPref(float distance)
    {
        return distance <= 0f ? 0 : distance < 60f ? 1 : distance < 140f ? 2 : 3;
    }

    private static void SetGamePref(EnumGamePrefs pref, int value)
    {
        GamePrefs.Set(pref, value);
        GameOptionsManager.OnGamePrefChanged(pref);
    }

    private static void SetGamePref(EnumGamePrefs pref, float value)
    {
        GamePrefs.Set(pref, value);
        GameOptionsManager.OnGamePrefChanged(pref);
    }

    private void CaptureFpsBoostSnapshot()
    {
        if (_fpsBoostSnapshotCaptured) return;
        _fpsBoostIntPrefs.Clear(); _fpsBoostBoolPrefs.Clear(); _fpsBoostFloatPrefs.Clear();
        foreach (string name in new[] { "OptionsGfxAA", "OptionsGfxGrassDistance",
            "OptionsGfxObjQuality", "OptionsGfxTerrainQuality", "OptionsGfxTreeDistance",
            "OptionsGfxReflectQuality", "OptionsGfxShadowQuality", "OptionsGfxShadowDistance", "OptionsGfxSignQuality",
            "OptionsGfxSSReflections", "OptionsGfxTexFilter", "OptionsGfxTexQuality", "OptionsGfxUMATexQuality",
            "OptionsGfxWaterQuality", "OptionsGfxDynamicMinFPS", "OptionsGfxLimitFpsInGame", "OptionsGfxVsync" })
        {
            EnumGamePrefs pref; if (Enum.TryParse(name, out pref)) _fpsBoostIntPrefs[name] = GamePrefs.GetInt(pref);
        }
        foreach (string name in new[] { "OptionsGfxBloom", "OptionsGfxDOF", "OptionsGfxMotionBlurEnabled",
            "OptionsGfxOcclusion", "OptionsGfxReflectShadows", "OptionsGfxSSAO", "OptionsGfxSunShafts",
            "OptionsGfxStreamMipmaps" })
        {
            EnumGamePrefs pref; if (Enum.TryParse(name, out pref)) _fpsBoostBoolPrefs[name] = GamePrefs.GetBool(pref);
        }
        foreach (string name in new[] { "OptionsGfxLODDistance", "OptionsGfxWaterPtlLimiter", "OptionsGfxAASharpness" })
        {
            EnumGamePrefs pref; if (Enum.TryParse(name, out pref)) _fpsBoostFloatPrefs[name] = GamePrefs.GetFloat(pref);
        }
        _boostAa=QualitySettings.antiAliasing; _boostPixelLights=QualitySettings.pixelLightCount;
        _boostMaxLod=QualitySettings.maximumLODLevel; _boostParticleBudget=QualitySettings.particleRaycastBudget;
        _boostAsyncSlice=QualitySettings.asyncUploadTimeSlice; _boostAsyncBuffer=QualitySettings.asyncUploadBufferSize;
        _boostVsync=QualitySettings.vSyncCount; _boostTargetFps=Application.targetFrameRate;
        _boostLodBias=QualitySettings.lodBias; _boostShadowDistance=QualitySettings.shadowDistance;
        _boostShadows=QualitySettings.shadows; _boostAniso=QualitySettings.anisotropicFiltering;
        _boostRealtimeReflections=QualitySettings.realtimeReflectionProbes;
        _boostSoftVegetation=QualitySettings.softVegetation; _boostStreaming=QualitySettings.streamingMipmapsActive;
        _boostGfxAf=_gfxAf; _boostGfxDynamic=_gfxDynamic; _boostGfxDt=_gfxDt; _boostGfxDti=_gfxDti;
        _boostGfxLod=_gfxLod; _boostGfxPixel=_gfxPixel; _boostGfxView=_gfxView; _boostGfxSkin=_gfxSkin;
        _boostGfxStream=_gfxStream; _boostGfxBias=_gfxBias; _boostGfxLimit=_gfxLimit;
        _boostGfxAo=_gfxAo; _boostGfxBloom=_gfxBloom; _boostGfxExposure=_gfxExposure; _boostGfxColor=_gfxColor;
        _boostGfxSsao=_gfxSsao; _boostGfxSunshafts=_gfxSunshafts; _boostGfxDof=_gfxDof; _boostGfxMotionBlur=_gfxMotionBlur;
        _fpsBoostSnapshotCaptured = true;
    }

    private void ToggleBookTracker()
    {
        bool enabled = XUiC_cHubBookInfoWindow.ToggleTracker();
        Notify(enabled
            ? "BOOK INFO ON • Informațiile pentru cărți și reviste sunt afișate"
            : "BOOK INFO OFF • Panoul pentru cărți și reviste este ascuns");
        SetButtonText("btnPauseBookTracker", enabled ? "BOOK INFO ON" : "BOOK INFO OFF");
        SetAllChildrenDirty();
    }

    private void RestoreBoostGfxCommandState()
    {
        _gfxAf=_boostGfxAf; _gfxDynamic=_boostGfxDynamic; _gfxDt=_boostGfxDt; _gfxDti=_boostGfxDti;
        _gfxLod=_boostGfxLod; _gfxPixel=_boostGfxPixel; _gfxView=_boostGfxView; _gfxSkin=_boostGfxSkin;
        _gfxStream=_boostGfxStream; _gfxBias=_boostGfxBias; _gfxLimit=_boostGfxLimit;
        _gfxAo=_boostGfxAo; _gfxBloom=_boostGfxBloom; _gfxExposure=_boostGfxExposure; _gfxColor=_boostGfxColor;
        _gfxSsao=_boostGfxSsao; _gfxSunshafts=_boostGfxSunshafts; _gfxDof=_boostGfxDof; _gfxMotionBlur=_boostGfxMotionBlur;
    }

    private static void RestoreFpsBoostSnapshot()
    {
        if (!_fpsBoostSnapshotCaptured) { _fpsBoostActive = false; return; }
        foreach (var item in _fpsBoostIntPrefs) { EnumGamePrefs p; if (Enum.TryParse(item.Key, out p)) GamePrefs.Set(p, item.Value); }
        foreach (var item in _fpsBoostBoolPrefs) { EnumGamePrefs p; if (Enum.TryParse(item.Key, out p)) GamePrefs.Set(p, item.Value); }
        foreach (var item in _fpsBoostFloatPrefs) { EnumGamePrefs p; if (Enum.TryParse(item.Key, out p)) GamePrefs.Set(p, item.Value); }
        QualitySettings.antiAliasing=_boostAa; QualitySettings.pixelLightCount=_boostPixelLights;
        QualitySettings.maximumLODLevel=_boostMaxLod; QualitySettings.particleRaycastBudget=_boostParticleBudget;
        QualitySettings.asyncUploadTimeSlice=_boostAsyncSlice; QualitySettings.asyncUploadBufferSize=_boostAsyncBuffer;
        QualitySettings.vSyncCount=_boostVsync; Application.targetFrameRate=_boostTargetFps;
        QualitySettings.lodBias=_boostLodBias; QualitySettings.shadowDistance=_boostShadowDistance;
        QualitySettings.shadows=_boostShadows; QualitySettings.anisotropicFiltering=_boostAniso;
        QualitySettings.realtimeReflectionProbes=_boostRealtimeReflections;
        QualitySettings.softVegetation=_boostSoftVegetation; QualitySettings.streamingMipmapsActive=_boostStreaming;
        try { GamePrefs.Instance.Save(); } catch { }
        _fpsBoostIntPrefs.Clear(); _fpsBoostBoolPrefs.Clear(); _fpsBoostFloatPrefs.Clear();
        _fpsBoostSnapshotCaptured=false; _fpsBoostActive=false;
    }

    private static void SetBoostPref(string name, int value)
    {
        EnumGamePrefs pref; if (!Enum.TryParse(name, out pref)) return;
        GamePrefs.Set(pref, value); GameOptionsManager.OnGamePrefChanged(pref);
    }

    private static void SetBoostBoolPref(string name, bool value)
    {
        EnumGamePrefs pref; if (!Enum.TryParse(name, out pref)) return;
        GamePrefs.Set(pref, value); GameOptionsManager.OnGamePrefChanged(pref);
    }

    private static void SetBoostFloatPref(string name, float value)
    {
        EnumGamePrefs pref; if (!Enum.TryParse(name, out pref)) return;
        GamePrefs.Set(pref, value); GameOptionsManager.OnGamePrefChanged(pref);
    }

    private void ApplyVanillaGraphics()
    {
        try
        {
            GameOptionsManager.ApplyAllOptions(xui?.playerUI);
            GamePrefs.Instance.Save();
        }
        catch (Exception ex)
        {
            TraceGraphic("VANILLA_APPLY_FAILED", ex.GetType().Name + ": " + ex.Message);
        }
    }

    private void ToggleContextMenu()
    {
        _contextVisible = !_contextVisible;
        _displaySubmenuVisible = false;
        _contextAnimation = 0f;
        _status = _contextVisible ? "TASKBAR SETTINGS OPEN" : "TASKBAR SETTINGS CLOSED";
    }

    private void OpenGfxSettingsDirect()
    {
        _gfxPanelVisible = !_gfxPanelVisible;
        _contextVisible = false;
        _displaySubmenuVisible = false;
        if (_gfxPanel?.ViewComponent != null)
            _gfxPanel.ViewComponent.IsVisible = _gfxPanelVisible;
        _gfxStatus = _gfxPanelVisible
            ? "GFX CONTROL CENTER • CLIENT-ONLY LIVE SETTINGS"
            : _gfxStatus;
        StartupTerminal.Trace("UI", "GFX_PANEL_DIRECT",
            "visible=" + _gfxPanelVisible + " sameFrame=true");
        SetAllChildrenDirty();
    }

    private void CloseGfxPanelDirect()
    {
        _gfxPanelVisible = false;
        CloseGfxPresets();
        if (_gfxPanel?.ViewComponent != null) _gfxPanel.ViewComponent.IsVisible = false;
        SetAllChildrenDirty();
    }

    private void OpenDevGraphicsDirect()
    {
        _gfxPresetsVisible = true;
        _gfxPresetsMinimized = false;
        _gfxDevGraphicsVisible = true;
        SyncGfxFlyoutVisibility();
    }

    private void SyncGfxFlyoutVisibility()
    {
        XUiController presets = FindDescendant<XUiController>(this, "gfxPresetManager");
        XUiController dev = FindDescendant<XUiController>(this, "gfxDevGraphics");
        XUiController live = FindDescendant<XUiController>(this, "gfxLiveSliders");
        XUiController presetRestore = FindDescendant<XUiController>(this, "btnGfxRestorePresets");
        XUiController liveRestore = FindDescendant<XUiController>(this, "btnGfxRestoreSliders");
        if (presets?.ViewComponent != null) presets.ViewComponent.IsVisible = _gfxPresetsVisible && !_gfxPresetsMinimized && !_gfxDevGraphicsVisible;
        if (dev?.ViewComponent != null) dev.ViewComponent.IsVisible = _gfxPresetsVisible && !_gfxPresetsMinimized && _gfxDevGraphicsVisible;
        if (live?.ViewComponent != null) live.ViewComponent.IsVisible = !_gfxSlidersMinimized && !_gfxPresetsVisible;
        if (presetRestore?.ViewComponent != null) presetRestore.ViewComponent.IsVisible = _gfxPresetsVisible && _gfxPresetsMinimized;
        if (liveRestore?.ViewComponent != null) liveRestore.ViewComponent.IsVisible = _gfxSlidersMinimized;
        SetAllChildrenDirty();
    }

    private void CloseContextMenu()
    {
        _contextVisible = false;
        _displaySubmenuVisible = false;
        _status = "TASKBAR SETTINGS CLOSED";
    }

    private void ToggleDisplaySubmenu()
    {
        _displaySubmenuVisible = !_displaySubmenuVisible;
        _status = _displaySubmenuVisible ? "DISPLAY SETTINGS" : "TASKBAR SETTINGS";
    }

    private void ToggleFpsDisplay()
    {
        ShowFpsOverlay = !ShowFpsOverlay;
        Notify(ShowFpsOverlay ? "FPS HUD ON • Contorul FPS este vizibil"
            : "FPS HUD OFF • Contorul FPS este ascuns");
    }

    private void Bind(string id, System.Action action)
    {
        XUiC_SimpleButton button = FindDescendant<XUiC_SimpleButton>(this, id);

        if (button == null)
        {
            UnityEngine.Debug.Log(
                "[cHub] [PauseTools] BIND FAILED // " + id);

            return;
        }

        button.OnPressed += (sender, mouseButton) =>
        {
            UnityEngine.Debug.Log(
                "[cHub] [PauseTools] PRESSED // " +
                id +
                " // mouseButton=" +
                mouseButton);

            // XUi simplebutton may report its normal activation as -1.
            // Accept both the standard left-click value 0 and XUi's -1 activation.
            if (mouseButton != 0 && mouseButton != -1)
                return;

            action();

            UnityEngine.Debug.Log(
                "[cHub] [PauseTools] ACTION EXECUTED // " + id);

            StartupTerminal.Trace(
                "UI",
                "BUTTON",
                id + " action=executed mouse=" + mouseButton);

            SetAllChildrenDirty();
        };
    }

    private static string OnOff(int value) { return value != 0 ? "ON" : "OFF"; }
    private static string OnOff(bool value) { return value ? "ON" : "OFF"; }

    private void ToggleGfxPanel()
    {
        _gfxPanelVisible = !_gfxPanelVisible;
        _contextVisible = false;
        _displaySubmenuVisible = false;
        _gfxStatus = _gfxPanelVisible ? "READY • CLIENT-ONLY LIVE CONTROLS" : _gfxStatus;
        SetAllChildrenDirty();
    }

    private void ToggleGfxSliders()
    {
        _gfxSlidersMinimized = !_gfxSlidersMinimized;
        if (!_gfxSlidersMinimized)
        {
            _gfxPresetsVisible = false;
            _gfxPresetsMinimized = false;
            _gfxDevGraphicsVisible = false;
        }
        XUiController sliders = FindDescendant<XUiController>(this, "gfxLiveSliders");
        XUiController restore = FindDescendant<XUiController>(this, "btnGfxRestoreSliders");
        if (sliders?.ViewComponent != null) sliders.ViewComponent.IsVisible = !_gfxSlidersMinimized;
        if (restore?.ViewComponent != null) restore.ViewComponent.IsVisible = _gfxSlidersMinimized;
        SyncGfxFlyoutVisibility();
        _gfxStatus = _gfxSlidersMinimized ? "LIVE SLIDERS MINIMIZED" : "LIVE SLIDERS RESTORED";
        SetAllChildrenDirty();
    }

    private void CycleGfxWindowMode()
    {
        _gfxWindowMode = (_gfxWindowMode + 1) % 3;
        FullScreenMode mode = _gfxWindowMode == 0 ? FullScreenMode.ExclusiveFullScreen :
            _gfxWindowMode == 1 ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        Screen.fullScreenMode = mode;
        Screen.SetResolution(Screen.width, Screen.height, mode, Screen.currentResolution.refreshRateRatio);
        _bindingRefreshBurst = 0.75f;
        StartupTerminal.Audit("DISPLAY_MODE", "APPLY", "OK", "requested=" + mode + " effective=" + Screen.fullScreenMode);
        _gfxStatus = "WINDOW MODE • " + (_gfxWindowMode == 0 ? "FULLSCREEN" :
            _gfxWindowMode == 1 ? "BORDERLESS" : "WINDOWED");
        Notify(_gfxStatus + " • Aplicat local");
        SetAllChildrenDirty();
    }

    private void BindGfxWindowManipulation()
    {
        XUiController drag = FindDescendant<XUiController>(this, "gfxDragHandle");
        if (drag != null) drag.OnDrag += (sender, type, delta) =>
        {
            if (type != EDragType.Dragging || _gfxPanel?.ViewComponent?.UiTransform == null) return;
            _gfxDragRemainder += delta;
            Vector3 p = _gfxPanel.ViewComponent.UiTransform.localPosition;
            p.x += _gfxDragRemainder.x; p.y += _gfxDragRemainder.y;
            _gfxDragRemainder = Vector2.zero;
            _gfxPanel.ViewComponent.UiTransform.localPosition = p;
        };
        XUiController resize = FindDescendant<XUiController>(this, "gfxResizeHandle");
        if (resize != null) resize.OnDrag += (sender, type, delta) =>
        {
            if (type != EDragType.Dragging || _gfxPanel?.ViewComponent?.UiTransform == null) return;
            float factor = 1f + delta.x / 900f - delta.y / 900f;
            Vector3 scale = _gfxPanel.ViewComponent.UiTransform.localScale * factor;
            float value = Mathf.Clamp(scale.x, 0.65f, 1.35f);
            _gfxUiScale = value;
            PlayerPrefs.SetFloat("cHub.GfxHubPanelScale", _gfxUiScale);
            ApplyTaskbarAndGfxScale(false);
        };
    }

    private void SlideTaskbarScale(float delta)
    {
        float step = Mathf.Max(0.01f, Mathf.Abs(delta) / 700f);
        _taskbarScale = Mathf.Clamp(_taskbarScale + (delta > 0f ? step : -step), 0.65f, 1.35f);
        _taskbarScale = Mathf.Round(_taskbarScale * 100f) / 100f;
        PlayerPrefs.SetFloat("cHub.TaskbarScale", _taskbarScale);
        PlayerPrefs.Save();
        ApplyTaskbarAndGfxScale(true);
    }

    private void SlideGfxPanelScale(float delta)
    {
        float step = Mathf.Max(0.01f, Mathf.Abs(delta) / 700f);
        _gfxUiScale = Mathf.Clamp(_gfxUiScale + (delta > 0f ? step : -step), 0.65f, 1.35f);
        _gfxUiScale = Mathf.Round(_gfxUiScale * 100f) / 100f;
        PlayerPrefs.SetFloat("cHub.GfxHubPanelScale", _gfxUiScale);
        PlayerPrefs.Save();
        ApplyTaskbarAndGfxScale(true);
    }

    private void ApplyTaskbarAndGfxScale(bool notify)
    {
        if (ViewComponent?.UiTransform != null)
            ViewComponent.UiTransform.localScale = Vector3.one;
        foreach (var pair in _taskbarBasePositions)
        {
            Transform transform = pair.Key?.ViewComponent?.UiTransform;
            if (transform == null) continue;
            Vector3 basePosition = pair.Value;
            Vector3 baseScale = _taskbarBaseScales[pair.Key];
            transform.localPosition = new Vector3(
                basePosition.x * _taskbarScale,
                basePosition.y * _taskbarScale,
                basePosition.z);
            transform.localScale = new Vector3(
                baseScale.x * _taskbarScale,
                baseScale.y * _taskbarScale,
                baseScale.z);
        }
        if (_gfxPanel?.ViewComponent?.UiTransform != null)
            _gfxPanel.ViewComponent.UiTransform.localScale = new Vector3(_gfxUiScale, _gfxUiScale, 1f);
        SetAllChildrenDirty();
        if (!notify) return;
        Notify("UI SCALE • TASKBAR " + Mathf.RoundToInt(_taskbarScale * 100f) +
            "% • gfxHub " + Mathf.RoundToInt(_gfxUiScale * 100f) + "%");
        StartupTerminal.Audit("UI_SCALE", "TASKBAR_GFX", "OK",
            "taskbar=" + _taskbarScale.ToString("0.00") + " gfx=" + _gfxUiScale.ToString("0.00"));
    }

    private void CaptureTaskbarTransforms()
    {
        _taskbarBasePositions.Clear();
        _taskbarBaseScales.Clear();
        foreach (XUiController child in Children)
        {
            Transform transform = child?.ViewComponent?.UiTransform;
            string id = child?.ViewComponent?.ID ?? string.Empty;
            if (transform == null || id.Equals("gfxControlCenter", StringComparison.OrdinalIgnoreCase) ||
                id.Equals("taskbarScaleSlider", StringComparison.OrdinalIgnoreCase) ||
                id.Equals("gfxPanelScaleSlider", StringComparison.OrdinalIgnoreCase)) continue;
            _taskbarBasePositions[child] = transform.localPosition;
            _taskbarBaseScales[child] = transform.localScale;
        }
    }

    private void BindGfxSliders()
    {
        BindSlider("gfxSliderAf", d => SlideInt(ref _gfxAf, d, 0, 2, "af", "ANISOTROPIC"));
        BindSlider("gfxSliderDynamic", d => SlideBool(ref _gfxDynamic, d, "dr", "DYNAMIC RESOLUTION", " 0.5 1"));
        BindSlider("gfxSliderDt", d => SlideBool(ref _gfxDt, d, "dt", "DISTANT TERRAIN"));
        BindSlider("gfxSliderDti", d => SlideBool(ref _gfxDti, d, "dti", "TERRAIN INSTANCING"));
        BindSlider("gfxSliderLod", d => SlideInt(ref _gfxLod, d, 0, 5, "dtmaxlod", "TERRAIN LOD"));
        BindSlider("gfxSliderPixel", d => SlideInt(ref _gfxPixel, d, 1, 200, "dtpix", "PIXEL ERROR"));
        BindSlider("gfxSliderView", d => SlideInt(ref _gfxView, d, 1, 20, "viewdist", "VIEW DISTANCE"));
        BindSlider("gfxSliderAo", d => SlideBool(ref _gfxAo, d, "pp ao", "AMBIENT OCCLUSION"));
        BindSlider("gfxSliderBloom", d => SlideBool(ref _gfxBloom, d, "pp bloom", "BLOOM"));
        BindSlider("gfxSliderSsao", d => SlideBool(ref _gfxSsao, d, "pp ssao", "SSAO"));
        BindSlider("gfxSliderSun", d => SlideBool(ref _gfxSunshafts, d, "pp sunshafts", "SUN SHAFTS"));
        BindSlider("gfxSliderDof", d => SlideBool(ref _gfxDof, d, "pp dof", "DEPTH OF FIELD"));
        BindSlider("gfxSliderMotion", d => SlideBool(ref _gfxMotionBlur, d, "pp motionblur", "MOTION BLUR"));
        BindSlider("gfxSliderExposure", d => SlideBool(ref _gfxExposure, d, "pp ae", "AUTO EXPOSURE"));
        BindSlider("gfxSliderColor", d => SlideBool(ref _gfxColor, d, "pp cg", "COLOR GRADING"));
        BindSlider("gfxSliderSkin", d => SlideSkin(d));
        BindSlider("gfxSliderStream", d => SlideBool(ref _gfxStream, d, "st budget", "MIP STREAMING"));
        BindSlider("gfxSliderBias", d => SlideInt(ref _gfxBias, d, -10, 10, "texbias", "TEXTURE BIAS"));
        BindSlider("gfxSliderLimit", d => SlideInt(ref _gfxLimit, d, 0, 8, "texlimit", "TEXTURE LIMIT"));
        BindSlider("gfxSliderUiScale", SlideInterfaceScale);
    }

    private void SlideInterfaceScale(float delta)
    {
        float step = Mathf.Max(0.01f, Mathf.Abs(delta) / 800f);
        float next = Mathf.Clamp(_interfaceScale + (delta > 0f ? step : -step), 0.65f, 1.25f);
        next = Mathf.Round(next * 100f) / 100f;
        if (Mathf.Approximately(next, _interfaceScale)) return;
        _interfaceScale = next;
        PlayerPrefs.SetFloat("cHub.InterfaceScale", _interfaceScale);
        PlayerPrefs.Save();
        ApplyInterfaceScale(true);
        _gfxVisualDirty = true;
        SetAllChildrenDirty();
    }

    private void ApplyInterfaceScale(bool notify)
    {
        if (xui == null) return;
        // Restore local transforms changed by the legacy implementation. The
        // native OptionsHudSize preference owns the actual render scale.
        string[] scalableGroups =
        {
            "crafting", "character", "cosmetics", "combine", "workstation_campfire",
            "workstation_forge", "workstation_forge_nosmelting", "workstation_cementMixer",
            "workstation_workbench", "workstation_chemistryStation", "looting", "creative",
            "backpack", "vehicle", "bagStorage", "junkDrone", "map", "players", "assemble",
            "skills", "quests", "challenges"
        };
        int restoredWindows = 0;
        foreach (string groupName in scalableGroups)
        {
            XUiController group = xui.FindWindowGroupByName(groupName);
            if (group == null) continue;
            if (group.ViewComponent?.UiTransform != null)
            {
                group.ViewComponent.UiTransform.localScale = Vector3.one;
                restoredWindows++;
            }
            foreach (XUiController window in group.Children)
            {
                if (window?.ViewComponent?.UiTransform == null) continue;
                window.ViewComponent.UiTransform.localScale = Vector3.one;
                restoredWindows++;
            }
        }

        float requestedHudSize = Mathf.Max(0.01f, _baseHudSize * _interfaceScale);
        GamePrefs.Set(EnumGamePrefs.OptionsHudSize, requestedHudSize);
        GameOptionsManager.OnGamePrefChanged(EnumGamePrefs.OptionsHudSize);
        float activeScale = Mathf.Max(0.01f, GameOptionsManager.GetActiveUiScale());

        // GameOptionsManager updates the preference, but an already open XUi
        // keeps its previous root scale. Apply the resolved value directly to
        // the live XUi root so TAB/ESC/inventory resize during the drag.
        xui.SetScale(activeScale);

        // Open XUi groups cache their geometry. Mark every scalable group dirty
        // after the native preference changes so inventory/crafting redraw while
        // the slider is still held instead of waiting for the next Tab/Esc open.
        int refreshedGroups = 0;
        foreach (string groupName in scalableGroups)
        {
            XUiController group = xui.FindWindowGroupByName(groupName);
            if (group == null) continue;
            group.SetAllChildrenDirty();
            refreshedGroups++;
        }

        // Native HUD scaling is global. Counter-scale only the compact HUD
        // elements that the user explicitly asked to keep unchanged.
        float inverse = _baseActiveUiScale / activeScale;
        int compensated = 0;
        foreach (string windowName in new[]
        {
            "HUDLeftStatBars", "HUDRightStatBars", "windowCompass", "windowQuestTracker",
            "windowRecipeTracker", "windowGroupBars", "windowLocation"
        })
        {
            XUiV_Window window = xui.GetWindow(windowName);
            if (window?.Controller?.ViewComponent?.UiTransform == null) continue;
            window.Controller.ViewComponent.UiTransform.localScale =
                new Vector3(inverse, inverse, 1f);
            compensated++;
        }

        if (!notify) return;
        string message = "UI SCALE " + Mathf.RoundToInt(_interfaceScale * 100f) +
            "% • meniuri si toolbelt; HUD vital neschimbat";
        Notify(message);
        StartupTerminal.Audit("UI_SCALE", "APPLY", "OK",
            "relative=" + _interfaceScale.ToString("0.00") +
            " requestedHud=" + requestedHudSize.ToString("0.000") +
            " active=" + activeScale.ToString("0.000") +
            " liveXui=" + xui.GetScale().ToString("0.000") +
            " restored=" + restoredWindows + " refreshed=" + refreshedGroups +
            " compensated=" + compensated +
            " excluded=hp,stamina,compass,trackers");
    }

    private void BindSlider(string id, Action<float> change)
    {
        XUiController slider = FindDescendant<XUiController>(this, id);
        if (slider == null) return;
        slider.OnDrag += (sender, dragType, delta) =>
        {
            _gfxSliderDragging = dragType == EDragType.Dragging;
            if (dragType == EDragType.Dragging && Mathf.Abs(delta.x) > 0.01f)
                change(delta.x);
        };
        slider.OnMouseUpDown += (sender, down) => _gfxSliderDragging = down;
    }

    private void SlideInt(ref int value, float delta, int min, int max, string command, string title)
    {
        int step = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(delta) / 8f));
        int next = Mathf.Clamp(value + (delta > 0f ? step : -step), min, max);
        if (next == value) return;
        value = next;
        ApplySliderGfx(command + " " + value, title + " " + value);
    }

    private void SlideBool(ref int value, float delta, string command, string title, string suffix = "")
    {
        int next = delta >= 0f ? 1 : 0;
        if (next == value) return;
        value = next;
        ApplySliderGfx(command + " " + value + suffix, title + " " + OnOff(value));
    }

    private void SlideBool(ref bool value, float delta, string command, string title)
    {
        bool next = delta >= 0f;
        if (next == value) return;
        value = next;
        ApplySliderGfx(command + " " + (value ? 1 : 0), title + " " + OnOff(value));
    }

    private void SlideSkin(float delta)
    {
        int[] values = { 1, 2, 4, 5 };
        int index = Array.IndexOf(values, _gfxSkin);
        index = Mathf.Clamp(index + (delta > 0f ? 1 : -1), 0, values.Length - 1);
        if (_gfxSkin == values[index]) return;
        _gfxSkin = values[index];
        ApplySliderGfx("skin " + _gfxSkin, "SKIN BONES " + _gfxSkin);
    }

    private void ApplySliderGfx(string arguments, string description)
    {
        _pendingSliderCommand = arguments;
        _pendingSliderDescription = description;
        if (_gfxSliderApplyCooldown <= 0f) _gfxSliderApplyCooldown = 0.075f;
        _gfxStatus = description + " - LIVE PREVIEW";
        _gfxVisualDirty = true;
        return;
#pragma warning disable 162
        _gfxSliderApplyCooldown = 0.025f;
        try { ExecuteGfxSilent(arguments); _gfxStatus = description + " • LIVE PREVIEW"; }
        catch (Exception ex) { _gfxStatus = description + " • FAILED: " + ex.Message; }
        SetAllChildrenDirty();
#pragma warning restore 162
    }

    private void UpdateGfxSliderPreview(float deltaTime)
    {
        _gfxSliderApplyCooldown = Mathf.Max(0f, _gfxSliderApplyCooldown - deltaTime);
        if (_gfxSliderDragging && !Input.GetMouseButton(0)) _gfxSliderDragging = false;
        float target = _gfxSliderDragging ? 0.22f : 1f;
        _gfxPanelOpacity = Mathf.Lerp(_gfxPanelOpacity, target, 1f - Mathf.Exp(-deltaTime * 12f));
        if (Mathf.Abs(_gfxPanelOpacity - _lastAppliedPanelOpacity) >= 0.025f ||
            (!_gfxSliderDragging && _lastAppliedPanelOpacity < 0.999f))
        {
            ApplyOpacityRecursive(_gfxPanel, _gfxPanelOpacity);
            _lastAppliedPanelOpacity = _gfxPanelOpacity;
        }
        if (_gfxVisualDirty)
        {
            UpdateAllGfxKnobs();
            SetAllChildrenDirty();
            _gfxVisualDirty = false;
        }
    }

    private void ProcessGfxCommands(float deltaTime)
    {
        if (_pendingSliderCommand != null && (_gfxSliderApplyCooldown <= 0f || !_gfxSliderDragging))
        {
            string command = _pendingSliderCommand;
            string description = _pendingSliderDescription;
            _pendingSliderCommand = null;
            try { ExecuteGfxSilent(command); _gfxStatus = description + " - APPLIED LOCALLY"; }
            catch (Exception ex) { _gfxStatus = description + " - FAILED: " + ex.Message; }
        }
        if (_gfxCommandQueue.Count == 0) return;
        _gfxCommandQueueDelay -= deltaTime;
        if (_gfxCommandQueueDelay > 0f) return;
        string queued = _gfxCommandQueue.Dequeue();
        try { ExecuteGfxSilent(queued); }
        catch (Exception ex) { StartupTerminal.Trace("GFX", "PRESET_COMMAND_FAILED", queued + " " + ex.Message); }
        _gfxCommandQueueDelay = 0.035f;
        if (_gfxCommandQueue.Count == 0)
        {
            _gfxStatus = "PRESET READY - VALUES REMAIN EDITABLE";
            Notify("GFX PRESET - Aplicat local; valorile raman editabile");
        }
    }

    private void UpdateAllGfxKnobs()
    {
        SetKnob("gfxKnobAf", _gfxAf / 2f); SetKnob("gfxKnobDynamic", _gfxDynamic);
        SetKnob("gfxKnobDt", _gfxDt); SetKnob("gfxKnobDti", _gfxDti);
        SetKnob("gfxKnobLod", _gfxLod / 5f); SetKnob("gfxKnobPixel", (_gfxPixel - 1f) / 199f);
        SetKnob("gfxKnobView", (_gfxView - 1f) / 19f); SetKnob("gfxKnobAo", _gfxAo ? 1f : 0f);
        SetKnob("gfxKnobBloom", _gfxBloom ? 1f : 0f); SetKnob("gfxKnobSsao", _gfxSsao ? 1f : 0f);
        SetKnob("gfxKnobSun", _gfxSunshafts ? 1f : 0f); SetKnob("gfxKnobDof", _gfxDof ? 1f : 0f);
        SetKnob("gfxKnobMotion", _gfxMotionBlur ? 1f : 0f); SetKnob("gfxKnobExposure", _gfxExposure ? 1f : 0f);
        SetKnob("gfxKnobColor", _gfxColor ? 1f : 0f);
        SetKnob("gfxKnobSkin", _gfxSkin <= 1 ? 0f : _gfxSkin == 2 ? 0.34f : _gfxSkin == 4 ? 0.68f : 1f);
        SetKnob("gfxKnobStream", _gfxStream); SetKnob("gfxKnobBias", (_gfxBias + 10f) / 20f);
        SetKnob("gfxKnobLimit", _gfxLimit / 8f);
        SetKnob("gfxKnobUiScale", (_interfaceScale - 0.65f) / 0.60f);
    }

    private void SetKnob(string id, float normalized)
    {
        XUiController knob = FindDescendant<XUiController>(this, id);
        if (knob?.ViewComponent == null) return;
        knob.ViewComponent.Position = new Vector2i(132 + Mathf.RoundToInt(Mathf.Clamp01(normalized) * 164f), -5);
    }

    private static void ApplyOpacityRecursive(XUiController controller, float opacity)
    {
        if (controller == null) return;
        object view = controller.ViewComponent;
        if (view != null)
        {
            PropertyInfo property = view.GetType().GetProperty("GlobalOpacityModifier",
                BindingFlags.Public | BindingFlags.Instance);
            if (property != null && property.CanWrite)
                property.SetValue(view, opacity, null);
        }
        foreach (XUiController child in controller.Children)
            ApplyOpacityRecursive(child, opacity);
    }

    private void ChangeGfxInt(ref int value, int delta, int min, int max, string command, string title)
    {
        value = Mathf.Clamp(value + delta, min, max);
        ApplyGfx(command + " " + value, title + " " + value);
    }

    private void TogglePostProcess(ref bool value, string effect, string title)
    {
        value = !value;
        ApplyGfx("pp " + effect + " " + (value ? 1 : 0), title + " " + OnOff(value));
    }

    private void ToggleAllCinematicEffects()
    {
        bool enabled = !(_gfxSsao && _gfxBloom && _gfxSunshafts && _gfxDof && _gfxMotionBlur);
        _gfxSsao = _gfxBloom = _gfxSunshafts = _gfxDof = _gfxMotionBlur = enabled;
        foreach (string effect in new[] { "ssao", "bloom", "sunshafts", "dof", "motionblur" })
            ExecuteGfxSilent("pp " + effect + " " + (enabled ? 1 : 0));
        _gfxStatus = "CINEMATIC EFFECTS " + OnOff(enabled) + " • APPLIED LOCALLY";
        Notify("CINEMATIC EFFECTS " + OnOff(enabled) + " • Toate efectele au fost aplicate local");
        SetAllChildrenDirty();
    }

    private void CycleGfxResolution()
    {
        if (_gfxResolutions.Count <= 1)
        {
            Notify("RESOLUTION LOCKED - Monitorul nu raporteaza alte rezolutii sigure");
            return;
        }
        _gfxResolutionIndex = (_gfxResolutionIndex + 1) % _gfxResolutions.Count;
        string[] parts = _gfxResolutions[_gfxResolutionIndex].Split('x');
        int width, height;
        if (!int.TryParse(parts[0], out width) || !int.TryParse(parts[1], out height)) return;
        Screen.SetResolution(width, height, Screen.fullScreenMode, Screen.currentResolution.refreshRateRatio);
        _gfxStatus = "RESOLUTION " + _gfxResolutions[_gfxResolutionIndex] + " APPLIED LOCALLY";
        Notify("RESOLUTION " + _gfxResolutions[_gfxResolutionIndex] + " - Aplicata local in timp real");
        _bindingRefreshBurst = 0.75f;
        SetAllChildrenDirty();
        StartupTerminal.Audit("RESOLUTION", "REQUEST", "OK",
            "selected=" + _gfxResolutions[_gfxResolutionIndex] + " monitorMax=" + Screen.currentResolution.width + "x" + Screen.currentResolution.height);
    }

    private void BuildSupportedResolutionList()
    {
        _gfxResolutions.Clear();
        int maxWidth = Screen.currentResolution.width;
        int maxHeight = Screen.currentResolution.height;
        foreach (string candidate in new[] { "1280x720", "1600x900", "1920x1080", "2560x1440", "3840x2160" })
        {
            string[] p = candidate.Split('x'); int w, h;
            if (!int.TryParse(p[0], out w) || !int.TryParse(p[1], out h)) continue;
            bool supported = Screen.resolutions.Any(r => r.width == w && r.height == h);
            if (supported && w <= maxWidth && h <= maxHeight) _gfxResolutions.Add(candidate);
        }
        string current = Screen.width + "x" + Screen.height;
        if (!_gfxResolutions.Contains(current)) _gfxResolutions.Add(current);
        _gfxResolutions.Sort((a,b) => int.Parse(a.Split('x')[0]).CompareTo(int.Parse(b.Split('x')[0])));
        _gfxResolutionIndex = Mathf.Max(0, _gfxResolutions.IndexOf(current));
    }

    private void ApplyGfx(string arguments, string description)
    {
        try
        {
            var result = SdtdConsole.Instance.ExecuteSync("gfx " + arguments, null);
            _gfxStatus = description + " • APPLIED LOCALLY";
            StartupTerminal.Trace("GFX", "LOCAL_APPLY", "command=gfx " + arguments + " result=" +
                (result == null ? "none" : string.Join(" | ", result.ToArray())));
            StartupTerminal.Audit("GFX_COMMAND", arguments, "OK", "description=" + description);
            Notify(description + " • Aplicat local în timp real");
        }
        catch (Exception ex)
        {
            _gfxStatus = description + " • FAILED: " + ex.Message;
            StartupTerminal.Trace("GFX", "LOCAL_APPLY_FAILED", "command=gfx " + arguments + " " + ex.Message);
            StartupTerminal.Audit("GFX_COMMAND", arguments, "FAILED", ex.GetType().Name + ": " + ex.Message);
        }
        SyncGfxFlyoutVisibility();
    }

    private void ExecuteGfxSilent(string arguments)
    {
        var result = SdtdConsole.Instance.ExecuteSync("gfx " + arguments, null);
        StartupTerminal.Trace("GFX", "LOCAL_APPLY", "command=gfx " + arguments + " result=" +
            (result == null ? "none" : string.Join(" | ", result.ToArray())));
    }


    private string GetGfxPresetDirectory()
    {
        string modFolder =
            System.IO.Path.GetDirectoryName(
                typeof(XUiC_cHubPauseTools)
                    .Assembly
                    .Location);

        string presetDirectory =
            System.IO.Path.Combine(
                modFolder,
                "Presets",
                "Graphics");

        if (!System.IO.Directory.Exists(presetDirectory))
        {
            System.IO.Directory.CreateDirectory(presetDirectory);
        }

        return presetDirectory;
    }
    private string GfxPresetKey { get { return "cHub.Gfx.Custom."; } }

    private string GfxPresetSlotKey(int slot)
    {
        return GfxPresetKey + slot + ".";
    }

    private void MigrateLegacyGfxPresetIfNeeded()
    {
        string slot1Data =
            PlayerPrefs.GetString(
                GfxPresetSlotKey(1) + "data",
                string.Empty);

        if (!string.IsNullOrWhiteSpace(slot1Data))
            return;


        string legacyData =
            PlayerPrefs.GetString(
                GfxPresetKey + "data",
                string.Empty);

        if (string.IsNullOrWhiteSpace(legacyData))
            return;


        string legacyName =
            PlayerPrefs.GetString(
                GfxPresetKey + "name",
                "Client save preset");

        if (string.IsNullOrWhiteSpace(legacyName))
            legacyName = "Client save preset";


        PlayerPrefs.SetString(
            GfxPresetSlotKey(1) + "name",
            legacyName);

        PlayerPrefs.SetString(
            GfxPresetSlotKey(1) + "data",
            legacyData);

        PlayerPrefs.Save();


        UnityEngine.Debug.Log(
            "[cHub] [GFXPRESET] LEGACY MIGRATION // " +
            "old single preset -> slot 1 // name=" +
            legacyName);
    }

    private bool TryGetGfxCustomPreset(
    int slot,
    out string name,
    out string data)
    {
        name = string.Empty;
        data = string.Empty;

        if (slot < 1 ||
            slot > GfxCustomPresetSlots)
        {
            return false;
        }


        string key =
            GfxPresetSlotKey(slot);

        data =
            PlayerPrefs.GetString(
                key + "data",
                string.Empty);

        if (string.IsNullOrWhiteSpace(data))
            return false;


        name =
            PlayerPrefs.GetString(
                key + "name",
                "Client preset " + slot);

        if (string.IsNullOrWhiteSpace(name))
            name =
                "Client preset " + slot;


        return true;
    }

    private int GetSelectedGfxCustomPresetSlot()
    {
        if (string.Equals(
            _gfxSelectedPreset,
            "client",
            StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }


        const string prefix =
            "client:";

        if (!_gfxSelectedPreset.StartsWith(
            prefix,
            StringComparison.OrdinalIgnoreCase))
        {
            return -1;
        }


        string raw =
            _gfxSelectedPreset.Substring(
                prefix.Length);

        if (!int.TryParse(
            raw,
            out int slot))
        {
            return -1;
        }


        if (slot < 1 ||
            slot > GfxCustomPresetSlots)
        {
            return -1;
        }


        return slot;
    }

    private void EnsureGfxCustomPresetBindings()
    {
        for (int slot = 2;
             slot <= GfxCustomPresetSlots;
             slot++)
        {
            if (_gfxBoundCustomPresetSlots.Contains(slot))
                continue;


            string id =
                "btnGfxPresetClient" + slot;


            XUiC_SimpleButton button =
                FindDescendant<XUiC_SimpleButton>(
                    this,
                    id);

            if (button == null)
                continue;


            int capturedSlot =
                slot;


            Bind(
                id,
                () =>
                {
                    SelectGfxPreset(
                        "client:" +
                        capturedSlot);
                });


            _gfxBoundCustomPresetSlots.Add(
                slot);


            UnityEngine.Debug.Log(
                "[cHub] [GFXPRESET] SLOT BOUND // " +
                "slot=" +
                slot +
                " id=" +
                id);
        }
    }

    private void ToggleGfxPresets()
    {
        if (_gfxPresetsVisible)
        {
            CloseGfxPresets();
        }
        else
        {
            _gfxPresetsVisible = true;
            _gfxPresetsMinimized = false;
            _gfxDevGraphicsVisible = false;


            RefreshGfxPresetFilter();

            SyncGfxFlyoutVisibility();


            // XUi wrapper wheel OFF.
            // Native NGUI UIScrollView owns scrolling + scrollbar.
            ConfigureGfxPresetNativeClip();


            // Deterministic native top position.
            ResetGfxPresetScroll();


            DebugGfxPresetScrollState(
                "OPEN");
            
            DebugGfxPresetScrollInternals(
                "OPEN");

            DebugGfxPresetNguiBounds(
                "OPEN");

            DebugGfxPresetPanelState(
                "OPEN");

            DebugGfxPresetNguiMotion(
                "OPEN");
        }


        StartupTerminal.Audit(
            "PRESET_UI",
            "TOGGLE",
            "OK",
            "visible=" + _gfxPresetsVisible +
            " minimized=" + _gfxPresetsMinimized);
    }
    private void CloseGfxPresets()
    {
        _gfxPresetsVisible = false;
        _gfxPresetsMinimized = false;
        _gfxDevGraphicsVisible = false;

        SyncGfxFlyoutVisibility();

        StartupTerminal.Audit(
            "PRESET_UI",
            "CLOSE",
            "OK",
            "visible=false dev=false");
    }


    private void ResetGfxPresetScroll()
    {
        XUiController gfxScroll =
            FindDescendant<XUiController>(
                this,
                "gfxPresetScroll");

        if (!(gfxScroll?.ViewComponent is XUiV_ScrollView scrollView))
            return;

        BindingFlags flags =
            BindingFlags.Instance |
            BindingFlags.NonPublic |
            BindingFlags.Public;


        // =========================================================
        // OUR AUTHORITATIVE SCROLL POSITION
        // =========================================================

        _gfxPresetManualY =
            0f;


        if (scrollView.UiTransform != null)
        {
            Vector3 position =
                scrollView.UiTransform.localPosition;

            position.x =
                0f;

            position.y =
                0f;

            scrollView.UiTransform.localPosition =
                position;
        }


        FieldInfo scrollViewField =
            typeof(XUiV_ScrollView).GetField(
                "scrollView",
                flags);


        object nguiScroll =
            scrollViewField?.GetValue(
                scrollView);


        if (nguiScroll == null)
            return;


        try
        {
            Type scrollType =
                nguiScroll.GetType();


            // =====================================================
            // STOP ALL NGUI MOTION
            // =====================================================

            FieldInfo momentumField =
                scrollType.GetField(
                    "mMomentum",
                    flags);


            if (momentumField != null &&
                momentumField.FieldType == typeof(Vector3))
            {
                momentumField.SetValue(
                    nguiScroll,
                    Vector3.zero);
            }


            FieldInfo scrollField =
                scrollType.GetField(
                    "mScroll",
                    flags);


            if (scrollField != null)
            {
                if (scrollField.FieldType == typeof(float))
                {
                    scrollField.SetValue(
                        nguiScroll,
                        0f);
                }
                else if (scrollField.FieldType == typeof(Vector2))
                {
                    scrollField.SetValue(
                        nguiScroll,
                        Vector2.zero);
                }
                else if (scrollField.FieldType == typeof(Vector3))
                {
                    scrollField.SetValue(
                        nguiScroll,
                        Vector3.zero);
                }
            }


            MethodInfo disableSpring =
                scrollType.GetMethod(
                    "DisableSpring",
                    flags,
                    null,
                    Type.EmptyTypes,
                    null);


            disableSpring?.Invoke(
                nguiScroll,
                null);


            // =====================================================
            // RESET PANEL CLIP
            // =====================================================

            object panel =
                null;


            PropertyInfo panelProperty =
                scrollType.GetProperty(
                    "panel",
                    flags);


            if (panelProperty != null &&
                panelProperty.GetIndexParameters().Length == 0)
            {
                try
                {
                    panel =
                        panelProperty.GetValue(
                            nguiScroll,
                            null);
                }
                catch
                {
                }
            }


            if (panel == null)
            {
                FieldInfo panelField =
                    scrollType.GetField(
                        "mPanel",
                        flags);


                panel =
                    panelField?.GetValue(
                        nguiScroll);
            }


            if (panel != null)
            {
                Type panelType =
                    panel.GetType();


                PropertyInfo clipOffsetProperty =
                    panelType.GetProperty(
                        "clipOffset",
                        flags);


                if (clipOffsetProperty != null &&
                    clipOffsetProperty.CanWrite)
                {
                    clipOffsetProperty.SetValue(
                        panel,
                        Vector2.zero,
                        null);
                }
                else
                {
                    FieldInfo clipOffsetField =
                        panelType.GetField(
                            "mClipOffset",
                            flags);


                    if (clipOffsetField != null)
                    {
                        clipOffsetField.SetValue(
                            panel,
                            Vector2.zero);
                    }
                }
            }


            // =====================================================
            // REBUILD BOUNDS
            // =====================================================

            MethodInfo invalidate =
                scrollType.GetMethod(
                    "InvalidateBounds",
                    flags,
                    null,
                    Type.EmptyTypes,
                    null);


            invalidate?.Invoke(
                nguiScroll,
                null);


            UnityEngine.Debug.Log(
                "[cHub] [GFXSCROLL] RESET // " +
                "manualY=0 // local=(0,0) // clipOffset=(0,0)");
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.Log(
                "[cHub] [GFXSCROLL] RESET FAILED // " +
                ex.GetType().Name +
                ": " +
                ex.Message);
        }
    }
    private void DebugGfxPresetNguiBounds(string stage)
    {
        XUiController gfxScroll =
            FindDescendant<XUiController>(this, "gfxPresetScroll");

        if (!(gfxScroll?.ViewComponent is XUiV_ScrollView scrollView))
            return;

        BindingFlags flags =
            BindingFlags.Instance |
            BindingFlags.NonPublic |
            BindingFlags.Public;

        FieldInfo scrollViewField =
            typeof(XUiV_ScrollView).GetField(
                "scrollView",
                flags);

        object nguiScroll =
            scrollViewField?.GetValue(scrollView);

        if (nguiScroll == null)
        {
            UnityEngine.Debug.Log(
                "[cHub] [GFXBOUNDS] " + stage +
                " // UIScrollView missing");
            return;
        }

        Type type = nguiScroll.GetType();

        FieldInfo restrictField =
            type.GetField(
                "restrictWithinPanel",
                flags);

        FieldInfo boundsField =
            type.GetField(
                "mBounds",
                flags);

        object restrictValue =
            restrictField?.GetValue(nguiScroll);

        object boundsValue =
            boundsField?.GetValue(nguiScroll);

        UnityEngine.Debug.Log(
            "[cHub] [GFXBOUNDS] " +
            stage +
            " // restrictWithinPanel=" +
            (restrictValue == null
                ? "FIELD_NOT_FOUND"
                : restrictValue.ToString()) +
            " // mBounds=" +
            (boundsValue == null
                ? "FIELD_NOT_FOUND"
                : boundsValue.ToString()) +
            " // scrollLocal=" +
            scrollView.UiTransform.localPosition);
    }

    private void ToggleGfxPresetMinimized()
    {
        _gfxPresetsVisible = true;
        _gfxPresetsMinimized = !_gfxPresetsMinimized;
        if (_gfxPresetsMinimized) _gfxDevGraphicsVisible = false;
        SyncGfxFlyoutVisibility();
        StartupTerminal.Audit("PRESET_UI", "MINIMIZE", "OK", "minimized=" + _gfxPresetsMinimized);
    }

    private void SelectGfxPreset(string preset)
    {
        if (string.IsNullOrWhiteSpace(preset))
            return;


        if (string.Equals(
            preset,
            "client",
            StringComparison.OrdinalIgnoreCase))
        {
            preset =
                "client:1";
        }


        _gfxSelectedPreset =
            preset;


        int customSlot =
            GetSelectedGfxCustomPresetSlot();

        if (customSlot > 0)
        {
            if (TryGetGfxCustomPreset(
                customSlot,
                out string name,
                out string data))
            {
                _gfxPresetName =
                    name;

                if (_gfxPresetNameInput != null)
                {
                    _gfxPresetNameInput.Text =
                        name;
                }
            }
        }


        RefreshGfxPresetFilter();

        SetAllChildrenDirty();


        UnityEngine.Debug.Log(
            "[cHub] [GFXPRESET] SELECT // " +
            _gfxSelectedPreset);
    }
    private void InvalidateGfxPresetScrollBounds()
    {
        XUiController gfxScroll =
            FindDescendant<XUiController>(
                this,
                "gfxPresetScroll");

        if (!(gfxScroll?.ViewComponent is XUiV_ScrollView scrollView))
            return;

        BindingFlags flags =
            BindingFlags.Instance |
            BindingFlags.NonPublic |
            BindingFlags.Public;

        FieldInfo scrollViewField =
            typeof(XUiV_ScrollView).GetField(
                "scrollView",
                flags);

        object nguiScroll =
            scrollViewField?.GetValue(
                scrollView);

        if (nguiScroll == null)
            return;

        Type type =
            nguiScroll.GetType();

        try
        {
            MethodInfo invalidate =
                type.GetMethod(
                    "InvalidateBounds",
                    flags,
                    null,
                    Type.EmptyTypes,
                    null);

            invalidate?.Invoke(
                nguiScroll,
                null);


            MethodInfo updateScrollbars =
                type.GetMethod(
                    "UpdateScrollbars",
                    flags,
                    null,
                    new Type[]
                    {
                    typeof(bool)
                    },
                    null);

            updateScrollbars?.Invoke(
                nguiScroll,
                new object[]
                {
                true
                });


            
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.Log(
                "[cHub] [GFXSCROLL] INVALIDATE FAILED // " +
                ex.GetType().Name +
                ": " +
                ex.Message);
        }
    }
    private void RefreshGfxPresetFilter()
    {
        MigrateLegacyGfxPresetIfNeeded();

        EnsureGfxCustomPresetBindings();


        string search =
            (_gfxPresetSearch ?? string.Empty)
            .Trim();


        int visibleIndex =
            0;


        //
        // =========================================================
        // BUILT-IN PRESETS
        // =========================================================
        //

        string[][] builtInPresets =
        {
        new[]
        {
            "btnGfxPresetMaxFps",
            "max fps performance boost 350 gpt"
        },

        new[]
        {
            "btnGfxPresetMaxQuality",
            "max quality graphics gpt"
        },

        new[]
        {
            "btnGfxPresetUltra",
            "ultra graphics quality sharp"
        },

        new[]
        {
            "btnGfxPresetCinema",
            "cinema reality cinematic realistic extreme"
        }
    };


        foreach (string[] preset in builtInPresets)
        {
            XUiController control =
                FindDescendant<XUiController>(
                    this,
                    preset[0]);

            if (control?.ViewComponent == null)
                continue;


            bool visible =
                string.IsNullOrWhiteSpace(search) ||
                preset[1].IndexOf(
                    search,
                    StringComparison.OrdinalIgnoreCase) >= 0;


            control.ViewComponent.IsVisible =
                visible;


            if (!visible)
                continue;


            int y =
                -(visibleIndex * 52);


            control.ViewComponent.Position =
                new Vector2i(
                    0,
                    y);


            if (control.ViewComponent.UiTransform != null)
            {
                Vector3 local =
                    control.ViewComponent.UiTransform.localPosition;

                local.x =
                    0f;

                local.y =
                    y;

                control.ViewComponent.UiTransform.localPosition =
                    local;
            }


            visibleIndex++;
        }


        //
        // =========================================================
        // CUSTOM CLIENT PRESETS
        // =========================================================
        //

        int selectedCustomSlot =
            GetSelectedGfxCustomPresetSlot();


        for (int slot = 1;
             slot <= GfxCustomPresetSlots;
             slot++)
        {
            string id =
                slot == 1
                    ? "btnGfxPresetClient"
                    : "btnGfxPresetClient" + slot;


            XUiC_SimpleButton button =
                FindDescendant<XUiC_SimpleButton>(
                    this,
                    id);


            if (button == null)
                continue;


            if (!TryGetGfxCustomPreset(
                slot,
                out string presetName,
                out string presetData))
            {
                if (button.ViewComponent != null)
                {
                    button.ViewComponent.IsVisible =
                        false;
                }

                continue;
            }


            bool selected =
                selectedCustomSlot == slot;


            button.Text =
                (selected
                    ? "★  "
                    : "☆  ") +
                presetName;


            string searchData =
                presetName +
                " client custom saved preset slot " +
                slot;


            bool visible =
                string.IsNullOrWhiteSpace(search) ||
                searchData.IndexOf(
                    search,
                    StringComparison.OrdinalIgnoreCase) >= 0;


            button.ViewComponent.IsVisible =
                visible;


            if (!visible)
                continue;


            int y =
                -(visibleIndex * 52);


            button.ViewComponent.Position =
                new Vector2i(
                    0,
                    y);


            if (button.ViewComponent.UiTransform != null)
            {
                Vector3 local =
                    button.ViewComponent.UiTransform.localPosition;

                local.x =
                    0f;

                local.y =
                    y;

                button.ViewComponent.UiTransform.localPosition =
                    local;
            }


            visibleIndex++;
        }


        //
        // =========================================================
        // NO RESULTS
        // =========================================================
        //

        XUiController empty =
            FindDescendant<XUiController>(
                this,
                "gfxPresetNoResults");


        if (empty?.ViewComponent != null)
        {
            empty.ViewComponent.IsVisible =
                visibleIndex == 0;
        }


        //
        // =========================================================
        // SCROLL CONTENT SIZE
        // =========================================================
        //

        _gfxPresetVisibleCount =
            visibleIndex;


        InvalidateGfxPresetScrollBounds();

        SetAllChildrenDirty();


        DebugGfxPresetScrollState(
            "FILTER");


        UnityEngine.Debug.Log(
            "[cHub] [GFXPRESET] REFRESH // " +
            "visible=" +
            visibleIndex +
            " // search=" +
            search);
    }
    private void LoadSelectedGfxPreset()
    {
        if (_gfxSelectedPreset == "maxfps")
        {
            LoadMaxFpsPreset();
            return;
        }


        if (_gfxSelectedPreset == "maxquality")
        {
            LoadMaxQualityPreset();
            return;
        }


        if (_gfxSelectedPreset == "ultra")
        {
            LoadUltraGraphicsPreset();
            return;
        }


        if (_gfxSelectedPreset == "cinema")
        {
            LoadCinemaRealityPreset();
            return;
        }


        if (_gfxSelectedPreset == "client" ||
            _gfxSelectedPreset.StartsWith(
                "client:",
                StringComparison.OrdinalIgnoreCase))
        {
            LoadGfxCustom();
            return;
        }


        _gfxStatus =
            "NO PRESET SELECTED";


        Notify(
            "GFX LOAD • Selectează un preset");
    }

    private void LoadMaxFpsPreset()
    {
        _gfxAf=0; _gfxDynamic=1; _gfxDt=0; _gfxDti=1; _gfxLod=0; _gfxPixel=200; _gfxView=6;
        _gfxAo=false; _gfxBloom=false; _gfxExposure=false; _gfxColor=false;
        _gfxSsao=false; _gfxSunshafts=false; _gfxDof=false; _gfxMotionBlur=false;
        _gfxSkin=1; _gfxStream=1; _gfxBias=2; _gfxLimit=2; _gfxResolutionIndex=0;
        SetGamePrefByName("OptionsGfxAA", 0);
        SetGamePrefByName("OptionsGfxGrassDistance", 0);
        SetGamePrefByName("OptionsGfxObjQuality", 0);
        SetGamePrefByName("OptionsGfxTerrainQuality", 0);
        SetGamePrefByName("OptionsGfxTreeDistance", 0);
        SetGamePrefByName("OptionsGfxReflectQuality", 0);
        SetGamePrefByName("OptionsGfxSSReflections", 0);
        SetGamePrefByName("OptionsGfxShadowQuality", 0);
        ApplyVanillaGraphics();
        ApplyAllGfx("MAX FPS");
    }

    private void LoadMaxQualityPreset()
    {
        _gfxAf=2; _gfxDynamic=0; _gfxDt=1; _gfxDti=1; _gfxLod=5; _gfxPixel=1; _gfxView=20;
        _gfxAo=true; _gfxBloom=true; _gfxExposure=true; _gfxColor=true;
        _gfxSsao=true; _gfxSunshafts=true; _gfxDof=true; _gfxMotionBlur=true;
        _gfxSkin=5; _gfxStream=1; _gfxBias=-2; _gfxLimit=0;
        ApplyAllGfx("MAX QUALITY");
    }

    private void LoadUltraGraphicsPreset()
    {
        _gfxAf=2; _gfxDynamic=0; _gfxDt=1; _gfxDti=1; _gfxLod=5; _gfxPixel=1; _gfxView=20;
        _gfxAo=true; _gfxBloom=true; _gfxExposure=true; _gfxColor=true;
        _gfxSsao=true; _gfxSunshafts=true; _gfxDof=false; _gfxMotionBlur=false;
        _gfxSkin=5; _gfxStream=1; _gfxBias=-4; _gfxLimit=0;
        ApplyAdvancedQuality(false);
        ApplyAllGfx("ULTRA GRAPHICS");
    }

    private void LoadCinemaRealityPreset()
    {
        _gfxAf=2; _gfxDynamic=0; _gfxDt=1; _gfxDti=1; _gfxLod=5; _gfxPixel=1; _gfxView=20;
        _gfxAo=true; _gfxBloom=true; _gfxExposure=true; _gfxColor=true;
        _gfxSsao=true; _gfxSunshafts=true; _gfxDof=true; _gfxMotionBlur=true;
        _gfxSkin=5; _gfxStream=0; _gfxBias=-6; _gfxLimit=0;
        ApplyAdvancedQuality(true);
        ApplyAllGfx("CINEMA - REALITY");
    }

    private void ApplyAdvancedQuality(bool cinematic)
    {
        _gfxAdvAa=3; _gfxAdvGrass=4; _gfxAdvObjects=4; _gfxAdvReflections=3; _gfxAdvShadows=3;
        SetGamePrefByName("OptionsGfxAA", _gfxAdvAa);
        SetGamePrefFloatByName("OptionsGfxAASharpness", cinematic ? 0.25f : 0.15f);
        SetGamePrefByName("OptionsGfxGrassDistance", 4);
        SetGamePrefByName("OptionsGfxObjQuality", 4);
        SetGamePrefByName("OptionsGfxTerrainQuality", 4);
        SetGamePrefByName("OptionsGfxTreeDistance", 4);
        SetGamePrefByName("OptionsGfxReflectQuality", 3);
        SetGamePrefByName("OptionsGfxSSReflections", 3);
        SetGamePrefByName("OptionsGfxShadowQuality", 3);
        SetGamePrefByName("OptionsGfxShadowDistance", 3);
        SetGamePrefByName("OptionsGfxTexQuality", 0);
        ApplyVanillaGraphics();
    }

    private void ApplyAdvancedControl(string pref, int value, string label)
    {
        CaptureDevGraphicsValue(pref);
        SetGamePrefByName(pref, value);
        ApplyVanillaGraphics();
        _gfxStatus = label + " " + value + " - APPLIED LOCALLY";
        Notify(_gfxStatus);
        SetAllChildrenDirty();
        AuditIntPref(pref, value, label);
    }

    private void CaptureDevGraphicsValue(string name)
    {
        if (_devOriginalIntPrefs.ContainsKey(name) || _devOriginalBoolPrefs.ContainsKey(name) || _devOriginalFloatPrefs.ContainsKey(name)) return;
        EnumGamePrefs pref; if (!Enum.TryParse(name, out pref)) return;
        bool boolPref = name == "OptionsGfxOcclusion" || name == "OptionsGfxReflectShadows" ||
            name == "OptionsGfxStreamMipmaps";
        bool floatPref = name == "OptionsGfxWaterPtlLimiter" || name == "OptionsGfxLODDistance";
        try
        {
            if (boolPref) _devOriginalBoolPrefs[name] = GamePrefs.GetBool(pref);
            else if (floatPref) _devOriginalFloatPrefs[name] = GamePrefs.GetFloat(pref);
            else _devOriginalIntPrefs[name] = GamePrefs.GetInt(pref);
        }
        catch (Exception ex) { StartupTerminal.Trace("GFX", "DEV_CAPTURE_FAILED", name + " " + ex.Message); }
    }

    private bool RestoreDevGraphicsSnapshot()
    {
        bool changed = _devOriginalIntPrefs.Count > 0 || _devOriginalBoolPrefs.Count > 0 || _devOriginalFloatPrefs.Count > 0;
        foreach (var item in _devOriginalIntPrefs) { EnumGamePrefs p; if (Enum.TryParse(item.Key, out p)) GamePrefs.Set(p, item.Value); }
        foreach (var item in _devOriginalBoolPrefs) { EnumGamePrefs p; if (Enum.TryParse(item.Key, out p)) GamePrefs.Set(p, item.Value); }
        foreach (var item in _devOriginalFloatPrefs) { EnumGamePrefs p; if (Enum.TryParse(item.Key, out p)) GamePrefs.Set(p, item.Value); }
        _devOriginalIntPrefs.Clear(); _devOriginalBoolPrefs.Clear(); _devOriginalFloatPrefs.Clear();
        return changed;
    }

    private void CycleDev(ref int value, int min, int max, string pref, string label)
    {
        value = value >= max ? min : value + 1;
        ApplyAdvancedControl(pref, value, label);
    }

    private void ToggleDevBool(ref int value, string pref, string label)
    {
        value = value == 0 ? 1 : 0;
        CaptureDevGraphicsValue(pref);
        SetGamePrefBoolByName(pref, value != 0);
        ApplyVanillaGraphics();
        _gfxStatus = label + " " + OnOff(value) + " - APPLIED LOCALLY";
        Notify(_gfxStatus); SetAllChildrenDirty();
        AuditFloatPref(pref, value, label);
    }

    private static void AuditIntPref(string name, int requested, string label)
    {
        try { EnumGamePrefs p; if (!Enum.TryParse(name, out p)) { StartupTerminal.Audit("DEV_GFX", label, "SKIPPED", "unknown=" + name); return; }
            int effective = GamePrefs.GetInt(p); StartupTerminal.Audit("DEV_GFX", label, effective == requested ? "OK" : "MISMATCH", "pref=" + name + " requested=" + requested + " effective=" + effective + " type=int"); }
        catch (Exception ex) { StartupTerminal.Audit("DEV_GFX", label, "FAILED", name + " " + ex.Message); }
    }

    private static void AuditFloatPref(string name, float requested, string label)
    {
        try { EnumGamePrefs p; if (!Enum.TryParse(name, out p)) { StartupTerminal.Audit("DEV_GFX", label, "SKIPPED", "unknown=" + name); return; }
            float effective = GamePrefs.GetFloat(p); StartupTerminal.Audit("DEV_GFX", label, Mathf.Abs(effective-requested) < 0.001f ? "OK" : "MISMATCH", "pref=" + name + " requested=" + requested.ToString("0.###") + " effective=" + effective.ToString("0.###") + " type=float"); }
        catch (Exception ex) { StartupTerminal.Audit("DEV_GFX", label, "FAILED", name + " " + ex.Message); }
    }

    private void CycleDevLodDistance()
    {
        _gfxDevLod = (_gfxDevLod + 1) % 5;
        float[] values = { 0.5f, 0.75f, 1f, 1.5f, 2f };
        ApplyAdvancedFloatControl("OptionsGfxLODDistance", values[_gfxDevLod], "LOD DISTANCE");
    }

    private void CycleWaterParticleLimiter()
    {
        _gfxDevWaterParticles = (_gfxDevWaterParticles + 1) % 5;
        float value = _gfxDevWaterParticles * 0.25f;
        ApplyAdvancedFloatControl("OptionsGfxWaterPtlLimiter", value, "WATER PARTICLES");
    }

    private void ApplyAdvancedFloatControl(string pref, float value, string label)
    {
        CaptureDevGraphicsValue(pref); SetGamePrefFloatByName(pref, value); ApplyVanillaGraphics();
        _gfxStatus = label + " " + value.ToString("0.00") + " - APPLIED LOCALLY";
        Notify(_gfxStatus); SetAllChildrenDirty();
    }

    private static void SetGamePrefByName(string name, int value)
    {
        try
        {
            EnumGamePrefs pref;
            if (!Enum.TryParse(name, out pref)) return;
            GamePrefs.Set(pref, value);
            GameOptionsManager.OnGamePrefChanged(pref);
        }
        catch (Exception ex) { StartupTerminal.Trace("GFX", "ADVANCED_PREF_SKIPPED", name + " " + ex.Message); }
    }

    private static void SetGamePrefBoolByName(string name, bool value)
    {
        try { EnumGamePrefs pref; if (Enum.TryParse(name, out pref)) { GamePrefs.Set(pref, value); GameOptionsManager.OnGamePrefChanged(pref); } }
        catch (Exception ex) { StartupTerminal.Trace("GFX", "BOOL_PREF_SKIPPED", name + " " + ex.Message); }
    }

    private static void SetGamePrefFloatByName(string name, float value)
    {
        try { EnumGamePrefs pref; if (Enum.TryParse(name, out pref)) { GamePrefs.Set(pref, value); GameOptionsManager.OnGamePrefChanged(pref); } }
        catch (Exception ex) { StartupTerminal.Trace("GFX", "FLOAT_PREF_SKIPPED", name + " " + ex.Message); }
    }

    private static void RepairLegacyGfxPrefTypes()
    {
        if (PlayerPrefs.GetInt("cHub.Gfx.TypeRepair.v2", 0) == 1) return;
        // Previous development builds wrote these four entries with incorrect
        // types. Rewrite safe values before ApplyAllOptions can read them.
        SetGamePrefFloatByName("OptionsGfxLODDistance", 0.75f);
        SetGamePrefFloatByName("OptionsGfxWaterPtlLimiter", 0.75f);
        SetGamePrefFloatByName("OptionsGfxAASharpness", 0f);
        SetGamePrefBoolByName("OptionsGfxOcclusion", true);
        SetGamePrefBoolByName("OptionsGfxReflectShadows", false);
        try { GamePrefs.Instance.Save(); } catch { }
        PlayerPrefs.SetInt("cHub.Gfx.TypeRepair.v2", 1); PlayerPrefs.Save();
    }

    private void SaveGfxCustom()
    {
        MigrateLegacyGfxPresetIfNeeded();


        //
        // =========================================================
        // PRESET NAME
        // =========================================================
        //

        string presetName =
            _gfxPresetNameInput != null
                ? _gfxPresetNameInput.Text
                : _gfxPresetName;


        presetName =
            (presetName ?? string.Empty)
            .Trim();


        if (string.IsNullOrWhiteSpace(presetName))
        {
            presetName =
                "Client preset";
        }


        //
        // =========================================================
        // EXISTING NAME = UPDATE SAME SLOT
        // =========================================================
        //

        int targetSlot =
            -1;


        for (int slot = 1;
             slot <= GfxCustomPresetSlots;
             slot++)
        {
            if (!TryGetGfxCustomPreset(
                slot,
                out string existingName,
                out string existingData))
            {
                continue;
            }


            if (string.Equals(
                existingName,
                presetName,
                StringComparison.OrdinalIgnoreCase))
            {
                targetSlot =
                    slot;

                break;
            }
        }


        //
        // =========================================================
        // NEW NAME = FIRST EMPTY SLOT
        // =========================================================
        //

        if (targetSlot < 0)
        {
            for (int slot = 1;
                 slot <= GfxCustomPresetSlots;
                 slot++)
            {
                string existingData =
                    PlayerPrefs.GetString(
                        GfxPresetSlotKey(slot) + "data",
                        string.Empty);


                if (!string.IsNullOrWhiteSpace(existingData))
                    continue;


                targetSlot =
                    slot;

                break;
            }
        }


        //
        // =========================================================
        // ALL 10 SLOTS USED
        // =========================================================
        //

        if (targetSlot < 0)
        {
            _gfxStatus =
                "CUSTOM PRESET LIMIT REACHED • MAX 10";


            Notify(
                "GFX SAVE • Ai deja 10 preseturi custom");


            StartupTerminal.Audit(
                "GFX_PRESET",
                "SAVE",
                "FAILED",
                "reason=max_slots_reached max=" +
                GfxCustomPresetSlots);


            return;
        }


        //
        // =========================================================
        // SERIALIZE CURRENT GRAPHICS
        // =========================================================
        //

        string data =
            string.Join(
                ";",
                new[]
                {
                _gfxAf.ToString(),
                _gfxDynamic.ToString(),
                _gfxDt.ToString(),
                _gfxDti.ToString(),
                _gfxLod.ToString(),
                _gfxPixel.ToString(),
                _gfxView.ToString(),

                _gfxAo
                    ? "1"
                    : "0",

                _gfxBloom
                    ? "1"
                    : "0",

                _gfxExposure
                    ? "1"
                    : "0",

                _gfxColor
                    ? "1"
                    : "0",

                _gfxSkin.ToString(),
                _gfxStream.ToString(),
                _gfxBias.ToString(),
                _gfxLimit.ToString(),
                _gfxResolutionIndex.ToString(),

                _gfxSsao
                    ? "1"
                    : "0",

                _gfxSunshafts
                    ? "1"
                    : "0",

                _gfxDof
                    ? "1"
                    : "0",

                _gfxMotionBlur
                    ? "1"
                    : "0"
                });


        string key =
            GfxPresetSlotKey(
                targetSlot);


        PlayerPrefs.SetString(
            key + "name",
            presetName);

        PlayerPrefs.SetString(
            key + "data",
            data);

        PlayerPrefs.Save();


        //
        // =========================================================
        // SELECT SAVED PRESET
        // =========================================================
        //

        _gfxPresetName =
            presetName;


        _gfxSelectedPreset =
            "client:" +
            targetSlot;


        if (_gfxPresetNameInput != null)
        {
            _gfxPresetNameInput.Text =
                presetName;
        }


        _gfxStatus =
            "CUSTOM PRESET SAVED • SLOT " +
            targetSlot +
            " • THIS CLIENT ONLY";


        RefreshGfxPresetFilter();


        Notify(
            "GFX SAVE • " +
            presetName +
            " salvat în slot " +
            targetSlot);


        StartupTerminal.Audit(
            "GFX_PRESET",
            "SAVE",
            "OK",
            "slot=" +
            targetSlot +
            " name=" +
            presetName);


        UnityEngine.Debug.Log(
            "[cHub] [GFXPRESET] SAVE // " +
            "slot=" +
            targetSlot +
            " // name=" +
            presetName);


        SetAllChildrenDirty();
    }

    private void LoadGfxCustom()
    {
        MigrateLegacyGfxPresetIfNeeded();


        int slot =
            GetSelectedGfxCustomPresetSlot();


        if (slot < 1)
        {
            _gfxStatus =
                "NO CUSTOM PRESET SELECTED";

            Notify(
                "GFX LOAD • Selectează mai întâi un preset custom");

            return;
        }


        if (!TryGetGfxCustomPreset(
            slot,
            out string presetName,
            out string raw))
        {
            _gfxStatus =
                "CUSTOM PRESET SLOT IS EMPTY";

            Notify(
                "GFX LOAD • Slotul selectat este gol");

            return;
        }


        string[] p =
            raw.Split(';');


        if (p.Length != 20)
        {
            _gfxStatus =
                "INCOMPATIBLE CUSTOM PRESET";

            Notify(
                "GFX LOAD • Preset incompatibil");

            return;
        }


        int[] v =
            new int[20];


        for (int i = 0;
             i < 20;
             i++)
        {
            if (!int.TryParse(
                p[i],
                out v[i]))
            {
                _gfxStatus =
                    "INVALID CUSTOM PRESET DATA";

                Notify(
                    "GFX LOAD • Datele presetului sunt invalide");

                return;
            }
        }


        _gfxAf =
            v[0];

        _gfxDynamic =
            v[1];

        _gfxDt =
            v[2];

        _gfxDti =
            v[3];

        _gfxLod =
            v[4];

        _gfxPixel =
            v[5];

        _gfxView =
            v[6];

        _gfxAo =
            v[7] != 0;

        _gfxBloom =
            v[8] != 0;

        _gfxExposure =
            v[9] != 0;

        _gfxColor =
            v[10] != 0;

        _gfxSkin =
            v[11];

        _gfxStream =
            v[12];

        _gfxBias =
            v[13];

        _gfxLimit =
            v[14];

        _gfxResolutionIndex =
            Mathf.Clamp(
                v[15],
                0,
                Mathf.Max(
                    0,
                    _gfxResolutions.Count - 1));

        _gfxSsao =
            v[16] != 0;

        _gfxSunshafts =
            v[17] != 0;

        _gfxDof =
            v[18] != 0;

        _gfxMotionBlur =
            v[19] != 0;


        _gfxPresetName =
            presetName;


        _gfxSelectedPreset =
            "client:" +
            slot;


        if (_gfxPresetNameInput != null)
        {
            _gfxPresetNameInput.Text =
                presetName;
        }


        ApplyAllGfx(
            presetName);


        _gfxStatus =
            "CUSTOM PRESET LOADED • SLOT " +
            slot +
            " • APPLIED LOCALLY";


        RefreshGfxPresetFilter();


        StartupTerminal.Audit(
            "GFX_PRESET",
            "LOAD",
            "OK",
            "slot=" +
            slot +
            " name=" +
            presetName);


        UnityEngine.Debug.Log(
            "[cHub] [GFXPRESET] LOAD // " +
            "slot=" +
            slot +
            " // name=" +
            presetName);


        SetAllChildrenDirty();
    }

    private void ResetGfxPanel()
    {
        CaptureFullVanillaSnapshot();
        RestoreFullGfxSnapshot(false, false);
        RestoreGfxPanelCommandState(false);
        ApplyAllGfxImmediate("RESET GFX");
        _interfaceScale = 1f;
        PlayerPrefs.SetFloat("cHub.InterfaceScale", _interfaceScale);
        PlayerPrefs.Save();
        ApplyInterfaceScale(false);
        _gfxStatus = "GFX PANEL RESET • Valorile gfxHub au revenit la starea inițială";
        Notify(_gfxStatus);
        StartupTerminal.Audit("RESET", "GFX_PANEL", "OK",
            "taskbarUntouched=true dt=" + _gfxDt + " view=" + _gfxView + " pixel=" + _gfxPixel);
    }

    private void LoadExactVanillaDefaults()
    {
        CaptureFullVanillaSnapshot();
        RestoreFullGfxSnapshot(true, true);
        RestoreGfxPanelCommandState(true);
        ApplyAllGfxImmediate("DEFAULT VANILLA");

        // DEFAULT VANILLA is intentionally the broad reset: it also removes
        // active taskbar overrides so nothing can re-apply over the snapshot.
        _fogOverride = false;
        _ambientOverride = false;
        _shadowOverride = false;
        _frameLimitOverride = false;
        _fpsBoostActive = false;
        _qualityBoostActive = false;
        _fpsBoostSnapshotCaptured = false;
        SkyManager.SetFogDebug(-1f, -1001f, -1001f);
        RenderSettings.fog = _defaultFogEnabled;
        RenderSettings.fogMode = _defaultFogMode;
        RenderSettings.fogDensity = _defaultFogDensity;
        RenderSettings.fogStartDistance = _defaultFogStart;
        RenderSettings.fogEndDistance = _defaultFogEnd;
        RenderSettings.ambientIntensity = _defaultUnityAmbient;
        QualitySettings.shadows = _defaultUnityShadows;
        QualitySettings.shadowDistance = _defaultUnityShadowDistance;
        QualitySettings.antiAliasing = _defaultAntiAliasing;
        Application.targetFrameRate = _defaultTargetFrameRate;
        ShowFpsOverlay = _defaultShowFpsOverlay;

        _gfxStatus = "DEFAULT VANILLA • Snapshotul complet inițial a fost restaurat";
        Notify(_gfxStatus);
        RefreshLiveText();
        SetAllChildrenDirty();
        StartupTerminal.Audit("RESET", "DEFAULT_VANILLA", "OK",
            "dt=" + _gfxDt + " terrainRestored=true screen=" + Screen.width + "x" + Screen.height);
    }

    private void CaptureFullVanillaSnapshot()
    {
        if (_fullVanillaSnapshotCaptured) return;

        _vanillaIntPrefs.Clear();
        _vanillaBoolPrefs.Clear();
        _vanillaFloatPrefs.Clear();
        foreach (string name in new[] { "OptionsGfxAA", "OptionsGfxGrassDistance", "OptionsGfxObjQuality",
            "OptionsGfxTerrainQuality", "OptionsGfxTreeDistance", "OptionsGfxReflectQuality",
            "OptionsGfxShadowQuality", "OptionsGfxShadowDistance", "OptionsGfxSignQuality",
            "OptionsGfxSSReflections", "OptionsGfxTexFilter", "OptionsGfxTexQuality",
            "OptionsGfxUMATexQuality", "OptionsGfxWaterQuality", "OptionsGfxDynamicMinFPS",
            "OptionsGfxLimitFpsInGame", "OptionsGfxVsync", "OptionsGfxUpscalerMode", "OptionsGfxFSRPreset" })
        {
            try { EnumGamePrefs pref; if (Enum.TryParse(name, out pref)) _vanillaIntPrefs[name] = GamePrefs.GetInt(pref); }
            catch (Exception ex) { StartupTerminal.Trace("RESET", "SNAPSHOT_INT_SKIPPED", name + " " + ex.Message); }
        }
        foreach (string name in new[] { "OptionsGfxBloom", "OptionsGfxDOF", "OptionsGfxMotionBlurEnabled",
            "OptionsGfxOcclusion", "OptionsGfxReflectShadows", "OptionsGfxSSAO", "OptionsGfxSunShafts",
            "OptionsGfxStreamMipmaps" })
        {
            try { EnumGamePrefs pref; if (Enum.TryParse(name, out pref)) _vanillaBoolPrefs[name] = GamePrefs.GetBool(pref); }
            catch (Exception ex) { StartupTerminal.Trace("RESET", "SNAPSHOT_BOOL_SKIPPED", name + " " + ex.Message); }
        }
        foreach (string name in new[] { "OptionsGfxLODDistance", "OptionsGfxWaterPtlLimiter", "OptionsGfxAASharpness" })
        {
            try { EnumGamePrefs pref; if (Enum.TryParse(name, out pref)) _vanillaFloatPrefs[name] = GamePrefs.GetFloat(pref); }
            catch (Exception ex) { StartupTerminal.Trace("RESET", "SNAPSHOT_FLOAT_SKIPPED", name + " " + ex.Message); }
        }

        _vanillaGfxAf=_gfxAf; _vanillaGfxDynamic=_gfxDynamic; _vanillaGfxDt=_gfxDt; _vanillaGfxDti=_gfxDti;
        _vanillaGfxLod=_gfxLod; _vanillaGfxPixel=_gfxPixel; _vanillaGfxView=_gfxView;
        _vanillaGfxSkin=_gfxSkin; _vanillaGfxStream=_gfxStream; _vanillaGfxBias=_gfxBias; _vanillaGfxLimit=_gfxLimit;
        _vanillaGfxAo=_gfxAo; _vanillaGfxExposure=_gfxExposure; _vanillaGfxColor=_gfxColor;
        _defaultScreenWidth=Screen.width; _defaultScreenHeight=Screen.height; _defaultFullScreenMode=Screen.fullScreenMode;
        _fullVanillaSnapshotCaptured = true;
        StartupTerminal.Audit("RESET", "VANILLA_SNAPSHOT", "OK",
            "int=" + _vanillaIntPrefs.Count + " bool=" + _vanillaBoolPrefs.Count + " float=" + _vanillaFloatPrefs.Count);
    }

    private void RestoreFullGfxSnapshot(bool useFactoryDefaults, bool restoreScreen)
    {
        foreach (var item in _vanillaIntPrefs) { EnumGamePrefs p; if (Enum.TryParse(item.Key, out p)) { int v=useFactoryDefaults?Convert.ToInt32(GamePrefs.GetDefault(p)):item.Value; GamePrefs.Set(p,v); GameOptionsManager.OnGamePrefChanged(p); } }
        foreach (var item in _vanillaBoolPrefs) { EnumGamePrefs p; if (Enum.TryParse(item.Key, out p)) { bool v=useFactoryDefaults?Convert.ToBoolean(GamePrefs.GetDefault(p)):item.Value; GamePrefs.Set(p,v); GameOptionsManager.OnGamePrefChanged(p); } }
        foreach (var item in _vanillaFloatPrefs) { EnumGamePrefs p; if (Enum.TryParse(item.Key, out p)) { float v=useFactoryDefaults?Convert.ToSingle(GamePrefs.GetDefault(p)):item.Value; GamePrefs.Set(p,v); GameOptionsManager.OnGamePrefChanged(p); } }
        _devOriginalIntPrefs.Clear(); _devOriginalBoolPrefs.Clear(); _devOriginalFloatPrefs.Clear();
        ApplyVanillaGraphics();
        if (restoreScreen && _defaultScreenWidth > 0 && _defaultScreenHeight > 0)
            Screen.SetResolution(_defaultScreenWidth, _defaultScreenHeight, _defaultFullScreenMode);
    }

    private void RestoreGfxPanelCommandState(bool factoryDefaults)
    {
        if (factoryDefaults)
        {
            _gfxAf=1; _gfxDynamic=0; _gfxDt=1; _gfxDti=1; _gfxLod=2; _gfxPixel=40; _gfxView=8;
            _gfxSkin=1; _gfxStream=1; _gfxBias=0; _gfxLimit=0;
            _gfxAo=true; _gfxBloom=true; _gfxExposure=true; _gfxColor=true;
            _gfxSsao=true; _gfxSunshafts=true; _gfxDof=true; _gfxMotionBlur=false;
            return;
        }
        _gfxAf=_vanillaGfxAf; _gfxDynamic=_vanillaGfxDynamic; _gfxDt=_vanillaGfxDt; _gfxDti=_vanillaGfxDti;
        _gfxLod=_vanillaGfxLod; _gfxPixel=_vanillaGfxPixel; _gfxView=_vanillaGfxView;
        _gfxSkin=_vanillaGfxSkin; _gfxStream=_vanillaGfxStream; _gfxBias=_vanillaGfxBias; _gfxLimit=_vanillaGfxLimit;
        _gfxAo=_vanillaGfxAo; _gfxBloom=_vanillaBloom; _gfxExposure=_vanillaGfxExposure; _gfxColor=_vanillaGfxColor;
        _gfxSsao=_vanillaSsao; _gfxSunshafts=_vanillaSunshafts; _gfxDof=_vanillaDof; _gfxMotionBlur=_vanillaMotionBlur;
    }

    private void ApplyAllGfxImmediate(string source)
    {
        _gfxCommandQueue.Clear();
        string[] commands = { "af "+_gfxAf, "dr "+_gfxDynamic+" 0.5 1", "dt "+_gfxDt, "dti "+_gfxDti,
            "dtmaxlod "+_gfxLod, "dtpix "+_gfxPixel, "viewdist "+_gfxView, "pp ao "+(_gfxAo?1:0),
            "pp bloom "+(_gfxBloom?1:0), "pp ae "+(_gfxExposure?1:0), "pp cg "+(_gfxColor?1:0),
            "pp ssao "+(_gfxSsao?1:0), "pp sunshafts "+(_gfxSunshafts?1:0), "pp dof "+(_gfxDof?1:0),
            "pp motionblur "+(_gfxMotionBlur?1:0), "skin "+_gfxSkin, "st budget "+_gfxStream,
            "texbias "+_gfxBias, "texlimit "+_gfxLimit };
        foreach (string command in commands)
        {
            try { ExecuteGfxSilent(command); }
            catch (Exception ex) { StartupTerminal.Audit("RESET", source + " " + command, "FAILED", ex.Message); }
        }
        _gfxVisualDirty = true;
        SetAllChildrenDirty();
    }

    private void ApplyAllGfx(string presetName)
    {
        string[] commands = { "af "+_gfxAf, "dr "+_gfxDynamic+" 0.5 1", "dt "+_gfxDt, "dti "+_gfxDti,
            "dtmaxlod "+_gfxLod, "dtpix "+_gfxPixel, "viewdist "+_gfxView, "pp ao "+(_gfxAo?1:0),
            "pp bloom "+(_gfxBloom?1:0), "pp ae "+(_gfxExposure?1:0), "pp cg "+(_gfxColor?1:0),
            "pp ssao "+(_gfxSsao?1:0), "pp sunshafts "+(_gfxSunshafts?1:0), "pp dof "+(_gfxDof?1:0),
            "pp motionblur "+(_gfxMotionBlur?1:0), "skin "+_gfxSkin, "st budget "+_gfxStream,
            "texbias "+_gfxBias, "texlimit "+_gfxLimit };
        _gfxCommandQueue.Clear();
        foreach (string command in commands) _gfxCommandQueue.Enqueue(command);
        _gfxCommandQueueDelay = 0f;
        _gfxStatus = presetName + " PRESET - APPLYING SMOOTHLY";
        _gfxVisualDirty = true;
        SetAllChildrenDirty();
    }

    private void CaptureVanillaCinematicEffects()
    {
        if (_gfxVanillaCaptured) return;
        try
        {
            _vanillaSsao = GamePrefs.GetBool(EnumGamePrefs.OptionsGfxSSAO);
            _vanillaBloom = GamePrefs.GetBool(EnumGamePrefs.OptionsGfxBloom);
            _vanillaSunshafts = GamePrefs.GetBool(EnumGamePrefs.OptionsGfxSunShafts);
            _vanillaDof = GamePrefs.GetBool(EnumGamePrefs.OptionsGfxDOF);
            _vanillaMotionBlur = GamePrefs.GetBool(EnumGamePrefs.OptionsGfxMotionBlurEnabled);
            _gfxVanillaCaptured = true;
        }
        catch (Exception ex) { StartupTerminal.Trace("GFX", "VANILLA_CAPTURE_FAILED", ex.Message); }
    }

    private void BindMouse(string id, System.Action<int> action)
    {
        XUiC_SimpleButton button = FindDescendant<XUiC_SimpleButton>(this, id);
        if (button == null) return;
        button.OnPressed += (sender, mouseButton) =>
        {
            action(mouseButton);
            SetAllChildrenDirty();
        };
    }

    private T FindDescendant<T>(
        XUiController root,
        string id)
        where T : XUiController
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        if (_controlCache.TryGetValue(
            id,
            out XUiController controller))
        {
            return controller as T;
        }

        return null;
    }
}
