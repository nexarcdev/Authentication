namespace NexArc.Authentication.Abstractions.Models;

public sealed record IssuedIdentity
{
    public string Subject { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Name { get; init; }
    public string? ProfileUrl { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, string?> Claims { get; init; } = new Dictionary<string, string?>();
    public string? SessionId { get; init; }
}
