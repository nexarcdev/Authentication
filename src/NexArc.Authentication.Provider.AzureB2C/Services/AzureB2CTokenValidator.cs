using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using NexArc.Authentication.Abstractions.Interfaces;
using NexArc.Authentication.Abstractions.Models;
using NexArc.Authentication.Api.Services;
using NexArc.Authentication.Provider.AzureB2C.Options;

namespace NexArc.Authentication.Provider.AzureB2C.Services;

public sealed class AzureB2CTokenValidator : IExternalIdentityValidator
{
    private readonly AzureB2COptions _options;
    private readonly OidcTokenValidator _validator;
    private readonly ILogger<AzureB2CTokenValidator> _logger;

    public AzureB2CTokenValidator(
        IOptions<AzureB2COptions> options,
        ILogger<AzureB2CTokenValidator>? logger = null)
    {
        _options = options.Value;
        _logger = logger ?? NullLogger<AzureB2CTokenValidator>.Instance;

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

        var tokenClaims = TryReadUnvalidatedClaims(token);

        try
        {
            var principal = await _validator.ValidateAsync(token, ct);
            EnforceTenantAllowList(principal, tokenClaims.Issuer);

            var subject = GetClaimValue(
                              principal,
                              "sub",
                              ClaimTypes.NameIdentifier,
                              "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
                          ?? tokenClaims.Subject;

            if (string.IsNullOrWhiteSpace(subject))
            {
                throw new SecurityTokenException("Subject claim is missing.");
            }

            var email = GetClaimValue(
                            principal,
                            "email",
                            "emails",
                            "preferred_username",
                            ClaimTypes.Email,
                            ClaimTypes.Upn)
                        ?? tokenClaims.Email
                        ?? tokenClaims.Emails
                        ?? tokenClaims.PreferredUsername;

            var name = GetClaimValue(principal, "name", ClaimTypes.Name) ?? tokenClaims.Name;
            var profileUrl = GetClaimValue(principal, "picture") ?? tokenClaims.Picture;

            return new ExternalIdentity
            {
                ProviderKey = tokenContext.ProviderKey,
                Subject = subject,
                Email = email,
                Name = name,
                ProfileUrl = profileUrl,
                Roles = Array.Empty<string>(),
                Claims = principal.Claims
                    .GroupBy(c => c.Type)
                    .ToDictionary(g => g.Key, g => (string?)g.First().Value)
            };
        }
        catch (SecurityTokenException ex)
        {
            var allowedTenants = string.Join(", ", _options.AllowedTenants.Select(value => value ?? "<null>"));
            _logger.LogWarning(
                ex,
                "AzureB2C token validation failed. ProviderKey={ProviderKey}, ConfigAuthority={Authority}, ConfigClientId={ClientId}, ConfigAllowedTenants=[{AllowedTenants}], Issuer={Issuer}, Audience={Audience}, TenantId={TenantId}",
                tokenContext.ProviderKey,
                _options.Authority,
                _options.ClientId,
                allowedTenants,
                tokenClaims.Issuer ?? "<missing>",
                tokenClaims.Audience ?? "<missing>",
                tokenClaims.TenantId ?? "<missing>");
            throw;
        }
    }

    private void EnforceTenantAllowList(ClaimsPrincipal principal, string? tokenIssuer)
    {
        var allowedTenants = _options.AllowedTenants
            .Select(NormalizeTenantMarker)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (allowedTenants.Length == 0)
        {
            return;
        }

        var issuer = GetClaimValue(principal, "iss") ?? tokenIssuer ?? string.Empty;
        var normalizedIssuer = issuer.Trim();
        var match = allowedTenants.Any(tenant =>
            normalizedIssuer.Contains(tenant, StringComparison.OrdinalIgnoreCase));

        if (!match)
        {
            throw new SecurityTokenException("Issuer tenant is not allowed.");
        }
    }

    private static string NormalizeTenantMarker(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value
            .Trim()
            .Trim('"')
            .Trim('{', '}')
            .Trim();
    }

    private static string? GetClaimValue(ClaimsPrincipal principal, params string[] claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var value = principal.FindFirstValue(claimType);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static TokenClaimSnapshot TryReadUnvalidatedClaims(string token)
    {
        try
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            return new TokenClaimSnapshot
            {
                Issuer = jwt.Claims.FirstOrDefault(c => c.Type == "iss")?.Value,
                Audience = jwt.Claims.FirstOrDefault(c => c.Type == "aud")?.Value,
                TenantId = jwt.Claims.FirstOrDefault(c => c.Type == "tid")?.Value,
                Subject = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value,
                Email = jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value,
                Emails = jwt.Claims.FirstOrDefault(c => c.Type == "emails")?.Value,
                PreferredUsername = jwt.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value,
                Name = jwt.Claims.FirstOrDefault(c => c.Type == "name")?.Value,
                Picture = jwt.Claims.FirstOrDefault(c => c.Type == "picture")?.Value
            };
        }
        catch
        {
            return new TokenClaimSnapshot();
        }
    }

    private sealed class TokenClaimSnapshot
    {
        public string? Issuer { get; init; }
        public string? Audience { get; init; }
        public string? TenantId { get; init; }
        public string? Subject { get; init; }
        public string? Email { get; init; }
        public string? Emails { get; init; }
        public string? PreferredUsername { get; init; }
        public string? Name { get; init; }
        public string? Picture { get; init; }
    }
}
