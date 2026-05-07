namespace Logging;

public class LogEntry
{
    public DateTimeOffset Timestamp { get; init; }
    public string Method { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public int StatusCode { get; init; }
    public string? UserName { get; init; }
    public string? IpAddress { get; init; }
    public long ElapsedMs { get; init; }
    public string? TraceId { get; init; }
    public string? ActionName { get; init; }
}
