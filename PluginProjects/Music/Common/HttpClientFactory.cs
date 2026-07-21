using System.Collections.Concurrent;
using System.Net;

namespace Music.Common;

public static class HttpClientFactory
{
    private static readonly ConcurrentDictionary<string, HttpClient> _clients = new();

    public static HttpClient GetOrCreate(string name, Action<HttpClientHandler>? configureHandler = null)
    {
        return _clients.GetOrAdd(name, key =>
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            configureHandler?.Invoke(handler);

            var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            return client;
        });
    }

    public static HttpClient CreateWithCookies(CookieContainer cookieContainer)
    {
        var handler = new HttpClientHandler
        {
            CookieContainer = cookieContainer,
            UseCookies = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };

        return new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }
}
