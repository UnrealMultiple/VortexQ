using System.Reflection;
using Vortex.Bot.Attributes;

namespace Vortex.Bot.Command;

internal abstract class CommandBase
{
    protected internal readonly struct ParseResult(CommandBase current, int num)
    {
        public readonly int Unmatched = num;
        public readonly CommandBase Current = current;
    }

    private static string NoPerm => "你没有权限执行此指令。";

    protected string[] Permissions = [];
    protected string? Info;

    public abstract Task<ParseResult> TryParseAsync(CommandArgs args, int current, string commandName);

    public override string? ToString() => Info;

    protected CommandBase(MemberInfo member)
    {
        Permissions = member.GetCustomAttributes<PermissionAttribute>()
            .SelectMany(p => p.Permissions)
            .ToArray();
    }

    protected CommandBase()
    {
        Permissions = [];
    }

    public bool CanExec(CommandArgs args)
    {
        if (Permissions.Length == 0)
            return true;

        return Permissions.All(args.HasPermission);
    }

    public string[] GetMissingPermissions(CommandArgs args)
    {
        return [.. Permissions.Where(p => !args.HasPermission(p))];
    }

    protected async Task<PermissionCheckResult> CheckPermissionAsync(CommandArgs args)
    {
        var result = await CommandEvents.TriggerPermissionChecking(args, Permissions);

        if (result.Result != PermissionResult.Default)
            return result;

        if (Permissions.Length > 0)
        {
            var missingPermissions = GetMissingPermissions(args);
            if (missingPermissions.Length > 0)
            {
                return new PermissionCheckResult
                {
                    Result = PermissionResult.Denied,
                    DenyMessage = $"{NoPerm} 缺少权限: {string.Join(", ", missingPermissions)}"
                };
            }
        }

        return new PermissionCheckResult { Result = PermissionResult.Granted };
    }

    protected bool CheckPermission(CommandArgs args, out string? errorMessage)
    {
        errorMessage = null;

        if (Permissions.Length > 0)
        {
            var missingPermissions = GetMissingPermissions(args);
            if (missingPermissions.Length > 0)
            {
                errorMessage = $"{NoPerm} 缺少权限: {string.Join(", ", missingPermissions)}";
                return false;
            }
        }

        return true;
    }

    protected ParseResult GetResult(int num) => new(this, num);
}
