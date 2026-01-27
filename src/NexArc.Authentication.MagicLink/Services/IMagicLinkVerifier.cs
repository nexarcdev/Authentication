using NexArc.Authentication.MagicLink.Models;

namespace NexArc.Authentication.MagicLink.Services;

public interface IMagicLinkVerifier
{
    Task<MagicLinkApproval> ApproveAsync(MagicLinkSession session, CancellationToken ct);
}
