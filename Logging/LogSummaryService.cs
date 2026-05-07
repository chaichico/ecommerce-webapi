using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Logging;

public class LogSummaryService : BackgroundService
{
    private readonly ConcurrentDictionary<string, int> _counts = new();
    private readonly ILogger<LogSummaryService> _logger;

    public LogSummaryService(ILogger<LogSummaryService> logger)
    {
        _logger = logger;
    }

    public void Increment(string key) => _counts.AddOrUpdate(key, 1, (_, v) => v + 1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(TimeSpan.FromMinutes(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await FlushAsync();
        }
    }

    private async Task FlushAsync()
    {
        Dictionary<string, int> snapshot = new(_counts);
        string json = JsonSerializer.Serialize(new
        {
            Timestamp = DateTimeOffset.UtcNow,
            Stats = snapshot
        });

        string path = Path.Combine("logs", $"summary-{DateTime.UtcNow:yyyyMMdd}.json");
        Directory.CreateDirectory("logs");
        await File.AppendAllTextAsync(path, json + Environment.NewLine);
        _logger.LogInformation("Summary flushed: {Count} keys", snapshot.Count);
    }
}
