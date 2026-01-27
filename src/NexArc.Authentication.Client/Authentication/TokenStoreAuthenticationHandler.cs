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
    private readonly ITokenStore _tokenStore;
    private readonly TimeProvider _timeProvider;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    #pragma warning disable CS0618
    public TokenStoreAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        TimeProvider timeProvider,
        ITokenStore tokenStore)
        : base(options, logger, encoder, clock)
    {
        _tokenStore = tokenStore;
        _timeProvider = timeProvider;
    }
    #pragma warning restore CS0618

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var tokenPair = await _tokenStore.GetAsync(Context.RequestAborted);
        if (tokenPair is null)
        {
            return AuthenticateResult.NoResult();
        }

        if (tokenPair.AccessTokenExpiresAt <= _timeProvider.GetUtcNow())
        {
            return AuthenticateResult.NoResult();
        }

        if (!_tokenHandler.CanReadToken(tokenPair.AccessToken))
        {
            return AuthenticateResult.Fail("Invalid access token.");
        }

        var jwt = _tokenHandler.ReadJwtToken(tokenPair.AccessToken);
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
