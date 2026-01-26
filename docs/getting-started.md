# Getting Started

This guide provides the core flow and then directs you to provider-specific setup steps.

## Core Flow (All Providers)
1. Client signs in with its configured IdP (OIDC Auth Code + PKCE)
2. Client exchanges external tokens with the API (`POST /auth/exchange/{providerKey}`)
3. API issues access tokens used for all API calls
4. Optional refresh keeps sessions alive

## Deployment Note (API Not Public)
The API may be internal-only. Public-facing interactions (magic link and device pairing) are handled by the client app, which exposes the user-facing endpoints and calls the API from the backend.

## Program.cs (API)
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAppAuthentication(options =>
    {
        options.Issuer = builder.Configuration["Auth:Issuer"];
        options.Audience = builder.Configuration["Auth:Audience"];
    })
    .AddProviderGoogleWorkspace(builder.Configuration.GetSection("Auth:Providers:GoogleWorkspace"));

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
        options.ProviderKey = "google-workspace";
        options.ApiBaseUrl = builder.Configuration["Auth:ApiBaseUrl"];
    })
    .AddProviderGoogleWorkspace(builder.Configuration.GetSection("Auth:Providers:GoogleWorkspace"));

var app = builder.Build();
app.Run();
```

Replace the provider and configuration section with the guide for your chosen provider below.

## Endpoints Created by `MapAuthentication` (API)
`MapAuthentication` is API-only. It registers exchange, refresh, and any provider-specific API endpoints.

Common API endpoints:
- `POST /auth/exchange/{providerKey}` – exchange external tokens for API tokens
- `POST /auth/refresh` – refresh API tokens (if enabled)

Source: API endpoint mappings created by `MapAuthentication`.

Provider API endpoints (examples):
- Device Pairing: `POST /auth/device-pairing/code`, `POST /auth/device-pairing/resolve`
- Magic Link: `POST /auth/magic-link/request`, `POST /auth/magic-link/redeem`
- QR visualization: `GET /auth/qr/{providerKey}/{code}` (if enabled)

Source: API endpoint mappings created by `MapAuthentication` plus provider-specific API modules.

## Client-Side Endpoints (OIDC Callbacks)
OIDC callbacks are handled by the client app, not the API. `MapAuthentication` is not used on clients.

Client callback endpoints (examples):
- `GET /signin-oidc` – default OIDC callback path
- `GET /signout-callback-oidc` – optional sign-out callback

Source: OpenIdConnect handler registered by `AddClientAuthentication` (middleware-owned endpoints).

## Client-Side Mapped Endpoints (Magic Link + Device Pairing)
Some providers require public client endpoints for user interaction:
- Magic Link redemption routes
- Device pairing routes (code entry, QR display)

Source: client endpoint mappings created by `MapClientAuthentication` in the client app.

Use the provider client configuration to set the callback paths expected by the middleware and your IdP.

## Provider Scheme and Endpoint Key
Each `AddProvider...` must set a unique scheme/key used to:
- filter authentication to a specific provider
- namespace provider-specific endpoint names

This can be overridden to support multiple instances of the same provider (e.g., separate Auth0 tenants for staff and customers).

## Choose Your Provider
- [Azure B2C](getting-started/azure-b2c.md)
- [Auth0 (B2C)](getting-started/auth0-b2c.md)
- [Google Workspace (SSO)](getting-started/google-workspace.md)
- [Microsoft 365 (SSO)](getting-started/microsoft-365.md)
- [Device Pairing](getting-started/device-pairing.md)
- [Magic Link](getting-started/magic-link.md)

## Shared Configuration Concepts
- API is the single token issuer
- Clients configure only the provider they use
- Development bypass is automatic and driven by per-provider config
- Google Workspace supports hosted domain allowlist via `AllowedDomains`

## Utilities
- [Secure Code Generator](getting-started/secure-code-generator.md)

## Development Bypass Summary
Enable per provider:
```json
{
  "Auth": {
    "Providers": {
      "GoogleWorkspace": {
        "DevBypass": {
          "Enabled": true,
          "Users": [
            { "name": "Test User", "email": "test@example.com", "roles": ["Staff"] }
          ]
        }
      }
    }
  }
}
```

Behavior:
- Bypass is automatic and per provider
- Startup fails if enabled outside `Development`
