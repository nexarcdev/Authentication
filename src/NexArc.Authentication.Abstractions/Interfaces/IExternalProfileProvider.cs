using NexArc.Authentication.Abstractions.Models;

namespace NexArc.Authentication.Abstractions.Interfaces;

public interface IExternalProfileProvider
{
    Task<ExternalIdentity> EnrichAsync(ExternalIdentity identity, CancellationToken ct);
}
