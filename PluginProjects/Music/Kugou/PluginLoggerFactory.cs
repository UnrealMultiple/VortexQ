using Microsoft.Extensions.Logging;

namespace Music.Kugou;

internal sealed class PluginLoggerFactory : ILoggerFactory
{
    private readonly ILogger _inner;

    public PluginLoggerFactory(ILogger inner) => _inner = inner;

    public ILogger CreateLogger(string categoryName) => new PluginLogger(_inner, categoryName);
    public void AddProvider(ILoggerProvider provider) { }
    public void Dispose() { }
}

internal sealed class PluginLogger : ILogger
{
    private readonly ILogger _inner;
    private readonly string _category;

    public PluginLogger(ILogger inner, string category)
    {
        _inner = inner;
        _category = category;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        _inner.Log(logLevel, eventId, state, exception,
            (s, e) => $"[{_category}] {formatter(s, e)}");
    }
}
