using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NexArc.Authentication.Api.Services;
using NexArc.Authentication.Abstractions.Models;
using NexArc.Authentication.DevBypass.Models;
using NexArc.Authentication.MagicLink.Models;
using NexArc.Authentication.MagicLink.Options;
using NexArc.Authentication.MagicLink.Services;
using NexArc.Authentication.Utilities;
using Microsoft.Extensions.Hosting;

namespace NexArc.Authentication.MagicLink.Endpoints;

public sealed class MagicLinkApiEndpoints : IApiEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var settings = endpoints.ServiceProvider.GetRequiredService<IOptions<MagicLinkOptions>>().Value;
        var group = endpoints.MapGroup($"/auth/{settings.ProviderKey}");

        group.MapPost("/request", async (
            MagicLinkRequest? request,
            ISecureCodeGenerator codeGenerator,
            IMagicLinkSessionStore sessionStore,
            CancellationToken ct) =>
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Destination))
            {
                return Results.BadRequest("Destination is required.");
            }

            var now = DateTimeOffset.UtcNow;
            var code = codeGenerator.Generate(settings.CodeLength, settings.CodeAlphabet);

            var session = new MagicLinkSession
            {
                SessionId = Guid.NewGuid().ToString("N"),
                Code = code,
                Destination = request.Destination,
                CreatedAt = now,
                ExpiresAt = now.AddSeconds(settings.CodeLifetimeSeconds)
            };

            await sessionStore.SaveAsync(session, ct);

            var response = new MagicLinkIssuedResponse
            {
                SessionId = session.SessionId,
                Code = session.Code,
                ExpiresAt = session.ExpiresAt,
                RedeemUrl = BuildRedeemUrl(settings.RedeemUrl, session.Code)
            };

            return Results.Ok(response);
        });

        group.MapPost("/redeem", async (
            MagicLinkRedeemRequest? request,
            IMagicLinkSessionStore sessionStore,
            IMagicLinkVerifier verifier,
            ITokenService tokenService,
            IHostEnvironment environment,
            CancellationToken ct) =>
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Code))
            {
                return Results.BadRequest("Code is required.");
            }

            var session = await sessionStore.FindByCodeAsync(request.Code, ct);
            if (session is null || session.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                return Results.Unauthorized();
            }

            var bypassUser = GetDevBypassUser(settings, session.Destination, environment);
            if (bypassUser is not null)
            {
                var subject = bypassUser.Subject ?? bypassUser.Email ?? bypassUser.Name;
                if (string.IsNullOrWhiteSpace(subject))
                {
                    return Results.Unauthorized();
                }

                var tokenPair = await tokenService.IssueAsync(new IssuedIdentity
                {
                    Subject = subject,
                    Email = bypassUser.Email,
                    Name = bypassUser.Name,
                    ProfileUrl = bypassUser.ProfileUrl,
                    Roles = bypassUser.Roles
                }, ct);

                await sessionStore.CompleteAsync(session.SessionId, ct);
                return Results.Ok(tokenPair);
            }

            var approval = await verifier.ApproveAsync(session, ct);
            var tokenPairNormal = await tokenService.IssueAsync(approval.Identity, ct);

            await sessionStore.CompleteAsync(session.SessionId, ct);
            return Results.Ok(tokenPairNormal);
        });
    }

    private static DevBypassUser? GetDevBypassUser(
        MagicLinkOptions settings,
        string destination,
        IHostEnvironment environment)
    {
        if (!environment.IsDevelopment() || !settings.DevBypass.Enabled)
        {
            return null;
        }

        return settings.DevBypass.Destinations
            .FirstOrDefault(d => string.Equals(d.Destination, destination, StringComparison.OrdinalIgnoreCase))
            ?.User;
    }

    private static string? BuildRedeemUrl(string? baseUrl, string code)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return null;
        }

        var separator = baseUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return $"{baseUrl}{separator}code={Uri.EscapeDataString(code)}";
    }
}
