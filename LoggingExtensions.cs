using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace LogBattery;

/// <summary>
/// Extension methods for configuring Serilog with compact structured logging.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Configures Serilog with a compact JSON file sink and console output.
    /// Requests matching <paramref name="excludedPaths"/> (and all logs emitted during those requests,
    /// such as DB queries) are silently dropped.
    /// <example>
    /// <para><b>Default usage:</b></para>
    /// <code>
    /// builder.AddCompactLogging("my-service");
    /// </code>
    /// </example>
    /// <example>
    /// <para><b>Custom log directory:</b></para>
    /// <code>
    /// builder.AddCompactLogging("my-service", logDirectory: "/var/logs/my-service");
    /// </code>
    /// </example>
    /// <example>
    /// <para><b>Custom excluded paths (suppresses all logs including DB queries):</b></para>
    /// <code>
    /// builder.AddCompactLogging("my-service", excludedPaths: ["/health", "/alive", "/status", "/ready"]);
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <param name="serviceName">Application name — used as the log file prefix and Serilog Application property.</param>
    /// <param name="logDirectory">Directory to write log files. Defaults to a "logs" subfolder next to the executable.</param>
    /// <param name="excludedPaths">Path prefixes to suppress. Defaults to /logs, /health, /alive.</param>
    public static WebApplicationBuilder AddCompactLogging(
        this WebApplicationBuilder builder,
        string serviceName,
        string? logDirectory = null,
        string[]? excludedPaths = null)
    {
        LogBatteryConfig.LogFilePrefix = serviceName.ToLowerInvariant()
            .Replace(' ', '-')
            .Replace('_', '-');

        LogBatteryConfig.LogDirectory = logDirectory
            ?? Path.Combine(Directory.GetCurrentDirectory(), "logs");

        if (excludedPaths is { Length: > 0 })
            LogBatteryConfig.ExcludedPaths = excludedPaths;

        var logPath = Path.Combine(LogBatteryConfig.LogDirectory, LogBatteryConfig.LogFilePrefix + "-.log");

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("Application", serviceName)
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(new CompactJsonFormatter(), path: logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                shared: true,
                flushToDiskInterval: TimeSpan.FromSeconds(1))
            .Filter.ByExcluding(e => e.Properties.ContainsKey(LogBatteryConfig.ExcludedPathProperty))
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Information)
            .CreateLogger();

        builder.Host.UseSerilog();

        return builder;
    }
}
