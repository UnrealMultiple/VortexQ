using Octokit;
using Vortex.Bot.Attributes;
using Vortex.Bot.Command;
using Vortex.Bot.Utility.Images;

namespace GitHook.Commands;

[Command("issue", "issues")]
[CommandType(CommandType.Group)]
[Permission("onebot.aichat.issue")]
[HelpText("issue")]
public static class IssueManagerCommand
{
    [Main]
    [Flexible]
    public static async Task Execute(GroupCommandArgs args)
    {
        if (args.Params.Count < 1)
        {
            await args.ReplyAsync($"语法错误，正确语法: \n{args.CommandPrefix}{args.CommandName} list\n" +
                $"{args.CommandPrefix}{args.CommandName} see [编号]\n" +
                $"{args.CommandPrefix}{args.CommandName} close [编号]\n" +
                $"{args.CommandPrefix}{args.CommandName} reply [编号] [回复]");
            return;
        }
        switch (args.Params[0].ToLower())
        {
            case "see":
                if (args.Params.Count > 1 && int.TryParse(args.Params[1], out var num))
                {
                    var issue = await TShockPluginRepoClient.GetIssueNumber(num);
                    var buffer = await GithubPageUtils.ScreenPage(issue.HtmlUrl);
                    await args.ReplyImageAsync(buffer);
                }
                else await args.ReplyWithAtAsync("请输入一个正确的Issue编号!");
                break;
            case "close":
                if (args.Params.Count > 1 && int.TryParse(args.Params[1], out var id))
                {
                    var issue = await TShockPluginRepoClient.CloseIssue(id);
                    await args.ReplyAsync(issue.State == ItemState.Closed ? $"Issue #{id} 关闭成功!" : "未知错误，关闭失败。");
                }
                else await args.ReplyWithAtAsync("请输入一个正确的Issue编号!");
                break;
            case "reply":
                if (args.Params.Count > 2 && int.TryParse(args.Params[1], out var index))
                {
                    await TShockPluginRepoClient.ReplyIssue(index, $"`{args.SenderDisplayName}({args.SenderUin}@qq.com) Reply:`{args.Params[2]}");
                    await args.ReplyAsync("回复成功!");
                }
                else await args.ReplyWithAtAsync("请输入一个正确的Issue编号与回复内容!");
                break;
            case "list":
                var issues = await TShockPluginRepoClient.GetIssueOpen();
                var tableBuilder = TableBuilder.Create()
                    .SetTitle("正在进行的Issue")
                    .SetLineMaxTextLength(60)
                    .SetMemberUin(args.SenderUin)
                    .SetHeader("编号", "标题", "发起人");
                foreach (var issue in issues)
                    tableBuilder.AddRow(issue.Number.ToString(), issue.Title, issue.User.Login);
                await args.ReplyImageAsync(tableBuilder.Build());
                break;
            default:
                await args.ReplyAsync("错误的子命令!");
                break;
        }
    }
}
