using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LogBattery;

/// <summary>
/// Middleware that captures and logs HTTP request bodies (inputs) and response bodies (outputs).
/// </summary>
public class RequestResponseLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestResponseLoggingMiddleware> logger,
    string? pathPrefix = "/api")
{
    private const int MaxPayloadLength = 4096;

    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;

        if (pathPrefix != null && !request.Path.StartsWithSegments(pathPrefix))
        {
            await next(context);
            return;
        }

        // --- Capture Request Body ---
        string requestBody = string.Empty;
        if (request.ContentLength > 0 || request.ContentType != null)
        {
            request.EnableBuffering();
            using var reader = new StreamReader(request.Body, leaveOpen: true);
            requestBody = await reader.ReadToEndAsync();
            request.Body.Position = 0;
        }

        // --- Capture Response Body ---
        var originalBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            await next(context);
        }
        finally
        {
            responseBodyStream.Position = 0;
            var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
            responseBodyStream.Position = 0;
            await responseBodyStream.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;

            var truncatedRequest = Truncate(requestBody);
            var truncatedResponse = Truncate(responseBody);

            logger.LogInformation(
                "HTTP {RequestMethod} {RequestPath} | Request: {RequestBody} | Response: {ResponseBody}",
                request.Method,
                request.Path.ToString(),
                truncatedRequest,
                truncatedResponse);
        }
    }

    private static string Truncate(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length > MaxPayloadLength
            ? string.Concat(value.AsSpan(0, MaxPayloadLength), "...[truncated]")
            : value;
    }
}
