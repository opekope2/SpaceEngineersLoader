using HarmonyLib;

namespace SpaceEngineers4Linux.RuntimePatches;

[HarmonyPatch]
public static class System_Environment_Patches
{
    private static string[]? arguments;

    [HarmonyPatch(typeof(Environment), "GetCommandLineArgs")]
    [HarmonyPrefix]
    static bool GetCommandLineArgs_Prefix(ref string[] __result)
    {
        if (arguments == null)
        {
            return true;
        }

        __result = arguments;
        return false;
    }

    public static void Set_CommandLineArgs(string[]? args)
    {
        arguments = args;
    }
}