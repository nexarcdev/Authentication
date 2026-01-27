using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using NexArc.Authentication.Abstractions.Interfaces;
using NexArc.Authentication.Abstractions.Models;
using NexArc.Authentication.Api.Services;
using NexArc.Authentication.Provider.Auth0B2C.Options;

namespace NexArc.Authentication.Provider.Auth0B2C.Services;

public sealed class Auth0B2CTokenValidator : IExternalIdentityValidator
{
    private readonly Auth0B2COptions _options;
    private readonly OidcTokenValidator _validator;

    public Auth0B2CTokenValidator(IOptions<Auth0B2COptions> options)
    {
        _options = options.Value;

        var parameters = new TokenValidationParameters
        {
            ValidIssuer = _options.Authority,
            ValidateIssuer = true,
            ValidAudience = _options.ClientId,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        _validator = new OidcTokenValidator(
            OidcTokenValidator.CreateConfigurationManager(_options.Authority),
            parameters);
    }

    public async Task<ExternalIdentity> ValidateAsync(ExternalTokenContext tokenContext, CancellationToken ct)
    {
        var token = tokenContext.IdToken ?? tokenContext.AccessToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new SecurityTokenException("Token is required.");
        }

        var principal = await _validator.ValidateAsync(token, ct);
        EnforceTenantAllowList(principal);

        return new ExternalIdentity
        {
            ProviderKey = tokenContext.ProviderKey,
            Subject = principal.FindFirstValue("sub") ?? string.Empty,
            Email = principal.FindFirstValue("email"),
            Name = principal.FindFirstValue("name"),
            ProfileUrl = principal.FindFirstValue("picture"),
            Roles = Array.Empty<string>(),
            Claims = principal.Claims
                .GroupBy(c => c.Type)
                .ToDictionary(g => g.Key, g => (string?)g.First().Value)
        };
    }

    private void EnforceTenantAllowList(ClaimsPrincipal principal)
    {
        if (_options.AllowedTenants.Length == 0)
        {
            return;
        }

        var issuer = principal.FindFirstValue("iss") ?? string.Empty;
        var match = _options.AllowedTenants.Any(t =>
            issuer.Contains(t, StringComparison.OrdinalIgnoreCase));

        if (!match)
        {
            throw new SecurityTokenException("Issuer tenant is not allowed.");
        }
    }
}
