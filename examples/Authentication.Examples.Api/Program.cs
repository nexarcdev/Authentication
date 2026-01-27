using System.Collections.Concurrent;
using System.Security.Claims;
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

builder.Services.AddAppAuthentication(options =>
    {
        options.Issuer = builder.Configuration["Auth:Issuer"] ?? "https://localhost:5001";
        options.Audience = builder.Configuration["Auth:Audience"] ?? "api";
    })
    .AddProviderGoogleWorkspace(builder.Configuration.GetSection("Auth:Providers:GoogleWorkspace"))
    .AddProviderAzureB2C(builder.Configuration.GetSection("Auth:Providers:AzureB2C"))
    .AddProviderAuth0B2C(builder.Configuration.GetSection("Auth:Providers:Auth0B2C"))
    .AddProviderMicrosoft365(builder.Configuration.GetSection("Auth:Providers:Microsoft365"))
    .AddProviderMagicLink(builder.Configuration.GetSection("Auth:Providers:MagicLink"))
    .AddProviderDevicePairing(builder.Configuration.GetSection("Auth:Providers:DevicePairing"));

builder.Services.AddSingleton<IMagicLinkSessionStore, InMemoryMagicLinkSessionStore>();
builder.Services.AddSingleton<IMagicLinkVerifier, ExampleMagicLinkVerifier>();
builder.Services.AddSingleton<IDevicePairingSessionStore, InMemoryDevicePairingSessionStore>();
builder.Services.AddSingleton<IDevicePairingVerifier, ExampleDevicePairingVerifier>();

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthentication();

app.MapGet("/", (HttpContext context) => Results.Ok(BuildWhoAmI(context)));

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

static object BuildWhoAmI(HttpContext context)
{
    var user = context.User;
    return new
    {
        user.Identity?.IsAuthenticated,
        user.Identity?.AuthenticationType,
        user.Identity?.Name,
        Roles = GetRoles(user),
        Claims = user.Claims.Select(c => new { c.Type, c.Value })
    };
}

static IReadOnlyList<string> GetRoles(ClaimsPrincipal user)
{
    return user.FindAll(ClaimTypes.Role)
        .Select(c => c.Value)
        .Concat(user.FindAll("role").Select(c => c.Value))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
}

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
