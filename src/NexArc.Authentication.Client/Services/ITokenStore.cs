using NexArc.Authentication.Abstractions.Models;

namespace NexArc.Authentication.Client.Services;

public interface ITokenStore
{
    Task SetAsync(TokenPair tokenPair, CancellationToken ct);
    Task<TokenPair?> GetAsync(CancellationToken ct);
    Task ClearAsync(CancellationToken ct);
}
