using System.Reflection;
using System.Text;
using Vortex.Bot.Attributes;

namespace Vortex.Bot.Command;

internal sealed class CommandExecutor : CommandBase
{
    private readonly CommandParser.Parser[] _argParsers;
    private readonly MethodInfo _method;
    private readonly bool _isFlexible;
    private readonly Type _argsType;

    public CommandExecutor(MethodInfo method, string infoPrefix, bool isFlexible = false) : base(method)
    {
        _isFlexible = isFlexible;
        _method = method;

        var param = method.GetParameters();
        var ap = new List<CommandParser.Parser>();
        var sb = new StringBuilder();
        sb.Append(infoPrefix);

        if (param.Length == 0 || !typeof(CommandArgs).IsAssignableFrom(param[0].ParameterType))
            throw new InvalidOperationException($"Method {method.Name} must have a CommandArgs parameter as the first argument");

        _argsType = param[0].ParameterType;

        foreach (var p in param.Skip(1))
        {
            if (!CommandParser.IsSupportedType(p.ParameterType))
                throw new NotSupportedException($"Parameter type {p.ParameterType.Name} is not supported for command method {method.Name}");

            ap.Add(CommandParser.GetParser(p.ParameterType));

            var paramAttr = p.GetCustomAttribute<ParamAttribute>();
            var paramDesc = paramAttr?.Description ?? p.Name ?? "param";
            sb.Append($"<{paramDesc}: {CommandParser.GetFriendlyName(p.ParameterType)}> ");
        }

        _argParsers = [.. ap];
        Info = sb.ToString().TrimEnd();
    }

    public bool SupportsArgsType(Type argsType) => _argsType.IsAssignableFrom(argsType);

    public override async Task<ParseResult> TryParseAsync(CommandArgs args, int current, string commandName)
    {
        if (!_argsType.IsAssignableFrom(args.GetType()))
            return GetResult(int.MaxValue);

        var p = args.Params;
        var n = _argParsers.Length;

        if (_isFlexible)
        {
            if (p.Count < n + current)
                return GetResult(Math.Abs(n + current - p.Count));
        }
        else
        {
            if (p.Count != n + current)
                return GetResult(Math.Abs(n + current - p.Count));
        }

        var a = new object?[n + 1];
        a[0] = args;
        var unmatched = _argParsers.Where((t, i) => !t(p[current + i], out a[i + 1])).Count();

        if (unmatched != 0)
            return GetResult(unmatched);

        var permResult = await CheckPermissionAsync(args);
        if (permResult.Result != PermissionResult.Granted)
        {
            await args.ReplyAsync(permResult.DenyMessage ?? "你没有权限执行此指令。");
            return GetResult(0);
        }

        var result = _method.Invoke(null, a);
        if (result is Task task)
            await task;

        return GetResult(0);
    }
}
