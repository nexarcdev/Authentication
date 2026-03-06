using NexArc.Authentication.Abstractions.Models;

namespace NexArc.Authentication.Api.Models;

public sealed record RefreshTokenRecord
{
    public string TokenHash { get; init; } = string.Empty;
    public IssuedIdentity Identity { get; init; } = new();
    public DateTimeOffset ExpiresAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset SessionStartedAt { get; init; }
    public DateTimeOffset? SessionExpiresAt { get; init; }
}
