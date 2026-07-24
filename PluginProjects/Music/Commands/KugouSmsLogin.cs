using Microsoft.Extensions.Logging;
using Music.Models;
using Music.Providers;
using Vortex.Bot.Attributes;
using Vortex.Bot.Command;

namespace Music.Commands;

/// <summary>
/// 发送酷狗登录验证码
/// </summary>
[Command("酷狗发送验证码", "kgsend")]
[HelpText("发送酷狗音乐手机验证码")]
[CommandType(CommandType.Group)]
public static class KugouSendSms
{
    [Main]
    public static async Task Execute(GroupCommandArgs args, [Param("手机号")] string mobile)
    {
        try
        {
            var provider = Music.Instance.MusicService.GetProvider<KugouProvider>(MusicSource.Kugou);
            if (provider is null)
            {
                await args.ReplyWithAtAsync("酷狗音乐未注册");
                return;
            }

            if (string.IsNullOrWhiteSpace(mobile) || mobile.Length < 11)
            {
                await args.ReplyWithAtAsync("请输入正确的手机号");
                return;
            }

            var success = await provider.SendSmsCodeAsync(mobile);
            if (success)
            {
                KugouVerifySms.PendingCodes[args.SenderUin.ToString()] = mobile;
                await args.ReplyWithAtAsync("验证码已发送，请使用 /kgverify <验证码> 完成登录");
            }
            else
            {
                await args.ReplyWithAtAsync("发送验证码失败，请稍后重试");
            }
        }
        catch (Exception ex)
        {
            args.Logger.LogError(ex, "发送酷狗验证码失败");
            await args.ReplyWithAtAsync("发送验证码失败，请稍后重试");
        }
    }
}

/// <summary>
/// 验证酷狗登录验证码
/// </summary>
[Command("酷狗验证码登录", "kgverify")]
[HelpText("使用收到的验证码完成酷狗音乐登录")]
[CommandType(CommandType.Group)]
public static class KugouVerifySms
{
    internal static readonly Dictionary<string, string> PendingCodes = new();

    [Main]
    public static async Task Execute(GroupCommandArgs args, [Param("验证码")] string code)
    {
        try
        {
            var provider = Music.Instance.MusicService.GetProvider<KugouProvider>(MusicSource.Kugou);
            if (provider is null)
            {
                await args.ReplyWithAtAsync("酷狗音乐未注册");
                return;
            }

            var userId = args.SenderUin.ToString();
            if (!PendingCodes.TryGetValue(userId, out var mobile))
            {
                await args.ReplyWithAtAsync("请先使用 /kgsend <手机号> 发送验证码");
                return;
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                await args.ReplyWithAtAsync("请输入验证码");
                return;
            }

            await args.ReplyWithAtAsync("正在登录...");

            var (success, _) = await provider.LoginByMobileAsync(mobile, code);
            if (success)
            {
                PendingCodes.Remove(userId);
                await args.ReplyWithAtAsync("酷狗音乐登录成功！");
            }
            else
            {
                await args.ReplyWithAtAsync("登录失败，请检查验证码是否正确");
            }
        }
        catch (Exception ex)
        {
            args.Logger.LogError(ex, "酷狗验证码登录失败");
            await args.ReplyWithAtAsync("登录失败，请稍后重试");
        }
    }
}
