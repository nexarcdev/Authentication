namespace NexArc.Authentication.DevBypass.Models;

public sealed record DevBypassUser
{
    public string? Subject { get; init; }
    public string? Email { get; init; }
    public string? Name { get; init; }
    public string? ProfileUrl { get; init; }
    public string[] Roles { get; init; } = Array.Empty<string>();
}
