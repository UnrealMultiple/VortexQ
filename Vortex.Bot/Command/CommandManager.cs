using Lagrange.Core.Events.EventArgs;
using Microsoft.Extensions.Logging;
using System.Reflection;
using Vortex.Bot.Attributes;
using Vortex.Bot.Configuration;
using Vortex.Bot.Extension;
using Vortex.Bot.Utility;
using Vortex.Bot.Plugins;
using Vortex.Plugin.Abstractions;
using Vortex.Protocol.Models;

namespace Vortex.Bot.Command;

public sealed class CommandManager(ILogger<CommandManager> logger, PluginCommandRegistry pluginCommands)
{
    private readonly Dictionary<string, CommandRegistration> _commands = [];
    private readonly ILogger<CommandManager> _logger = logger;
    private readonly PluginCommandRegistry _pluginCommands = pluginCommands;

    public void AutoRegister(Assembly assembly)
    {
        var commandTypes = assembly.GetTypes()
            .Where(static t => t.IsClass && !t.IsNested && t.GetCustomAttributes<CommandAttribute>().Any());

        foreach (var type in commandTypes)
        {
            try
            {
                Register(type);
            }
            catch (Exception ex)
            {
                _logger.LogFailedToRegisterCommand(type.FullName, ex);
            }
        }
    }

    public void Register(Type type)
    {
        (var names, var tree) = CommandHelper.Register(type);
        var commandTypeAttr = type.GetCustomAttribute<CommandTypeAttribute>();
        var commandTypes = commandTypeAttr?.CommandType ?? CommandType.Group;

        foreach (var name in names)
        {
            var key = name.ToLowerInvariant();
            if (_commands.TryGetValue(key, out CommandRegistration? existing))
            {
                existing.Types |= commandTypes;
                _logger.LogExtendedCommand(name, commandTypes, existing.Types);
            }
            else
            {
                _commands[key] = new CommandRegistration(tree, commandTypes, names);
                _logger.LogRegisteredCommand(name, commandTypes);
            }
        }
    }

    public Task<bool> ExecuteGroupAsync(string commandText, BotMessageEvent messageEvent, VortexContext context)
    {
        return ExecuteAsync(commandText, messageEvent, CommandType.Group,
            (@params, evt) => new GroupCommandArgs(context, @params, evt), context);
    }

    public Task<bool> ExecutePrivateAsync(string commandText, BotMessageEvent messageEvent, VortexContext context)
    {
        return ExecuteAsync(commandText, messageEvent, CommandType.Friend,
            (@params, evt) => new PrivateCommandArgs(context, @params, evt), context);
    }

    public async Task<bool> ExecuteServerAsync(string commandText, VortexContext? context, Player player, int sessionId)
    {
        if (context == null) return false;

        var parameters = CommandUtility.ParseParameters(commandText);
        if (parameters.Count == 0) return false;

        if (!TryExtractCommandName(parameters, context.Configuration.Command, out string? cmdName, out List<string>? argsParams))
            return false;

        var args = CreateServerArgs(context, argsParams, player, sessionId, parameters, context.Configuration.Command);

        if (await TriggerCommandExecuting(args, cmdName))
            return true;

        if (TryGetCommandRegistration(cmdName, CommandType.Server, out CommandRegistration? registration))
        {
            await CommandHelper.ExecuteAsync(registration.Tree, args, cmdName);
            return true;
        }

        return await ExecutePluginCommandAsync(cmdName, PluginCommandScope.Server, args);
    }

    private async Task<bool> ExecuteAsync<TArgs>(
        string commandText,
        BotMessageEvent messageEvent,
        CommandType requiredType,
        Func<List<string>, BotMessageEvent, TArgs> argsFactory,
        VortexContext context)
        where TArgs : CommandArgs
    {
        var parameters = CommandUtility.ParseParameters(commandText);
        if (parameters.Count == 0) return false;

        if (!TryExtractCommandName(parameters, context.Configuration.Command, out string? cmdName, out List<string>? argsParams))
            return false;

        var args = argsFactory(argsParams, messageEvent);
        args.CommandName = parameters[0];
        args.CommandPrefix = context.Configuration.Command.EnablePrefix ? context.Configuration.Command.Prefix : string.Empty;

        if (await TriggerCommandExecuting(args, cmdName))
            return true;

        if (TryGetCommandRegistration(cmdName, requiredType, out CommandRegistration? registration))
        {
            await CommandHelper.ExecuteAsync(registration.Tree, args, cmdName);
            return true;
        }

        var scope = requiredType == CommandType.Group ? PluginCommandScope.Group : PluginCommandScope.Friend;
        return await ExecutePluginCommandAsync(cmdName, scope, args);
    }

    public bool HasCommand(string name, CommandType? commandType = null) => _commands.TryGetValue(name.ToLowerInvariant(), out CommandRegistration? registration)
        && (commandType == null || registration.Types.Supports(commandType.Value));

    public IEnumerable<string> GetAllCommands(CommandType? commandType = null) => commandType == null
            ? _commands.Keys
            : _commands
            .Where(kv => kv.Value.Types.Supports(commandType.Value))
            .Select(kv => kv.Key);

    public CommandType? GetCommandTypes(string name) => _commands.TryGetValue(name.ToLowerInvariant(), out CommandRegistration? registration)
            ? registration.Types
            : null;

    public IEnumerable<CommandInfo> GetAllCommandInfos(CommandType? commandType = null, bool includeSubCommands = true)
    {
        var processedTrees = new HashSet<Command>();

        foreach ((var _, var registration) in _commands)
        {
            if (commandType != null && !registration.Types.Supports(commandType.Value))
                continue;

            if (!processedTrees.Add(registration.Tree))
                continue;

            var extractor = new CommandInfoExtractor(registration.Tree, registration.Aliases, includeSubCommands);
            foreach (var info in extractor.Extract())
            {
                yield return info;
            }
        }
    }

    private static bool TryExtractCommandName(
        List<string> parameters,
        CommandConfiguration config,
        out string cmdName,
        out List<string> argsParams)
    {
        cmdName = string.Empty;
        argsParams = [];

        if (config.EnablePrefix && !string.IsNullOrEmpty(config.Prefix))
        {
            if (!parameters[0].StartsWith(config.Prefix, StringComparison.OrdinalIgnoreCase))
                return false;

            parameters[0] = parameters[0][config.Prefix.Length..];
            if (string.IsNullOrEmpty(parameters[0]))
                return false;
        }

        cmdName = parameters[0].ToLowerInvariant();
        argsParams = [.. parameters.Skip(1)];
        return true;
    }

    private bool TryGetCommandRegistration(string cmdName, CommandType requiredType, out CommandRegistration registration)
    {
        registration = null!;

        if (!_commands.TryGetValue(cmdName, out CommandRegistration? reg))
        {
            return false;
        }

        if (!reg.Types.Supports(requiredType))
        {
            _logger.LogCommandTypeNotSupported(cmdName, requiredType, reg.Types);
            return false;
        }

        registration = reg;
        return true;
    }

    private async Task<bool> TriggerCommandExecuting(CommandArgs args, string cmdName)
    {
        if (await CommandEvents.TriggerCommandExecuting(args, cmdName))
        {
            _logger.LogCommandIntercepted(cmdName);
            return true;
        }
        return false;
    }

    private async Task<bool> ExecutePluginCommandAsync(string cmdName, PluginCommandScope scope, CommandArgs args)
    {
        if (!_pluginCommands.TryGet(cmdName, scope, out var command))
        {
            _logger.LogCommandNotFound(cmdName);
            return false;
        }

        if (!string.IsNullOrEmpty(command.RequiredPermission) && !args.HasPermission(command.RequiredPermission))
        {
            await args.ReplyWithAtAsync("你没有权限执行此指令。");
            return true;
        }

        try
        {
            await command.Handler(new PluginCommandContextAdapter(scope, args), CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogPluginCommandFailed(cmdName, ex);
            await args.ReplyWithAtAsync("插件指令执行失败，请查看机器人日志。");
        }

        return true;
    }
        
    private static ServerCommandArgs CreateServerArgs(
        VortexContext context,
        List<string> argsParams,
        Player player,
        int sessionId,
        List<string> parameters,
        CommandConfiguration config)
    {
        return new ServerCommandArgs(context, argsParams, player, sessionId)
        {
            CommandName = parameters[0],
            CommandPrefix = config.EnablePrefix ? config.Prefix : string.Empty
        };
    }
}

internal sealed class PluginCommandContextAdapter(PluginCommandScope scope, CommandArgs args) : IPluginCommandContext
{
    public PluginCommandScope Scope { get; } = scope;
    public string CommandName => args.CommandName;
    public IReadOnlyList<string> Arguments => args.Params;
    public long UserId => args.SenderUin;
    public long GroupId => args is GroupCommandArgs group ? group.GroupUin : 0;
    public string SenderName => args switch
    {
        GroupCommandArgs group => group.SenderDisplayName ?? string.Empty,
        PrivateCommandArgs friend => friend.FriendNickname ?? string.Empty,
        _ => string.Empty
    };
    public string PlayerName => args is ServerCommandArgs server ? server.Player.Name : string.Empty;
    public long BoundUserId => args is ServerCommandArgs server ? server.User?.Id ?? 0 : 0;
    public string ServerName => args is ServerCommandArgs server ? server.Server?.Config.Name ?? string.Empty : string.Empty;
    public bool HasPermission(string permission) => args.HasPermission(permission);
    public Task ReplyAsync(string message) => args.ReplyAsync(message);
    public Task ReplyWithAtAsync(string message) => args.ReplyWithAtAsync(message);
    public Task ReplyImageAsync(byte[] imageData) => args.ReplyImageAsync(imageData);
}

public static partial class CommandManagerLoggerExtension
{
    [LoggerMessage(Microsoft.Extensions.Logging.LogLevel.Error, "Failed to register command class {Type}")]
    public static partial void LogFailedToRegisterCommand(this ILogger<CommandManager> logger, string? type, Exception ex);

    [LoggerMessage(Microsoft.Extensions.Logging.LogLevel.Information, "Extended command '{Command}' to type: {NewType} (now supports: {AllTypes})")]
    public static partial void LogExtendedCommand(this ILogger<CommandManager> logger, string command, CommandType newType, CommandType allTypes);

    [LoggerMessage(Microsoft.Extensions.Logging.LogLevel.Information, "Registered command '{Command}' with types: {Types}")]
    public static partial void LogRegisteredCommand(this ILogger<CommandManager> logger, string command, CommandType types);

    [LoggerMessage(Microsoft.Extensions.Logging.LogLevel.Debug, "Command not found: {Command}")]
    public static partial void LogCommandNotFound(this ILogger<CommandManager> logger, string command);

    [LoggerMessage(Microsoft.Extensions.Logging.LogLevel.Debug, "Command '{Command}' does not support {Type} (supports: {Supported})")]
    public static partial void LogCommandTypeNotSupported(this ILogger<CommandManager> logger, string command, CommandType type, CommandType supported);

    [LoggerMessage(Microsoft.Extensions.Logging.LogLevel.Debug, "Command '{Command}' was intercepted by event")]
    public static partial void LogCommandIntercepted(this ILogger<CommandManager> logger, string command);

    [LoggerMessage(Microsoft.Extensions.Logging.LogLevel.Error, "Plugin command '{Command}' failed")]
    public static partial void LogPluginCommandFailed(this ILogger<CommandManager> logger, string command, Exception ex);
}
