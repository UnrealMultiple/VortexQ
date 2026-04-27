using System.Reflection;
using Vortex.Bot.Attributes;

namespace Vortex.Bot.Command;

internal abstract class CommandBase
{
    private static readonly string NoPermissionMessage = "你没有权限执行此指令。";

    protected string[] Permissions { get; init; }
    protected string? Info { get; set; }
    public string? HelpText { get; init; }

    public abstract Task<ParseResult> TryParseAsync(CommandArgs args, int current, string commandName);

    public override string? ToString() => Info;

    protected CommandBase(MemberInfo member)
    {
        Permissions = [.. member.GetCustomAttributes<PermissionAttribute>().SelectMany(p => p.Permissions)];
        HelpText = member.GetCustomAttribute<HelpTextAttribute>()?.Description;
    }

    protected CommandBase()
    {
        Permissions = [];
    }

    public bool CanExecute(CommandArgs args) => Permissions.Length == 0 || Permissions.All(args.HasPermission);

    public string[] GetMissingPermissions(CommandArgs args) => [.. Permissions.Where(p => !args.HasPermission(p))];

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
                    DenyMessage = $"{NoPermissionMessage} 缺少权限: {string.Join(", ", missingPermissions)}"
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
                errorMessage = $"{NoPermissionMessage} 缺少权限: {string.Join(", ", missingPermissions)}";
                return false;
            }
        }

        return true;
    }

    protected ParseResult CreateResult(int unmatched) => new(this, unmatched);
}
