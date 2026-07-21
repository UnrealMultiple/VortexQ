using Microsoft.Extensions.Logging;
using Music.QQ;
using Music.QQ.Enums;
using Music.QQ.Internal.MusicToken;
using Vortex.Bot.Attributes;
using Vortex.Bot.Command;

namespace Music.Commands;

[Command("QQ登录", "qqlogin")]
[HelpText("使用二维码登录QQ音乐")]
[CommandType(CommandType.Group)]
public static class QrCodeLogin
{
    [Main]
    public static async Task Execute(GroupCommandArgs args)
    {
        try
        {
            await args.ReplyWithAtAsync("正在获取登录二维码...");
            
            var (qrsig, qrBytes) = await Login.GetLoginQrcode();
            await args.ReplyImageAsync(qrBytes);
            await args.ReplyWithAtAsync("请使用QQ扫描二维码登录（2分钟内有效）");

            TokenInfo? token = null;
            var loginTask = Login.CheckLoginQrcode(qrsig, 120, (type, t) =>
            {
                HandleLoginState(args, type, t);
                if (type == QrcodeLoginType.DONE && t != null)
                {
                    token = t;
                }
            });

            await loginTask;

            if (token != null)
            {
                Music.Instance.SetQQToken(token);
                await args.ReplyWithAtAsync($"✅ QQ音乐登录成功！欢迎 {token.Nick}");
            }
        }
        catch (Exception ex)
        {
            args.Logger.LogError(ex, "QQ登录失败");
            await args.ReplyWithAtAsync("❌ 登录失败，请稍后重试");
        }
    }

    private static void HandleLoginState(GroupCommandArgs args, QrcodeLoginType type, TokenInfo? token)
    {
        switch (type)
        {
            case QrcodeLoginType.SCAN:
                args.Logger.LogInformation("[Music] 二维码已扫描");
                break;
            case QrcodeLoginType.CONF:
                args.Logger.LogInformation("[Music] 等待确认登录");
                break;
            case QrcodeLoginType.DONE:
                args.Logger.LogInformation("[Music] QQ音乐登录成功");
                break;
            case QrcodeLoginType.REFUSE:
                args.Logger.LogInformation("[Music] 登录被拒绝");
                break;
            case QrcodeLoginType.TIMEOUT:
                args.Logger.LogInformation("[Music] 二维码已过期");
                break;
            case QrcodeLoginType.CANCEL:
                args.Logger.LogInformation("[Music] 登录超时");
                break;
            case QrcodeLoginType.OTHER:
                args.Logger.LogInformation("[Music] 登录失败");
                break;
        }
    }
}
