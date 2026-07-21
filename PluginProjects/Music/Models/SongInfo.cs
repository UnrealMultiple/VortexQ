namespace Music.Models;

public sealed record SongInfo
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public IReadOnlyList<string> Artists { get; init; } = [];

    public string Album { get; init; } = string.Empty;

    public string AlbumCover { get; init; } = string.Empty;

    public string PlayUrl { get; init; } = string.Empty;

    public int Duration { get; init; }

    public required MusicSource Source { get; init; }

    public string PageUrl => Source switch
    {
        MusicSource.QQMusic => $"https://y.qq.com/n/yqq/song/{Id}.html",
        MusicSource.NetEase => $"https://music.163.com/song?id={Id}",
        _ => string.Empty
    };

    public string ArtistString => string.Join(",", Artists);

    public string DisplayText => $"{Name} - {ArtistString}";
}

public enum MusicSource
{
    QQMusic,
    NetEase
}

public sealed record PlaylistInfo
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string Cover { get; init; } = string.Empty;
    public IReadOnlyList<SongInfo> Songs { get; init; } = Array.Empty<SongInfo>();
    public int SongCount => Songs.Count;
    public string Creator { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}
