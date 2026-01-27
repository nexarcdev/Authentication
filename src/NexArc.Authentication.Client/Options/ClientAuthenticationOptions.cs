namespace NexArc.Authentication.Client.Options;

public sealed class ClientAuthenticationOptions
{
    public string ProviderKey { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = string.Empty;
    public string ApiClientName { get; set; } = "Api";
    public bool UpstreamSignOutEnabled { get; set; }
}
