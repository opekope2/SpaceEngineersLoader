using System.Reflection;
using HarmonyLib;

namespace SpaceEngineersLoader.Patches;

[HarmonyPatch]
internal static class MyWhitelistBatch_Patches
{
    [HarmonyPrefix, HarmonyPatch]
    private static void AllowMembers_Prefix(ref MemberInfo?[] members)
    {
        if (members.Any(m => m == null))
        {
            members = members.Where(m => m != null).ToArray();
        }
    }

    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("VRage.Scripting.MyScriptWhitelist+MyWhitelistBatch:AllowMembers");
    }
}
