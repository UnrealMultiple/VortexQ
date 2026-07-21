using System.Globalization;
using Lagrange.Core;
using Lagrange.Core.Common.Interface;
using Lagrange.Core.Message;
using Octokit.Webhooks;
using Octokit.Webhooks.Events;
using Octokit.Webhooks.Events.Issues;
using Octokit.Webhooks.Events.PullRequest;
using Octokit.Webhooks.Events.Star;
using Vortex.Bot;
using Vortex.Bot.Utility.Images;

namespace GitHook;

public record StartOperationRecords(string Repo, string User, string Operation, DateTime Time);

public class WebHook : WebhookEventProcessor
{
    private readonly VortexContext _vortex;
    private HashSet<StartOperationRecords> _operations = [];

    private BotContext BotContext => _vortex.BotContext;

    public WebHook(VortexContext vortex) => _vortex = vortex;

    private static bool VerifyRepoFeature(WebhookEvent e, WebhookHeaders headers, out uint[] groups)
    {
        if (Config.Instance.Notices.TryGetValue(e.Repository?.FullName ?? "", out var notice))
        {
            if (notice.Features.Contains(headers.Event))
            {
                groups = notice.Groups;
                return true;
            }
        }
        groups = [];
        return false;
    }

    public static async Task SendGroupMsg(MessageChain chain, uint[] groups, BotContext botContext)
    {
        var tasks = groups.Select(i => botContext.SendGroupMessage(i, chain));
        await Task.WhenAll(tasks);
    }

    protected override async ValueTask ProcessIssuesWebhookAsync(WebhookHeaders headers, IssuesEvent issuesEvent, IssuesAction action, CancellationToken cancellationToken = default)
    {
        if (action.Equals(IssuesAction.Opened) && VerifyRepoFeature(issuesEvent, headers, out var groups))
        {
            var title = issuesEvent.Issue.Title;
            var userName = issuesEvent.Issue.User.Login;
            var repName = issuesEvent.Repository?.FullName;
            var tableBuilder = new TableBuilder()
                .SetTitle("新议题")
                .SetMemberUin(_vortex.BotContext.BotUin)
                .SetHeader("序号", issuesEvent.Issue.Number.ToString())
                .AddRow("发起者", userName)
                .AddRow("仓库", repName ?? "")
                .AddRow("标题", title);
            var builder = new MessageBuilder();
            builder.Image(tableBuilder.Build());
            await SendGroupMsg(builder.Build(), groups, BotContext);
        }
    }

    protected override async ValueTask ProcessStarWebhookAsync(WebhookHeaders headers, StarEvent starEvent, StarAction action, CancellationToken cancellationToken = default)
    {
        if (VerifyRepoFeature(starEvent, headers, out var groups))
        {
            var record = new StartOperationRecords(starEvent.Repository?.FullName ?? "empty",
                starEvent.Sender?.Login ?? "empty",
                CultureInfo.InvariantCulture.TextInfo.ToTitleCase(action),
                DateTime.Now.Date);
            if (_operations.Contains(record))
                return;
            else
                _operations.Add(record);
            var msg = $"用户 {starEvent.Sender?.Login} {CultureInfo.InvariantCulture.TextInfo.ToTitleCase(action)} Start 仓库 {starEvent.Repository?.FullName} 共计({starEvent.Repository?.StargazersCount})个Star";
            var builder = new MessageBuilder();
            builder.Text(msg);
            await SendGroupMsg(builder.Build(), groups, BotContext);
        }
    }

    protected override async ValueTask ProcessPullRequestWebhookAsync(WebhookHeaders headers, PullRequestEvent pullRequestEvent, PullRequestAction action, CancellationToken cancellationToken = default)
    {
        if (action == PullRequestAction.Opened && VerifyRepoFeature(pullRequestEvent, headers, out var groups))
        {
            var title = pullRequestEvent.PullRequest.Title;
            var userName = pullRequestEvent.PullRequest.User.Login;
            var repName = pullRequestEvent.Repository?.FullName;
            var tableBuilder = new TableBuilder()
                .SetTitle("新的拉取请求")
                .SetMemberUin(_vortex.BotContext.BotUin)
                .SetHeader("序号", pullRequestEvent.Number.ToString())
                .AddRow("发起者", userName)
                .AddRow("仓库", repName ?? "")
                .AddRow("标题", title);
            var builder = new MessageBuilder();
            builder.Image(tableBuilder.Build());
            await SendGroupMsg(builder.Build(), groups, BotContext);
        }
    }
}
