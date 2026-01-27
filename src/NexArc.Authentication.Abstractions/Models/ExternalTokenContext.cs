namespace NexArc.Authentication.Abstractions.Models;

public sealed record ExternalTokenContext
{
    public string ProviderKey { get; init; } = string.Empty;
    public string? AccessToken { get; init; }
    public string? IdToken { get; init; }
    public string? AuthorizationCode { get; init; }
}
