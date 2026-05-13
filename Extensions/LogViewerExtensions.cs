using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Arctumn.LogBattery.Extensions;

/// <summary>
/// Extension methods for mapping the built-in log viewer UI and API.
/// </summary>
public static class LogViewerExtensions
{
    /// <summary>
    /// Maps a browser-based log viewer UI and its JSON API at <paramref name="basePath"/>.
    /// <para>
    /// By default the viewer is open. To require authentication, call
    /// <see cref="LogBatteryAuthExtensions.RequireLogBatteryAuth"/> beforehand.
    /// </para>
    /// <example>
    /// <para><b>Default path (/logs), open access:</b></para>
    /// <code>
    /// app.MapLogViewer();
    /// </code>
    /// </example>
    /// <example>
    /// <para><b>Custom path:</b></para>
    /// <code>
    /// app.MapLogViewer("/admin/logs");
    /// </code>
    /// </example>
    /// <example>
    /// <para><b>Protected with a custom authentication scheme:</b></para>
    /// <code>
    /// app.RequireLogBatteryAuth(ApiKeyAuthenticationHandler.SchemeName)
    ///    .MapLogViewer();
    /// </code>
    /// </example>
    /// </summary>
    public static IEndpointRouteBuilder MapLogViewer(this IEndpointRouteBuilder app, string basePath = "/logs")
    {
        var logDir = LogBatteryConfig.LogDirectory;
        var prefix = LogBatteryConfig.LogFilePrefix;
        var pattern = prefix + "-*.log";

        // --- File list ---
        app.MapGet(basePath + "/api/files", () =>
        {
            if (!Directory.Exists(logDir))
                return Results.Ok(Array.Empty<object>());

            var files = Directory.GetFiles(logDir, pattern)
                .Select(f => new
                {
                    name = Path.GetFileName(f),
                    date = Path.GetFileName(f).Replace(prefix + "-", "").Replace(".log", ""),
                    size = new FileInfo(f).Length
                })
                .OrderByDescending(f => f.date)
                .ToList();

            return Results.Ok(files);
        }).WithLogBatteryAuth();

        // --- Log entries (paginated) ---
        app.MapGet(basePath + "/api/entries", (string? file, string? levels, string? search, int? page, int? pageSize) =>
        {
            if (!Directory.Exists(logDir))
                return Results.Ok(new { entries = Array.Empty<object>(), page = 1, pageSize = 100, totalCount = 0, totalPages = 0 });

            var targetFile = string.IsNullOrEmpty(file)
                ? Directory.GetFiles(logDir, pattern).OrderByDescending(f => f).FirstOrDefault()
                : Path.Combine(logDir, file);

            if (targetFile == null || !File.Exists(targetFile))
                return Results.Ok(new { entries = Array.Empty<object>(), page = 1, pageSize = 100, totalCount = 0, totalPages = 0 });

            var currentPage = Math.Max(1, page ?? 1);
            var size = Math.Clamp(pageSize ?? 100, 10, 500);

            var lines = LogParser.ReadAllLines(targetFile);

            var levelSet = string.IsNullOrWhiteSpace(levels)
                ? null
                : new HashSet<string>(
                    levels.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                    StringComparer.OrdinalIgnoreCase);

            var filtered = lines
                .Select(LogParser.ParseJsonLogLine)
                .Where(e => e != null)
                .Where(e => levelSet == null || levelSet.Contains(e!.Level))
                .Where(e => string.IsNullOrEmpty(search) ||
                    (e!.Message?.Contains(search, StringComparison.OrdinalIgnoreCase) == true) ||
                    (e!.RequestPath?.Contains(search, StringComparison.OrdinalIgnoreCase) == true))
                .ToList();

            var totalCount = filtered.Count;
            var totalPages = (int)Math.Ceiling((double)totalCount / size);
            currentPage = Math.Min(currentPage, Math.Max(1, totalPages));

            var entries = filtered
                .Skip((currentPage - 1) * size)
                .Take(size)
                .Select(e => new
                {
                    timestamp = e!.Timestamp,
                    level = e.Level,
                    message = e.Message,
                    requestMethod = e.RequestMethod,
                    requestPath = e.RequestPath,
                    statusCode = e.StatusCode,
                    elapsed = e.Elapsed,
                    machineName = e.MachineName,
                    threadId = e.ThreadId,
                    exception = e.Exception,
                    traceId = e.TraceId,
                    properties = e.Properties
                })
                .ToList();

            return Results.Ok(new
            {
                entries,
                page = currentPage,
                pageSize = size,
                totalCount,
                totalPages
            });
        }).WithLogBatteryAuth();

        // --- UI ---
        app.MapGet(basePath, () => Results.Content(LogViewerHtml.GetHtml(), "text/html"))
           .WithLogBatteryAuth();

        return app;
    }
}
