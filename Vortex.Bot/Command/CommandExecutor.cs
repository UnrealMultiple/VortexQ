using System.Reflection;

namespace Vortex.Bot.Command;

internal sealed class CommandExecutor : CommandBase
{
    private readonly MethodInfo _method;
    private readonly Type _argsType;
    private readonly ArgumentBinder _argumentBinder;
    private readonly ExecutorType _executorType;
    private readonly int _minArgs;
    private readonly string _parentPrefix;
    private readonly string _commandName;

    public CommandExecutor(
        MethodInfo method,
        string parentPrefix,
        string commandName,
        ExecutorType executorType = ExecutorType.Normal,
        int minArgs = 0) : base(method)
    {
        _method = method;
        _parentPrefix = parentPrefix;
        _commandName = commandName;
        _executorType = executorType;
        _minArgs = minArgs;

        ParameterInfo[] parameters = method.GetParameters();
        ValidateParameters(parameters);

        _argsType = parameters[0].ParameterType;
        _argumentBinder = new ArgumentBinder(parameters);

        UpdateInfo(commandName);
    }

    public string ParameterInfo => _argumentBinder.ParameterInfo;

    public bool SupportsArgsType(Type argsType) => _argsType.IsAssignableFrom(argsType);

    public void UpdateInfo(string actualCommandName)
    {
        Info = string.IsNullOrEmpty(actualCommandName)
            ? $"{_parentPrefix}{ParameterInfo}"
            : $"{_parentPrefix} {actualCommandName}{ParameterInfo}";
    }

    public override async Task<ParseResult> TryParseAsync(CommandArgs args, int current, string commandName)
    {
        if (current > 0)
            UpdateInfo(args.Params[current - 1]);

        return !SupportsArgsType(args.GetType()) ? CreateResult(int.MaxValue) : await ExecuteAsync(args, current);
    }

    private async Task<ParseResult> ExecuteAsync(CommandArgs args, int current)
    {
        var parameters = args.Params;
        var validation = _argumentBinder.GetValidationRules(current, _executorType, _minArgs);

        if (parameters.Count < validation.MinArgs)
            return CreateResult(validation.MinArgs - parameters.Count);

        if (_executorType == ExecutorType.Normal)
        {
            var expectedCount = _argumentBinder.ParameterInfo.Split('<').Length - 1 + current;
            if (parameters.Count != expectedCount)
                return CreateResult(Math.Abs(expectedCount - parameters.Count));
        }

        var parsedArgs = _argumentBinder.ParseArguments(parameters, current);
        return parsedArgs == null ? CreateResult(1) : await InvokeMethodAsync(args, parsedArgs);
    }

    private async Task<ParseResult> InvokeMethodAsync(CommandArgs args, object?[] invokeArgs)
    {
        invokeArgs[0] = args;

        var permResult = await CheckPermissionAsync(args);
        if (permResult.Result != PermissionResult.Granted)
        {
            await args.ReplyWithAtAsync(permResult.DenyMessage ?? "你没有权限执行此指令。");
            return CreateResult(0);
        }

        var result = _method.Invoke(null, invokeArgs);
        if (result is Task task)
            await task;

        return CreateResult(0);
    }

    private static void ValidateParameters(ParameterInfo[] parameters)
    {
        if (parameters.Length == 0 || !typeof(CommandArgs).IsAssignableFrom(parameters[0].ParameterType))
        {
            throw new InvalidOperationException(
                $"Method must have a CommandArgs parameter as the first argument");
        }
    }
}
