using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NexArc.Authentication.Abstractions.Models;
using NexArc.Authentication.Client.Options;

namespace NexArc.Authentication.Client.Services;

public sealed class TokenStoreAccessTokenProvider : IApiAccessTokenProvider
{
    private readonly ITokenStore _tokenStore;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<ClientAuthenticationOptions> _clientOptions;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<TokenStoreAccessTokenProvider> _logger;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public TokenStoreAccessTokenProvider(
        ITokenStore tokenStore,
        IHttpClientFactory httpClientFactory,
        IOptions<ClientAuthenticationOptions> clientOptions,
        TimeProvider timeProvider,
        ILogger<TokenStoreAccessTokenProvider> logger)
    {
        _tokenStore = tokenStore;
        _httpClientFactory = httpClientFactory;
        _clientOptions = clientOptions;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<string?> GetAccessTokenAsync(CancellationToken ct)
    {
        var now = _timeProvider.GetUtcNow();
        var tokenPair = await _tokenStore.GetAsync(ct);
        if (tokenPair is null)
        {
            return null;
        }

        if (HasAccessTokenRemaining(tokenPair, now, _clientOptions.Value.RefreshBeforeExpiry))
        {
            return tokenPair.AccessToken;
        }

        var refreshed = await RefreshTokenPairAsync(tokenPair, now, ct);
        return refreshed?.AccessToken;
    }

    private async Task<TokenPair?> RefreshTokenPairAsync(TokenPair current, DateTimeOffset now, CancellationToken ct)
    {
        if (!CanAttemptRefresh(current, now))
        {
            return HandleNoRefreshPath(current, now);
        }

        await _refreshLock.WaitAsync(ct);
        try
        {
            var latest = await _tokenStore.GetAsync(ct);
            if (latest is null)
            {
                return null;
            }

            if (HasAccessTokenRemaining(latest, now, _clientOptions.Value.RefreshBeforeExpiry))
            {
                return latest;
            }

            if (!CanAttemptRefresh(latest, now))
            {
                return HandleNoRefreshPath(latest, now);
            }

            var refreshed = await TryRefreshWithApiAsync(latest.RefreshToken!, ct);
            if (refreshed is not null)
            {
                await _tokenStore.SetAsync(refreshed, ct);
                return refreshed;
            }

            if (latest.AccessTokenExpiresAt <= now)
            {
                await _tokenStore.ClearAsync(ct);
                return null;
            }

            return latest;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private TokenPair? HandleNoRefreshPath(TokenPair tokenPair, DateTimeOffset now)
    {
        if (tokenPair.AccessTokenExpiresAt <= now)
        {
            return null;
        }

        return tokenPair;
    }

    private bool CanAttemptRefresh(TokenPair tokenPair, DateTimeOffset now)
    {
        if (!_clientOptions.Value.AutomaticTokenRefreshEnabled)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(tokenPair.RefreshToken) || !tokenPair.RefreshTokenExpiresAt.HasValue)
        {
            return false;
        }

        return tokenPair.RefreshTokenExpiresAt.Value > now;
    }

    private static bool HasAccessTokenRemaining(TokenPair tokenPair, DateTimeOffset now, TimeSpan refreshBeforeExpiry)
    {
        return tokenPair.AccessTokenExpiresAt > now.Add(refreshBeforeExpiry);
    }

    private async Task<TokenPair?> TryRefreshWithApiAsync(string refreshToken, CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(_clientOptions.Value.AuthApiClientName);
            using var response = await client.PostAsJsonAsync(
                "/auth/refresh",
                new RefreshTokenRequest { RefreshToken = refreshToken },
                ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Token refresh failed with status code {StatusCode}.", (int)response.StatusCode);
                return null;
            }

            var refreshed = await response.Content.ReadFromJsonAsync<TokenPair>(cancellationToken: ct);
            if (refreshed is null || string.IsNullOrWhiteSpace(refreshed.AccessToken))
            {
                _logger.LogWarning("Token refresh returned an invalid payload.");
                return null;
            }

            return refreshed;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token refresh request failed.");
            return null;
        }
    }

    private sealed record RefreshTokenRequest
    {
        public string RefreshToken { get; init; } = string.Empty;
    }
}
