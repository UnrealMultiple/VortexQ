using System.Reflection;
using Microsoft.Extensions.Logging;
using Vortex.Bot.Command;
using Vortex.Bot.Configuration;
using Vortex.Bot.Database;

namespace Vortex.Bot.Plugins;

public abstract class PluginBase : IPlugin
{
    private bool _disposed;
    private List<Type>? _configTypes;

    public virtual string Name => GetType().Name;
    public virtual string Author => "Unknown";
    public virtual string Description => string.Empty;
    public virtual Version Version => new(1, 0, 0);
    public virtual int LoadOrder => 100;
    public Assembly Assembly => GetType().Assembly;
    public PluginContext Context { get; set; } = null!;

    protected ILogger Logger => Context.Logger;
    protected VortexContext VortexContext => Context.VortexContext;
    protected CommandManager CommandManager => VortexContext.CommandManager;
    protected IDatabaseService Database => VortexContext.Database;

    public abstract void Initialize();
    public abstract void Shutdown();

    protected void RegisterCommands()
    {
        CommandManager.AutoRegister(Assembly);
        Logger.LogInformation("Plugin [{PluginName}] registered commands from assembly", Name);
    }

    protected void RegisterEventHandlers()
    {
    }

    protected void UnregisterEventHandlers()
    {
    }

    private void LoadConfigs()
    {
        _configTypes = FindConfigTypes(Assembly);

        foreach (var configType in _configTypes)
        {
            try
            {
                var loadMethod = configType.GetMethod("Load", BindingFlags.Public | BindingFlags.Static);
                var fileName = loadMethod?.Invoke(null, null) as string;
                Logger.LogInformation("Plugin [{PluginName}] loaded config: {ConfigName}", Name, fileName ?? configType.Name);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Plugin [{PluginName}] failed to load config: {ConfigName}", Name, configType.Name);
            }
        }
    }

    private void UnloadConfigs()
    {
        if (_configTypes == null) return;

        foreach (var configType in _configTypes)
        {
            try
            {
                var unloadMethod = configType.GetMethod("Unload", BindingFlags.Public | BindingFlags.Static);
                unloadMethod?.Invoke(null, null);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Plugin [{PluginName}] failed to unload config: {ConfigName}", Name, configType.Name);
            }
        }
        _configTypes = null;
    }

    private static List<Type> FindConfigTypes(Assembly assembly)
    {
        var configBaseType = typeof(JsonConfigBase<>);
        var result = new List<Type>();

        foreach (var type in assembly.GetExportedTypes())
        {
            if (type.IsAbstract || type.IsInterface)
                continue;

            var baseType = type.BaseType;
            while (baseType != null)
            {
                if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == configBaseType)
                {
                    result.Add(type);
                    break;
                }
                baseType = baseType.BaseType;
            }
        }

        return result;
    }

    internal void OnInitialize()
    {
        Logger.LogInformation("Initializing plugin [{PluginName}] v{Version} by {Author}", Name, Version, Author);
        Initialize();
        RegisterCommands();
        RegisterEventHandlers();
        LoadConfigs();
        Logger.LogInformation("Plugin [{PluginName}] initialized", Name);
    }

    internal void OnShutdown()
    {
        Logger.LogInformation("Shutting down plugin [{PluginName}]", Name);
        UnregisterEventHandlers();
        UnloadConfigs();
        Shutdown();
        Logger.LogInformation("Plugin [{PluginName}] shut down", Name);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            OnShutdown();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }

    ~PluginBase()
    {
        Dispose();
    }
}
