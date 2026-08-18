using System.Collections.Concurrent;

namespace RetailCommerce.Api.Common;

/// <summary>Minimal rolling-file logger — no external package needed. Console output (the
/// framework's default) is invisible once the app runs as a Windows Service or its terminal is
/// closed, which made diagnosing a reported 500 on a deployed machine impossible: the client only
/// ever sees GlobalExceptionHandler's generic "Something went wrong" body. This writes Warning+
/// (so routine request-per-line Information logs don't fill the disk) to one file per day under
/// `logs/` next to the executable, so a full exception + stack trace survives past the request
/// that triggered it.</summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _logDirectory;
    private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();
    private readonly object _writeLock = new();

    public FileLoggerProvider(string logDirectory)
    {
        _logDirectory = logDirectory;
        Directory.CreateDirectory(_logDirectory);
    }

    public ILogger CreateLogger(string categoryName) =>
        _loggers.GetOrAdd(categoryName, name => new FileLogger(name, this));

    internal void Write(string message)
    {
        var path = Path.Combine(_logDirectory, $"app-{DateTime.UtcNow:yyyy-MM-dd}.log");
        lock (_writeLock)
        {
            File.AppendAllText(path, message);
        }
    }

    public void Dispose() => _loggers.Clear();

    private sealed class FileLogger(string categoryName, FileLoggerProvider provider) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            var line = $"{DateTimeOffset.UtcNow:O} [{logLevel}] {categoryName}: {formatter(state, exception)}";
            if (exception is not null) line += Environment.NewLine + exception;
            provider.Write(line + Environment.NewLine);
        }
    }
}
