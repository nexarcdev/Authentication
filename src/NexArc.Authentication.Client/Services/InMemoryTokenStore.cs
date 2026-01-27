using NexArc.Authentication.Abstractions.Models;

namespace NexArc.Authentication.Client.Services;

public sealed class InMemoryTokenStore : ITokenStore
{
    private TokenPair? _tokenPair;

    public Task SetAsync(TokenPair tokenPair, CancellationToken ct)
    {
        _tokenPair = tokenPair;
        return Task.CompletedTask;
    }

    public Task<TokenPair?> GetAsync(CancellationToken ct)
        => Task.FromResult(_tokenPair);

    public Task ClearAsync(CancellationToken ct)
    {
        _tokenPair = null;
        return Task.CompletedTask;
    }
}
