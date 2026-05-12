using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Logging;

public class ApiStats
{
    public int Count { get; set; }
    public int Errors { get; set; }
    public int Success { get; set; }
}

public class LogSummaryService : BackgroundService
{
    private readonly ConcurrentDictionary<string, ApiStats> _stats = new();
    private readonly ILogger<LogSummaryService> _logger;

    public LogSummaryService(ILogger<LogSummaryService> logger)
    {
        _logger = logger;
    }

    public void IncrementSuccess(string actionName)
    {
        ApiStats stats = _stats.GetOrAdd(actionName, _ => new ApiStats());
        lock (stats)
        {
            stats.Count++;
            stats.Success++;
        }
    }

    public void IncrementFailed(string actionName)
    {
        ApiStats stats = _stats.GetOrAdd(actionName, _ => new ApiStats());
        lock (stats)
        {
            stats.Count++;
            stats.Errors++;
        }
    }

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
        Dictionary<string, ApiStats> snapshot = new(_stats);
        
        // Round timestamp to nearest 5-minute interval
        DateTimeOffset now = DateTimeOffset.UtcNow;
        int roundedMinutes = (now.Minute / 5) * 5;
        DateTimeOffset roundedTimestamp = new DateTimeOffset(now.Year, now.Month, now.Day, 
            now.Hour, roundedMinutes, 0, TimeSpan.Zero);
        
        string json = JsonSerializer.Serialize(new
        {
            Timestamp = roundedTimestamp,
            PeriodMinutes = 5,
            Stats = snapshot
        }, new JsonSerializerOptions { WriteIndented = false });

        string path = Path.Combine("logs", $"summary-{DateTime.UtcNow:yyyyMMdd}.json");
        Directory.CreateDirectory("logs");
        await File.AppendAllTextAsync(path, json + Environment.NewLine);
        _logger.LogInformation("Summary flushed: {Count} APIs tracked", snapshot.Count);
    }
}
