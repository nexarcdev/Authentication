using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NexArc.Authentication.Abstractions.Models;
using NexArc.Authentication.Abstractions.Options;
using NexArc.Authentication.Api.Services;

namespace NexArc.Authentication.Api.Tests;

public class TokenServiceTests
{
    [Fact]
    public async Task IssueAsync_Returns_Access_And_Refresh_Tokens()
    {
        var options = new AuthenticationOptions
        {
            Issuer = "https://issuer.example",
            Audience = "api",
            AccessTokenLifetime = TimeSpan.FromMinutes(5),
            RefreshTokensEnabled = true,
            RefreshTokenLifetime = TimeSpan.FromDays(1)
        };

        var tokenService = new TokenService(
            Options.Create(options),
            new TestSigningKeyProvider(),
            new InMemoryRefreshTokenStore(),
            TimeProvider.System);

        var tokenPair = await tokenService.IssueAsync(new IssuedIdentity
        {
            Subject = "user-1",
            Email = "user@example.com",
            Name = "Test User",
            Roles = new[] { "Staff" }
        }, CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(tokenPair.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(tokenPair.RefreshToken));
        Assert.NotNull(tokenPair.RefreshTokenExpiresAt);
        Assert.True(tokenPair.AccessTokenExpiresAt > DateTimeOffset.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task RefreshAsync_Issues_New_Token_Pair()
    {
        var options = new AuthenticationOptions
        {
            Issuer = "https://issuer.example",
            Audience = "api",
            AccessTokenLifetime = TimeSpan.FromMinutes(5),
            RefreshTokensEnabled = true,
            RefreshTokenLifetime = TimeSpan.FromDays(1)
        };

        var tokenService = new TokenService(
            Options.Create(options),
            new TestSigningKeyProvider(),
            new InMemoryRefreshTokenStore(),
            TimeProvider.System);

        var original = await tokenService.IssueAsync(new IssuedIdentity
        {
            Subject = "user-1"
        }, CancellationToken.None);

        var refreshed = await tokenService.RefreshAsync(original.RefreshToken!, CancellationToken.None);

        Assert.NotNull(refreshed);
        Assert.NotEqual(original.AccessToken, refreshed!.AccessToken);
        Assert.NotEqual(original.RefreshToken, refreshed.RefreshToken);
    }

    private sealed class TestSigningKeyProvider : ISigningKeyProvider
    {
        private readonly SymmetricSecurityKey _key = new(
            "test-signing-key-test-signing-key"u8.ToArray());

        public SigningCredentials GetSigningCredentials()
            => new(_key, SecurityAlgorithms.HmacSha256);

        public IEnumerable<SecurityKey> GetValidationKeys()
            => new[] { _key };
    }
}
