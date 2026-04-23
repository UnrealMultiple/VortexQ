using Vortex.Bot.Command;

namespace Vortex.Bot.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class CommandAttribute(params string[] aliases) : Attribute
{
    public HashSet<string> Alias { get; } = [.. aliases];
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
