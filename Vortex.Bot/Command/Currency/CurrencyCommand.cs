using Vortex.Bot.Attributes;
using Vortex.Bot.Database.Models;
using Vortex.Bot.Utility.Images;

namespace Vortex.Bot.Command.Currencys;

[Command("金币", "money", "currency")]
[HelpText("金币系统")]
[CommandType(CommandType.Group | CommandType.Friend)]
[Permission("vortex.currency")]
public static class CurrencyCommand
{
    [Command("query", "查询")]
    [CommandType(CommandType.Group | CommandType.Friend)]
    [Permission("vortex.currency.query")]
    public static class QueryCmd
    {
        [Main]
        public static async Task Execute(CommandArgs args, [Param("QQ号(可选)")] long? targetUserId = null)
        {
            var userId = targetUserId ?? args.SenderUin;
            var balance = Currency.GetBalance(userId);

            if (targetUserId.HasValue)
            {
                await args.ReplyWithAtAsync($"用户 {userId} 的金币余额: {balance}");
            }
            else
            {
                await args.ReplyWithAtAsync($"你的金币余额: {balance}");
            }
        }
    }

    [Command("add", "增加")]
    [CommandType(CommandType.Group | CommandType.Friend)]
    [Permission("vortex.currency.admin")]
    public static class AddCmd
    {
        [Main]
        public static async Task Execute(CommandArgs args, [Param("QQ号")] long userId, [Param("数量")] long amount)
        {
            try
            {
                var currency = Currency.Add(userId, amount);
                await args.ReplyWithAtAsync($"已给用户 {userId} 增加 {amount} 金币\n当前余额: {currency.Num}");
            }
            catch (Exception ex)
            {
                await args.ReplyWithAtAsync($"操作失败: {ex.Message}");
            }
        }
    }

    [Command("deduct", "扣除")]
    [CommandType(CommandType.Group | CommandType.Friend)]
    [Permission("vortex.currency.admin")]
    public static class DeductCmd
    {
        [Main]
        public static async Task Execute(CommandArgs args, [Param("QQ号")] long userId, [Param("数量")] long amount)
        {
            try
            {
                var currency = Currency.Deduct(userId, amount);
                await args.ReplyWithAtAsync($"已从用户 {userId} 扣除 {amount} 金币\n当前余额: {currency.Num}");
            }
            catch (Exception ex)
            {
                await args.ReplyWithAtAsync($"操作失败: {ex.Message}");
            }
        }
    }

    [Command("set", "设置")]
    [CommandType(CommandType.Group | CommandType.Friend)]
    [Permission("vortex.currency.admin")]
    public static class SetCmd
    {
        [Main]
        public static async Task Execute(CommandArgs args, [Param("QQ号")] long userId, [Param("数量")] long amount)
        {
            try
            {
                var currency = Currency.Set(userId, amount);
                await args.ReplyWithAtAsync($"已设置用户 {userId} 的金币为 {amount}\n当前余额: {currency.Num}");
            }
            catch (Exception ex)
            {
                await args.ReplyWithAtAsync($"操作失败: {ex.Message}");
            }
        }
    }

    [Command("top", "排行")]
    [CommandType(CommandType.Group | CommandType.Friend)]
    [Permission("vortex.currency.top")]
    public static class TopCmd
    {
        [Main]
        public static async Task Execute(CommandArgs args, [Param("数量(默认10)")] int top = 10)
        {
            var topList = Currency.GetTop(top);

            if (topList.Count == 0)
            {
                await args.ReplyWithAtAsync("暂无金币排行榜数据");
                return;
            }

            var builder = TableBuilder.Create()
                .SetHeader("排名", "用户", "金币")
                .SetTitle("金币排行榜")
                .SetMemberUin(args.SenderUin);

            for (var i = 0; i < topList.Count; i++)
            {
                builder.AddRow((i + 1).ToString(), topList[i].UserId.ToString(), topList[i].Num.ToString());
            }

            await args.ReplyImageAsync(builder.Build());
        }
    }

    [Command("transfer", "转账")]
    [CommandType(CommandType.Group | CommandType.Friend)]
    [Permission("vortex.currency.transfer")]
    public static class TransferCmd
    {
        [Main]
        public static async Task Execute(CommandArgs args, [Param("目标QQ")] long targetUserId, [Param("数量")] long amount)
        {
            if (targetUserId == args.SenderUin)
            {
                await args.ReplyWithAtAsync("不能给自己转账!");
                return;
            }

            if (amount <= 0)
            {
                await args.ReplyWithAtAsync("转账金额必须大于0!");
                return;
            }

            try
            {
                Currency.Transfer(args.SenderUin, targetUserId, amount);
                await args.ReplyWithAtAsync($"转账成功!\n已向 {targetUserId} 转账 {amount} 金币");
            }
            catch (Exception ex)
            {
                await args.ReplyWithAtAsync($"转账失败: {ex.Message}");
            }
        }
    }
}
