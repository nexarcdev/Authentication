# Magic Link (Getting Started)

## Summary
Magic link uses a short-lived code delivered to the user (email/SMS). The client redeems the code to obtain API tokens.

## Endpoints
API:
- `POST /auth/magic-link/request`
- `POST /auth/magic-link/redeem`
Source: API endpoint mappings created by `MapAuthentication` plus the magic link module.

Client:
- `POST /magic-link/request`
- `POST /magic-link/redeem`
Source: client endpoint mappings created by `MapClientAuthentication`.

Paths assume the default provider key `magic-link`. If you override `ProviderKey`, replace the prefix accordingly.

## Required API Services
The API owns the magic link workflow and must provide storage and verification via DI.

Example interfaces (API):
```csharp
public interface IMagicLinkSessionStore
{
    Task SaveAsync(MagicLinkSession session, CancellationToken ct);
    Task<MagicLinkSession?> FindByCodeAsync(string code, CancellationToken ct);
    Task CompleteAsync(string sessionId, CancellationToken ct);
}

public interface IMagicLinkVerifier
{
    Task<MagicLinkApproval> ApproveAsync(MagicLinkSession session, CancellationToken ct);
}
```

API registration example:
```csharp
builder.Services.AddScoped<IMagicLinkSessionStore, MagicLinkSessionStore>();
builder.Services.AddScoped<IMagicLinkVerifier, MagicLinkVerifier>();
```

How the library finds these:
- The provider resolves them from DI at runtime
- Missing registrations are treated as startup errors

## Required Client Services
The client is responsible for delivering the magic link (email/SMS/push). Provide a notifier implementation via DI.

Example interface:
```csharp
public interface IMagicLinkNotifier
{
    Task SendAsync(string destination, string code, string link, CancellationToken ct);
}
```

Client registration example:
```csharp
builder.Services.AddScoped<IMagicLinkNotifier, MagicLinkNotifier>();
```

## Scheme and Endpoint Key
Set a unique scheme/key for this provider to control auth filtering and endpoint naming. Override it if you need multiple magic link experiences in the same app.
Override with `ProviderKey` and `Scheme` in the provider configuration section.

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

builder.Services.AddScoped<IMagicLinkSessionStore, MagicLinkSessionStore>();
builder.Services.AddScoped<IMagicLinkVerifier, MagicLinkVerifier>();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapAuthentication();
app.Run();
```

## Program.cs (Client)
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddClientAuthentication(options =>
    {
        options.ProviderKey = builder.Configuration["Auth:ProviderKey"] ?? "magic-link";
        options.ApiBaseUrl = builder.Configuration["Auth:ApiBaseUrl"];
    })
    .AddProviderMagicLink(builder.Configuration.GetSection("Auth:Providers:MagicLink"));

builder.Services.AddScoped<IMagicLinkNotifier, MagicLinkNotifier>();
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
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
        "CodeLifetimeSeconds": 600,
        "RedeemUrl": "https://app.example.local/magic"
      }
    }
  }
}
```

`RedeemUrl` is used by the API to build the link returned from `POST /auth/{providerKey}/request`. If you omit it, `RedeemUrl` will be null and the client must construct the link.

## Client Configuration
```json
{
  "Auth": {
    "ProviderKey": "magic-link",
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
- When enabled in Development, configured destinations auto-approve on redeem
- The API still enforces code lifetime and single-use semantics

## Uses Secure Code Generator
See [Secure Code Generator](secure-code-generator.md) for details on allowed alphabets and QR payloads.
