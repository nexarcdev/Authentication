using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NexArc.Authentication.Abstractions.Models;
using NexArc.Authentication.Client.Options;
using NexArc.Authentication.Client.Services;

namespace NexArc.Authentication.Client.Extensions;

public static class OidcApiTokenExchangeServiceCollectionExtensions
{
    public static IServiceCollection AddApiTokenExchangeOnOidcSignIn(
        this IServiceCollection services,
        string scheme,
        string providerKey)
    {
        services.AddOptions<OpenIdConnectOptions>(scheme)
            .Configure<IHttpClientFactory, IOptions<ClientAuthenticationOptions>, ITokenStore, ILoggerFactory>((options, clientFactory, clientOptions, tokenStore, loggerFactory) =>
            {
                var logger = loggerFactory.CreateLogger("NexArc.Authentication.Client.TokenExchange");
                var existing = options.Events.OnTokenValidated;

                options.Events.OnTokenValidated = async context =>
                {
                    if (existing is not null)
                    {
                        await existing(context);
                    }

                    var configuredProviderKey = clientOptions.Value.ProviderKey;
                    var accessToken = context.TokenEndpointResponse?.AccessToken ?? context.Properties?.GetTokenValue("access_token");
                    var idToken = context.TokenEndpointResponse?.IdToken
                                  ?? context.Properties?.GetTokenValue("id_token")
                                  ?? context.SecurityToken?.RawData;

                    if (string.IsNullOrWhiteSpace(idToken))
                    {
                        var tokenType = context.SecurityToken?.GetType().Name ?? "<null>";
                        logger.LogWarning(
                            "OIDC token exchange aborted: id_token missing. ProviderKey={ProviderKey} ConfiguredProviderKey={ConfiguredProviderKey} TokenType={TokenType}",
                            providerKey,
                            configuredProviderKey,
                            tokenType);
                        context.Fail($"No identity-provider id_token was returned. SecurityTokenType={tokenType}.");
                        return;
                    }

                    logger.LogInformation(
                        "Attempting token exchange. ProviderKey={ProviderKey} ConfiguredProviderKey={ConfiguredProviderKey} HasAccessToken={HasAccessToken} HasIdToken={HasIdToken}",
                        providerKey,
                        configuredProviderKey,
                        !string.IsNullOrWhiteSpace(accessToken),
                        true);

                    var client = clientFactory.CreateClient(clientOptions.Value.ApiClientName);
                    using var response = await client.PostAsJsonAsync(
                        $"/auth/exchange/{providerKey}",
                        new { AccessToken = accessToken, IdToken = idToken },
                        context.HttpContext.RequestAborted);

                    if (!response.IsSuccessStatusCode)
                    {
                        var body = await response.Content.ReadAsStringAsync(context.HttpContext.RequestAborted);
                        logger.LogWarning(
                            "Token exchange failed. ProviderKey={ProviderKey} StatusCode={StatusCode} ResponseBody={ResponseBody}",
                            providerKey,
                            (int)response.StatusCode,
                            string.IsNullOrWhiteSpace(body) ? "<empty>" : body);
                        var detail = string.IsNullOrWhiteSpace(body) ? string.Empty : $" Body: {body}";
                        context.Fail($"Token exchange failed with status code {(int)response.StatusCode}.{detail}");
                        return;
                    }

                    var tokenPair = await response.Content.ReadFromJsonAsync<TokenPair>(cancellationToken: context.HttpContext.RequestAborted);
                    if (tokenPair is null)
                    {
                        context.Fail("Token exchange returned an invalid payload.");
                        return;
                    }

                    await tokenStore.SetAsync(tokenPair, context.HttpContext.RequestAborted);
                };
            });

        return services;
    }
}
