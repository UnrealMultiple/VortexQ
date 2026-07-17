using System.Reflection;

namespace Vortex.Bot;

internal static class Constants
{
    public const string Banner = """
    __     _____  ____ _____ _______  __
    \ \   / / _ \|  _ \_   _| ____\ \/ /
     \ \ / / | | | |_) || | |  _|  \  / 
      \ V /| |_| |  _ < | | | |___ /  \ 
       \_/  \___/|_| \_\|_| |_____/_/\_\
                Vortex.Bot
    """;

    public const string ConfigFileName = "appsettings.jsonc";
    public const string ConfigResourceName = $"Vortex.Bot.Resources.Json.{ConfigFileName}";

    public static string ImplementationName = "Vortex.Bot";

    public static string ImplementationVersion = GetImplementationVersion();

    public static string Version = "1.0";

    private static string GetImplementationVersion()
    {
        var version = typeof(Constants).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        if (string.IsNullOrWhiteSpace(version))
            return "Unknown";

        var metadataSeparator = version.IndexOf('+');
        return metadataSeparator >= 0 && metadataSeparator < version.Length - 1
            ? version[(metadataSeparator + 1)..]
            : version;
    }
}
