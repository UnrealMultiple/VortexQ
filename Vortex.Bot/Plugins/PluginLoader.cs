using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Vortex.Bot.Plugins;

public sealed class PluginLoader(IServiceProvider services, ILogger<PluginLoader> logger)
{
    private readonly IServiceProvider _services = services;
    private readonly ILogger<PluginLoader> _logger = logger;

    public PluginLoadResult Load(string directory)
    {
        _logger.LogLoadingFrom(Path.GetFileName(directory));

        try
        {
            var context = new PluginLoadContext(directory);
            var assemblies = context.LoadAssemblies();

            if (assemblies.Count == 0)
                return PluginLoadResult.Fail(directory, new InvalidOperationException("No assemblies found"));

            var plugins = context.ResolvePlugins(_services).ToList();
            if (plugins.Count == 0)
                return PluginLoadResult.Fail(directory, new InvalidOperationException("No plugin implementations found"));

            var plugin = plugins.First();
            var info = BuildInfo(plugin, directory);

            return PluginLoadResult.Ok(directory, plugin, info);
        }
        catch (Exception ex)
        {
            _logger.LogLoadFailed(directory, ex);
            return PluginLoadResult.Fail(directory, ex);
        }
    }

    private static PluginInfo BuildInfo(IPlugin plugin, string directory)
    {
        var attr = plugin.GetType().GetCustomAttribute<PluginAttribute>();

        return attr is not null
            ? new PluginInfo(
                string.IsNullOrEmpty(attr.Name) ? plugin.GetType().Name : attr.Name,
                attr.Author,
                attr.Description,
                attr.Version,
                attr.LoadOrder,
                directory,
                DateTime.UtcNow)
            : new PluginInfo(
                plugin.Name,
                plugin.Author,
                plugin.Description,
                plugin.Version,
                plugin.LoadOrder,
                directory,
                DateTime.UtcNow);
    }
}
