using Octokit;
using Vortex.Bot.Attributes;
using Vortex.Bot.Command;

namespace GitHook.Commands;

[Command("approve")]
[CommandType(CommandType.Group)]
[Permission("onebot.approve")]
[HelpText("Approve a pull request")]
public static class ApproveCommand
{
    [Main]
    public static async Task Execute(GroupCommandArgs args)
    {
        if (args.Params.Count >= 1 && int.TryParse(args.Params[0], out var num))
        {
            var state = await TShockPluginRepoClient.Approve(num);
            if (state == PullRequestReviewState.Approved)
                await args.ReplyWithAtAsync($"仓库 {TShockPluginRepoClient.Owner}/{TShockPluginRepoClient.Repo} Pull Request #{num} 被批准合并!");
            else
                await args.ReplyAsync($"批准失败，返回状态码:{state}");
        }
        else
        {
            await args.ReplyWithAtAsync("请输入一个正确的pr号!");
        }
    }
}
