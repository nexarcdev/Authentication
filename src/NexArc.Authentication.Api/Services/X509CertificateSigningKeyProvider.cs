using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NexArc.Authentication.Api.Configuration;

namespace NexArc.Authentication.Api.Services;

internal sealed class X509CertificateSigningKeyProvider(IOptions<CertificateSigningKeyOptions> options) : ISigningKeyProvider
{
    private readonly CertificateSigningKeyOptions options = options.Value;
    private readonly Lazy<X509Certificate2> certificate = new(() => LoadCertificate(options.Value));

    public SigningCredentials GetSigningCredentials()
    {
        var cert = certificate.Value;
        var key = new X509SecurityKey(cert) { KeyId = GetKeyId(cert) };
        return new SigningCredentials(key, SecurityAlgorithms.RsaSha256);
    }

    public IEnumerable<SecurityKey> GetValidationKeys()
    {
        var cert = certificate.Value;
        return [ new X509SecurityKey(cert) { KeyId = GetKeyId(cert) } ];
    }

    private string GetKeyId(X509Certificate2 cert) =>
        !string.IsNullOrWhiteSpace(options.KeyId)
            ? options.KeyId
            : cert.Thumbprint;

    private static X509Certificate2 LoadCertificate(CertificateSigningKeyOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.PfxBase64))
            throw new InvalidOperationException(
                $"JWT signing key is not configured. Set '{CertificateSigningKeyOptions.SectionName}:PfxBase64' in non-Development environments.");

        byte[] pfxBytes;
        try
        {
            pfxBytes = Convert.FromBase64String(options.PfxBase64);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException(
                $"'{CertificateSigningKeyOptions.SectionName}:PfxBase64' must be valid base64.", ex);
        }

        var cert = X509CertificateLoader.LoadPkcs12(
            pfxBytes,
            options.PfxPassword,
            X509KeyStorageFlags.EphemeralKeySet);

        if (!cert.HasPrivateKey)
            throw new InvalidOperationException(
                $"'{CertificateSigningKeyOptions.SectionName}:PfxBase64' must include the certificate private key.");

        return cert;
    }
}
