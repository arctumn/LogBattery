using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace LogBattery;

/// <summary>
/// Extension methods for registering logging middleware.
/// </summary>
public static class MiddlewareExtensions
{
    /// <summary>
    /// Registers all compact logging middleware in the recommended order:
    /// excluded-path suppression, request/response body capture, and Serilog request summary.
    /// <para>
    /// To register each middleware individually (e.g. to control ordering or skip body capture),
    /// use <see cref="UseExcludedPathLogging"/>, <see cref="UseRequestResponseLogging"/>,
    /// and <see cref="UseSerilogCompactRequestLogging"/> instead.
    /// </para>
    /// <example>
    /// <para><b>All-in-one (recommended):</b></para>
    /// <code>
    /// app.UseCompactRequestLogging();
    /// </code>
    /// </example>
    /// <example>
    /// <para><b>Override excluded paths at middleware level:</b></para>
    /// <code>
    /// app.UseCompactRequestLogging(excludedPaths: ["/health", "/ready", "/metrics"]);
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="excludedPaths">Path prefixes to suppress. When <c>null</c>, uses the paths configured in <see cref="LoggingExtensions.AddCompactLogging"/>.</param>
    public static IApplicationBuilder UseCompactRequestLogging(this IApplicationBuilder app, string[]? excludedPaths = null, string? requestResponsePathPrefix = null)
    {
        var paths = excludedPaths ?? LogBatteryConfig.ExcludedPaths;

        app.UseExcludedPathLogging(paths);
        app.UseRequestResponseLogging(requestResponsePathPrefix);
        app.UseSerilogCompactRequestLogging(paths);

        return app;
    }

    /// <summary>
    /// Suppresses all log events (HTTP summary, DB queries, application logs, etc.) for requests
    /// whose path matches the given excluded prefixes.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="excludedPaths">Path prefixes to suppress. When <c>null</c>, uses the paths configured in <see cref="LoggingExtensions.AddCompactLogging"/>.</param>
    public static IApplicationBuilder UseExcludedPathLogging(this IApplicationBuilder app, string[]? excludedPaths = null)
    {
        var paths = excludedPaths ?? LogBatteryConfig.ExcludedPaths;
        app.Use(next => new ExcludedPathLoggingMiddleware(next, paths).InvokeAsync);
        return app;
    }

    /// <summary>
    /// Captures and logs HTTP request/response bodies.
    /// Payloads are truncated to 4 KB.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="pathPrefix">Path prefix to filter. Defaults to <c>/api</c>. Pass <c>null</c> to log all requests.</param>
    public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder app, string? pathPrefix = null)
    {
        app.Use(next =>
        {
            var loggerFactory = app.ApplicationServices.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<RequestResponseLoggingMiddleware>();
            return new RequestResponseLoggingMiddleware(next, logger, pathPrefix).InvokeAsync;
        });
        return app;
    }

    /// <summary>
    /// Adds the Serilog request logging summary line.
    /// Requests whose path matches the given excluded prefixes
    /// are logged at <see cref="LogEventLevel.Verbose"/> (suppressed by the default minimum level).
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="excludedPaths">Path prefixes to suppress. When <c>null</c>, uses the paths configured in <see cref="LoggingExtensions.AddCompactLogging"/>.</param>
    public static IApplicationBuilder UseSerilogCompactRequestLogging(this IApplicationBuilder app, string[]? excludedPaths = null)
    {
        var paths = excludedPaths ?? LogBatteryConfig.ExcludedPaths;

        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
            options.GetLevel = (ctx, _, _) =>
                paths.Any(p => ctx.Request.Path.StartsWithSegments(p))
                    ? LogEventLevel.Verbose
                    : LogEventLevel.Information;
        });

        return app;
    }
}
