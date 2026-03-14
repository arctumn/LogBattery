using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace LogBattery;

/// <summary>
/// Pushes a Serilog <see cref="LogContext"/> property for requests whose path matches an excluded
/// prefix. The property causes <b>all</b> log events emitted during the request (DB queries,
/// application logs, etc.) to be suppressed by the global Serilog filter configured in
/// <see cref="LoggingExtensions.AddCompactLogging"/>.
/// </summary>
public class ExcludedPathLoggingMiddleware(RequestDelegate next, string[] excludedPaths)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (excludedPaths.Any(p => context.Request.Path.StartsWithSegments(p)))
        {
            using (LogContext.PushProperty(LogBatteryConfig.ExcludedPathProperty, true))
            {
                await next(context);
            }

            return;
        }

        await next(context);
    }
}
