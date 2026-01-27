using NexArc.Authentication.Abstractions.Models;

namespace NexArc.Authentication.Abstractions.Interfaces;

public interface IExternalIdentityValidator
{
    Task<ExternalIdentity> ValidateAsync(ExternalTokenContext tokenContext, CancellationToken ct);
}
