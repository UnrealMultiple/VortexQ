using Music.Models;

namespace Music.Abstractions;

public interface IMusicProvider : IDisposable
{
    string Name { get; }

    MusicSource Source { get; }

    Task<IReadOnlyList<SongInfo>> SearchAsync(string keyword, int limit = 10, CancellationToken cancellationToken = default);

    Task<string> GetPlayUrlAsync(string songId, CancellationToken cancellationToken = default);

    Task<SongInfo?> GetSongDetailAsync(string songId, CancellationToken cancellationToken = default);

    Task<PlaylistInfo?> GetPlaylistAsync(string playlistId, CancellationToken cancellationToken = default);
}

public interface IAuthenticatableProvider : IMusicProvider
{
    bool IsAuthenticated { get; }

    event EventHandler<AuthStateChangedEventArgs>? AuthStateChanged;

    Task<bool> RefreshTokenAsync(CancellationToken cancellationToken = default);
}

public class AuthStateChangedEventArgs : EventArgs
{
    public bool IsAuthenticated { get; set; }
    public string? Message { get; set; }
}
