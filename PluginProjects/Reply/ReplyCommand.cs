using Microsoft.Extensions.Logging;
using Vortex.Bot.Attributes;
using Vortex.Bot.Command;
using Vortex.Bot.Utility.Images;

namespace Reply;

[Command("reply")]
[CommandType(CommandType.Group)]
[Permission("onebot.reply")]
[HelpText("reply <add|del|list|var|content>")]
public static class ReplyCommand
{
    [Main]
    [Flexible]
    public static async Task Execute(GroupCommandArgs args)
    {
        var action = args.Params.Count > 0 ? args.Params[0].ToLower() : "";
        switch (action)
        {
            case "add":
                await Add(args);
                break;
            case "remove":
            case "del":
                await Remove(args);
                break;
            case "list":
                await ReplyList(args);
                break;
            case "var":
                await Variables(args);
                break;
            case "content":
                await Contents(args);
                break;
            default:
                await args.ReplyAsync($"语法错误，正确语法:" +
                    $"\n{args.CommandPrefix}{args.CommandName} add <规则> <回复>" +
                    $"\n{args.CommandPrefix}{args.CommandName} del <序号>" +
                    $"\n{args.CommandPrefix}{args.CommandName} list" +
                    $"\n{args.CommandPrefix}{args.CommandName} var" +
                    $"\n{args.CommandPrefix}{args.CommandName} content");
                break;
        }
    }

    private static async Task Contents(GroupCommandArgs args)
    {
        var handlers = ReplyAdapter.GetContentHandlers();
        if (handlers.Count == 0)
        {
            await args.ReplyWithAtAsync("没有任何内容处理器");
            return;
        }
        await args.ReplyAsync("内容处理器列表: " + string.Join(", ", handlers));
    }

    private static async Task Variables(GroupCommandArgs args)
    {
        var variables = ReplyAdapter.GetVariables();
        var builtIn = new[] { "uin", "card", "groupid" };
        var all = builtIn.Concat(variables).ToList();
        if (all.Count == 0)
        {
            await args.ReplyWithAtAsync("没有任何变量");
            return;
        }
        await args.ReplyAsync("变量列表: " + string.Join(", ", all));
    }

    private static async Task ReplyList(GroupCommandArgs args)
    {
        var rules = Config.Instance.Rules;
        if (rules.Count == 0)
        {
            await args.ReplyWithAtAsync("没有任何规则");
            return;
        }
        var builder = TableBuilder.Create()
            .SetTitle("ReplyRule")
            .SetMemberUin(args.SenderUin)
            .SetHeader("序号", "规则", "回复");
        for (var i = 0; i < rules.Count; i++)
        {
            builder.AddRow((i + 1).ToString(), rules[i].MatchPattern, rules[i].ReplyTemplate);
        }
        await args.ReplyImageAsync(builder.Build());
    }

    private static async Task Remove(GroupCommandArgs args)
    {
        if (args.Params.Count < 2)
        {
            await args.ReplyWithAtAsync($"语法错误，正确语法: {args.CommandPrefix}{args.CommandName} remove <序号>");
            return;
        }
        if (int.TryParse(args.Params[1], out var index))
        {
            Config.Instance.RemoveRule(index);
            Config.Instance.Save();
            await args.ReplyWithAtAsync("删除成功");
        }
        else
        {
            await args.ReplyWithAtAsync("序号必须是数字");
        }
    }

    private static async Task Add(GroupCommandArgs args)
    {
        if (args.Params.Count < 3)
        {
            await args.ReplyWithAtAsync($"语法错误，正确语法: {args.CommandPrefix}{args.CommandName} add <匹配词> <回复词>");
            return;
        }
        Config.Instance.Rules.Add(new(args.Params[1], args.Params[2]));
        Config.Instance.Save();
        await args.ReplyWithAtAsync("添加成功");
    }
}
