using System.Reflection;

namespace SpaceEngineersLoader;

internal static class AssemblyLoader
{
    private static readonly List<string> Paths = new();
    private static readonly Dictionary<string, Assembly> ManagedAssemblies = new();
    private static readonly string[] AssemblyExtensions = { "", ".dll" };

    public static Assembly? GetOrLoadManaged(string name)
    {
        if (ManagedAssemblies.TryGetValue(name, out var managed))
        {
            return managed;
        }

        Assembly? loaded = LoadManaged(name);
        if (loaded == null)
        {
            return null;
        }

        ManagedAssemblies[name] = loaded;
        return loaded;
    }

    private static Assembly? LoadManaged(string name)
    {
        name = name.Split(',')[0];
        foreach (string path in Paths)
        {
            foreach (var ext in AssemblyExtensions)
            {
                string file = Path.Combine(path, name + ext);
                if (File.Exists(file))
                {
                    return Assembly.LoadFrom(file);
                }
            }
        }

        return null;
    }

    public static void AddPath(string path)
    {
        if (!Paths.Contains(path))
        {
            Paths.Add(path);
        }
    }
}
