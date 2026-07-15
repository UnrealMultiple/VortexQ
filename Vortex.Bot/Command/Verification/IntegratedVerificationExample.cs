using Vortex.Bot.Attributes;
using Vortex.Bot.Database.Models;

namespace Vortex.Bot.Command.Verification;

internal sealed record CurrencyTransferVerificationData(long TargetUserId, long Amount);

[Command("转账", "transfer")]
[HelpText("转账给指定用户（需要确认）")]
[CommandType(CommandType.Group)]
[Permission("vortex.transfer")]
public static class IntegratedTransferCommand
{
    [Main]
    public static async Task Execute(GroupCommandArgs args, long targetUserId, long amount)
    {
        var currencyName = args.Context.Configuration.Miscellaneous.CurrencyName;

        if (targetUserId == args.SenderUin)
        {
            await args.ReplyWithAtAsync("不能给自己转账！");
            return;
        }

        if (amount <= 0)
        {
            await args.ReplyWithAtAsync("转账金额必须大于 0！");
            return;
        }

        if (args.GetPendingVerification("transfer") != null)
        {
            await args.ReplyWithAtAsync("您已有待确认的转账操作，请发送 /确认转账 或 /取消转账。");
            return;
        }

        args.CreateVerification(
            actionType: "transfer",
            actionName: "转账",
            timeoutSeconds: 60,
            data: new CurrencyTransferVerificationData(targetUserId, amount));

        await args.ReplyWithAtAsync(
            $"转账确认\n" +
            $"目标用户: {targetUserId}\n" +
            $"金额: {amount} {currencyName}\n" +
            "请在 60 秒内发送 /确认转账 确认，或发送 /取消转账 取消。");

        _ = args.StartVerificationTimeoutAsync("transfer", async _ =>
        {
            await args.ReplyWithAtAsync("转账确认已超时，操作已取消。");
        });
    }
}

[Command("确认转账", "confirmtransfer")]
[HelpText("确认转账操作")]
[CommandType(CommandType.Group)]
public static class IntegratedConfirmTransferCommand
{
    [Main]
    public static async Task Execute(GroupCommandArgs args)
    {
        var result = args.Verify("transfer");
        if (!result.Success)
        {
            await args.ReplyWithAtAsync($"操作失败: {result.Message}");
            return;
        }

        if (result.Verification?.Data is not CurrencyTransferVerificationData data)
        {
            await args.ReplyWithAtAsync("转账确认数据无效，请重新发起转账。");
            return;
        }

        try
        {
            Currency.Transfer(args.SenderUin, data.TargetUserId, data.Amount);
            var currencyName = args.Context.Configuration.Miscellaneous.CurrencyName;
            await args.ReplyWithAtAsync(
                $"{result.Message}！\n已向 {data.TargetUserId} 转账 {data.Amount} {currencyName}");
        }
        catch (Exception ex)
        {
            await args.ReplyWithAtAsync($"转账失败: {ex.Message}");
        }
    }
}

[Command("取消转账", "canceltransfer")]
[HelpText("取消转账操作")]
[CommandType(CommandType.Group)]
public static class IntegratedCancelTransferCommand
{
    [Main]
    public static async Task Execute(GroupCommandArgs args)
    {
        var result = args.CancelVerification("transfer");
        await args.ReplyWithAtAsync(result.Success ? result.Message ?? "已取消。" : $"操作失败: {result.Message}");
    }
}
