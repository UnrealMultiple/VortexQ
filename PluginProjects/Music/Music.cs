using Microsoft.Extensions.Logging;
using Music.Kugou;
using Music.Models;
using Music.Providers;
using Music.QQ.Internal.MusicToken;
using Music.Services;
using Vortex.Bot.Plugins;

namespace Music;

public class Music : PluginBase
{
    public override string Name => "Music";
    public override string Author => "VortexQ";
    public override string Description => "QQ音乐和网易云音乐点歌插件";
    public override Version Version => new(2, 0, 0);

    public static Music Instance { get; private set; } = null!;

    public MusicService MusicService { get; private set; } = null!;

    protected override async ValueTask OnInitializeAsync(CancellationToken cancellationToken)
    {
        Instance = this;

        MusicService = new MusicService(Logger);

        var qqProvider = new QQMusicProvider(Logger);
        if (Config.Instance.QQToken is { } token)
        {
            qqProvider.SetToken(token);
            qqProvider.AuthStateChanged += (_, args) =>
            {
                if (args.IsAuthenticated)
                {
                    Logger.LogInformation("[Music] QQ音乐登录成功");
                }
            };
        }
        MusicService.RegisterProvider(qqProvider);

        var netEaseProvider = new NetEaseProvider(Logger);
        MusicService.RegisterProvider(netEaseProvider);

        var kugouProvider = new KugouProvider(Logger);
        if (kugouProvider.IsAuthenticated)
        {
            Logger.LogInformation("[Music] 酷狗音乐会话已恢复");
        }
        kugouProvider.AuthStateChanged += (_, args) =>
        {
            if (args.IsAuthenticated)
            {
                Logger.LogInformation("[Music] 酷狗音乐登录成功");
            }
        };
        MusicService.RegisterProvider(kugouProvider);

        Logger.LogInformation("[Music] 插件初始化完成");
    }

    protected override async ValueTask OnShutdownAsync(CancellationToken cancellationToken)
    {
        MusicService.Dispose();
        Logger.LogInformation("[Music] 插件已关闭");
    }

    public void SetQQToken(TokenInfo token)
    {
        MusicService.GetProvider<QQMusicProvider>(MusicSource.QQMusic)?.SetToken(token);
        Config.Instance.SetQQToken(token);
    }

    public ILogger GetLogger() => Logger;
}
