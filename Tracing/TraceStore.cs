using System.Collections.Concurrent;
using System.Diagnostics;

namespace LogBattery;

internal sealed class TraceStore
{
    private readonly ConcurrentQueue<SpanRecord> _spans = new();
    private const int MaxSpans = 5000;

    internal void Add(Activity activity)
    {
        var httpMethod = activity.GetTagItem("http.request.method")?.ToString()
                         ?? activity.GetTagItem("http.method")?.ToString();

        var httpRoute = activity.GetTagItem("http.route")?.ToString()
                        ?? activity.GetTagItem("url.path")?.ToString()
                        ?? activity.GetTagItem("http.target")?.ToString();

        int? httpStatusCode = null;
        var statusRaw = activity.GetTagItem("http.response.status_code")
                        ?? activity.GetTagItem("http.status_code");
        if (statusRaw is int code) httpStatusCode = code;
        else if (statusRaw is string s && int.TryParse(s, out var parsed)) httpStatusCode = parsed;

        var attributes = new Dictionary<string, string>();
        foreach (var tag in activity.Tags)
            attributes[tag.Key] = tag.Value ?? "";

        _spans.Enqueue(new SpanRecord(
            TraceId: activity.TraceId.ToString(),
            SpanId: activity.SpanId.ToString(),
            ParentSpanId: activity.ParentSpanId == default ? null : activity.ParentSpanId.ToString(),
            OperationName: activity.DisplayName,
            Kind: activity.Kind.ToString(),
            StartTimeUtc: activity.StartTimeUtc,
            DurationMs: activity.Duration.TotalMilliseconds,
            Status: activity.Status == ActivityStatusCode.Error ? "Error" : "Ok",
            HttpMethod: httpMethod,
            HttpRoute: httpRoute,
            HttpStatusCode: httpStatusCode,
            Attributes: attributes
        ));

        while (_spans.Count > MaxSpans)
            _spans.TryDequeue(out _);
    }

    internal List<TraceSummary> GetTraces(string? search = null, int limit = 50)
    {
        var spans = _spans.ToArray();

        var groups = spans
            .GroupBy(s => s.TraceId)
            .Select(g =>
            {
                var sorted = g.OrderBy(s => s.StartTimeUtc).ToList();
                var root = sorted.FirstOrDefault(s => s.ParentSpanId == null) ?? sorted[0];
                var traceStart = sorted.Min(s => s.StartTimeUtc);
                var traceEnd = sorted.Max(s => s.StartTimeUtc.AddMilliseconds(s.DurationMs));

                return new TraceSummary(
                    TraceId: root.TraceId,
                    RootSpan: root.OperationName,
                    HttpMethod: root.HttpMethod,
                    HttpRoute: root.HttpRoute,
                    HttpStatusCode: root.HttpStatusCode,
                    StartTime: traceStart,
                    DurationMs: (traceEnd - traceStart).TotalMilliseconds,
                    SpanCount: sorted.Count,
                    HasErrors: sorted.Any(s => s.Status == "Error")
                );
            });

        if (!string.IsNullOrEmpty(search))
        {
            groups = groups.Where(t =>
                (t.RootSpan?.Contains(search, StringComparison.OrdinalIgnoreCase) == true) ||
                (t.HttpRoute?.Contains(search, StringComparison.OrdinalIgnoreCase) == true) ||
                t.TraceId.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        return groups
            .OrderByDescending(t => t.StartTime)
            .Take(limit)
            .ToList();
    }

    internal List<SpanRecord> GetSpans(string traceId)
    {
        return _spans
            .Where(s => s.TraceId == traceId)
            .OrderBy(s => s.StartTimeUtc)
            .ToList();
    }

    internal record SpanRecord(
        string TraceId,
        string SpanId,
        string? ParentSpanId,
        string OperationName,
        string Kind,
        DateTime StartTimeUtc,
        double DurationMs,
        string Status,
        string? HttpMethod,
        string? HttpRoute,
        int? HttpStatusCode,
        Dictionary<string, string> Attributes
    );

    internal record TraceSummary(
        string TraceId,
        string? RootSpan,
        string? HttpMethod,
        string? HttpRoute,
        int? HttpStatusCode,
        DateTime StartTime,
        double DurationMs,
        int SpanCount,
        bool HasErrors
    );
}
