using System.Collections.Frozen;
using System.Reflection;
using Lagrange.Core;
using Lagrange.Core.Common.Entity;
using Lagrange.Core.Events.EventArgs;
using Lagrange.Core.Message.Entities;
using Microsoft.Extensions.Logging;
using Vortex.Bot.Attributes;
using Vortex.Bot.Utility;

namespace Vortex.Bot.Command;

public sealed class CommandManager
{
    private readonly Dictionary<string, CommandRegistration> _commands = [];
    private readonly ILogger<CommandManager> _logger;

    public CommandManager(ILogger<CommandManager> logger)
    {
        _logger = logger;
    }

    public void AutoRegister(Assembly assembly)
    {
        var commandTypes = assembly.GetTypes()
            .Where(static t => t.IsClass && t.GetCustomAttribute<CommandAttribute>() != null);

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
        var (names, tree) = CommandHelper.Register(type);
        var commandTypeAttr = type.GetCustomAttribute<CommandTypeAttribute>();
        var commandTypes = commandTypeAttr?.CommandType ?? CommandType.Group;

        foreach (var name in names)
        {
            var key = name.ToLowerInvariant();
            if (_commands.TryGetValue(key, out var existing))
            {
                existing.Types |= commandTypes;
                _logger.LogInformation("Extended command '{Command}' to type: {NewType} (now supports: {AllTypes})",
                    name, commandTypes, existing.Types);
            }
            else
            {
                _commands[key] = new CommandRegistration(tree, commandTypes);
                _logger.LogInformation("Registered command '{Command}' with types: {Types}", name, commandTypes);
            }
        }
    }

    public Task<bool> ExecuteGroupAsync(string commandText, BotMessageEvent messageEvent, VortexContext context)
    {
        return ExecuteAsync(commandText, messageEvent, CommandType.Group,
            (@params, evt) => new GroupCommandArgs(context, @params, evt));
    }

    public Task<bool> ExecutePrivateAsync(string commandText, BotMessageEvent messageEvent, VortexContext context)
    {
        return ExecuteAsync(commandText, messageEvent, CommandType.Friend,
            (@params, evt) => new PrivateCommandArgs(context, @params, evt));
    }

    public async Task<bool> ExecuteServerAsync(string commandText, VortexContext context, string executorName = "Console", bool hasServerPermission = true)
    {
        var parameters = CommandUtility.ParseParameters(commandText);
        if (parameters.Count == 0) return false;

        var cmdName = parameters[0].ToLowerInvariant();
        if (!_commands.TryGetValue(cmdName, out var registration))
        {
            _logger.LogDebug("Command not found: {Command}", cmdName);
            return false;
        }

        if (!registration.Types.HasFlag(CommandType.Server))
        {
            _logger.LogDebug("Command '{Command}' does not support Server type", cmdName);
            return false;
        }

        var argsParams = parameters.Skip(1).ToList();
        var args = new ServerCommandArgs(context, argsParams, executorName, hasServerPermission);

        if (await CommandEvents.TriggerCommandExecuting(args, cmdName))
        {
            _logger.LogDebug("Command '{Command}' was intercepted by event", cmdName);
            return true;
        }

        await CommandHelper.ExecuteAsync(registration.Tree, args, cmdName);
        return true;
    }

    private async Task<bool> ExecuteAsync<TArgs>(
        string commandText,
        BotMessageEvent messageEvent,
        CommandType requiredType,
        Func<List<string>, BotMessageEvent, TArgs> argsFactory)
        where TArgs : CommandArgs
    {
        var parameters = CommandUtility.ParseParameters(commandText);
        if (parameters.Count == 0) return false;

        var cmdName = parameters[0].ToLowerInvariant();
        if (!_commands.TryGetValue(cmdName, out var registration))
        {
            _logger.LogDebug("Command not found: {Command}", cmdName);
            return false;
        }

        if (!registration.Types.HasFlag(requiredType))
        {
            _logger.LogDebug("Command '{Command}' does not support {Type} (supports: {Supported})",
                cmdName, requiredType, registration.Types);
            return false;
        }

        var argsParams = parameters.Skip(1).ToList();
        var args = argsFactory(argsParams, messageEvent);

        if (await CommandEvents.TriggerCommandExecuting(args, cmdName))
        {
            _logger.LogDebug("Command '{Command}' was intercepted by event", cmdName);
            return true;
        }

        await CommandHelper.ExecuteAsync(registration.Tree, args, cmdName);
        return true;
    }

    public bool HasCommand(string name, CommandType? commandType = null)
    {
        if (!_commands.TryGetValue(name.ToLowerInvariant(), out var registration))
            return false;

        return commandType == null || registration.Types.HasFlag(commandType.Value);
    }

    public IEnumerable<string> GetAllCommands(CommandType? commandType = null)
    {
        if (commandType == null)
            return _commands.Keys;

        return _commands
            .Where(kv => kv.Value.Types.HasFlag(commandType.Value))
            .Select(kv => kv.Key);
    }

    public CommandType? GetCommandTypes(string name)
    {
        return _commands.TryGetValue(name.ToLowerInvariant(), out var registration)
            ? registration.Types
            : null;
    }

    private sealed class CommandRegistration(Command tree, CommandType types)
    {
        public Command Tree { get; } = tree;
        public CommandType Types { get; set; } = types;
    }
}
