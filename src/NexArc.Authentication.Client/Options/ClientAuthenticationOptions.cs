namespace NexArc.Authentication.Client.Options;

public sealed class ClientAuthenticationOptions
{
    public string ProviderKey { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = string.Empty;
    public string ApiClientName { get; set; } = "Api";
    public string AuthApiClientName { get; set; } = "ApiAuth";
    public bool UpstreamSignOutEnabled { get; set; }
    public bool AutomaticTokenRefreshEnabled { get; set; } = true;
    public TimeSpan RefreshBeforeExpiry { get; set; } = TimeSpan.FromMinutes(1);
}
