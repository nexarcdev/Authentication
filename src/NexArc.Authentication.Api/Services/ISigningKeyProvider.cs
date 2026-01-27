using Microsoft.IdentityModel.Tokens;

namespace NexArc.Authentication.Api.Services;

public interface ISigningKeyProvider
{
    SigningCredentials GetSigningCredentials();
    IEnumerable<SecurityKey> GetValidationKeys();
}
