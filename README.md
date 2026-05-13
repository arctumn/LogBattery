# LogBattery

Log Battery Module — provides structured logging with Serilog, compact JSON file sinks, and a built-in browser log viewer.

## Features

- **Structured Logging** — pre-configured Serilog with compact JSON file sinks and console output.
- **Log Enrichment** — automatic enrichment with environment name, machine name, thread ID, and application name.
- **Request Logging** — `UseCompactRequestLogging` middleware with configurable path exclusions (e.g. `/health`, `/alive`).
- **Request/Response Body Capture** — logs request and response bodies for all endpoints by default (configurable prefix), truncated to 4 KB.
- **Built-in Log Viewer** — browser-based UI at `/logs` for viewing, filtering, and searching log entries by file, level, type (HTTP / Application), date range, and free-text.
- **Pluggable Viewer Authentication** — `RequireLogBatteryAuth(scheme)` lets you protect the viewer with any ASP.NET Core authentication scheme (API key, JWT bearer, cookies, OAuth, or a custom `AuthenticationHandler`).
- **Rolling Files** — daily rolling log files with 30-day retention.

## Installation

```
dotnet add package Arctumn.LogBattery
```

## Quick Start

All extension methods live under the `Arctumn.LogBattery.Extensions` namespace:

```csharp
using Arctumn.LogBattery.Extensions;

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

### Protecting the log viewer with authentication

The viewer is open by default. To require authentication, register your own
`AuthenticationHandler` and call `RequireLogBatteryAuth` before `MapLogViewer`:

```csharp
// 1. Register a custom authentication handler (e.g. an API-key handler)
services.AddAuthentication(ApiKeyAuthenticationHandler.SchemeName)
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationHandler.SchemeName, _ => { });
services.AddAuthorization();

// 2. Wire the auth pipeline
app.UseAuthentication();
app.UseAuthorization();

// 3. Tell the log viewer which scheme to enforce, then map it
app.RequireLogBatteryAuth(ApiKeyAuthenticationHandler.SchemeName);
app.MapLogViewer();

// Fluent equivalent:
app.RequireLogBatteryAuth(ApiKeyAuthenticationHandler.SchemeName)
   .MapLogViewer();
```

Any ASP.NET Core auth scheme works — JWT bearer, cookies, OAuth, or a custom
handler — the parameter is just the scheme name. See [Sample API](#sample-api)
for a runnable example that uses HTTP Basic auth and triggers the browser's
native login prompt.

## Sample API

A runnable demo lives under `samples/Arctumn.LogBattery.Sample.Api/`. It targets
.NET 10 and exercises every LogBattery feature against a small set of dummy
endpoints, plus Basic-auth protection on the viewer. The project sets
`<IsPackable>false</IsPackable>` and is excluded from the NuGet package via
`Compile Remove="samples/**"` in `Arctumn.LogBattery.csproj`.

### Running

```bash
cd samples/Arctumn.LogBattery.Sample.Api
dotnet run
```

The app listens on `http://localhost:5080` and redirects `/` to `/logs`.

### Endpoints

The `/api/...` endpoints are throwaway test surfaces — left open so you can hit
them with `curl` without juggling credentials. Only the LogBattery viewer
routes (`/logs` and `/logs/api/...`) are gated by Basic auth.

| Method | Path | Behaviour |
|---|---|---|
| `GET`  | `/api/orders` | Returns 5 dummy orders + `Information` log |
| `GET`  | `/api/orders/{id}` | Returns one order; logs `Warning` if the id is out of range |
| `POST` | `/api/orders` | Creates an order; logs `Warning` for quantities > 50 |
| `GET`  | `/api/error` | Throws to produce an `Error` log entry |
| `GET`  | `/api/slow?ms=N` | `Task.Delay(N)` to generate a slow request log |
| `GET`  | `/api/burst?count=N` | Emits N logs in one shot — 70% Info, 20% Warn, 10% Error |

A `LogSimulator : BackgroundService` also emits 3–8 random logs every 5 seconds
(operations like `payment.process`, `user.signin`, etc., enriched with `UserId`,
`Region`, `Elapsed`) so the viewer keeps filling even with no inbound traffic.

### Viewer authentication

The sample wires `RequireLogBatteryAuth` to a Basic-auth handler under
`Auth/BasicAuthenticationHandler.cs`. Visiting `/logs` triggers the browser's
native login prompt:

| Field | Default value |
|---|---|
| Username | `admin` |
| Password | `logbattery` |

Override either via `appsettings.json`:

```json
"LogViewerUser": {
  "Username": "admin",
  "Password": "logbattery"
}
```

Only the viewer routes (`/logs`, `/logs/api/...`) are protected — the sample's
`/api/...` endpoints stay open. Basic auth sends credentials base64-encoded, so
use HTTPS in any non-local environment.

## Project Structure

```
LogBattery/
├── Arctumn.LogBattery.csproj                   # NuGet package project
├── LogBatteryConfig.cs                         # Internal shared configuration
│
├── Extensions/
│   ├── LoggingExtensions.cs                    # AddCompactLogging()
│   ├── MiddlewareExtensions.cs                 # UseCompactRequestLogging(), ...
│   ├── LogViewerExtensions.cs                  # MapLogViewer()
│   └── LogBatteryAuthExtensions.cs             # RequireLogBatteryAuth()
│
├── Middleware/
│   ├── ExcludedPathLoggingMiddleware.cs        # Suppresses logs for excluded paths
│   └── RequestResponseLoggingMiddleware.cs     # Captures HTTP request/response bodies
│
├── Viewer/
│   ├── LogParser.cs                            # JSON log parsing + template rendering
│   └── LogViewerHtml.cs                        # Embedded HTML/CSS/JS for the viewer
│
└── samples/
    └── Arctumn.LogBattery.Sample.Api/           # Demo API (IsPackable=false, excluded from NuGet)
        ├── Program.cs                           # Endpoints + AddCompactLogging
        ├── LogSimulator.cs                      # BackgroundService that emits log bursts
        └── Auth/BasicAuthenticationHandler.cs   # HTTP Basic handler protecting /logs
```

| Path | Description |
|---|---|
| `Extensions/LoggingExtensions.cs` | `AddCompactLogging` — Serilog configuration and setup |
| `Extensions/MiddlewareExtensions.cs` | `UseCompactRequestLogging`, `UseExcludedPathLogging`, `UseRequestResponseLogging`, `UseSerilogCompactRequestLogging` |
| `Extensions/LogViewerExtensions.cs` | `MapLogViewer` — log viewer and JSON API |
| `Extensions/LogBatteryAuthExtensions.cs` | `RequireLogBatteryAuth` — protects the viewer with an ASP.NET Core authentication scheme |
| `Middleware/ExcludedPathLoggingMiddleware.cs` | Suppresses logs for excluded path prefixes |
| `Middleware/RequestResponseLoggingMiddleware.cs` | Captures and logs HTTP request/response bodies |
| `Viewer/LogParser.cs` | JSON log file parsing and Serilog template rendering |
| `Viewer/LogViewerHtml.cs` | Embedded HTML/CSS/JS for the browser-based log viewer |
| `LogBatteryConfig.cs` | Internal shared configuration state |
