using System.Diagnostics;
using System.Security.Claims;
using Logging;

namespace Middleware;

public class LoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogChannel _channel;
    private readonly LogSummaryService _summary;

    public LoggingMiddleware(
        RequestDelegate next,
        ILogChannel channel,
        LogSummaryService summary)
    {
        _next = next;
        _channel = channel;
        _summary = summary;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        Stopwatch sw = Stopwatch.StartNew();

        await _next(context);

        sw.Stop();

        string? userName = context.User.FindFirst(ClaimTypes.Name)?.Value
                        ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        LogEntry entry = new()
        {
            Timestamp   = DateTimeOffset.UtcNow,
            Method      = context.Request.Method,
            Path        = context.Request.Path,
            StatusCode  = context.Response.StatusCode,
            UserName    = userName,
            IpAddress   = context.Connection.RemoteIpAddress?.ToString(),
            ElapsedMs   = sw.ElapsedMilliseconds,
            TraceId     = context.TraceIdentifier
        };

        // Non-blocking write
        await _channel.WriteAsync(entry);

        // In-memory stats
        string key = $"{context.Request.Method}:{context.Response.StatusCode}";
        _summary.Increment(key);
    }
}
