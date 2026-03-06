using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NexArc.Authentication.Client.Options;
using NexArc.Authentication.Client.Services;
using NexArc.Authentication.MagicLink.Models;
using NexArc.Authentication.MagicLink.Options;
using NexArc.Authentication.MagicLink.Services;

namespace NexArc.Authentication.MagicLink.Endpoints;

public sealed class MagicLinkClientEndpoints : IClientEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var settings = endpoints.ServiceProvider.GetRequiredService<IOptions<MagicLinkOptions>>().Value;
        var group = endpoints.MapGroup($"/{settings.ProviderKey}");

        group.MapPost("/request", async (
            MagicLinkRequest? request,
            IHttpClientFactory httpClientFactory,
            IOptions<ClientAuthenticationOptions> clientOptions,
            IMagicLinkNotifier notifier,
            CancellationToken ct) =>
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Destination))
            {
                return Results.BadRequest("Destination is required.");
            }

            var client = httpClientFactory.CreateClient(clientOptions.Value.AuthApiClientName);
            var response = await client.PostAsJsonAsync("/auth/magic-link/request", request, ct);
            if (!response.IsSuccessStatusCode)
            {
                return Results.StatusCode((int)response.StatusCode);
            }

            var issued = await response.Content.ReadFromJsonAsync<MagicLinkIssuedResponse>(cancellationToken: ct);
            if (issued is null)
            {
                return Results.Problem("Invalid response from API.");
            }

            var link = issued.RedeemUrl ?? string.Empty;
            await notifier.SendAsync(request.Destination, issued.Code, link, ct);

            return Results.Ok(new { issued.SessionId, issued.ExpiresAt });
        });

        group.MapPost("/redeem", async (
            MagicLinkRedeemRequest? request,
            IHttpClientFactory httpClientFactory,
            IOptions<ClientAuthenticationOptions> clientOptions,
            ITokenStore tokenStore,
            CancellationToken ct) =>
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Code))
            {
                return Results.BadRequest("Code is required.");
            }

            var client = httpClientFactory.CreateClient(clientOptions.Value.AuthApiClientName);
            var response = await client.PostAsJsonAsync("/auth/magic-link/redeem", request, ct);
            if (!response.IsSuccessStatusCode)
            {
                return Results.StatusCode((int)response.StatusCode);
            }

            var tokenPair = await response.Content.ReadFromJsonAsync<NexArc.Authentication.Abstractions.Models.TokenPair>(cancellationToken: ct);
            if (tokenPair is null)
            {
                return Results.Problem("Invalid response from API.");
            }

            await tokenStore.SetAsync(tokenPair, ct);
            return Results.Ok();
        });
    }
}
