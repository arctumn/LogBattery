using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace LogBattery;

/// <summary>
/// Extension methods for configuring OpenTelemetry distributed tracing.
/// </summary>
public static class TracingExtensions
{
    /// <summary>
    /// Configures OpenTelemetry tracing with ASP.NET Core and HttpClient instrumentation.
    /// Captured traces are stored in memory and displayed in the built-in log viewer UI.
    /// When an OTLP endpoint is configured (via parameter or the <c>OTEL_EXPORTER_OTLP_ENDPOINT</c>
    /// environment variable), traces are also exported via OTLP.
    /// <example>
    /// <para><b>Default usage (in-memory viewer only):</b></para>
    /// <code>
    /// builder.AddCompactLogging("my-service")
    ///        .AddLogBatteryTracing();
    /// </code>
    /// </example>
    /// <example>
    /// <para><b>With OTLP export:</b></para>
    /// <code>
    /// builder.AddCompactLogging("my-service")
    ///        .AddLogBatteryTracing(otlpEndpoint: "http://localhost:4317");
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <param name="serviceName">Service name for the OTel resource. Defaults to the name configured in <see cref="LoggingExtensions.AddCompactLogging"/>.</param>
    /// <param name="otlpEndpoint">OTLP collector endpoint. When <c>null</c>, OTLP export is added using the standard <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> environment variable (if set).</param>
    /// <param name="configureTracing">Optional callback to further customise the <see cref="TracerProviderBuilder"/> (e.g. add extra instrumentation).</param>
    public static WebApplicationBuilder AddLogBatteryTracing(
        this WebApplicationBuilder builder,
        string? serviceName = null,
        string? otlpEndpoint = null,
        Action<TracerProviderBuilder>? configureTracing = null)
    {
        var resolvedName = serviceName ?? LogBatteryConfig.ServiceName ?? LogBatteryConfig.LogFilePrefix;

        builder.Services.AddSingleton<TraceStore>();

        builder.Services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing
                    .SetResourceBuilder(ResourceBuilder.CreateDefault()
                        .AddService(resolvedName))
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddProcessor<TraceStoreProcessor>();

                if (otlpEndpoint != null)
                    tracing.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
                else
                    tracing.AddOtlpExporter();

                configureTracing?.Invoke(tracing);
            });

        return builder;
    }
}
