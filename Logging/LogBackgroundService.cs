using System.Text.Json;
using Microsoft.Extensions.Hosting;

namespace Logging;

public class LogBackgroundService : BackgroundService
{
    private readonly ILogChannel _channel;
    private readonly string _auditPath;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false
    };

    public LogBackgroundService(ILogChannel channel, IHostEnvironment env)
    {
        _channel = channel;
        string dir = Path.Combine(env.ContentRootPath, "logs");
        Directory.CreateDirectory(dir);
        _auditPath = Path.Combine(dir, $"audit-{DateOnly.FromDateTime(DateTime.UtcNow):yyyyMMdd}.json");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (LogEntry entry in _channel.ReadAllAsync(stoppingToken))
        {
            string line = JsonSerializer.Serialize(entry, _jsonOptions);
            await File.AppendAllTextAsync(_auditPath, line + Environment.NewLine, stoppingToken);
        }
    }
}
