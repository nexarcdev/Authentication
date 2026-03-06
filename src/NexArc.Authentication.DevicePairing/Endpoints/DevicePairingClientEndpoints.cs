using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NexArc.Authentication.Client.Options;
using NexArc.Authentication.Client.Services;
using NexArc.Authentication.DevicePairing.Models;
using NexArc.Authentication.DevicePairing.Options;

namespace NexArc.Authentication.DevicePairing.Endpoints;

public sealed class DevicePairingClientEndpoints : IClientEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var settings = endpoints.ServiceProvider.GetRequiredService<IOptions<DevicePairingOptions>>().Value;
        var group = endpoints.MapGroup($"/{settings.ProviderKey}");

        group.MapPost("/code", async (
            DevicePairingRequest? request,
            IHttpClientFactory httpClientFactory,
            IOptions<ClientAuthenticationOptions> clientOptions,
            CancellationToken ct) =>
        {
            var client = httpClientFactory.CreateClient(clientOptions.Value.AuthApiClientName);
            var response = await client.PostAsJsonAsync($"/auth/{settings.ProviderKey}/code", request, ct);
            if (!response.IsSuccessStatusCode)
            {
                return Results.StatusCode((int)response.StatusCode);
            }

            var issued = await response.Content.ReadFromJsonAsync<DevicePairingIssuedResponse>(cancellationToken: ct);
            return issued is null ? Results.Problem("Invalid response from API.") : Results.Ok(issued);
        });

        group.MapPost("/resolve", async (
            DevicePairingResolveRequest? request,
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
            var response = await client.PostAsJsonAsync($"/auth/{settings.ProviderKey}/resolve", request, ct);
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
