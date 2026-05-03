using Vortex.Bot.Command;

namespace Vortex.Bot.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class CommandAttribute(params string[] aliases) : Attribute
{
    public HashSet<string> Alias { get; } = [.. aliases];
}

[AttributeUsage(AttributeTargets.All)]
public class DefaultCommandAttribute : Attribute
{

}

[AttributeUsage(AttributeTargets.Class)]
public class CommandTypeAttribute(CommandType commandType) : Attribute
{
    public CommandType CommandType { get; } = commandType;
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class PermissionAttribute(params string[] permissions) : Attribute
{
    public string[] Permissions { get; } = permissions;
}

[AttributeUsage(AttributeTargets.Method)]
public class MainAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Method)]
public class FlexibleAttribute : Attribute
{
    public int MinArgs { get; }

    public FlexibleAttribute()
    {
        MinArgs = 0;
    }

    public FlexibleAttribute(int minArgs)
    {
        MinArgs = minArgs;
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class AliasAttribute(params string[] aliases) : Attribute
{
    public HashSet<string> Alias { get; } = [.. aliases];
}

[AttributeUsage(AttributeTargets.Parameter)]
public class ParamAttribute(string description) : Attribute
{
    public string Description { get; } = description;
}

[AttributeUsage(AttributeTargets.Class)]
public class SkipHelpAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class HelpTextAttribute(string description) : Attribute
{
    public string Description { get; } = description;
}
