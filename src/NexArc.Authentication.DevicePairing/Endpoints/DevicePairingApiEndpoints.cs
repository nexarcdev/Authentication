using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NexArc.Authentication.Api.Services;
using NexArc.Authentication.Abstractions.Models;
using NexArc.Authentication.DevBypass.Models;
using NexArc.Authentication.DevicePairing.Models;
using NexArc.Authentication.DevicePairing.Options;
using NexArc.Authentication.DevicePairing.Services;
using NexArc.Authentication.Utilities;
using Microsoft.Extensions.Hosting;

namespace NexArc.Authentication.DevicePairing.Endpoints;

public sealed class DevicePairingApiEndpoints : IApiEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var settings = endpoints.ServiceProvider.GetRequiredService<IOptions<DevicePairingOptions>>().Value;
        var group = endpoints.MapGroup($"/auth/{settings.ProviderKey}");

        group.MapPost("/code", async (
            DevicePairingRequest? request,
            ISecureCodeGenerator codeGenerator,
            IDevicePairingSessionStore sessionStore,
            CancellationToken ct) =>
        {
            var now = DateTimeOffset.UtcNow;
            var code = codeGenerator.Generate(settings.CodeLength, settings.CodeAlphabet);

            var session = new DevicePairingSession
            {
                SessionId = Guid.NewGuid().ToString("N"),
                Code = code,
                DeviceId = request?.DeviceId,
                CreatedAt = now,
                ExpiresAt = now.AddSeconds(settings.CodeLifetimeSeconds)
            };

            await sessionStore.SaveAsync(session, ct);

            var response = new DevicePairingIssuedResponse
            {
                SessionId = session.SessionId,
                Code = session.Code,
                ExpiresAt = session.ExpiresAt,
                PairingUrl = BuildPairingUrl(settings.PairingUrl, session.Code)
            };

            return Results.Ok(response);
        });

        group.MapPost("/resolve", async (
            DevicePairingResolveRequest? request,
            IDevicePairingSessionStore sessionStore,
            IDevicePairingVerifier verifier,
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

            var bypassUser = GetDevBypassUser(settings, session.DeviceId, environment);
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

        group.MapGet("/qr/{code}", (
            string code) =>
        {
            var payload = BuildPairingUrl(settings.PairingUrl, code) ?? code;
            return Results.Ok(new { payload });
        });
    }

    private static DevBypassUser? GetDevBypassUser(
        DevicePairingOptions settings,
        string? deviceId,
        IHostEnvironment environment)
    {
        if (!environment.IsDevelopment() || !settings.DevBypass.Enabled)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return null;
        }

        return settings.DevBypass.Devices
            .FirstOrDefault(d => string.Equals(d.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase))
            ?.User;
    }

    private static string? BuildPairingUrl(string? baseUrl, string code)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return null;
        }

        var separator = baseUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return $"{baseUrl}{separator}code={Uri.EscapeDataString(code)}";
    }
}
