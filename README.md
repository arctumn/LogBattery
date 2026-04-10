# LogBattery

Log Battery Module — provides structured logging with Serilog, OpenTelemetry distributed tracing, compact JSON file sinks, and a built-in browser log/trace viewer.

## Features

- **Structured Logging** — pre-configured Serilog with compact JSON file sinks and console output.
- **Log Enrichment** — automatic enrichment with environment name, machine name, thread ID, and application name.
- **Request Logging** — `UseCompactRequestLogging` middleware with configurable path exclusions (e.g. `/health`, `/alive`).
- **Request/Response Body Capture** — logs request and response bodies for all endpoints by default (configurable prefix), truncated to 4 KB.
- **Distributed Tracing** — OpenTelemetry tracing with ASP.NET Core and HttpClient instrumentation, OTLP export, and in-memory trace store.
- **Built-in Log & Trace Viewer** — browser-based UI at `/logs` with two tabs: **Logs** for viewing, filtering, and searching log entries with request timeline grouping; **Traces** with waterfall visualization of spans.
- **Rolling Files** — daily rolling log files with 30-day retention.

## Installation

```
dotnet add package Arctumn.LogBattery
```

## Quick Start

```csharp
// In Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.AddCompactLogging("MyApp");

var app = builder.Build();

app.UseCompactRequestLogging();
app.MapLogViewer();  // browse to /logs
```

### With OpenTelemetry tracing

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddCompactLogging("MyApp")
       .AddLogBatteryTracing();  // in-memory trace viewer

var app = builder.Build();

app.UseCompactRequestLogging();
app.MapLogViewer();  // /logs → Logs tab + Traces tab
```

### With OTLP export (Jaeger, Grafana Tempo, etc.)

```csharp
builder.AddCompactLogging("MyApp")
       .AddLogBatteryTracing(otlpEndpoint: "http://localhost:4317");
```

The OTLP exporter also respects the standard `OTEL_EXPORTER_OTLP_ENDPOINT` environment variable.

## Configuration

### Custom log directory and excluded paths

```csharp
builder.AddCompactLogging("MyApp",
    logDirectory: "/var/logs/my-service",
    excludedPaths: ["/health", "/alive", "/status", "/ready"]);
```

### Tracing options

```csharp
builder.AddLogBatteryTracing(
    serviceName: "my-service",               // defaults to the name from AddCompactLogging
    otlpEndpoint: "http://localhost:4317",   // optional — OTLP collector endpoint
    configureTracing: tracing =>             // optional — further customisation
    {
        tracing.AddSqlClientInstrumentation();
    });
```

### Request/Response body capture

By default, request and response bodies are captured for **all** endpoints. To restrict capture to a specific path prefix:

```csharp
// All-in-one — only capture bodies for /api routes
app.UseCompactRequestLogging(requestResponsePathPrefix: "/api");

// Or individually
app.UseRequestResponseLogging(pathPrefix: "/api");
```

### Individual middleware registration

If you need control over middleware ordering or want to skip body capture:

```csharp
app.UseExcludedPathLogging();          // suppress logs for excluded paths
app.UseRequestResponseLogging();       // capture request/response bodies (all routes)
app.UseSerilogCompactRequestLogging(); // Serilog HTTP request summary
```

### Custom log viewer path

```csharp
app.MapLogViewer("/admin/logs");
```

## Project Structure

```
LogBattery/
├── LogBatteryConfig.cs                        # Internal shared configuration
│
├── Extensions/
│   ├── LoggingExtensions.cs                   # AddCompactLogging()
│   ├── TracingExtensions.cs                   # AddLogBatteryTracing()
│   ├── MiddlewareExtensions.cs                # UseCompactRequestLogging(), ...
│   └── LogViewerExtensions.cs                 # MapLogViewer() + trace APIs
│
├── Middleware/
│   ├── ExcludedPathLoggingMiddleware.cs        # Suppresses logs for excluded paths
│   └── RequestResponseLoggingMiddleware.cs     # Captures HTTP request/response bodies
│
├── Tracing/
│   ├── TraceStore.cs                           # In-memory ring buffer (5000 spans)
│   └── TraceStoreProcessor.cs                  # OTel processor → TraceStore
│
└── Viewer/
    ├── LogParser.cs                            # JSON log parsing + template rendering
    └── LogViewerHtml.cs                        # Embedded HTML/CSS/JS (Logs + Traces UI)
```

| Path | Description |
|---|---|
| `Extensions/LoggingExtensions.cs` | `AddCompactLogging` — Serilog configuration and setup |
| `Extensions/TracingExtensions.cs` | `AddLogBatteryTracing` — OpenTelemetry tracing configuration |
| `Extensions/MiddlewareExtensions.cs` | `UseCompactRequestLogging`, `UseExcludedPathLogging`, `UseRequestResponseLogging`, `UseSerilogCompactRequestLogging` |
| `Extensions/LogViewerExtensions.cs` | `MapLogViewer` — log viewer and trace API endpoints |
| `Middleware/ExcludedPathLoggingMiddleware.cs` | Suppresses logs for excluded path prefixes |
| `Middleware/RequestResponseLoggingMiddleware.cs` | Captures and logs HTTP request/response bodies |
| `Tracing/TraceStore.cs` | In-memory ring buffer for captured trace spans |
| `Tracing/TraceStoreProcessor.cs` | OpenTelemetry processor that feeds spans into the trace store |
| `Viewer/LogParser.cs` | JSON log file parsing and Serilog template rendering |
| `Viewer/LogViewerHtml.cs` | Embedded HTML/CSS/JS for the browser-based log and trace viewer |
| `LogBatteryConfig.cs` | Internal shared configuration state |
