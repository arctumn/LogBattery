namespace LogBattery;

internal static class LogBatteryConfig
{
    internal static string LogFilePrefix { get; set; } = "app";
    internal static string LogDirectory { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "logs");
    internal static string[] ExcludedPaths { get; set; } = ["/logs", "/health", "/alive"];
    internal const string ExcludedPathProperty = "ExcludedPath";
}
