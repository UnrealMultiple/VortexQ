using Lagrange.Core.Events.EventArgs;
using Microsoft.Extensions.Logging;
using System.Reflection;
using Vortex.Bot.Attributes;
using Vortex.Bot.Configuration;
using Vortex.Bot.Extension;
using Vortex.Bot.Utility;
using Vortex.Protocol.Models;

namespace Vortex.Bot.Command;

public sealed class CommandManager(ILogger<CommandManager> logger)
{
    private readonly Dictionary<string, CommandRegistration> _commands = [];
    private readonly ILogger<CommandManager> _logger = logger;

    public void AutoRegister(Assembly assembly)
    {
        var commandTypes = assembly.GetTypes()
            .Where(static t => t.IsClass && !t.IsNested && t.GetCustomAttribute<CommandAttribute>() != null);

        foreach (var type in commandTypes)
        {
            try
            {
                Register(type);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register command class {Type}", type.FullName);
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
                _logger.LogInformation("Extended command '{Command}' to type: {NewType} (now supports: {AllTypes})",
                    name, commandTypes, existing.Types);
            }
            else
            {
                _commands[key] = new CommandRegistration(tree, commandTypes, names);
                _logger.LogInformation("Registered command '{Command}' with types: {Types}", name, commandTypes);
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

        if (!TryGetCommandRegistration(cmdName, CommandType.Server, out CommandRegistration? registration))
            return false;

        var args = CreateServerArgs(context, argsParams, player, sessionId, parameters, context.Configuration.Command);

        if (await TriggerCommandExecuting(args, cmdName))
            return true;

        await CommandHelper.ExecuteAsync(registration.Tree, args, cmdName);
        return true;
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

        if (!TryGetCommandRegistration(cmdName, requiredType, out CommandRegistration? registration))
            return false;

        var args = argsFactory(argsParams, messageEvent);
        args.CommandName = parameters[0];
        args.CommandPrefix = context.Configuration.Command.EnablePrefix ? context.Configuration.Command.Prefix : string.Empty;

        if (await TriggerCommandExecuting(args, cmdName))
            return true;

        await CommandHelper.ExecuteAsync(registration.Tree, args, cmdName);
        return true;
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
            _logger.LogDebug("Command not found: {Command}", cmdName);
            return false;
        }

        if (!reg.Types.Supports(requiredType))
        {
            _logger.LogDebug("Command '{Command}' does not support {Type} (supports: {Supported})",
                cmdName, requiredType, reg.Types);
            return false;
        }

        registration = reg;
        return true;
    }

    private async Task<bool> TriggerCommandExecuting(CommandArgs args, string cmdName)
    {
        if (await CommandEvents.TriggerCommandExecuting(args, cmdName))
        {
            _logger.LogDebug("Command '{Command}' was intercepted by event", cmdName);
            return true;
        }
        return false;
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
