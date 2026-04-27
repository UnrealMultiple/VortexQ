namespace Vortex.Bot.Command;

public abstract class CommandEventArgs(CommandArgs args, string commandName) : EventArgs
{
    public CommandArgs Args { get; } = args;
    public bool Handled { get; set; }
    public string CommandName { get; } = commandName;
}

public class GroupCommandEventArgs(GroupCommandArgs args, string commandName) : CommandEventArgs(args, commandName)
{
    public new GroupCommandArgs Args => (GroupCommandArgs)base.Args;
}

public class PrivateCommandEventArgs(PrivateCommandArgs args, string commandName) : CommandEventArgs(args, commandName)
{
    public new PrivateCommandArgs Args => (PrivateCommandArgs)base.Args;
}

public class ServerCommandEventArgs(ServerCommandArgs args, string commandName) : CommandEventArgs(args, commandName)
{
    public new ServerCommandArgs Args => (ServerCommandArgs)base.Args;
}

public class PermissionCheckEventArgs(CommandArgs args, string[] requiredPermissions) : EventArgs
{
    public CommandArgs Args { get; } = args;
    public string[] RequiredPermissions { get; } = requiredPermissions;
    public PermissionResult Result { get; set; } = PermissionResult.Default;
    public string? DenyMessage { get; set; }
}

public enum PermissionResult
{
    Default,
    Granted,
    Denied
}

public static class CommandEvents
{
    public static event Func<CommandEventArgs, Task>? OnCommandExecuting;
    public static event Func<GroupCommandEventArgs, Task>? OnGroupCommandExecuting;
    public static event Func<PrivateCommandEventArgs, Task>? OnPrivateCommandExecuting;
    public static event Func<ServerCommandEventArgs, Task>? OnServerCommandExecuting;
    public static event Func<PermissionCheckEventArgs, Task>? OnPermissionChecking;

    internal static async Task<bool> TriggerCommandExecuting(CommandArgs args, string commandName)
    {
        CommandEventArgs eventArgs = args switch
        {
            GroupCommandArgs groupArgs => new GroupCommandEventArgs(groupArgs, commandName),
            PrivateCommandArgs privateArgs => new PrivateCommandEventArgs(privateArgs, commandName),
            ServerCommandArgs serverArgs => new ServerCommandEventArgs(serverArgs, commandName),
            _ => throw new NotSupportedException($"不支持的指令参数类型: {args.GetType().Name}")
        };

        if (OnCommandExecuting != null)
        {
            await OnCommandExecuting(eventArgs);
            if (eventArgs.Handled)
                return true;
        }

        switch (eventArgs)
        {
            case GroupCommandEventArgs groupEvent when OnGroupCommandExecuting != null:
                await OnGroupCommandExecuting(groupEvent);
                return groupEvent.Handled;

            case PrivateCommandEventArgs privateEvent when OnPrivateCommandExecuting != null:
                await OnPrivateCommandExecuting(privateEvent);
                return privateEvent.Handled;

            case ServerCommandEventArgs serverEvent when OnServerCommandExecuting != null:
                await OnServerCommandExecuting(serverEvent);
                return serverEvent.Handled;

            default:
                return false;
        }
    }

    internal static async Task<PermissionCheckResult> TriggerPermissionChecking(CommandArgs args, string[] requiredPermissions)
    {
        var eventArgs = new PermissionCheckEventArgs(args, requiredPermissions);

        if (OnPermissionChecking != null)
        {
            await OnPermissionChecking(eventArgs);
        }

        return new PermissionCheckResult
        {
            Result = eventArgs.Result,
            DenyMessage = eventArgs.DenyMessage
        };
    }
}

public class PermissionCheckResult
{
    public PermissionResult Result { get; set; }
    public string? DenyMessage { get; set; }
}
