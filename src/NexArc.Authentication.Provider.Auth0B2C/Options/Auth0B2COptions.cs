using NexArc.Authentication.Abstractions.Options;
using NexArc.Authentication.DevBypass.Models;

namespace NexArc.Authentication.Provider.Auth0B2C.Options;

public sealed class Auth0B2COptions : ProviderOptions
{
    public string Authority { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string? ClientSecret { get; set; }
    public string[] AllowedTenants { get; set; } = Array.Empty<string>();
    public DevBypassUsersOptions DevBypass { get; set; } = new();
}
