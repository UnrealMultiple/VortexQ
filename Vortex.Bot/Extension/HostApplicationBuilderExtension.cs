using Lagrange.Core.Common;
using Lagrange.Core.Common.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vortex.Bot.Command;
using Vortex.Bot.Configuration;
using Vortex.Bot.Core.Service;
using Vortex.Bot.Database;
using Vortex.Bot.Interface;
using Vortex.Bot.Plugins;
using Vortex.Bot.Utility;
using Vortex.Bot.Utility.CaptchaResolver;
using CoreLogLevel = Lagrange.Core.Events.EventArgs.LogLevel;

namespace Vortex.Bot.Extension;

public static class HostApplicationBuilderExtension
{
    public static HostApplicationBuilder ConfigureConfiguration(this HostApplicationBuilder builder, Action<ConfigurationManager> configurer)
    {
        configurer(builder.Configuration);
        return builder;
    }

    public static HostApplicationBuilder ConfigureServices(this HostApplicationBuilder builder, Action<IServiceCollection> configurer)
    {
        configurer(builder.Services);
        return builder;
    }

    public static HostApplicationBuilder ConfigureCore(this HostApplicationBuilder builder) => builder
#pragma warning disable IL2026, IL3050
        // https://github.com/dotnet/runtime/issues/94544
        .ConfigureServices(services => services
            .Configure<CoreConfiguration>(builder.Configuration.GetSection("Core"))
#pragma warning restore IL2026, IL3050
            // Signer
            .AddSingleton<Signer>()
            // BotConfig
            .AddSingleton(services =>
            {
                var loggerConfiguration = services.GetRequiredService<IOptions<LoggerFilterOptions>>().Value;
                var coreConfiguration = services.GetRequiredService<IOptions<CoreConfiguration>>().Value;
                var signer = services.GetRequiredService<Signer>();

                return new BotConfig
                {
                    Protocol = Protocols.Linux,
                    LogLevel = (CoreLogLevel)loggerConfiguration.GetDefaultLogLevel(),
                    AutoReconnect = coreConfiguration.Server.AutoReconnect,
                    UseIPv6Network = coreConfiguration.Server.UseIPv6Network,
                    GetOptimumServer = coreConfiguration.Server.GetOptimumServer,
                    AutoReLogin = coreConfiguration.Login.AutoReLogin,
                    SignProvider = signer,
                };
            })
            // BotKeystore
            .AddSingleton(services =>
            {
                var configuration = services.GetRequiredService<IOptions<CoreConfiguration>>().Value;
                string path = $"{configuration.Login.Uin}.keystore";

                BotKeystore keystore;
                if (File.Exists(path))
                {
                    var keystoreNullable = JsonUtility.Deserialize<BotKeystore>(File.ReadAllBytes(path));
                    keystore = keystoreNullable ?? throw new Exception(
                        $"Invalid keystore detected. Please remove the '{path}' file and re-authenticate."
                    );
                }
                else
                {
                    keystore = BotKeystore.CreateEmpty();
                }

                keystore.DeviceName = configuration.Login.DeviceName;
                return keystore;
            })
            // BotContext
            .AddSingleton(services =>
            {
                var config = services.GetRequiredService<BotConfig>();
                var keystore = services.GetRequiredService<BotKeystore>();

                return BotFactory.Create(config, keystore);
            })

            // CaptchaResolver
            .AddSingleton<ICaptchaResolver>(services =>
            {
                var configuration = services.GetRequiredService<IOptions<CoreConfiguration>>().Value;

                return configuration.Login.UseOnlineCaptchaResolver
                    ? ActivatorUtilities.CreateInstance<OnlineCaptchaResolver>(services)
                    : ActivatorUtilities.CreateInstance<ManualCaptchaResolver>(services);
            })
            .AddSingleton<CommandManager>()

            // Database
            .AddSingleton<IDatabaseService>(services =>
            {
                var configuration = services.GetRequiredService<IOptions<CoreConfiguration>>().Value;
                var dbPath = Path.Combine(Environment.CurrentDirectory, configuration.Database.DbPath);
                return new DatabaseService(dbPath);
            })

            // VortexContext (暴露API)
            .AddSingleton<VortexContext>()
            .AddHostedService(services => services.GetRequiredService<VortexContext>())

            // PluginManager
            .AddSingleton<PluginManager>()

            // Logger
            .AddHostedService<CoreLoggerService>()

            // Login
            .AddHostedService<CoreLoginService>()

            // PluginLoaderService (在登录成功后加载插件)
            .AddHostedService<PluginLoaderService>()
        );
}