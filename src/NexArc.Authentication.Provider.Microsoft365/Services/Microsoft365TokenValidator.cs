using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using NexArc.Authentication.Abstractions.Interfaces;
using NexArc.Authentication.Abstractions.Models;
using NexArc.Authentication.Api.Services;
using NexArc.Authentication.Provider.Microsoft365.Options;

namespace NexArc.Authentication.Provider.Microsoft365.Services;

public sealed class Microsoft365TokenValidator : IExternalIdentityValidator
{
    private readonly Microsoft365Options _options;
    private readonly OidcTokenValidator _validator;

    public Microsoft365TokenValidator(IOptions<Microsoft365Options> options)
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
            Email = principal.FindFirstValue("email") ?? principal.FindFirstValue("preferred_username"),
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

        var tenantId = principal.FindFirstValue("tid") ?? string.Empty;
        if (!_options.AllowedTenants.Contains(tenantId, StringComparer.OrdinalIgnoreCase))
        {
            throw new SecurityTokenException("Tenant is not allowed.");
        }
    }
}
