using Microsoft.Extensions.Logging;
namespace DataMonitor.App.Services;

public sealed class ReleaseLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new ReleaseLogger(categoryName);
    public void Dispose() { }
    private sealed class ReleaseLogger(string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => level >= LogLevel.Warning;
        public void Log<TState>(LogLevel level, EventId id, TState state, Exception? ex, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(level)) Infrastructure.Diagnostics.ReleaseLog.Write(category + ": " + formatter(state, ex), ex);
        }
    }
}
