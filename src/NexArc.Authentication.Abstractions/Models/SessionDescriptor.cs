namespace NexArc.Authentication.Abstractions.Models;

public sealed record SessionDescriptor
{
    public string SessionId { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string ProviderKey { get; init; } = string.Empty;
    public DateTimeOffset IssuedAt { get; init; }
}
