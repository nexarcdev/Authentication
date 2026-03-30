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
        var options = new ApiAuthenticationOptions
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
        Assert.NotNull(tokenPair.SessionExpiresAt);
    }

    [Fact]
    public async Task RefreshAsync_Issues_New_Token_Pair()
    {
        var options = new ApiAuthenticationOptions
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

    [Fact]
    public async Task IssueAsync_Clamps_RefreshToken_Expiry_To_Session_Expiry()
    {
        var now = new DateTimeOffset(2026, 3, 5, 12, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(now);
        var options = new ApiAuthenticationOptions
        {
            Issuer = "https://issuer.example",
            Audience = "api",
            AccessTokenLifetime = TimeSpan.FromHours(16),
            RefreshTokensEnabled = true,
            RefreshTokenLifetime = TimeSpan.FromDays(30),
            SessionAbsoluteLifetime = TimeSpan.FromDays(7)
        };

        var tokenService = new TokenService(
            Options.Create(options),
            new TestSigningKeyProvider(),
            new InMemoryRefreshTokenStore(),
            timeProvider);

        var issued = await tokenService.IssueAsync(new IssuedIdentity
        {
            Subject = "user-1"
        }, CancellationToken.None);

        Assert.Equal(now.AddDays(7), issued.SessionExpiresAt);
        Assert.Equal(issued.SessionExpiresAt, issued.RefreshTokenExpiresAt);
    }

    [Fact]
    public async Task RefreshAsync_Returns_Null_When_Absolute_Session_Lifetime_Exceeded()
    {
        var now = new DateTimeOffset(2026, 3, 5, 12, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(now);
        var options = new ApiAuthenticationOptions
        {
            Issuer = "https://issuer.example",
            Audience = "api",
            AccessTokenLifetime = TimeSpan.FromMinutes(5),
            RefreshTokensEnabled = true,
            RefreshTokenLifetime = TimeSpan.FromDays(30),
            SessionAbsoluteLifetime = TimeSpan.FromDays(7)
        };

        var tokenService = new TokenService(
            Options.Create(options),
            new TestSigningKeyProvider(),
            new InMemoryRefreshTokenStore(),
            timeProvider);

        var original = await tokenService.IssueAsync(new IssuedIdentity
        {
            Subject = "user-1"
        }, CancellationToken.None);

        timeProvider.Advance(TimeSpan.FromDays(7).Add(TimeSpan.FromSeconds(1)));
        var refreshed = await tokenService.RefreshAsync(original.RefreshToken!, CancellationToken.None);
        Assert.Null(refreshed);
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

    private sealed class FakeTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public FakeTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
            => _utcNow;

        public void Advance(TimeSpan delta)
            => _utcNow = _utcNow.Add(delta);
    }
}
