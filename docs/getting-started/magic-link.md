# Magic Link (Getting Started)

## Summary
Magic link uses a short-lived code delivered to the user (email/SMS). The client redeems the code to obtain API tokens.

## Endpoints
API:
- `POST /auth/magic-link/request`
- `POST /auth/magic-link/redeem`
- `GET /auth/qr/magic-link/{code}` (optional)
Source: API endpoint mappings created by `MapAuthentication` plus the magic link module.

Client:
- `GET /magic` (example redemption UI route)
Source: client endpoint mappings created by `MapClientAuthentication` plus host app UI routes.

## Required Client Services
The library runs the magic link flow, but the client app supplies storage, verification, and delivery via DI.

Example interfaces:
```csharp
public interface IMagicLinkSessionStore
{
    Task SaveAsync(MagicLinkSession session, CancellationToken ct);
    Task<MagicLinkSession?> FindByCodeAsync(string code, CancellationToken ct);
    Task CompleteAsync(string sessionId, CancellationToken ct);
}

public interface IMagicLinkVerifier
{
    Task<MagicLinkApproval> ApproveAsync(MagicLinkRequest request, CancellationToken ct);
}

public interface IMagicLinkNotifier
{
    Task SendAsync(string destination, string code, string link, CancellationToken ct);
}
```

Registration example:
```csharp
builder.Services.AddScoped<IMagicLinkSessionStore, MagicLinkSessionStore>();
builder.Services.AddScoped<IMagicLinkVerifier, MagicLinkVerifier>();
builder.Services.AddScoped<IMagicLinkNotifier, MagicLinkNotifier>();
```

How the library finds these:
- The provider resolves them from DI at runtime
- Missing registrations are treated as startup errors

## Scheme and Endpoint Key
Set a unique scheme/key for this provider to control auth filtering and endpoint naming. Override it if you need multiple magic link experiences in the same app.

## Program.cs (API)
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAppAuthentication(options =>
    {
        options.Issuer = builder.Configuration["Auth:Issuer"];
        options.Audience = builder.Configuration["Auth:Audience"];
    })
    .AddProviderMagicLink(builder.Configuration.GetSection("Auth:Providers:MagicLink"));

var app = builder.Build();
app.MapAuthentication();
app.Run();
```

## Program.cs (Client)
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddClientAuthentication(options =>
    {
        options.ProviderKey = "magic-link";
        options.ApiBaseUrl = builder.Configuration["Auth:ApiBaseUrl"];
    })
    .AddProviderMagicLink(builder.Configuration.GetSection("Auth:Providers:MagicLink"));

var app = builder.Build();
app.MapClientAuthentication();
app.Run();
```

## Flow
1. User requests a magic link
2. API generates a short-lived code and a redemption URL
3. Client delivers the code via an app-defined notifier (email/SMS)
4. User redeems the link or code to complete sign-in

## API Configuration
```json
{
  "Auth": {
    "Issuer": "https://auth.example.local",
    "Audience": "api",
    "Providers": {
      "MagicLink": {
        "CodeLength": 8,
        "CodeAlphabet": "Unambiguous",
        "CodeLifetimeSeconds": 600
      }
    }
  }
}
```

## Client Configuration
```json
{
  "Auth": {
    "ApiBaseUrl": "https://api.example.local",
    "Providers": {
      "MagicLink": {
        "RedeemUrl": "https://app.example.local/magic" 
      }
    }
  }
}
```

## Notifier Interface
Clients must provide an interface for user delivery, e.g.:
```csharp
public interface IMagicLinkNotifier
{
    Task SendAsync(string destination, string code, string link, CancellationToken ct);
}
```

## Development Bypass
```json
{
  "Auth": {
    "Providers": {
      "MagicLink": {
        "DevBypass": {
          "Enabled": true,
          "Destinations": [
            {
              "destination": "test@example.com",
              "user": { "name": "Test User", "email": "test@example.com", "roles": ["Staff"] }
            }
          ]
        }
      }
    }
  }
}
```

Behavior:
- When enabled in Development, the API returns the code in the response
- Delivery still flows through your notifier in non-bypass environments

## Uses Secure Code Generator
See [Secure Code Generator](secure-code-generator.md) for details on allowed alphabets and QR payloads.
