using System.Reflection;
using System.Runtime.Loader;

namespace Vortex.Bot.Plugins;

public class PluginLoadContext(string pluginDirectory, string contextName) : AssemblyLoadContext(contextName, isCollectible: true)
{
    private readonly AssemblyDependencyResolver _resolver = new AssemblyDependencyResolver(pluginDirectory);
    private readonly List<Assembly> _loadedAssemblies = [];
    private readonly List<IPlugin> _loadedPlugins = [];
    private readonly string _pluginDirectory = pluginDirectory;

    public IReadOnlyList<Assembly> LoadedAssemblies => _loadedAssemblies;
    public IReadOnlyList<IPlugin> LoadedPlugins => _loadedPlugins;
    public string PluginDirectory => _pluginDirectory;
    public string ContextName { get; } = contextName;

    public void LoadAssemblies()
    {
        if (!Directory.Exists(_pluginDirectory))
        {
            Directory.CreateDirectory(_pluginDirectory);
            return;
        }

        var dllFiles = Directory.GetFiles(_pluginDirectory, "*.dll", SearchOption.AllDirectories);

        foreach (var dllPath in dllFiles)
        {
            try
            {
                var assembly = LoadFromAssemblyPath(dllPath);
                if (!_loadedAssemblies.Contains(assembly))
                {
                    _loadedAssemblies.Add(assembly);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PluginLoadContext] Failed to load assembly: {dllPath}, Error: {ex.Message}");
            }
        }
    }

    public List<IPlugin> CreatePluginInstances(IServiceProvider serviceProvider)
    {
        var plugins = new List<IPlugin>();

        foreach (var assembly in _loadedAssemblies)
        {
            try
            {
                var pluginTypes = assembly.GetExportedTypes()
                    .Where(t => typeof(IPlugin).IsAssignableFrom(t)
                                && !t.IsAbstract
                                && !t.IsInterface);

                foreach (var type in pluginTypes)
                {
                    try
                    {
                        IPlugin? plugin = null;

                        if (serviceProvider.GetService(type) is IPlugin fromService)
                        {
                            plugin = fromService;
                        }
                        else
                        {
                            plugin = Activator.CreateInstance(type) as IPlugin;
                        }

                        if (plugin != null)
                        {
                            plugins.Add(plugin);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[PluginLoadContext] Failed to create plugin instance: {type.FullName}, Error: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PluginLoadContext] Failed to scan assembly: {assembly.FullName}, Error: {ex.Message}");
            }
        }

        _loadedPlugins.AddRange(plugins);
        return plugins;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        foreach (var assembly in _loadedAssemblies)
        {
            if (assembly.FullName == assemblyName.FullName)
            {
                return assembly;
            }
        }

        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        return Default.LoadFromAssemblyName(assemblyName);
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        return IntPtr.Zero;
    }

    public void UnloadPlugins()
    {
        foreach (var plugin in _loadedPlugins)
        {
            try
            {
                plugin.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PluginLoadContext] Failed to unload plugin: {plugin.Name}, Error: {ex.Message}");
            }
        }

        _loadedPlugins.Clear();
        _loadedAssemblies.Clear();
    }
}

public class PluginInfo(IPlugin plugin, string directory, PluginLoadContext loadContext)
{
    public IPlugin Plugin { get; } = plugin;
    public string Directory { get; } = directory;
    public PluginLoadContext LoadContext { get; } = loadContext;
    public DateTime LoadTime { get; } = DateTime.Now;
    public bool IsInitialized { get; set; }
}
