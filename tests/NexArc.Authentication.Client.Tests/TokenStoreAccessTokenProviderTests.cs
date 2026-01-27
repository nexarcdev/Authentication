using NexArc.Authentication.Abstractions.Models;
using NexArc.Authentication.Client.Services;

namespace NexArc.Authentication.Client.Tests;

public class TokenStoreAccessTokenProviderTests
{
    [Fact]
    public async Task Returns_Null_When_Expired()
    {
        var store = new InMemoryTokenStore();
        await store.SetAsync(new TokenPair
        {
            AccessToken = "expired",
            AccessTokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1)
        }, CancellationToken.None);

        var provider = new TokenStoreAccessTokenProvider(store);
        var token = await provider.GetAccessTokenAsync(CancellationToken.None);

        Assert.Null(token);
    }

    [Fact]
    public async Task Returns_Token_When_Valid()
    {
        var store = new InMemoryTokenStore();
        await store.SetAsync(new TokenPair
        {
            AccessToken = "valid",
            AccessTokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5)
        }, CancellationToken.None);

        var provider = new TokenStoreAccessTokenProvider(store);
        var token = await provider.GetAccessTokenAsync(CancellationToken.None);

        Assert.Equal("valid", token);
    }
}
