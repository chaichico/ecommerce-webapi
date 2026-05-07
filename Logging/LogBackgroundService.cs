using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Logging;

public class LogBackgroundService : BackgroundService
{
    private readonly ILogChannel _channel;
    private readonly ILogger<LogBackgroundService> _logger;

    public LogBackgroundService(ILogChannel channel, ILogger<LogBackgroundService> logger)
    {
        _channel = channel;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (LogEntry entry in _channel.ReadAllAsync(stoppingToken))
        {
            _logger.LogInformation(
                "{ActionName} {Method} {Path} → {StatusCode} | TraceId: {TraceId}, IP: {IpAddress}, User: {UserName}, Elapsed: {ElapsedMs}ms",
                entry.ActionName ?? "Unknown", entry.Method, entry.Path, entry.StatusCode, 
                entry.TraceId, entry.IpAddress, entry.UserName, entry.ElapsedMs);
        }
    }
}
