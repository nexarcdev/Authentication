using System.Security.Claims;

namespace NexArc.Authentication.Abstractions.Interfaces;

public interface ICurrentUser
{
    ClaimsPrincipal Principal { get; }
    bool IsAuthenticated { get; }
    string? Subject { get; }
    string? Email { get; }
    string? Name { get; }
    string? ProfileUrl { get; }
    IReadOnlyList<string> Roles { get; }
}
