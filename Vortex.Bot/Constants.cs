using System.Reflection;

namespace Vortex.Bot;

internal static class Constants
{
    public const string Banner = """
        __  ___  _   __  __
       /  |/  / (_) / / / /__  __  __
      / /|_/ / / / / / / //_/ / / / /
     / /  / / / / / / / ,<   / /_/ /
    /_/  /_/ /_/ /_/ /_/|_|  \__, /
    Powered by Lagrange.Core/____/
    """;

    public const string ConfigFileName = "appsettings.jsonc";
    public const string ConfigResourceName = $"Vortex.Bot.Resources.{ConfigFileName}";

    public static string ImplementationName = "Vortex.Bot";

    public static string ImplementationVersion = typeof(Constants).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
        ?.InformationalVersion?[6..]
        ?? "Unknown";

    public static string MilkyVersion = "1.1";
}