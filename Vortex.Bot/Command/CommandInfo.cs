namespace Vortex.Bot.Command;

public sealed class CommandInfo
{
    public required string Path { get; init; }
    public string[] Aliases { get; init; } = [];
    public string? HelpText { get; init; }
    public string? ParameterInfo { get; init; }
    public bool IsSubCommand => Path.Contains(' ');
    public string PrimaryAlias => Aliases.FirstOrDefault() ?? Path;
}
