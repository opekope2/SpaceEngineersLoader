using System.Reflection;
using System.Runtime.InteropServices;

static class AssemblyLoader
{
    private static readonly List<string> paths = new();
    private static readonly Dictionary<string, Assembly> managedAssemblies = new();
    private static readonly Dictionary<string, IntPtr> nativeAssemblies = new();

    private static readonly Dictionary<string, string> nativeNames = new()
    {
        { "steam_api64", "libsteam_api.so" },
        { "dxgi.dll", "libdxvk_dxgi.so" },
        { "d3d9.dll", "libdxvk_d3d9.so" },
        { "d3d11.dll", "libdxvk_d3d11.so" },
    };

    public static Action<Assembly> OnAssemblyLoaded = a => { };

    public static Assembly? GetOrLoadManaged(string name)
    {
        if (managedAssemblies.ContainsKey(name))
        {
            return managedAssemblies[name];
        }

        Assembly? loaded = LoadManaged(name);
        if (loaded == null)
        {
            return null;
        }

        managedAssemblies[name] = loaded;
        OnAssemblyLoaded(loaded);
        return loaded;
    }

    private static Assembly? LoadManaged(string name)
    {
        name = name.Split(',')[0];
        foreach (string path in paths)
        {
            string file = Path.Combine(path, name + ".dll");
            if (File.Exists(file))
            {
                return Assembly.LoadFile(file);
            }
        }

        return null;
    }

    public static IntPtr LoadNative(string name)
    {
        if (nativeAssemblies.ContainsKey(name))
        {
            return nativeAssemblies[name];
        }

        if (!nativeNames.TryGetValue(name, out name!))
        {
            return IntPtr.Zero;
        }

        foreach (string path in paths)
        {
            string file = Path.Combine(path, name);
            if (File.Exists(file) && NativeLibrary.TryLoad(file, out IntPtr handle))
            {
                return nativeAssemblies[name] = handle;
            }
        }

        return IntPtr.Zero;
    }

    public static void AddPath(string path)
    {
        if (!paths.Contains(path))
        {
            paths.Add(path);
        }
    }
}