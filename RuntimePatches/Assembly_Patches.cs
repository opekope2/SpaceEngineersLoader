using System.Reflection;
using HarmonyLib;

namespace SpaceEngineers4Linux.RuntimePatches;

[HarmonyPatch]
public static class Assembly_Patches
{
    private static Assembly? entryAssembly;

    [HarmonyPatch(typeof(Assembly), "GetEntryAssembly")]
    [HarmonyPrefix]
    public static bool GetEntryAssembly_Prefix(ref Assembly __result)
    {
        if (entryAssembly == null)
        {
            return true;
        }

        __result = entryAssembly;
        return false;
    }

    public static void SetEntryAssembly(Assembly assembly)
    {
        entryAssembly = assembly;
    }
}