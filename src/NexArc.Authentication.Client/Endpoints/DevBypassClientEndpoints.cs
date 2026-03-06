using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NexArc.Authentication.Abstractions.Models;
using NexArc.Authentication.Client.Options;
using NexArc.Authentication.Client.Services;
using NexArc.Authentication.DevBypass.Models;
using NexArc.Authentication.DevBypass.Services;

namespace NexArc.Authentication.Client.Endpoints;

public sealed class DevBypassClientEndpoints : IClientEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/auth");

        group.MapGet("/dev-login", (
            HttpRequest request,
            IHostEnvironment environment,
            IOptions<ClientAuthenticationOptions> clientOptions,
            IEnumerable<IDevelopmentBypassUserProvider> providers) =>
        {
            if (!environment.IsDevelopment())
            {
                return Results.NotFound();
            }

            var provider = GetProvider(clientOptions.Value.ProviderKey, providers);
            if (provider is null || !provider.Enabled)
            {
                return Results.NotFound();
            }

            var returnUrl = NormalizeReturnUrl(request.Query["returnUrl"].ToString());
            var html = BuildDevLoginPage(provider.ProviderKey, provider.Users, returnUrl);
            return Results.Content(html, "text/html");
        });

        group.MapPost("/dev-login", async (
            HttpRequest request,
            IHostEnvironment environment,
            IHttpClientFactory httpClientFactory,
            IOptions<ClientAuthenticationOptions> clientOptions,
            IEnumerable<IDevelopmentBypassUserProvider> providers,
            ITokenStore tokenStore,
            CancellationToken ct) =>
        {
            if (!environment.IsDevelopment())
            {
                return Results.NotFound();
            }

            var provider = GetProvider(clientOptions.Value.ProviderKey, providers);
            if (provider is null || !provider.Enabled)
            {
                return Results.NotFound();
            }

            var (user, returnUrl, isForm) = await ReadLoginRequestAsync(request, ct);
            if (string.IsNullOrWhiteSpace(user))
            {
                return Results.BadRequest("User is required.");
            }

            var client = httpClientFactory.CreateClient(clientOptions.Value.AuthApiClientName);
            var response = await client.PostAsJsonAsync(
                $"/auth/exchange/{clientOptions.Value.ProviderKey}",
                new { devBypassUser = user },
                ct);

            if (!response.IsSuccessStatusCode)
            {
                return Results.StatusCode((int)response.StatusCode);
            }

            var tokenPair = await response.Content.ReadFromJsonAsync<TokenPair>(cancellationToken: ct);
            if (tokenPair is null)
            {
                return Results.Problem("Invalid response from API.");
            }

            await tokenStore.SetAsync(tokenPair, ct);

            if (isForm)
            {
                return Results.Redirect(NormalizeReturnUrl(returnUrl));
            }

            return Results.Ok(new { redirectUrl = NormalizeReturnUrl(returnUrl) });
        });
    }

    private static IDevelopmentBypassUserProvider? GetProvider(
        string providerKey,
        IEnumerable<IDevelopmentBypassUserProvider> providers)
    {
        return providers.FirstOrDefault(p =>
            string.Equals(p.ProviderKey, providerKey, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<(string? User, string? ReturnUrl, bool IsForm)> ReadLoginRequestAsync(
        HttpRequest request,
        CancellationToken ct)
    {
        if (request.HasFormContentType)
        {
            var form = await request.ReadFormAsync(ct);
            return (form["user"].ToString(), form["returnUrl"].ToString(), true);
        }

        var body = await request.ReadFromJsonAsync<DevLoginRequest>(cancellationToken: ct);
        return (body?.User, body?.ReturnUrl ?? request.Query["returnUrl"].ToString(), false);
    }

    private static string BuildDevLoginPage(
        string providerKey,
        IEnumerable<DevBypassUser> users,
        string returnUrl)
    {
        var title = $"{providerKey} Dev Login";
        var safeTitle = WebUtility.HtmlEncode(title);
        var sb = new StringBuilder();
        var safeReturnUrl = WebUtility.HtmlEncode(returnUrl);

        sb.AppendLine("<!doctype html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"utf-8\" />");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
        sb.AppendLine($"  <title>{safeTitle}</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine("    body { font-family: system-ui, sans-serif; margin: 2rem; }");
        sb.AppendLine("    .card { max-width: 520px; margin: 0 auto; }");
        sb.AppendLine("    form { margin: 0.5rem 0; }");
        sb.AppendLine("    button { width: 100%; padding: 0.75rem 1rem; }");
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("  <div class=\"card\">");
        sb.AppendLine($"    <h1>{safeTitle}</h1>");
        sb.AppendLine("    <p>Select a dev user to sign in.</p>");

        foreach (var user in users)
        {
            var label = WebUtility.HtmlEncode(user.Name ?? user.Email ?? user.Subject ?? "Dev User");
            var value = WebUtility.HtmlEncode(user.Email ?? user.Subject ?? user.Name ?? string.Empty);
            sb.AppendLine("    <form method=\"post\" action=\"/auth/dev-login\">");
            sb.AppendLine($"      <input type=\"hidden\" name=\"user\" value=\"{value}\" />");
            sb.AppendLine($"      <input type=\"hidden\" name=\"returnUrl\" value=\"{safeReturnUrl}\" />");
            sb.AppendLine($"      <button type=\"submit\">Continue as {label}</button>");
            sb.AppendLine("    </form>");
        }

        sb.AppendLine("  </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
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

    private sealed record DevLoginRequest
    {
        public string? User { get; init; }
        public string? ReturnUrl { get; init; }
    }
}
