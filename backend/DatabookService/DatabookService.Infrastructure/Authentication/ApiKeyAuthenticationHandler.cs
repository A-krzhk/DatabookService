using System.Security.Claims;
using System.Text.Encodings.Web;
using DatabookService.Domain.Entities;
using DatabookService.Infrastructure.Data;
using DatabookService.Application.Interfaces.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DatabookService.Infrastructure.Authentication;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IApiKeyHasher _hasher;

    public const string Scheme = "ApiKey";

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        ApplicationDbContext dbContext,
        IApiKeyHasher hasher)
        : base(options, logger, encoder, clock)
    {
        _dbContext = dbContext;
        _hasher = hasher;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var apiKey = ExtractApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return AuthenticateResult.NoResult();
        }

        var now = DateTimeOffset.UtcNow;
        // We cannot search by hash directly without hashing the input; we'll verify against active keys
        var activeKeys = await _dbContext.ApiKeys
            .Where(k => k.IsActive && (!k.ExpiresAt.HasValue || k.ExpiresAt > now))
            .ToListAsync(Context.RequestAborted);

        var matched = activeKeys.FirstOrDefault(k => _hasher.Verify(apiKey, k.KeyHash));
        if (matched is null)
        {
            return AuthenticateResult.Fail("Invalid API key");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, matched.Id.ToString()),
            new(ClaimTypes.Name, matched.Name),
            new(ClaimTypes.Role, matched.Role.ToString())
        };

        var identity = new ClaimsIdentity(claims, Scheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme);
        return AuthenticateResult.Success(ticket);
    }

    private string? ExtractApiKey()
    {
        if (Request.Headers.TryGetValue(Options.HeaderName, out var headerValues))
        {
            var value = headerValues.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        if (Options.AllowAuthorizationHeader && Request.Headers.TryGetValue("Authorization", out var authValues))
        {
            var auth = authValues.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(auth))
            {
                var prefix = Options.AuthorizationScheme + " ";
                if (auth.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return auth.Substring(prefix.Length).Trim();
                }
            }
        }

        return null;
    }
}


