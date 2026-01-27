using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace NexArc.Authentication.Api.Services;

public sealed class EphemeralSigningKeyProvider : ISigningKeyProvider
{
    private readonly RsaSecurityKey _key;

    public EphemeralSigningKeyProvider()
    {
#pragma warning disable CA1416
        var rsa = RSA.Create(2048);
#pragma warning restore CA1416
        _key = new RsaSecurityKey(rsa) { KeyId = Guid.NewGuid().ToString("N") };
    }

    public SigningCredentials GetSigningCredentials()
        => new(_key, SecurityAlgorithms.RsaSha256);

    public IEnumerable<SecurityKey> GetValidationKeys()
        => new[] { _key };
}
