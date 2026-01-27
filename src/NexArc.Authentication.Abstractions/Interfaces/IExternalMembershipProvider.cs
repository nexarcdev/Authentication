using NexArc.Authentication.Abstractions.Models;

namespace NexArc.Authentication.Abstractions.Interfaces;

public interface IExternalMembershipProvider
{
    Task<IReadOnlyList<string>> GetRolesAsync(ExternalIdentity identity, CancellationToken ct);
}
