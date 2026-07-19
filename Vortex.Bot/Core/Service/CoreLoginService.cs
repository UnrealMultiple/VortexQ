using Lagrange.Core;
using Lagrange.Core.Common;
using Lagrange.Core.Common.Entity;
using Lagrange.Core.Common.Interface;
using Lagrange.Core.Events.EventArgs;
using Lagrange.Core.Message;
using Lagrange.Core.Message.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using Vortex.Bot.Command;
using Vortex.Bot.Configuration;
using Vortex.Bot.Database.Models;
using Vortex.Bot.Extension;
using Vortex.Bot.Interface;
using Vortex.Bot.Utility;
using static Lagrange.Core.Events.EventArgs.BotQrCodeQueryEvent;
using MSLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Vortex.Bot.Core.Service;

public class CoreLoginService(ILogger<CoreLoginService> logger, IOptions<CoreConfiguration> options, IHost host, BotContext bot, CommandManager cmd, VortexContext vortexContext, ICaptchaResolver captchaResolver) : IHostedService
{
    private readonly ILogger<CoreLoginService> _logger = logger;
    private readonly CoreConfiguration _configuration = options.Value;
    private readonly IHost _host = host;
    private readonly BotContext _bot = bot;
    private readonly CommandManager _cmd = cmd;
    private readonly VortexContext _vortexContext = vortexContext;
    private readonly ICaptchaResolver _captchaResolver = captchaResolver;

    private CancellationTokenSource? _cts;

    public async Task StartAsync(CancellationToken token)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        _cmd.AutoRegister(Assembly.GetExecutingAssembly());
        _bot.EventInvoker.RegisterEvent<BotQrCodeEvent>(HandleQrCode);
        _bot.EventInvoker.RegisterEvent<BotQrCodeQueryEvent>(HandleQrCodeQuery);
        _bot.EventInvoker.RegisterEvent<BotRefreshKeystoreEvent>(HandleRefreshKeystore);
        _bot.EventInvoker.RegisterEvent<BotCaptchaEvent>(HandleCaptcha);
        _bot.EventInvoker.RegisterEvent<BotSMSEvent>(HandleSms);
        _bot.EventInvoker.RegisterEvent<BotNewDeviceVerifyEvent>(HandleNewDeviceVerify);
        _bot.EventInvoker.RegisterEvent<BotMessageEvent>(HandleMessage);

        var uin = _configuration.Login.Uin;
        var password = _configuration.Login.Password ?? string.Empty;
        var result = await _bot.Login(uin, password, token);
        if (!result)
        {
            _logger.LogLoginFailed();
            _ = _host.StopAsync(CancellationToken.None);
        }
        _logger.LogLoginSuccessful(_bot.BotUin, _bot.Config.Protocol, _bot.AppInfo.CurrentVersion);
    }

    private async Task HandleMessage(BotContext ctx, BotMessageEvent e)
    {
        await (e.Message.Type switch
        {
            Lagrange.Core.Message.MessageType.Group => CommandGroupAdapter(ctx, e),
            Lagrange.Core.Message.MessageType.Private => CommandPrivateAdapter(ctx, e),
            Lagrange.Core.Message.MessageType.Temp => throw new NotImplementedException(),
            _ => throw new NotImplementedException()
        });
    }

    private async Task CommandGroupAdapter(BotContext ctx, BotMessageEvent e)
    {
        var text = BuildCommandText(e.Message.Entities);
        RecordMessage(e);
        await _cmd.ExecuteGroupAsync(text, e, _vortexContext);
    }

    private void RecordMessage(BotMessageEvent e)
    {
        var msg = e.Message;
        long fromUin = msg.Contact.Uin;
        long toUin = msg.Type switch
        {
            Lagrange.Core.Message.MessageType.Group when msg.Contact is BotGroupMember member => member.Group.GroupUin,
            _ => msg.Receiver.Uin
        };

        MessageRecord.Insert(new MessageRecord()
        {
            TypeInt = (int)msg.Type,
            SequenceLong = msg.Sequence,
            ClientSequenceLong = msg.ClientSequence,
            MessageIdLong = msg.MessageId,
            Time = msg.Time,
            FromUinLong = fromUin,
            ToUinLong = toUin,
            Entities = MessageChainSerializer.SerializeToUtf8Bytes(msg.Entities)
        });
    }

    private async Task CommandPrivateAdapter(BotContext ctx, BotMessageEvent e)
    {
        var text = BuildCommandText(e.Message.Entities);
        RecordMessage(e);
        await _cmd.ExecutePrivateAsync(text, e, _vortexContext);
    }

    private static string BuildCommandText(MessageChain entities) => entities.GetEnitys<TextEntity>().ToJoinedString(x => x.Text, "");

    private void HandleNewDeviceVerify(BotContext _, BotNewDeviceVerifyEvent @event)
    {
        _logger.LogQrCode(QrCodeUtility.GenerateAscii(@event.Url, _configuration.Login.CompatibleQrCode));
        _logger.LogNewDeviceVerify(_bot.BotUin);
    }

    private async Task HandleSms(BotContext bot, BotSMSEvent @event)
    {
        // Allow interrupt input
        await Task.Run(() =>
        {
            Console.WriteLine("Please enter the SMS code:");
            var code = Console.ReadLine();
            if (string.IsNullOrEmpty(code))
            {
                _logger.LogSmsCodeEmpty();
                _host.StopAsync();
                return;
            }

            _bot.SubmitSMSCode(code);
        }, _cts?.Token ?? default);
    }

    private async Task HandleCaptcha(BotContext bot, BotCaptchaEvent @event)
    {
        (var ticket, var randstr) = await _captchaResolver.ResolveCaptchaAsync(@event.CaptchaUrl, _cts?.Token ?? default);
        _bot.SubmitCaptcha(ticket, randstr);
    }

    private async Task HandleRefreshKeystore(BotContext bot, BotRefreshKeystoreEvent @event)
    {
        var keystore = @event.Keystore;
        await File.WriteAllBytesAsync(
            $"{keystore.Uin}.keystore",
            JsonUtility.SerializeToUtf8Bytes(keystore),
            _cts?.Token ?? default
        );
    }

    private void HandleQrCodeQuery(BotContext bot, BotQrCodeQueryEvent @event)
    {
        var level = @event.State switch
        {
            TransEmpState.Confirmed or
            TransEmpState.WaitingForScan or
            TransEmpState.WaitingForConfirm => MSLogLevel.Debug,
            _ => MSLogLevel.Error,
        };
        _logger.LogQrCodeState(level, @event.State);
    }

    private async Task HandleQrCode(BotContext bot, BotQrCodeEvent @event)
    {
        await File.WriteAllBytesAsync("qrcode.png", @event.Image, _cts?.Token ?? default);

        _logger.LogQrCode(QrCodeUtility.GenerateAscii(@event.Url, _configuration.Login.CompatibleQrCode));
        _logger.LogFetchQrCodeSuccess(@event.Url);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _bot.EventInvoker.UnregisterEvent<BotQrCodeEvent>(HandleQrCode);
        _bot.EventInvoker.UnregisterEvent<BotQrCodeQueryEvent>(HandleQrCodeQuery);
        _bot.EventInvoker.UnregisterEvent<BotRefreshKeystoreEvent>(HandleRefreshKeystore);
        _bot.EventInvoker.UnregisterEvent<BotCaptchaEvent>(HandleCaptcha);
        _bot.EventInvoker.UnregisterEvent<BotSMSEvent>(HandleSms);
        _bot.EventInvoker.UnregisterEvent<BotNewDeviceVerifyEvent>(HandleNewDeviceVerify);

        await _bot.Logout();
    }
}

public static partial class CoreLoginServiceLoggerExtension
{
    [LoggerMessage(MSLogLevel.Information, "\n{qrcode}")]
    public static partial void LogQrCode(this ILogger<CoreLoginService> logger, string qrcode);

    [LoggerMessage(MSLogLevel.Information, "Fetch QrCode Success, Expiration: 120 seconds, Url: {url}")]
    public static partial void LogFetchQrCodeSuccess(this ILogger<CoreLoginService> logger, string url);

    [LoggerMessage("QrCode State: {state}")]
    public static partial void LogQrCodeState(this ILogger<CoreLoginService> logger, MSLogLevel level, TransEmpState state);

    [LoggerMessage(MSLogLevel.Information, "NewDevice verify required, please scan the QrCode with the device that has already logged in with uin {uin}")]
    public static partial void LogNewDeviceVerify(this ILogger<CoreLoginService> logger, long uin);

    [LoggerMessage(MSLogLevel.Information, "{uin} successfully logged in via {protocol} {version}")]
    public static partial void LogLoginSuccessful(this ILogger<CoreLoginService> logger, long uin, Protocols protocol, string version);

    [LoggerMessage(MSLogLevel.Critical, "Login failed")]
    public static partial void LogLoginFailed(this ILogger<CoreLoginService> logger);

    [LoggerMessage(MSLogLevel.Critical, "SMS code is empty")]
    public static partial void LogSmsCodeEmpty(this ILogger<CoreLoginService> logger);
}
