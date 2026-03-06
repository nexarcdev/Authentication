using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NexArc.Authentication.Abstractions.Claims;
using NexArc.Authentication.Client.Services;

namespace NexArc.Authentication.Client.Authentication;

public sealed class TokenStoreAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IApiAccessTokenProvider _accessTokenProvider;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    #pragma warning disable CS0618
    public TokenStoreAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        IApiAccessTokenProvider accessTokenProvider)
        : base(options, logger, encoder, clock)
    {
        _accessTokenProvider = accessTokenProvider;
    }
    #pragma warning restore CS0618

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var accessToken = await _accessTokenProvider.GetAccessTokenAsync(Context.RequestAborted);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return AuthenticateResult.NoResult();
        }

        if (!_tokenHandler.CanReadToken(accessToken))
        {
            return AuthenticateResult.Fail("Invalid access token.");
        }

        var jwt = _tokenHandler.ReadJwtToken(accessToken);
        var identity = new ClaimsIdentity(
            jwt.Claims,
            TokenStoreAuthenticationDefaults.AuthenticationScheme,
            CanonicalClaimTypes.Name,
            CanonicalClaimTypes.Role);

        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, TokenStoreAuthenticationDefaults.AuthenticationScheme);
        return AuthenticateResult.Success(ticket);
    }
}
