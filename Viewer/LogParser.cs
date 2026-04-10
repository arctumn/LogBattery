using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LogBattery;

internal static partial class LogParser
{
    internal record LogEntry(
        string Timestamp,
        string Level,
        string? Message,
        string? RequestMethod,
        string? RequestPath,
        int? StatusCode,
        double? Elapsed,
        string? MachineName,
        int? ThreadId,
        string? Exception,
        string? TraceId,
        Dictionary<string, string?> Properties
    );

    internal static List<string> ReadAllLines(string filePath)
    {
        var lines = new List<string>();
        try
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fs);

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (!string.IsNullOrWhiteSpace(line))
                    lines.Add(line);
            }

            lines.Reverse();
            return lines;
        }
        catch
        {
            return lines;
        }
    }

    internal static LogEntry? ParseJsonLogLine(string line)
    {
        if (!line.StartsWith('{')) return null;

        try
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;

            var rawTimestamp = root.TryGetProperty("@t", out var t) ? t.GetString() : null;
            // CompactJsonFormatter omits @l when level is Information
            var level = root.TryGetProperty("@l", out var l) ? l.GetString() ?? "Information" : "Information";
            string? message;
            if (root.TryGetProperty("@m", out var m))
                message = m.GetString();
            else if (root.TryGetProperty("@mt", out var mt) && mt.GetString() is string tmpl)
                message = RenderTemplate(tmpl, root);
            else
                message = null;
            var exception = root.TryGetProperty("@x", out var x) ? x.GetString() : null;

            string? requestMethod = root.TryGetProperty("RequestMethod", out var rm) ? rm.GetString() : null;
            string? requestPath = root.TryGetProperty("RequestPath", out var rp) ? rp.GetString() : null;

            int? statusCode = null;
            if (root.TryGetProperty("StatusCode", out var sc) && sc.ValueKind == JsonValueKind.Number)
                statusCode = sc.GetInt32();

            double? elapsed = null;
            if (root.TryGetProperty("Elapsed", out var el) && el.ValueKind == JsonValueKind.Number)
                elapsed = el.GetDouble();

            string? machineName = root.TryGetProperty("MachineName", out var mn) ? mn.GetString() : null;

            int? threadId = null;
            if (root.TryGetProperty("ThreadId", out var ti) && ti.ValueKind == JsonValueKind.Number)
                threadId = ti.GetInt32();

            var formattedTimestamp = "";
            if (rawTimestamp != null && DateTime.TryParse(rawTimestamp, null, DateTimeStyles.RoundtripKind, out var dt))
                formattedTimestamp = dt.ToString("yyyy-MM-dd HH:mm:ss.fff");

            string? traceId = root.TryGetProperty("@tr", out var tr) ? tr.GetString() : null;

            var coreProps = new HashSet<string> {
                "@t", "@mt", "@m", "@l", "@x", "@i", "@r", "@tr", "@sp",
                "RequestMethod", "RequestPath", "StatusCode", "Elapsed",
                "MachineName", "ThreadId", "Application", "EnvironmentName",
                "RequestId", "ConnectionId", "Protocol", "Scheme", "Host"
            };

            var extraProps = new Dictionary<string, string?>();
            foreach (var prop in root.EnumerateObject())
            {
                if (!coreProps.Contains(prop.Name))
                    extraProps[prop.Name] = prop.Value.ValueKind == JsonValueKind.String
                        ? prop.Value.GetString()
                        : prop.Value.ToString();
            }

            return new LogEntry(formattedTimestamp, level, message, requestMethod, requestPath,
                statusCode, elapsed, machineName, threadId, exception, traceId, extraProps);
        }
        catch
        {
            return null;
        }
    }

    // Matches Serilog message template tokens: {Name}, {Name:fmt}, {@Name}, {$Name}, {Name,align:fmt}
    [GeneratedRegex(@"\{@?\$?(\w+)(?:,-?\d+)?(?::([^}]*))?\}")]
    private static partial Regex TemplateTokenRegex();

    private static string RenderTemplate(string template, JsonElement root)
    {
        var renderings = new Queue<string>();
        if (root.TryGetProperty("@r", out var r) && r.ValueKind == JsonValueKind.Array)
            foreach (var item in r.EnumerateArray())
                renderings.Enqueue(item.ValueKind == JsonValueKind.String
                    ? item.GetString() ?? "" : item.ToString());

        return TemplateTokenRegex().Replace(template, match =>
        {
            var propName = match.Groups[1].Value;
            var hasFormat = match.Groups[2].Success;

            if (hasFormat && renderings.Count > 0)
                return renderings.Dequeue();

            if (root.TryGetProperty(propName, out var prop))
                return prop.ValueKind == JsonValueKind.String
                    ? prop.GetString() ?? match.Value
                    : prop.ToString();

            return match.Value;
        });
    }
}
