using NexArc.Authentication.MagicLink.Models;

namespace NexArc.Authentication.MagicLink.Services;

public interface IMagicLinkSessionStore
{
    Task SaveAsync(MagicLinkSession session, CancellationToken ct);
    Task<MagicLinkSession?> FindByCodeAsync(string code, CancellationToken ct);
    Task CompleteAsync(string sessionId, CancellationToken ct);
}
