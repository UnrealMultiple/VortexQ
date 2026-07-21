namespace Bilibili.Models;

public class VideoInfo
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Pic { get; init; } = string.Empty;
    public string OwnerName { get; init; } = string.Empty;
    public long Aid { get; init; }
    public DateTime PublishTime { get; init; }
    public int View { get; init; }
    public int Like { get; init; }
    public int Coin { get; init; }
    public int Favorite { get; init; }
    public int Share { get; init; }
    public int Danmaku { get; init; }
    public int Reply { get; init; }
}
