using NexArc.Authentication.Abstractions.Models;

namespace NexArc.Authentication.Client.Services;

public sealed class TokenStoreAccessTokenProvider : IApiAccessTokenProvider
{
    private readonly ITokenStore _tokenStore;

    public TokenStoreAccessTokenProvider(ITokenStore tokenStore)
    {
        _tokenStore = tokenStore;
    }

    public async Task<string?> GetAccessTokenAsync(CancellationToken ct)
    {
        var tokenPair = await _tokenStore.GetAsync(ct);
        if (tokenPair is null)
        {
            return null;
        }

        if (tokenPair.AccessTokenExpiresAt <= DateTimeOffset.UtcNow)
        {
            return null;
        }

        return tokenPair.AccessToken;
    }
}
