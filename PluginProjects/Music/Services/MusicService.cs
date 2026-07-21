using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Music.Abstractions;
using Music.Models;

namespace Music.Services;

public sealed class MusicService(ILogger logger) : IDisposable
{
    private readonly ConcurrentDictionary<MusicSource, IMusicProvider> _providers = new();
    private readonly ILogger _logger = logger;

    public void RegisterProvider(IMusicProvider provider)
    {
        if (!_providers.TryAdd(provider.Source, provider))
        {
            _logger.LogWarning("Provider for {Source} already registered", provider.Source);
        }
        else
        {
            _logger.LogInformation("Registered provider: {ProviderName}", provider.Name);
        }
    }

    public IMusicProvider? GetProvider(MusicSource source)
    {
        _providers.TryGetValue(source, out var provider);
        return provider;
    }

    public T? GetProvider<T>(MusicSource source) where T : class, IMusicProvider
    {
        if (_providers.TryGetValue(source, out var provider) && provider is T typedProvider)
        {
            return typedProvider;
        }
        _logger.LogWarning("No provider of type {Type} found for {Source}", typeof(T).Name, source);
        return null;
    }

    public async Task<IReadOnlyList<SongInfo>> SearchAllAsync(string keyword, MusicSource source, int limit = 10)
    {
        if (_providers.TryGetValue(source, out var provider))
        {
            try
            {
                return await provider.SearchAsync(keyword, limit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Search failed for {Source}", source);
                return [];
            }
        }
        _logger.LogWarning("No provider found for {Source}", source);
        return [];
    }

    public async Task<string?> GetPlayUrlAsync(string songId, MusicSource source)
    {
        if (_providers.TryGetValue(source, out var provider))
        {
            try
            {
                return await provider.GetPlayUrlAsync(songId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get play URL for {Source}", source);
                return null;
            }
        }
        _logger.LogWarning("No provider found for {Source}", source);
        return null;
    }

    public async Task<IReadOnlyList<SongInfo>> SearchBySourceAsync(
        string keyword,
        MusicSource source,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        if (_providers.TryGetValue(source, out var provider))
        {
            try
            {
                return await provider.SearchAsync(keyword, limit, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Search failed for {Source}", source);
                return [];
            }
        }

        return [];
    }

    public async Task<Dictionary<MusicSource, IReadOnlyList<SongInfo>>> SearchAllAsync(
        string keyword, 
        int limit = 10, 
        CancellationToken cancellationToken = default)
    {
        var tasks = _providers.Select(async p =>
        {
            try
            {
                var songs = await p.Value.SearchAsync(keyword, limit, cancellationToken);
                return (Source: p.Key, Songs: songs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Search failed for {Source}", p.Key);
                return (Source: p.Key, Songs: (IReadOnlyList<SongInfo>)[]);
            }
        });

        var results = await Task.WhenAll(tasks);
        return results.ToDictionary(r => r.Source, r => r.Songs);
    }

    public async Task<IReadOnlyList<SongInfo>> SearchAggregatedAsync(
        string keyword, 
        int limit = 10, 
        CancellationToken cancellationToken = default)
    {
        var results = await SearchAllAsync(keyword, limit, cancellationToken);
        return [.. results.SelectMany(r => r.Value).Take(limit)];
    }

    public void Dispose()
    {
        foreach (var provider in _providers.Values)
        {
            provider.Dispose();
        }
    }
}
