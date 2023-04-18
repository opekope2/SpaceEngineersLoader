using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using SpaceEngineersLoader.Patches;

namespace SpaceEngineersLoader;

internal static class Loader
{
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            MessageBox.Show("PID: " + Environment.ProcessId, "Space Engineer Loader");
            Prepare();
            Run(args);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Console.WriteLine("Press enter to exit");
            Console.ReadLine();
            Environment.Exit(0);
        }
    }

    private static void Prepare()
    {
        Console.WriteLine("Initializing runtime...");

        AssemblyLoader.AddPath(typeof(Loader).Assembly.Location);
        AssemblyLoader.AddPath(Environment.CurrentDirectory);

        AppDomain.CurrentDomain.AssemblyResolve += (_, e) => AssemblyLoader.GetOrLoadManaged(e.Name);

        Console.WriteLine("Applying patches...");

        Assembly_Patches.SetEntryAssembly(AssemblyLoader.GetOrLoadManaged("SpaceEngineers.exe")!);
        AssemblyLoader.GetOrLoadManaged("VRage.Scripting");

        Harmony harmony = new Harmony("opekope2.spaceengineersloader");
        harmony.PatchAll();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Run(string[] args)
    {
        Console.WriteLine("Starting game...");

        string file = Environment.GetEnvironmentVariable("SELDR_EXE") ?? "SpaceEngineers.exe";
        Assembly? se = AssemblyLoader.GetOrLoadManaged(file);
        if (se == null)
        {
            throw new FileNotFoundException("Cannot find entry point", file);
        }

        Action<string[]> main = (Action<string[]>)Delegate.CreateDelegate(typeof(Action<string[]>), se.EntryPoint!);

        main(args);
    }
}
