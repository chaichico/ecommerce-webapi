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

        // Extract action name from endpoint metadata
        string? actionName = GetActionName(context);

        LogEntry entry = new()
        {
            Timestamp   = DateTimeOffset.UtcNow,
            Method      = context.Request.Method,
            Path        = context.Request.Path,
            StatusCode  = context.Response.StatusCode,
            UserName    = userName,
            IpAddress   = context.Connection.RemoteIpAddress?.ToString(),
            ElapsedMs   = sw.ElapsedMilliseconds,
            TraceId     = context.TraceIdentifier,
            ActionName  = actionName
        };

        // Non-blocking write
        await _channel.WriteAsync(entry);

        // In-memory stats - track success/failed per action
        if (actionName != null)
        {
            if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 400)
            {
                _summary.IncrementSuccess(actionName);
            }
            else
            {
                _summary.IncrementFailed(actionName);
            }
        }
    }

    private static string? GetActionName(HttpContext context)
    {
        Endpoint? endpoint = context.GetEndpoint();
        if (endpoint == null) return null;

        // Try to get controller and action names
        string? controllerName = endpoint.Metadata
            .GetMetadata<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>()
            ?.ControllerName;
        
        string? actionMethodName = endpoint.Metadata
            .GetMetadata<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>()
            ?.ActionName;

        if (controllerName != null && actionMethodName != null)
        {
            return $"API.{controllerName}.{actionMethodName}";
        }

        return null;
    }
}
