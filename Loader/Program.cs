using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using HarmonyLib;
using SpaceEngineers4Linux.RuntimePatches;

// Good luck finding documentation. I haven't found any. Please notify me if you found one.
if (args[0] != "waitforexitandrun")
{
    return;
}

Process.Start("zenity", $"--info --text \"PID: {Environment.ProcessId}\"").WaitForExit();

Console.WriteLine("Initializing runtime...");

string gameFile = args[1];
string bin64 = Path.GetDirectoryName(gameFile)!;

string loaderPath = Path.GetDirectoryName(typeof(Program).Assembly.Location)!;
string nativePath = Path.Combine(loaderPath, "..", "Native");

Environment.CurrentDirectory = bin64;

AssemblyLoader.AddPath(bin64);
AssemblyLoader.AddPath("/usr/lib/mono/4.5/");
AssemblyLoader.AddPath(nativePath);
AssemblyLoader.OnAssemblyLoaded = asm =>
{
    string name = asm.FullName!;
    if (name.StartsWith("Steamworks.NET") || name.StartsWith("SharpDX"))
    {
        NativeLibrary.SetDllImportResolver(asm,
            (libraryName, assembly, searchPath) => AssemblyLoader.LoadNative(libraryName));
    }
};

AppDomain.CurrentDomain.AssemblyResolve += (o, e) => AssemblyLoader.GetOrLoadManaged(e.Name);

Assembly se = Assembly.LoadFile(args[1]);
Action<string[]> main = (Action<string[]>)Delegate.CreateDelegate(typeof(Action<string[]>), se.EntryPoint!);

string[] cliArgs = new string[args.Length - 1];
string[] mainArgs = new string[args.Length - 2];
Array.Copy(args, 1, cliArgs, 0, cliArgs.Length);
Array.Copy(args, 2, mainArgs, 0, mainArgs.Length);

Console.WriteLine("Applying patches...");

// Workaround for .NET 6, doesn't work on .NEt 7+
// .NET loads .NET 6 System.Drawing, I couldn't find a way to load Mono System.Drawing
AppContext.SetSwitch("System.Drawing.EnableUnixSupport", true);

Assembly_Patches.SetEntryAssembly(se);
System_Environment_Patches.Set_CommandLineArgs(cliArgs);
Ansel.Disable();
VRagePlatformWindows_MyWindowsSystem_Patches.Load_VRagePlatformWindows();
VRageLibrary_MyZipFileProvider_Patches.Load_VRageLibrary();

Harmony harmony = new Harmony("opekope2.spaceengineers4linux");
harmony.PatchAll(typeof(Assembly_Patches).Assembly);

Console.WriteLine("Starting game...");

main(mainArgs);