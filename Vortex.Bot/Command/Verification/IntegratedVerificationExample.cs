using Vortex.Bot.Attributes;

namespace Vortex.Bot.Command.Verification;


[Command("转账", "transfer")]
[HelpText("转账给指定用户（需要确认）")]
[CommandType(CommandType.Group)]
[Permission("vortex.transfer")]
public static class IntegratedTransferCommand
{
    [Main]
    public static async Task Execute(GroupCommandArgs args, long targetUser, int amount)
    {
        if (args.GetPendingVerification("transfer") != null)
        {
            await args.ReplyWithAtAsync("您已有待确认的转账操作，请发送 /确认转账 或 /取消转账");
            return;
        }

        args.CreateVerification(
            actionType: "transfer",
            actionName: "转账",
            timeoutSeconds: 60,
            data: new { TargetUser = targetUser, Amount = amount }
        );

        await args.ReplyWithAtAsync(
            $"💰 转账确认\n" +
            $"目标用户: {targetUser}\n" +
            $"金额: {amount} 金币\n" +
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

        if (result.Success && result.Verification?.Data != null)
        {
            dynamic data = result.Verification.Data;
            await args.ReplyWithAtAsync(
                $"✅ {result.Message}!\n" +
                $"已向 {data.TargetUser} 转账 {data.Amount} 金币"
            );
        }
        else
        {
            await args.ReplyWithAtAsync($"❌ {result.Message}");
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
        await args.ReplyWithAtAsync(result.Success ? $"✅ {result.Message}" : $"❌ {result.Message}");
    }
}
