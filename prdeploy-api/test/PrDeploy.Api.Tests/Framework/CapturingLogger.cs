using Microsoft.Extensions.Logging;

namespace PrDeploy.Api.Tests.Framework;

/// <summary>One captured log call, exposing the structured template and properties for assertions.</summary>
/// <param name="Level">Severity of the entry.</param>
/// <param name="Template">Unformatted message template, which must not contain interpolated values.</param>
/// <param name="Message">Fully rendered message.</param>
/// <param name="Properties">Named structured properties passed to the log call.</param>
/// <param name="Exception">Exception attached to the entry, if any.</param>
public sealed record LogEntry(
    LogLevel Level,
    string Template,
    string Message,
    IReadOnlyDictionary<string, object?> Properties,
    Exception? Exception);

/// <summary>In-memory logger that records every entry so tests can assert on structured logging.</summary>
/// <typeparam name="T">Category type of the logger.</typeparam>
public sealed class CapturingLogger<T> : ILogger<T>
{
    private readonly List<LogEntry> _entries = new();

    /// <summary>Gets the entries captured so far.</summary>
    public IReadOnlyList<LogEntry> Entries => _entries;

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <summary>Records the entry along with its structured properties.</summary>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var properties = new Dictionary<string, object?>();
        var template = string.Empty;
        if (state is IEnumerable<KeyValuePair<string, object?>> pairs)
        {
            foreach (var pair in pairs)
            {
                if (pair.Key == "{OriginalFormat}")
                {
                    template = pair.Value?.ToString() ?? string.Empty;
                }
                else
                {
                    properties[pair.Key] = pair.Value;
                }
            }
        }

        _entries.Add(new LogEntry(logLevel, template, formatter(state, exception), properties, exception));
    }
}
