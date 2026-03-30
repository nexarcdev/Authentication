namespace NexArc.Authentication.Api.Configuration;

internal sealed class CertificateSigningKeyOptions
{
    public const string SectionName = "Auth:SigningKey";

    public string? PfxBase64 { get; init; }

    public string? PfxPassword { get; init; }

    public string? KeyId { get; init; }
}
