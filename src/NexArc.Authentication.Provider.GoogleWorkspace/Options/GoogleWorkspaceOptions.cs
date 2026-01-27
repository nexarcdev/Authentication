using NexArc.Authentication.Abstractions.Options;
using NexArc.Authentication.DevBypass.Models;

namespace NexArc.Authentication.Provider.GoogleWorkspace.Options;

public sealed class GoogleWorkspaceOptions : ProviderOptions
{
    public string Authority { get; set; } = "https://accounts.google.com";
    public string ClientId { get; set; } = string.Empty;
    public string? ClientSecret { get; set; }
    public string[] AllowedDomains { get; set; } = Array.Empty<string>();
    public DevBypassUsersOptions DevBypass { get; set; } = new();
}
