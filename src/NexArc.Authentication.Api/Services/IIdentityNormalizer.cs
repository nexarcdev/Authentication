using NexArc.Authentication.Abstractions.Models;

namespace NexArc.Authentication.Api.Services;

public interface IIdentityNormalizer
{
    IssuedIdentity Normalize(ExternalIdentity identity, IReadOnlyList<string> roles);
}
