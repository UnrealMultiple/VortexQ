using Vortex.Bot.Attributes;
using Vortex.Bot.Database.Models;

namespace Vortex.Bot.Command.Verification;


[Command("转账", "transfer")]
[HelpText("转账给指定用户（需要确认）")]
[CommandType(CommandType.Group)]
[Permission("vortex.transfer")]
public static class IntegratedTransferCommand
{
    [Main]
    public static async Task Execute(GroupCommandArgs args, long targetUser, long amount)
    {
        if (targetUser == args.SenderUin)
        {
            await args.ReplyWithAtAsync("不能给自己转账。");
            return;
        }

        if (amount <= 0)
        {
            await args.ReplyWithAtAsync("转账金额必须大于 0。");
            return;
        }

        if (args.GetPendingVerification("transfer") != null)
        {
            await args.ReplyWithAtAsync("您已有待确认的转账操作，请发送 /确认转账 或 /取消转账");
            return;
        }

        args.CreateVerification(
            actionType: "transfer",
            actionName: "转账",
            timeoutSeconds: 60,
            data: new TransferVerificationData(targetUser, amount)
        );

        var currencyName = args.Context.Configuration.Miscellaneous.CurrencyName;
        await args.ReplyWithAtAsync(
            $"转账确认\n" +
            $"目标用户: {targetUser}\n" +
            $"金额: {amount} {currencyName}\n" +
            $"请在60秒内发送 /确认转账 确认，或发送 /取消转账 取消。"
        );

        _ = args.StartVerificationTimeoutAsync("transfer", async (v) =>
        {
            await args.ReplyWithAtAsync($"转账确认已超时，操作已取消。");
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

        if (result.Success && result.Verification?.Data is TransferVerificationData data)
        {
            try
            {
                Currency.Transfer(args.SenderUin, data.TargetUser, data.Amount);
                var currencyName = args.Context.Configuration.Miscellaneous.CurrencyName;
                var senderBalance = Currency.GetBalance(args.SenderUin);
                var targetBalance = Currency.GetBalance(data.TargetUser);

                await args.ReplyWithAtAsync(
                    $"{result.Message}!\n" +
                    $"已向 {data.TargetUser} 转账 {data.Amount} {currencyName}\n" +
                    $"你的余额：{senderBalance} {currencyName}\n" +
                    $"对方余额：{targetBalance} {currencyName}"
                );
            }
            catch (Exception ex)
            {
                await args.ReplyWithAtAsync($"转账失败：{ex.Message}");
            }
        }
        else
        {
            await args.ReplyWithAtAsync($"❌ {result.Message}");
        }
    }
}

internal sealed record TransferVerificationData(long TargetUser, long Amount);

[Command("取消转账", "canceltransfer")]
[HelpText("取消转账操作")]
[CommandType(CommandType.Group)]
public static class IntegratedCancelTransferCommand
{
    [Main]
    public static async Task Execute(GroupCommandArgs args)
    {
        var result = args.CancelVerification("transfer");
        await args.ReplyWithAtAsync(result.Success ? $"✅ {result.Message}" : $"❌ {result.Message}");
    }
}
