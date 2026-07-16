using System.Reflection;
using Microsoft.Extensions.Logging;
using Vortex.Plugin.Abstractions;

namespace Vortex.Bot.Plugins;

public sealed class PluginLoader(ILogger<PluginLoader> logger)
{
    private readonly ILogger<PluginLoader> _logger = logger;

    public PluginLoadResult Load(string directory)
    {
        _logger.LogLoadingFrom(Path.GetFileName(directory));
        PluginLoadContext? context = null;

        try
        {
            context = new PluginLoadContext(directory);
            var assemblies = context.LoadAssemblies();

            if (assemblies.Count == 0)
            {
                context.Unload();
                return PluginLoadResult.Fail(directory, new InvalidOperationException("No assemblies found"));
            }

            var plugins = context.ResolvePlugins().ToList();
            if (plugins.Count == 0)
            {
                context.Unload();
                return PluginLoadResult.Fail(directory, new InvalidOperationException("No plugin implementations found"));
            }

            var plugin = plugins.First();
            var info = BuildInfo(plugin, directory);

            return PluginLoadResult.Ok(directory, plugin, info, context);
        }
        catch (Exception ex)
        {
            context?.Unload();
            _logger.LogLoadFailed(directory, ex);
            return PluginLoadResult.Fail(directory, ex);
        }
    }

    private static PluginInfo BuildInfo(IPlugin plugin, string directory)
    {
        var metadata = plugin.Metadata;
        return new PluginInfo(
            metadata.Name,
            metadata.Author,
            metadata.Description,
            metadata.Version,
            metadata.LoadOrder,
            directory,
            DateTime.UtcNow);
    }
}
