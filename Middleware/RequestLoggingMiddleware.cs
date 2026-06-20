using System.Diagnostics;

namespace BarrelMonkeyApi.Middleware;

// Logs every incoming HTTP request along with how long it took.
// Sits at the very start of the pipeline so it catches everything,
// including errors thrown by later middleware.
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();

        // Let the rest of the pipeline run
        await _next(context);

        sw.Stop();

        // Log at Warning level if the response is an error — makes them easy to spot in logs
        var statusCode = context.Response.StatusCode;
        var level = statusCode >= 500 ? LogLevel.Error
                  : statusCode >= 400 ? LogLevel.Warning
                  : LogLevel.Information;

        _logger.Log(level,
            "{Method} {Path} → {StatusCode} ({ElapsedMs}ms)",
            context.Request.Method,
            context.Request.Path,
            statusCode,
            sw.ElapsedMilliseconds);
    }
}

// Extension method so we can do app.UseRequestLogging() in Program.cs
public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
        => builder.UseMiddleware<RequestLoggingMiddleware>();
}
