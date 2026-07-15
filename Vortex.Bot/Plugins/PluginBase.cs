using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vortex.Bot.Command;
using Vortex.Bot.Configuration;

namespace Vortex.Bot.Plugins;

public abstract class PluginBase : IPlugin
{
    private bool _disposed;

    public virtual string Name => GetType().Name;
    public virtual string Author => "Unknown";
    public virtual string Description => string.Empty;
    public virtual Version Version => new(1, 0, 0);
    public virtual int LoadOrder => 100;

    protected IServiceProvider Services { get; private set; } = null!;
    protected ILogger Logger { get; private set; } = null!;
    protected string PluginDirectory { get; private set; } = null!;
    protected VortexContext Vortex { get; private set; } = null!;
    public PluginContext Context { get; private set; } = null!;

    public async ValueTask InitializeAsync(IPluginContext context, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        Services = context.Services;
        Logger = context.Logger;
        PluginDirectory = context.PluginDirectory;
        Vortex = context.Vortex;
        Context = new PluginContext(Services, Logger, PluginDirectory, Vortex);

        Logger.LogInitializing(Name, Version, Author);

        await OnInitializeAsync(cancellationToken);
        await AutoRegisterCommandsAsync();
        await AutoLoadConfigsAsync();

        Logger.LogInitialized(Name);
    }

    public async ValueTask ShutdownAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        Logger.LogShuttingDown(Name);
        await OnShutdownAsync(cancellationToken);
        Logger.LogShutDown(Name);
    }

    public virtual void Initialize()
    {
    }

    public virtual void Shutdown()
    {
    }

    protected virtual ValueTask OnInitializeAsync(CancellationToken cancellationToken)
    {
        Initialize();
        return ValueTask.CompletedTask;
    }

    protected virtual ValueTask OnShutdownAsync(CancellationToken cancellationToken)
    {
        Shutdown();
        return ValueTask.CompletedTask;
    }

    protected virtual ValueTask AutoRegisterCommandsAsync()
    {
        var commandManager = Services.GetRequiredService<CommandManager>();
        commandManager.AutoRegister(GetType().Assembly);
        Logger.LogCommandsAutoRegistered(Name);
        return ValueTask.CompletedTask;
    }

    protected virtual ValueTask AutoLoadConfigsAsync()
    {
        var configTypes = ScanConfigTypes(GetType().Assembly);

        foreach (var type in configTypes)
        {
            try
            {
                var loadMethod = type.GetMethod("Load", BindingFlags.Public | BindingFlags.Static);
                var fileName = loadMethod?.Invoke(null, null) as string;
                Logger.LogConfigLoaded(Name, fileName ?? type.Name);
            }
            catch (Exception ex)
            {
                Logger.LogConfigLoadFailed(Name, type.Name, ex);
            }
        }

        return ValueTask.CompletedTask;
    }

    private static IEnumerable<Type> ScanConfigTypes(Assembly assembly)
    {
        foreach (var type in assembly.GetExportedTypes())
        {
            if (type.IsAbstract || type.IsInterface)
                continue;

            var baseType = type.BaseType;
            while (baseType != null)
            {
                if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(JsonConfigBase<>))
                {
                    yield return type;
                    break;
                }
                baseType = baseType.BaseType;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        try
        {
            await ShutdownAsync();
        }
        catch (Exception ex)
        {
            Logger?.LogDisposalError(Name, ex);
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, GetType().Name);
    }
}
