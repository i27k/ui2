using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cHub.Patches.Player
{
    internal class PlayerPatch
    {
    }
}

[HarmonyLib.HarmonyPatch(typeof(Party), "IsFull")]
internal static class cHubPartyMaximumPatch
{
    private static bool Prefix(Party __instance, ref bool __result)
    {
        try
        {
            var field = typeof(Party).GetField("MemberList",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic);
            var members = field?.GetValue(__instance) as System.Collections.ICollection;
            if (members == null) return true;
            __result = members.Count >= cHub.Modules.Party.PartyService.MaximumMembers;
            return false;
        }
        catch { return true; }
    }
}
