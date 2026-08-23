using System;
using System.Reflection;

public class XUiC_cHubCompassWindow : XUiC_CompassWindow
{
    private float _refresh;
    private float _smoothedFps;

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        float instantaneous = deltaTime > 0.0001f ? 1f / deltaTime : 0f;
        _smoothedFps = _smoothedFps <= 0f ? instantaneous :
            UnityEngine.Mathf.Lerp(_smoothedFps, instantaneous,
                1f - UnityEngine.Mathf.Exp(-deltaTime * 5f));
        cHub.Core.StartupTerminal.RecordFps(_smoothedFps);
        _refresh += deltaTime;
        if (_refresh < 0.2f) return;
        _refresh = 0f;
        SetAllChildrenDirty();
    }

    public override bool GetBindingValueInternal(ref string value, string bindingName)
    {
        if (string.Equals(bindingName, "chub_skill_points", StringComparison.OrdinalIgnoreCase))
        {
            value = ReadSkillPoints().ToString();
            return true;
        }
        if (string.Equals(bindingName, "chub_hud_fps", StringComparison.OrdinalIgnoreCase))
        {
            value = UnityEngine.Mathf.RoundToInt(_smoothedFps).ToString();
            return true;
        }
        if (string.Equals(bindingName, "chub_hud_fps_visible", StringComparison.OrdinalIgnoreCase))
        {
            value = XUiC_cHubPauseTools.ShowFpsOverlay.ToString().ToLowerInvariant();
            return true;
        }
        if (string.Equals(bindingName, "chub_hud_fps_color", StringComparison.OrdinalIgnoreCase))
        {
            int fps = UnityEngine.Mathf.RoundToInt(_smoothedFps);
            value = fps < 30 ? "255,55,62,255" : fps < 60 ? "255,245,245,255" :
                    fps < 144 ? "92,245,130,255" : fps < 200 ? "75,225,255,255" :
                    fps < 240 ? "80,155,255,255" : fps < 300 ? "175,100,255,255" :
                    fps < 350 ? "255,75,220,255" : fps < 370 ? "255,55,185,255" :
                    fps < 390 ? "255,35,135,255" : fps < 398 ? "255,65,80,255" :
                    fps < 400 ? "255,110,45,255" : fps < 500 ? "255,150,45,255" :
                    "255,235,85,255";
            return true;
        }
        if (string.Equals(bindingName, "chub_hud_fps_glow", StringComparison.OrdinalIgnoreCase))
        {
            int fps = UnityEngine.Mathf.RoundToInt(_smoothedFps);
            value = fps < 30 ? "255,0,8,100" : fps < 60 ? "255,255,255,65" :
                    fps < 144 ? "0,255,75,105" : fps < 200 ? "0,190,255,125" :
                    fps < 240 ? "30,90,255,145" : fps < 300 ? "130,30,255,160" :
                    fps < 350 ? "255,20,185,175" : fps < 370 ? "255,0,145,185" :
                    fps < 390 ? "255,0,95,195" : fps < 398 ? "255,20,35,205" :
                    fps < 400 ? "255,65,0,212" : fps < 500 ? "255,75,0,220" :
                    "255,215,0,220";
            return true;
        }
        return base.GetBindingValueInternal(ref value, bindingName);
    }

    private int ReadSkillPoints()
    {
        object progression = xui?.playerUI?.entityPlayer?.Progression;
        if (progression == null) return 0;
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public |
                                   BindingFlags.NonPublic | BindingFlags.IgnoreCase;
        try
        {
            Type type = progression.GetType();
            foreach (string name in new[] { "SkillPoints", "skillPoints", "Points", "points" })
            {
                PropertyInfo property = type.GetProperty(name, flags);
                if (property != null) return Convert.ToInt32(property.GetValue(progression, null));
                FieldInfo field = type.GetField(name, flags);
                if (field != null) return Convert.ToInt32(field.GetValue(progression));
            }
        }
        catch { }
        return 0;
    }
}
