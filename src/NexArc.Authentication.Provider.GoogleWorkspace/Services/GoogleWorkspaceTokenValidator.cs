using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using NexArc.Authentication.Abstractions.Interfaces;
using NexArc.Authentication.Abstractions.Models;
using NexArc.Authentication.Api.Services;
using NexArc.Authentication.Provider.GoogleWorkspace.Options;
using Microsoft.Extensions.Options;

namespace NexArc.Authentication.Provider.GoogleWorkspace.Services;

public sealed class GoogleWorkspaceTokenValidator : IExternalIdentityValidator
{
    private readonly GoogleWorkspaceOptions _options;
    private readonly OidcTokenValidator _validator;

    public GoogleWorkspaceTokenValidator(IOptions<GoogleWorkspaceOptions> options)
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

        var subject = principal.FindFirstValue("sub") ?? string.Empty;
        var email = principal.FindFirstValue("email");
        var name = principal.FindFirstValue("name");
        var picture = principal.FindFirstValue("picture");

        EnforceDomainAllowList(principal, email);

        return new ExternalIdentity
        {
            ProviderKey = tokenContext.ProviderKey,
            Subject = subject,
            Email = email,
            Name = name,
            ProfileUrl = picture,
            Roles = Array.Empty<string>(),
            Claims = principal.Claims
                .GroupBy(c => c.Type)
                .ToDictionary(g => g.Key, g => (string?)g.First().Value)
        };
    }

    private void EnforceDomainAllowList(ClaimsPrincipal principal, string? email)
    {
        if (_options.AllowedDomains.Length == 0)
        {
            return;
        }

        var hostedDomain = principal.FindFirstValue("hd");
        var emailDomain = email?.Split('@').LastOrDefault();
        var domain = hostedDomain ?? emailDomain;

        if (string.IsNullOrWhiteSpace(domain))
        {
            throw new SecurityTokenException("Domain claim is required.");
        }

        if (!_options.AllowedDomains.Contains(domain, StringComparer.OrdinalIgnoreCase))
        {
            throw new SecurityTokenException("User domain is not allowed.");
        }
    }
}
