using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Arctumn.LogBattery.Extensions;

/// <summary>
/// Extension methods for configuring authentication on the LogBattery viewer endpoints.
/// </summary>
public static class LogBatteryAuthExtensions
{
    /// <summary>
    /// Configures every subsequent <see cref="LogViewerExtensions.MapLogViewer"/> registration
    /// to require authorization against the given authentication scheme.
    /// <para>
    /// The consumer is responsible for registering the matching
    /// <see cref="Microsoft.AspNetCore.Authentication.AuthenticationHandler{TOptions}"/>
    /// (e.g. <c>services.AddAuthentication(...).AddScheme&lt;TOptions, THandler&gt;(...)</c>)
    /// and for calling <c>UseAuthentication</c>/<c>UseAuthorization</c> in the pipeline.
    /// </para>
    /// <example>
    /// <code>
    /// app.RequireLogBatteryAuth(ApiKeyAuthenticationHandler.SchemeName)
    ///    .MapLogViewer();
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <param name="authenticationScheme">The authentication scheme name to enforce on viewer endpoints.</param>
    public static IEndpointRouteBuilder RequireLogBatteryAuth(this IEndpointRouteBuilder app, string authenticationScheme)
    {
        if (string.IsNullOrWhiteSpace(authenticationScheme))
            throw new ArgumentException("Authentication scheme name must be provided.", nameof(authenticationScheme));

        LogBatteryConfig.AuthenticationScheme = authenticationScheme;
        return app;
    }

    internal static RouteHandlerBuilder WithLogBatteryAuth(this RouteHandlerBuilder builder)
    {
        var scheme = LogBatteryConfig.AuthenticationScheme;
        if (!string.IsNullOrWhiteSpace(scheme))
            builder.RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = scheme });
        return builder;
    }
}
