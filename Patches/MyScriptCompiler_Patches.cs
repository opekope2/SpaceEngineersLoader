using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using HarmonyLib;
using Microsoft.CodeAnalysis;
using VRage.Scripting;

namespace SpaceEngineersLoader.Patches;

[HarmonyPatch]
internal static class MyScriptCompiler_Patches
{
    // [HarmonyPatch(typeof(MyScriptCompiler), MethodType.Constructor)]
    // [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> MyScriptCompiler_Ctor_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Call && instruction.operand is ConstructorInfo)
            {
                yield return instruction;

                // Call AddReferencedAssemblies after instance if initialized
                yield return new CodeInstruction(OpCodes.Ldarg_0);

                MethodInfo prefix = SymbolExtensions.GetMethodInfo(() => MyScriptCompiler_Ctor_Prefix(null!));
                yield return new CodeInstruction(OpCodes.Call, prefix);
            }
            else
            {
                yield return instruction;
            }
        }
    }

    static void MyScriptCompiler_Ctor_Prefix(MyScriptCompiler @this)
    {
        @this.AddReferencedAssemblies(
            typeof(LinkedList<>).Assembly.Location,
            typeof(Regex).Assembly.Location,
            typeof(Enumerable).Assembly.Location,
            typeof(ConcurrentBag<>).Assembly.Location,
            typeof(System.Timers.Timer).Assembly.Location,
            typeof(TraceEventType).Assembly.Location,
            typeof(INotifyPropertyChanging).Assembly.Location
        );
        //VRage.Scripting.MyScriptWhitelist.MyScriptBlacklistBatch.WhitelistBatch
    }
}
