namespace NexArc.Authentication.Api.Models;

public sealed record TokenExchangeRequest
{
    public string? AccessToken { get; init; }
    public string? IdToken { get; init; }
    public string? AuthorizationCode { get; init; }
    public string? RedirectUri { get; init; }
    public string? DevBypassUser { get; init; }
}
