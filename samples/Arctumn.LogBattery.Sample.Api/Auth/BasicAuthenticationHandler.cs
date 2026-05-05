using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Arctumn.LogBattery.Sample.Auth;

public sealed class BasicAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "Basic";

    public string Realm { get; set; } = "LogBattery";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}

public sealed class BasicAuthenticationHandler(
    IOptionsMonitor<BasicAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<BasicAuthenticationOptions>(options, logger, encoder)
{
    public const string SchemeName = BasicAuthenticationOptions.DefaultScheme;

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
            return Task.FromResult(AuthenticateResult.NoResult());

        if (!AuthenticationHeaderValue.TryParse(authHeader.ToString(), out var parsed) ||
            !string.Equals(parsed.Scheme, "Basic", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrEmpty(parsed.Parameter))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        string decoded;
        try
        {
            decoded = Encoding.UTF8.GetString(Convert.FromBase64String(parsed.Parameter));
        }
        catch (FormatException)
        {
            return Task.FromResult(AuthenticateResult.Fail("Malformed Basic credentials"));
        }

        var sep = decoded.IndexOf(':');
        if (sep < 0)
            return Task.FromResult(AuthenticateResult.Fail("Malformed Basic credentials"));

        var username = decoded[..sep];
        var password = decoded[(sep + 1)..];

        if (!string.Equals(username, Options.Username, StringComparison.Ordinal) ||
            !string.Equals(password, Options.Password, StringComparison.Ordinal))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid username or password"));
        }

        var claims = new[] { new Claim(ClaimTypes.Name, username) };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers["WWW-Authenticate"] = $"Basic realm=\"{Options.Realm}\", charset=\"UTF-8\"";
        return base.HandleChallengeAsync(properties);
    }
}
