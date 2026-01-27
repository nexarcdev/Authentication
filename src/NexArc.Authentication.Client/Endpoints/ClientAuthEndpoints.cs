using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NexArc.Authentication.Abstractions.Models;
using NexArc.Authentication.Client.Options;
using NexArc.Authentication.Client.Services;

namespace NexArc.Authentication.Client.Endpoints;

public sealed class ClientAuthEndpoints : IClientEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/login", async (
            HttpContext context,
            HttpRequest request,
            IOptions<ClientAuthenticationOptions> clientOptions,
            IEnumerable<ProviderDescriptor> providers,
            IAuthenticationSchemeProvider schemeProvider) =>
        {
            var scheme = ResolveScheme(clientOptions.Value.ProviderKey, providers);
            if (scheme is null || await schemeProvider.GetSchemeAsync(scheme) is null)
            {
                return Results.NotFound();
            }

            var returnUrl = NormalizeReturnUrl(request.Query["returnUrl"].ToString());
            var properties = new AuthenticationProperties
            {
                RedirectUri = returnUrl
            };

            return Results.Challenge(properties, new[] { scheme });
        });

        endpoints.MapMethods("/logout", new[] { "GET", "POST" }, async (
            HttpRequest request,
            HttpContext context,
            ITokenStore tokenStore,
            IOptions<ClientAuthenticationOptions> clientOptions,
            IEnumerable<ProviderDescriptor> providers,
            IAuthenticationSchemeProvider schemeProvider,
            IAuthenticationHandlerProvider handlerProvider,
            CancellationToken ct) =>
        {
            await tokenStore.ClearAsync(ct);
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            var returnUrl = NormalizeReturnUrl(await ReadReturnUrlAsync(request, ct));

            if (clientOptions.Value.UpstreamSignOutEnabled)
            {
                var scheme = ResolveScheme(clientOptions.Value.ProviderKey, providers);
                if (!string.IsNullOrWhiteSpace(scheme))
                {
                    var handler = await handlerProvider.GetHandlerAsync(context, scheme);
                    if (handler is IAuthenticationSignOutHandler)
                    {
                        await context.SignOutAsync(scheme, new AuthenticationProperties
                        {
                            RedirectUri = returnUrl
                        });

                        return Results.Empty;
                    }
                }
            }

            if (request.Method == HttpMethods.Get || request.HasFormContentType)
            {
                return Results.Redirect(returnUrl);
            }

            return Results.Ok(new { redirectUrl = returnUrl });
        });
    }

    private static string? ResolveScheme(string providerKey, IEnumerable<ProviderDescriptor> providers)
    {
        return providers.FirstOrDefault(p =>
            string.Equals(p.ProviderKey, providerKey, StringComparison.OrdinalIgnoreCase))?.Scheme ?? providerKey;
    }

    private static async Task<string?> ReadReturnUrlAsync(HttpRequest request, CancellationToken ct)
    {
        if (request.HasFormContentType)
        {
            var form = await request.ReadFormAsync(ct);
            return form["returnUrl"].ToString();
        }

        return request.Query["returnUrl"].ToString();
    }

    private static string NormalizeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return "/";
        }

        return IsLocalUrl(returnUrl) ? returnUrl : "/";
    }

    private static bool IsLocalUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return false;
        }

        if (url[0] == '/')
        {
            if (url.Length == 1)
            {
                return true;
            }

            return url[1] != '/' && url[1] != '\\';
        }

        return url[0] == '~' && url.Length > 1 && url[1] == '/';
    }
}
