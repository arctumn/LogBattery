# LogBattery

Log Battery Module — provides structured logging with Serilog, compact JSON file sinks, and a built-in browser log viewer.

## Features

- **Structured Logging** — pre-configured Serilog with compact JSON file sinks and console output.
- **Log Enrichment** — automatic enrichment with environment name, machine name, thread ID, and application name.
- **Request Logging** — `UseCompactRequestLogging` middleware with configurable path exclusions (e.g. `/health`, `/alive`).
- **Request/Response Body Capture** — logs request and response bodies for `/api` endpoints (configurable prefix), truncated to 4 KB.
- **Built-in Log Viewer** — browser-based UI at `/logs` for viewing, filtering, and searching log entries with request timeline grouping by trace ID.
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

## Configuration

### Custom log directory and excluded paths

```csharp
builder.AddCompactLogging("MyApp",
    logDirectory: "/var/logs/my-service",
    excludedPaths: ["/health", "/alive", "/status", "/ready"]);
```

### Individual middleware registration

If you need control over middleware ordering or want to skip body capture:

```csharp
app.UseExcludedPathLogging();          // suppress logs for excluded paths
app.UseRequestResponseLogging();       // capture request/response bodies
app.UseSerilogCompactRequestLogging(); // Serilog HTTP request summary
```

### Custom log viewer path

```csharp
app.MapLogViewer("/admin/logs");
```

## Project Structure

| File | Description |
|---|---|
| `LoggingExtensions.cs` | `AddCompactLogging` — Serilog configuration and setup |
| `MiddlewareExtensions.cs` | `UseCompactRequestLogging`, `UseExcludedPathLogging`, `UseRequestResponseLogging`, `UseSerilogCompactRequestLogging` |
| `LogViewerExtensions.cs` | `MapLogViewer` — log viewer API endpoints |
| `ExcludedPathLoggingMiddleware.cs` | Middleware that suppresses logs for excluded path prefixes |
| `RequestResponseLoggingMiddleware.cs` | Middleware that captures and logs HTTP request/response bodies |
| `LogParser.cs` | JSON log file parsing and Serilog template rendering |
| `LogViewerHtml.cs` | Embedded HTML/CSS/JS for the browser-based log viewer |
| `LogBatteryConfig.cs` | Internal shared configuration state |
