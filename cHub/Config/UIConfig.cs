using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cHub.Config
{
    public class UIConfig
    {
        public int HomeTeleportCooldownSeconds { get; set; } = 60;
        public string Theme { get; set; } = "Minimal Survivor";
        public string AccentColor { get; set; } = "#EF3943";
        public bool GlowEnabled { get; set; }
        public bool WaterdropAnimations { get; set; } = true;
        public bool ScanlineEffect { get; set; }
        public float EffectIntensity { get; set; } = 0.22f;
        public float BackgroundOpacity { get; set; } = 0.08f;
        public float BackgroundDensity { get; set; } = 1.1f;
        public string CustomThemeCss { get; set; } = string.Empty;
        public bool KeybindSetupCompleted { get; set; }
        public Dictionary<string, string> Keybinds { get; set; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["OpenMenu"] = "U",
                ["MyHomes"] = string.Empty,
                ["PlayerTeleport"] = string.Empty,
                ["ZombieDirector"] = string.Empty,
                ["Options"] = string.Empty
            };

        public static readonly string[] Themes =
        {
            "Blood Moon", "Ashen Night", "Wasteland Ember", "Crimson Glass",
            "Nomad Red", "Radiated", "Midnight Steel", "Black Ice",
            "Inferno", "Toxic Rain", "Desert Dusk", "Cyber Horde",
            "Old World Terminal", "Vampire", "Minimal Survivor"
        };

        public static readonly string[] ThemeAccentColors =
        {
            "#EF3943","#8D353C","#D86622","#FF3048","#C3483A",
            "#42D968","#547AA5","#37B8E8","#FF5426","#7AC943",
            "#C48755","#D34BE3","#52B77A","#A71954","#A4A8B3"
        };

        public static readonly string[] UniqueColors =
        {
            "#EF3943","#FF5A5F","#D71920","#A00012","#FF7849","#FF9F1C",
            "#FFD166","#F4D35E","#7AE582","#2DD881","#00C853","#36C5F0",
            "#00B8D9","#0084FF","#536DFE","#7C4DFF","#9C5CFF","#C77DFF",
            "#E056FD","#FF4DAD","#FF6B9A","#F8F9FA","#D8DEE9","#A7B0C0",
            "#6C7589","#3D4455","#20242E","#0B0D12","#8B5E3C","#C08457"
        };

        public static readonly string[] ImmersiveColors =
        {
            "#6E0B14","#3B070B","#180304","#4A2511","#6B3F16",
            "#75613B","#33452F","#173827","#234449","#19324A",
            "#2E294E","#42253B","#46302A","#4B4B42","#24201E"
        };
    }
}
