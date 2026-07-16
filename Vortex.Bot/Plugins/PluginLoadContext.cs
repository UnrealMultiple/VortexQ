using System.Reflection;
using System.Runtime.Loader;
using Vortex.Plugin.Abstractions;

namespace Vortex.Bot.Plugins;

public sealed class PluginLoadContext(string pluginDirectory) : AssemblyLoadContext(isCollectible: true)
{
    private static readonly HashSet<string> SharedAssemblyNames =
    [
        typeof(IPlugin).Assembly.GetName().Name!,
    ];

    private readonly AssemblyDependencyResolver _resolver = new(pluginDirectory);
    private readonly List<Assembly> _assemblies = [];

    public string PluginDirectory { get; } = pluginDirectory;
    public IReadOnlyList<Assembly> LoadedAssemblies => _assemblies;

    public IReadOnlyList<Assembly> LoadAssemblies()
    {
        if (!Directory.Exists(PluginDirectory))
        {
            Directory.CreateDirectory(PluginDirectory);
            return _assemblies;
        }

        foreach (var dll in Directory.GetFiles(PluginDirectory, "*.dll", SearchOption.AllDirectories))
        {
            try
            {
                if (SharedAssemblyNames.Contains(AssemblyName.GetAssemblyName(dll).Name!))
                    continue;

                var assembly = LoadFromAssemblyPath(dll);
                if (!_assemblies.Contains(assembly))
                    _assemblies.Add(assembly);
            }
            catch { /* ignore unloadable assemblies */ }
        }

        return _assemblies;
    }

    public IReadOnlyList<IPlugin> ResolvePlugins()
    {
        var plugins = new List<IPlugin>();

        foreach (var assembly in _assemblies)
        {
            try
            {
                var types = assembly.GetExportedTypes()
                    .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);

                foreach (var type in types)
                {
                    try
                    {
                        var plugin = Instantiate(type);
                        if (plugin is not null)
                            plugins.Add(plugin);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Plugin] Failed to instantiate {type.FullName}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Plugin] Failed to scan assembly {assembly.FullName}: {ex.Message}");
            }
        }

        return plugins;
    }

    private static IPlugin? Instantiate(Type type)
    {
        return Activator.CreateInstance(type) as IPlugin;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (assemblyName.Name is not null && SharedAssemblyNames.Contains(assemblyName.Name))
            return Default.LoadFromAssemblyName(assemblyName);

        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        return path is not null ? LoadFromAssemblyPath(path) : Default.LoadFromAssemblyName(assemblyName);
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return path is not null ? LoadUnmanagedDllFromPath(path) : IntPtr.Zero;
    }
}
