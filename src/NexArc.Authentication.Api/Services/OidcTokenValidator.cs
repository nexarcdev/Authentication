using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace NexArc.Authentication.Api.Services;

public sealed class OidcTokenValidator
{
    private readonly IConfigurationManager<OpenIdConnectConfiguration> _configurationManager;
    private readonly TokenValidationParameters _baseValidationParameters;
    private readonly JwtSecurityTokenHandler _handler = new();

    public OidcTokenValidator(
        IConfigurationManager<OpenIdConnectConfiguration> configurationManager,
        TokenValidationParameters validationParameters)
    {
        _configurationManager = configurationManager;
        _baseValidationParameters = validationParameters;
    }

    public async Task<ClaimsPrincipal> ValidateAsync(string token, CancellationToken ct)
    {
        var config = await _configurationManager.GetConfigurationAsync(ct);

        var parameters = _baseValidationParameters.Clone();
        parameters.IssuerSigningKeys = config.SigningKeys;

        var principal = _handler.ValidateToken(token, parameters, out _);
        return principal;
    }

    public static IConfigurationManager<OpenIdConnectConfiguration> CreateConfigurationManager(string authority)
    {
        var metadataAddress = authority.TrimEnd('/') + "/.well-known/openid-configuration";
        var retriever = new OpenIdConnectConfigurationRetriever();
        var documentRetriever = new HttpDocumentRetriever { RequireHttps = true };

        return new ConfigurationManager<OpenIdConnectConfiguration>(
            metadataAddress,
            retriever,
            documentRetriever);
    }
}
