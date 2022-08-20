using System.Reflection;
using System.Runtime.InteropServices;
using HarmonyLib;

class Loader
{
    [STAThread]
    static void Main(string[] args)
    {
        try
        {
            Load(args);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Console.ReadLine();
        }
    }

    static void Load(string[] args)
    {
        System.Windows.Forms.MessageBox.Show($"PID: {Environment.ProcessId}", "Space Engineer Loader");

        Console.WriteLine("Initializing runtime...");

        string gameFile = args[0];
        string bin64 = Path.GetDirectoryName(gameFile)!;

        string loaderPath = Path.GetDirectoryName(typeof(Loader).Assembly.Location)!;
        string nativePath = Path.Combine(loaderPath, "..", "Native");

        Environment.CurrentDirectory = bin64;

        AssemblyLoader.AddPath(bin64);
        AssemblyLoader.AddPath("C:/Windows/Microsoft.NET/Framework64/v4.0.30319/");
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

        Assembly se = Assembly.LoadFile(args[0]);
        Action<string[]> main = (Action<string[]>)Delegate.CreateDelegate(typeof(Action<string[]>), se.EntryPoint!);

        string[] mainArgs = new string[args.Length - 1];
        Array.Copy(args, 1, mainArgs, 0, mainArgs.Length);

        Console.WriteLine("Applying patches...");

        global::Patches.LoadAssembliesToPatch();
        global::Patches.SetEntryAssembly(se);
        global::Patches.Set_CommandLineArgs(args);

        Harmony harmony = new Harmony("opekope2.spaceengineersloader");
        harmony.PatchAll();

        Console.WriteLine("Starting game...");

        main(mainArgs);
    }
}
