namespace Logging;

public interface ILogChannel
{
    ValueTask WriteAsync(LogEntry entry, CancellationToken ct = default);
    IAsyncEnumerable<LogEntry> ReadAllAsync(CancellationToken ct = default);
}
