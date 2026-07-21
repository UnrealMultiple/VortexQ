using Octokit;
using Vortex.Bot.Attributes;
using Vortex.Bot.Command;
using Vortex.Bot.Utility.Images;

namespace GitHook.Commands;

[Command("pr")]
[CommandType(CommandType.Group)]
[Permission("onebot.pr")]
[HelpText("管理Pull Request")]
public static class PullRequestManagerCommand
{
    [Main]
    [Flexible]
    public static async Task Execute(GroupCommandArgs args)
    {
        if (args.Params.Count < 1)
        {
            await args.ReplyAsync($"语法错误，正确语法: \n{args.CommandPrefix}{args.CommandName} list\n" +
                $"{args.CommandPrefix}{args.CommandName} see [编号]\n" +
                $"{args.CommandPrefix}{args.CommandName} close [编号]");
            return;
        }
        switch (args.Params[0].ToLower())
        {
            case "see":
                if (args.Params.Count > 1 && int.TryParse(args.Params[1], out var num))
                {
                    var pr = await TShockPluginRepoClient.GetPullRequestNumber(num);
                    var buffer = await GithubPageUtils.ScreenPage($"{pr.HtmlUrl}/files", "#hide-file-tree-button");
                    await args.ReplyImageAsync(buffer);
                }
                else await args.ReplyWithAtAsync("请输入一个正确的PR编号!");
                break;
            case "close":
                if (args.Params.Count > 1 && int.TryParse(args.Params[1], out var id))
                {
                    var pr = await TShockPluginRepoClient.ClosePullRequest(id);
                    await args.ReplyAsync(pr.State == ItemState.Closed ? $"Pull Request #{id} 关闭成功!" : "未知错误，关闭失败。");
                }
                else await args.ReplyWithAtAsync("请输入一个正确的PR编号!");
                break;
            case "list":
                var prs = await TShockPluginRepoClient.GetPullRequestOpen();
                var tableBuilder = TableBuilder.Create()
                    .SetTitle("正在进行的Pull Request")
                    .SetLineMaxTextLength(60)
                    .SetMemberUin(args.SenderUin)
                    .SetHeader("编号", "标题", "发起人");
                foreach (var pr in prs)
                    tableBuilder.AddRow(pr.Number.ToString(), pr.Title, pr.User.Login);
                await args.ReplyImageAsync(tableBuilder.Build());
                break;
            default:
                await args.ReplyAsync("错误的子命令!");
                break;
        }
    }
}
