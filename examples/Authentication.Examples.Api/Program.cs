using System.Collections.Concurrent;
using System.Security.Claims;
using Examples.ServiceDefaults;
using Microsoft.AspNetCore.Authorization;
using NexArc.Authentication.Abstractions.Models;
using NexArc.Authentication.Api.Extensions;
using NexArc.Authentication.DevicePairing.Extensions;
using NexArc.Authentication.DevicePairing.Models;
using NexArc.Authentication.DevicePairing.Services;
using NexArc.Authentication.MagicLink.Extensions;
using NexArc.Authentication.MagicLink.Models;
using NexArc.Authentication.MagicLink.Services;
using NexArc.Authentication.Provider.Auth0B2C.Extensions;
using NexArc.Authentication.Provider.AzureB2C.Extensions;
using NexArc.Authentication.Provider.GoogleWorkspace.Extensions;
using NexArc.Authentication.Provider.Microsoft365.Extensions;

var builder = WebApplication.CreateBuilder(args);
var auth = builder.Configuration.GetRequiredSection("Auth");
var providers = auth.GetRequiredSection("Providers");

builder.AddApiAuthentication(auth);
builder.Services.AddProviderGoogleWorkspace(providers.GetRequiredSection("GoogleWorkspace"))
    .AddProviderAzureB2C(providers.GetRequiredSection("AzureB2C"))
    .AddProviderAuth0B2C(providers.GetRequiredSection("Auth0B2C"))
    .AddProviderMicrosoft365(providers.GetRequiredSection("Microsoft365"))
    .AddProviderMagicLink(providers.GetRequiredSection("MagicLink"))
    .AddProviderDevicePairing(providers.GetRequiredSection("DevicePairing"));

builder.Services.AddSingleton<IMagicLinkSessionStore, InMemoryMagicLinkSessionStore>();
builder.Services.AddSingleton<IMagicLinkVerifier, ExampleMagicLinkVerifier>();
builder.Services.AddSingleton<IDevicePairingSessionStore, InMemoryDevicePairingSessionStore>();
builder.Services.AddSingleton<IDevicePairingVerifier, ExampleDevicePairingVerifier>();

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthentication();

app.MapGet("/", (HttpContext context) => Results.Ok(ExampleAuthenticationPages.BuildWhoAmIPayload(context)));

app.MapGet("/me", [Authorize] (HttpContext context) =>
{
    var claims = context.User.Claims.Select(c => new { c.Type, c.Value });
    return Results.Ok(new
    {
        context.User.Identity?.Name,
        Claims = claims
    });
});

app.Run();

sealed class InMemoryMagicLinkSessionStore : IMagicLinkSessionStore
{
    private readonly ConcurrentDictionary<string, MagicLinkSession> _sessions = new(StringComparer.OrdinalIgnoreCase);

    public Task SaveAsync(MagicLinkSession session, CancellationToken ct)
    {
        _sessions[session.SessionId] = session;
        _sessions[session.Code] = session;
        return Task.CompletedTask;
    }

    public Task<MagicLinkSession?> FindByCodeAsync(string code, CancellationToken ct)
    {
        _sessions.TryGetValue(code, out var session);
        return Task.FromResult(session);
    }

    public Task CompleteAsync(string sessionId, CancellationToken ct)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            _sessions.TryRemove(sessionId, out _);
            _sessions.TryRemove(session.Code, out _);
        }

        return Task.CompletedTask;
    }
}

sealed class ExampleMagicLinkVerifier : IMagicLinkVerifier
{
    public Task<MagicLinkApproval> ApproveAsync(MagicLinkSession session, CancellationToken ct)
    {
        var identity = new IssuedIdentity
        {
            Subject = session.Destination,
            Email = session.Destination,
            Name = "Magic Link User"
        };

        return Task.FromResult(new MagicLinkApproval { Identity = identity });
    }
}

sealed class InMemoryDevicePairingSessionStore : IDevicePairingSessionStore
{
    private readonly ConcurrentDictionary<string, DevicePairingSession> _sessions = new(StringComparer.OrdinalIgnoreCase);

    public Task SaveAsync(DevicePairingSession session, CancellationToken ct)
    {
        _sessions[session.SessionId] = session;
        _sessions[session.Code] = session;
        return Task.CompletedTask;
    }

    public Task<DevicePairingSession?> FindByCodeAsync(string code, CancellationToken ct)
    {
        _sessions.TryGetValue(code, out var session);
        return Task.FromResult(session);
    }

    public Task CompleteAsync(string sessionId, CancellationToken ct)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            _sessions.TryRemove(sessionId, out _);
            _sessions.TryRemove(session.Code, out _);
        }

        return Task.CompletedTask;
    }
}

sealed class ExampleDevicePairingVerifier : IDevicePairingVerifier
{
    public Task<DevicePairingApproval> ApproveAsync(DevicePairingSession session, CancellationToken ct)
    {
        var identity = new IssuedIdentity
        {
            Subject = session.DeviceId ?? session.Code,
            Name = "Paired Device",
            Roles = new[] { "Device" }
        };

        return Task.FromResult(new DevicePairingApproval { Identity = identity });
    }
}
