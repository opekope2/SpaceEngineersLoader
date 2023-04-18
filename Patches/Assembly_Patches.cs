using System.Reflection;
using HarmonyLib;

namespace SpaceEngineersLoader.Patches;

[HarmonyPatch]
internal static class Assembly_Patches
{
    private static Assembly? _entryAssembly;

    [HarmonyPatch(typeof(Assembly), nameof(Assembly.GetEntryAssembly))]
    [HarmonyPrefix]
    // ReSharper disable once UnusedMember.Local
    // ReSharper disable once InconsistentNaming
    private static bool GetEntryAssembly_Prefix(ref Assembly __result)
    {
        if (_entryAssembly == null)
        {
            return true;
        }

        __result = _entryAssembly;
        return false;
    }

    public static void SetEntryAssembly(Assembly assembly)
    {
        _entryAssembly = assembly;
    }
}
