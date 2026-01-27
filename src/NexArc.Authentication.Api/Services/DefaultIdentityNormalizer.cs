using NexArc.Authentication.Abstractions.Models;

namespace NexArc.Authentication.Api.Services;

public sealed class DefaultIdentityNormalizer : IIdentityNormalizer
{
    public IssuedIdentity Normalize(ExternalIdentity identity, IReadOnlyList<string> roles)
    {
        return new IssuedIdentity
        {
            Subject = identity.Subject,
            Email = identity.Email,
            Name = identity.Name,
            ProfileUrl = identity.ProfileUrl,
            Roles = roles,
            Claims = identity.Claims
        };
    }
}
