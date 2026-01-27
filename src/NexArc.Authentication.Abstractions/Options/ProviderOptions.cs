namespace NexArc.Authentication.Abstractions.Options;

public abstract class ProviderOptions
{
    public string ProviderKey { get; set; } = string.Empty;
    public string Scheme { get; set; } = string.Empty;
}
