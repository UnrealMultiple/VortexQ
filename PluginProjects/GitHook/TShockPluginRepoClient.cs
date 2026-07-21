using Octokit;

namespace GitHook;

internal class TShockPluginRepoClient
{
    private static readonly GitHubClient _client = new(new ProductHeaderValue("XocMat")) { Credentials = new(Config.Instance.Token) };

    public const string Owner = "UnrealMultiple";

    public const string Repo = "TShockPlugin";

    public static async Task<StringEnum<PullRequestReviewState>> Approve(int id)
    {
        var review = new PullRequestReviewCreate() { Event = PullRequestReviewEvent.Approve };
        var result = await _client.PullRequest.Review.Create(Owner, Repo, id, review);
        return result.State;
    }

    public static async Task<StringEnum<PullRequestReviewState>> RequestChange(int id)
    {
        var review = new PullRequestReviewCreate() { Event = PullRequestReviewEvent.RequestChanges };
        var result = await _client.PullRequest.Review.Create(Owner, Repo, id, review);
        return result.State;
    }

    public static async Task<IReadOnlyList<Issue>> GetIssueOpen()
    {
        var opt = new RepositoryIssueRequest() { State = ItemStateFilter.Open };
        return await _client.Issue.GetAllForRepository(Owner, Repo, opt);
    }

    public static async Task<Issue> GetIssueNumber(int id) => await _client.Issue.Get(Owner, Repo, id);

    public static async Task<Issue> CloseIssue(int id)
    {
        var opt = new IssueUpdate() { State = ItemState.Closed };
        return await _client.Issue.Update(Owner, Repo, id, opt);
    }

    public static async Task<Octokit.PullRequest> ClosePullRequest(int id)
    {
        var opt = new PullRequestUpdate() { State = ItemState.Closed };
        return await _client.PullRequest.Update(Owner, Repo, id, opt);
    }

    public static async Task<IssueComment> ReplyIssue(int id, string text)
        => await _client.Issue.Comment.Create(Owner, Repo, id, text);

    public static async Task<bool> PullRequestMerger(int id)
        => await _client.PullRequest.Merged(Owner, Repo, id);

    public static async Task<IReadOnlyList<Octokit.PullRequest>> GetPullRequestOpen()
    {
        var review = new PullRequestRequest() { State = ItemStateFilter.Open };
        return await _client.PullRequest.GetAllForRepository(Owner, Repo, review);
    }

    public static async Task<Octokit.PullRequest> GetPullRequestNumber(int id)
        => await _client.PullRequest.Get(Owner, Repo, id);
}
