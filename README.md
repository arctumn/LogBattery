# LogBattery

Log Battery Module — provides structured logging with Serilog, compact JSON file sinks, and a built-in browser log viewer.

## Features

- **Structured Logging** — pre-configured Serilog with compact JSON file sinks and console output.
- **Log Enrichment** — automatic enrichment with environment name, machine name, thread ID, and application name.
- **Request Logging** — `UseCompactRequestLogging` middleware with configurable path exclusions (e.g. `/health`, `/alive`).
- **Built-in Log Viewer** — browser-based UI at `/logs` for viewing, filtering, and searching log entries with request timeline grouping by trace ID.
- **Rolling Files** — daily rolling log files with 30-day retention.

## Installation

```
dotnet add package LogBattery
```

## Usage

```csharp
// In Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.AddCompactLogging("MyApp");

var app = builder.Build();

app.UseCompactRequestLogging();
app.MapLogViewer();  // browse to /logs
```
