using System.Text.Json.Serialization;
using Octokit.Webhooks;
using Vortex.Bot.Configuration;
using Vortex.Bot.Events;

namespace GitHook;

public class Config : JsonConfigBase<Config>
{
    public override string FileName => "GitHook";

    [JsonPropertyName("启用")]
    public bool Enable { get; set; } = true;

    [JsonPropertyName("路由")]
    public string Path { get; set; } = "/update/";

    [JsonPropertyName("端口")]
    public int Port { get; set; } = 7000;

    [JsonPropertyName("私人令牌")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("通知群")]
    public Dictionary<string, RepoNotice> Notices { get; set; } = [];

    public override void OnReloaded(ReloadEventArgs args)
    {
        Console.WriteLine($"[GitHook] 配置已重载，触发者: {args.TriggerUin}");
    }

    public override void SetDefault()
    {
        Notices["Owner/Repo"] = new RepoNotice
        {
            Groups = [123456789],
            Features = [WebhookEventType.Star, WebhookEventType.PullRequest, WebhookEventType.Issues]
        };
    }
}

public class RepoNotice
{
    [JsonPropertyName("通知群")]
    public uint[] Groups { get; set; } = [];

    [JsonPropertyName("功能")]
    public string[] Features { get; set; } = [];
}
