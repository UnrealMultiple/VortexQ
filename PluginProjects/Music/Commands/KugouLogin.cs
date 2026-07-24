using Microsoft.Extensions.Logging;
using Music.Kugou;
using Music.Models;
using Music.Providers;
using Vortex.Bot.Attributes;
using Vortex.Bot.Command;

namespace Music.Commands;

[Command("酷狗登录", "kgl")]
[HelpText("使用二维码登录酷狗音乐")]
[CommandType(CommandType.Group)]
public static class KugouLogin
{
    [Main]
    public static async Task Execute(GroupCommandArgs args)
    {
        try
        {
            var provider = Music.Instance.MusicService.GetProvider<KugouProvider>(MusicSource.Kugou);
            if (provider is null)
            {
                await args.ReplyWithAtAsync("酷狗音乐未注册");
                return;
            }

            if (provider.IsAuthenticated)
            {
                await args.ReplyWithAtAsync("您已经登录了酷狗音乐");
                return;
            }

            await args.ReplyWithAtAsync("正在获取登录二维码...");

            var qrResult = await provider.GetQrCodeAsync();
            if (qrResult is null)
            {
                await args.ReplyWithAtAsync("获取二维码失败，请稍后重试");
                return;
            }

            // 发送二维码图片：优先使用 API 原生 base64 图片，失败则尝试第三方下载
            byte[] imgBytes;
            if (qrResult.QrCodeImageBytes is { Length: > 0 })
            {
                imgBytes = qrResult.QrCodeImageBytes;
            }
            else
            {
                var fallbackUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=300x300&data={Uri.EscapeDataString(qrResult.QrCodeUrl)}";
                using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                try
                {
                    imgBytes = await httpClient.GetByteArrayAsync(fallbackUrl);
                    if (imgBytes.Length <= 100) throw new InvalidOperationException("图片数据不足");
                }
                catch
                {
                    await args.ReplyWithAtAsync($"请使用酷狗App扫描以下二维码链接：\n{qrResult.QrCodeUrl}");
                    return;
                }
            }

            await args.ReplyImageAsync(imgBytes);

            await args.ReplyWithAtAsync("请在2分钟内完成扫码登录");

            // 轮询扫码状态
            var cts = new CancellationTokenSource();
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(120), cts.Token);

            QrLoginResult? loginResult = null;
            var qrKey = qrResult.QrCodeKey;
            var pollingTask = PollQrStatusAsync(provider, qrKey, args, result =>
            {
                if (result.Status == 4)
                {
                    loginResult = result;
                    cts.Cancel();
                }
            }, cts.Token);

            try
            {
                await Task.WhenAny(pollingTask, timeoutTask);
            }
            catch (OperationCanceledException) { }

            if (loginResult is not null)
            {
                provider.ApplyQrLogin(loginResult);
                await args.ReplyWithAtAsync($"酷狗音乐登录成功！欢迎 {loginResult.Nickname ?? "未知用户"}");
            }
            else
            {
                // 检查最终状态
                var finalStatus = await provider.CheckQrStatusAsync(qrKey);
                if (finalStatus?.Status == 4)
                {
                    provider.ApplyQrLogin(finalStatus);
                    await args.ReplyWithAtAsync($"酷狗音乐登录成功！欢迎 {finalStatus.Nickname ?? "未知用户"}");
                }
                else
                {
                    await args.ReplyWithAtAsync("登录超时或已取消，请稍后重试");
                }
            }
        }
        catch (Exception ex)
        {
            args.Logger.LogError(ex, "酷狗二维码登录失败");
            await args.ReplyWithAtAsync("登录失败，请稍后重试");
        }
    }

    private static async Task PollQrStatusAsync(KugouProvider provider, string qrText,
        GroupCommandArgs args, Action<QrLoginResult> onSuccess, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await Task.Delay(3000, token);

            var result = await provider.CheckQrStatusAsync(qrText);
            if (result is null) continue;

            switch (result.Status)
            {
                case 1:
                    args.Logger.LogInformation("[Kugou] 等待扫码");
                    break;
                case 2:
                    args.Logger.LogInformation("[Kugou] 已扫码，等待确认");
                    await args.ReplyWithAtAsync("已扫码，请在手机上确认登录");
                    break;
                case 4:
                    onSuccess(result);
                    return;
                case 0:
                    args.Logger.LogInformation("[Kugou] 二维码已过期");
                    return;
            }
        }
    }
}
