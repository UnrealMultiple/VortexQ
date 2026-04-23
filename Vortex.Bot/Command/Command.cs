using System.Reflection;
using Microsoft.Extensions.Logging;
using Vortex.Bot.Attributes;

namespace Vortex.Bot.Command;

internal sealed class Command : CommandBase
{
    private readonly Dictionary<string, List<CommandBase>> _dict = [];
    private readonly List<CommandBase> _main = [];
    private readonly string _infoPrefix;
    private readonly string _name;

    public Command(MemberInfo type, string name, string infoPrefix) : base(type)
    {
        Info = $"{infoPrefix} <...>";
        _infoPrefix = infoPrefix;
        _name = name;
    }

    public void PostBuildTree() => _main.Add(new HelpCommand(this, _infoPrefix));

    public void Add(string? cmd, CommandBase sub)
    {
        if (string.IsNullOrEmpty(cmd))
        {
            _main.Add(sub);
        }
        else if (_dict.TryGetValue(cmd, out var lst))
        {
            lst.Add(sub);
        }
        else
        {
            _dict.Add(cmd, [sub]);
        }
    }

    public override async Task<ParseResult> TryParseAsync(CommandArgs args, int current, string commandName)
    {
        var permResult = await CheckPermissionAsync(args);
        if (permResult.Result != PermissionResult.Granted)
        {
            await args.ReplyAsync(permResult.DenyMessage ?? "你没有权限执行此指令。");
            return GetResult(0);
        }

        var most = GetResult(args.Params.Count - current + 1);

        if (current < args.Params.Count && _dict.TryGetValue(args.Params[current], out var subs))
        {
            foreach (var sub in subs)
            {
                var res = await sub.TryParseAsync(args, current + 1, commandName);
                if (res.Unmatched == 0)
                    return res;

                if (res.Unmatched < most.Unmatched)
                    most = res;
            }
        }

        foreach (var sub in _main)
        {
            var res = await sub.TryParseAsync(args, current, commandName);
            if (res.Unmatched == 0)
                return res;

            if (res.Unmatched < most.Unmatched)
                most = res;
        }

        return most;
    }

    public string GetName() => _name;

    public IEnumerable<CommandBase> GetAllSubCommands() => _dict.Values.SelectMany(subs => subs).Concat(_main).Distinct();

    private sealed class HelpCommand : CommandBase
    {
        private readonly Command _parent;

        public HelpCommand(Command parent, string infoPrefix)
        {
            _parent = parent;
            Permissions = [];
            Info = infoPrefix + "help";
        }

        public override async Task<ParseResult> TryParseAsync(CommandArgs args, int current, string commandName)
        {
            if (current != args.Params.Count - 1)
                return GetResult(Math.Abs(args.Params.Count - 1 - current));

            if (args.Params[current] != "help")
                return GetResult(1);

            var helpText = "可用指令:\n" + string.Join("\n",
                _parent.GetAllSubCommands()
                    .Where(sub => sub.CanExec(args))
                    .Select(sub => "  " + sub.ToString()));
            await args.ReplyAsync(helpText);

            return GetResult(0);
        }
    }
}
