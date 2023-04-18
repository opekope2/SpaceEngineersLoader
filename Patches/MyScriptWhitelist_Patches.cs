using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Policy;
using System.Text.RegularExpressions;
using HarmonyLib;
using Microsoft.CodeAnalysis;
using VRage.FileSystem;
using VRage.Scripting;

namespace SpaceEngineersLoader.Patches;

[HarmonyPatch]
internal static class MyScriptWhitelist_Patches
{
    // https://github.com/PveTeam/Torch/blob/181e9297a1b630510b21ae2b57ba3009983499e4/Torch/Patches/ScriptCompilerPatch.cs#L46
    [HarmonyPrefix, HarmonyPatch(typeof(MyScriptWhitelist), MethodType.Constructor, typeof(MyScriptCompiler))]
    private static void Ctor_Prefix(MyScriptCompiler scriptCompiler)
    {
        var baseDir = new FileInfo(typeof(Type).Assembly.Location).DirectoryName!;

        scriptCompiler.AddReferencedAssemblies(
            typeof(Type).Assembly.Location,
            typeof(LinkedList<>).Assembly.Location,
            typeof(Regex).Assembly.Location,
            typeof(Enumerable).Assembly.Location,
            typeof(ConcurrentBag<>).Assembly.Location,
            typeof(ImmutableArray).Assembly.Location,
            typeof(PropertyChangedEventArgs).Assembly.Location,
            typeof(TypeConverter).Assembly.Location,
            typeof(TraceSource).Assembly.Location,
            typeof(Evidence).Assembly.Location,
            Path.Combine(baseDir, "System.Xml.ReaderWriter.dll"),
            Path.Combine(baseDir, "netstandard.dll"),
            Path.Combine(baseDir, "System.Runtime.dll"),
            Path.Combine(MyFileSystem.ExePath, "ProtoBuf.Net.Core.dll"),
            Path.Combine(MyFileSystem.ExePath, "Sandbox.Game.dll"),
            Path.Combine(MyFileSystem.ExePath, "Sandbox.Common.dll"),
            Path.Combine(MyFileSystem.ExePath, "Sandbox.Graphics.dll"),
            Path.Combine(MyFileSystem.ExePath, "VRage.dll"),
            Path.Combine(MyFileSystem.ExePath, "VRage.Library.dll"),
            Path.Combine(MyFileSystem.ExePath, "VRage.Math.dll"),
            Path.Combine(MyFileSystem.ExePath, "VRage.Game.dll"),
            Path.Combine(MyFileSystem.ExePath, "VRage.Render.dll"),
            Path.Combine(MyFileSystem.ExePath, "VRage.Input.dll"),
            Path.Combine(MyFileSystem.ExePath, "SpaceEngineers.ObjectBuilders.dll"),
            Path.Combine(MyFileSystem.ExePath, "SpaceEngineers.Game.dll"));
    }

    [HarmonyPostfix, HarmonyPatch(typeof(MyScriptWhitelist), MethodType.Constructor, typeof(MyScriptCompiler))]
    // ReSharper disable once InconsistentNaming
    private static void Ctor_Postfix(MyScriptWhitelist __instance)
    {
        using var batch = __instance.OpenBatch();
        batch.AllowTypes(MyWhitelistTarget.ModApi, typeof(ConcurrentQueue<>));
    }

    private static IEnumerable<CodeInstruction> Register_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return instructions.Select(instruction => instruction switch
        {
            _ when instruction.opcode == OpCodes.Newobj &&
                   instruction.operand is ConstructorInfo { DeclaringType.Name: nameof(MyWhitelistException) } =>
                new CodeInstruction(OpCodes.Pop),
            _ when instruction.opcode == OpCodes.Throw => new CodeInstruction(OpCodes.Ret),
            _ => instruction
        });
    }

    [HarmonyTranspiler, HarmonyPatch(typeof(MyScriptWhitelist), "Register",
         typeof(MyWhitelistTarget), typeof(INamespaceSymbol), typeof(Type))]
    private static IEnumerable<CodeInstruction> Register_Transpiler_1(IEnumerable<CodeInstruction> instructions)
    {
        return Register_Transpiler(instructions);
    }

    [HarmonyTranspiler, HarmonyPatch(typeof(MyScriptWhitelist), "Register",
         typeof(MyWhitelistTarget), typeof(ITypeSymbol), typeof(Type))]
    private static IEnumerable<CodeInstruction> Register_Transpiler_2(IEnumerable<CodeInstruction> instructions)
    {
        return Register_Transpiler(instructions);
    }
}
