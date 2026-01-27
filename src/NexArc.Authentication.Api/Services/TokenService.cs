using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using NexArc.Authentication.Abstractions.Claims;
using NexArc.Authentication.Abstractions.Models;
using Microsoft.Extensions.Options;
using NexArc.Authentication.Abstractions.Options;
using NexArc.Authentication.Api.Models;

namespace NexArc.Authentication.Api.Services;

public sealed class TokenService : ITokenService
{
    private readonly AuthenticationOptions _options;
    private readonly ISigningKeyProvider _signingKeyProvider;
    private readonly IRefreshTokenStore _refreshTokenStore;
    private readonly TimeProvider _timeProvider;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    public TokenService(
        IOptions<AuthenticationOptions> options,
        ISigningKeyProvider signingKeyProvider,
        IRefreshTokenStore refreshTokenStore,
        TimeProvider timeProvider)
    {
        _options = options.Value;
        _signingKeyProvider = signingKeyProvider;
        _refreshTokenStore = refreshTokenStore;
        _timeProvider = timeProvider;
    }

    public async Task<TokenPair> IssueAsync(IssuedIdentity identity, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(identity.Subject))
        {
            throw new InvalidOperationException("Subject is required for token issuance.");
        }

        var now = _timeProvider.GetUtcNow();
        var sessionId = string.IsNullOrWhiteSpace(identity.SessionId)
            ? Guid.NewGuid().ToString("N")
            : identity.SessionId!;

        var accessToken = CreateAccessToken(identity with { SessionId = sessionId }, now);
        var tokenPair = new TokenPair
        {
            AccessToken = accessToken,
            AccessTokenExpiresAt = now.Add(_options.AccessTokenLifetime)
        };

        if (_options.RefreshTokensEnabled)
        {
            var refreshToken = GenerateToken();
            var refreshTokenHash = ComputeHash(refreshToken);

            await _refreshTokenStore.StoreAsync(new RefreshTokenRecord
            {
                TokenHash = refreshTokenHash,
                Identity = identity with { SessionId = sessionId },
                CreatedAt = now,
                ExpiresAt = now.Add(_options.RefreshTokenLifetime)
            }, ct);

            tokenPair = tokenPair with
            {
                RefreshToken = refreshToken,
                RefreshTokenExpiresAt = now.Add(_options.RefreshTokenLifetime)
            };
        }

        return tokenPair;
    }

    public async Task<TokenPair?> RefreshAsync(string refreshToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        var now = _timeProvider.GetUtcNow();
        var tokenHash = ComputeHash(refreshToken);
        var record = await _refreshTokenStore.FindAsync(tokenHash, ct);
        if (record is null)
        {
            return null;
        }

        if (record.ExpiresAt <= now)
        {
            await _refreshTokenStore.RevokeAsync(tokenHash, ct);
            return null;
        }

        await _refreshTokenStore.RevokeAsync(tokenHash, ct);
        return await IssueAsync(record.Identity, ct);
    }

    private string CreateAccessToken(IssuedIdentity identity, DateTimeOffset now)
    {
        var claims = new List<Claim>
        {
            new(CanonicalClaimTypes.Subject, identity.Subject),
            new(CanonicalClaimTypes.SessionId, identity.SessionId ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        if (!string.IsNullOrWhiteSpace(identity.Email))
        {
            claims.Add(new Claim(CanonicalClaimTypes.Email, identity.Email));
        }

        if (!string.IsNullOrWhiteSpace(identity.Name))
        {
            claims.Add(new Claim(CanonicalClaimTypes.Name, identity.Name));
        }

        if (!string.IsNullOrWhiteSpace(identity.ProfileUrl))
        {
            claims.Add(new Claim(CanonicalClaimTypes.ProfileUrl, identity.ProfileUrl));
        }

        foreach (var role in identity.Roles)
        {
            if (!string.IsNullOrWhiteSpace(role))
            {
                claims.Add(new Claim(CanonicalClaimTypes.Role, role));
            }
        }

        foreach (var kvp in identity.Claims)
        {
            if (!string.IsNullOrWhiteSpace(kvp.Key) && kvp.Value is not null)
            {
                claims.Add(new Claim(kvp.Key, kvp.Value));
            }
        }

        var signingCredentials = _signingKeyProvider.GetSigningCredentials();
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: now.Add(_options.AccessTokenLifetime).UtcDateTime,
            signingCredentials: signingCredentials);

        return _tokenHandler.WriteToken(token);
    }

    private static string GenerateToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Base64UrlEncoder.Encode(bytes);
    }

    private static string ComputeHash(string value)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        var hash = SHA256.HashData(bytes);
        return Base64UrlEncoder.Encode(hash);
    }
}
