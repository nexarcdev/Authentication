using NexArc.Authentication.Abstractions.Models;

namespace NexArc.Authentication.Api.Services;

public interface ITokenService
{
    Task<TokenPair> IssueAsync(IssuedIdentity identity, CancellationToken ct);
    Task<TokenPair?> RefreshAsync(string refreshToken, CancellationToken ct);
}
