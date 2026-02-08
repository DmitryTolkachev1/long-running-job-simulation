using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace LongJobProcessor.API.Authentication;

public sealed class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly string _validUsername;
    private readonly string _validPassword;

    public BasicAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
        _validUsername = Environment.GetEnvironmentVariable("USERNAME") ?? "user";
        _validPassword = Environment.GetEnvironmentVariable("PASSWORD") ?? "passowrd";
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!AuthenticationHeaderValue.TryParse(Request.Headers["Authorization"], out var authHeader))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid header"));
        }

        if (!authHeader.Scheme.Equals("Basic", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.Fail("Authentication scheme is not supported"));
        }

        if (string.IsNullOrEmpty(authHeader.Parameter))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing credentials"));
        }

        try
        {
            var credentialsBytes = Convert.FromBase64String(authHeader.Parameter);
            var credentials = Encoding.UTF8.GetString(credentialsBytes).Split(':', 2);

            var username = credentials[0];
            var password = credentials.Length > 1 ? credentials[1] : string.Empty;

            if (username == _validUsername && password == _validPassword)
            {
                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.NameIdentifier, username),
                };

                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);

                return Task.FromResult(AuthenticateResult.Success(ticket));
            }

            return Task.FromResult(AuthenticateResult.Fail("Invalid credentials"));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error parsing authentication credentials");
            return Task.FromResult(AuthenticateResult.Fail("Invalid credentials format"));
        }
    }
}
