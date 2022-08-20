using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using HarmonyLib;
using Microsoft.CodeAnalysis;
using Sandbox.Definitions;
using VRage.Game;
using VRage.Scripting;

[HarmonyPatch]
static class Patches
{
    private static string[]? arguments;
    private static Assembly? entryAssembly;

    [HarmonyPatch(typeof(Environment), nameof(Environment.GetCommandLineArgs))]
    [HarmonyPrefix]
    static bool Environment_GetCommandLineArgs_Prefix(ref string[] __result)
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

    [HarmonyPatch(typeof(Assembly), nameof(Assembly.GetEntryAssembly))]
    [HarmonyPrefix]
    static bool Assembly_GetEntryAssembly_Prefix(ref Assembly __result)
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

    [HarmonyPatch(typeof(MyScriptCompiler), MethodType.Constructor)]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> MyScriptCompiler_Ctor_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Call && instruction.operand is ConstructorInfo)
            {
                yield return instruction;

                // Call AddReferencedAssemblies after instance if initialized
                yield return new CodeInstruction(OpCodes.Ldarg_0, null);

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
    }

    [HarmonyPatch(typeof(MyScriptWhitelist), MethodType.Constructor, new[] { typeof(MyScriptCompiler) })]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> MyScriptWhitelist_Ctor_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        bool alert = false;
        bool removing = false;

        foreach (var instruction in instructions)
        {
            if (instruction.LoadsConstant("System.RuntimeType"))
            {
                alert = true;
                yield return instruction;
            }
            else if (alert && instruction.LoadsConstant(2))
            {
                yield return new CodeInstruction(OpCodes.Ldc_I4_1);
            }
            else if (alert && instruction.opcode == OpCodes.Ldloc_1)
            {
                if (removing)
                {
                    removing = false;
                    alert = false;

                    yield return instruction;
                }
                else
                {
                    removing = true;
                }
            }
            else if (!removing)
            {
                yield return instruction;
            }
        }
    }

    [HarmonyPatch(typeof(MyScriptWhitelist), "Register", new[] { typeof(MyWhitelistTarget), typeof(INamespaceSymbol), typeof(Type) })]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> MyScriptWhitelist_Register_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        bool remove = false;

        foreach (var instruction in instructions)
        {
            if (instruction.Branches(out var _))
            {
                yield return instruction;
                yield return new CodeInstruction(OpCodes.Ret);

                remove = true;
            }
            else if (instruction.opcode == OpCodes.Throw)
            {
                remove = false;
            }
            else if (!remove)
            {
                yield return instruction;
            }
        }
    }

    public static void LoadAssembliesToPatch()
    {
        Type _ = typeof(MyScriptCompiler);
    }
}