using NexArc.Authentication.Api.Models;

namespace NexArc.Authentication.Api.Services;

public interface IRefreshTokenStore
{
    Task StoreAsync(RefreshTokenRecord record, CancellationToken ct);
    Task<RefreshTokenRecord?> FindAsync(string tokenHash, CancellationToken ct);
    Task RevokeAsync(string tokenHash, CancellationToken ct);
}
