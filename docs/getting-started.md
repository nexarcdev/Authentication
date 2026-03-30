# Getting Started

This guide provides the core flow and then directs you to provider-specific setup steps.

## Core Flows
OIDC web client:
1. Client signs in with its configured IdP (OIDC Auth Code + PKCE)
2. Client exchanges external tokens with the API (`POST /auth/exchange/{providerKey}`)
3. API issues access tokens used for all API calls
4. Automatic refresh keeps sessions alive

Non-OIDC client:
1. Client completes the provider-specific interaction
2. Client calls the provider-specific API endpoint
3. API issues access tokens used for all API calls

## Default Session Policy
- Access token lifetime: `16 hours`
- Refresh token lifetime (sliding idle window): `16 hours`
- Absolute session lifetime cap: `7 days`
- Client automatic refresh: enabled (`RefreshBeforeExpiry = 1 minute`)

Override example:
```csharp
var auth = builder.Configuration.GetRequiredSection("Auth");

builder.Services.AddApiAuthentication(options =>
{
    options.Issuer = auth["Issuer"];
    options.Audience = auth["Audience"];
    options.AccessTokenLifetime = TimeSpan.FromHours(16);
    options.RefreshTokensEnabled = true;
    options.RefreshTokenLifetime = TimeSpan.FromHours(16);
    options.SessionAbsoluteLifetime = TimeSpan.FromDays(7);
});
```

## Install Packages
API:
```powershell
dotnet add package NexArc.Authentication.Abstractions
dotnet add package NexArc.Authentication.Api
dotnet add package NexArc.Authentication.Provider.GoogleWorkspace
```

Client:
```powershell
dotnet add package NexArc.Authentication.Abstractions
dotnet add package NexArc.Authentication.Client
dotnet add package NexArc.Authentication.Provider.GoogleWorkspace
```

Optional (only if you need them):
```powershell
dotnet add package NexArc.Authentication.MagicLink
dotnet add package NexArc.Authentication.DevicePairing
dotnet add package NexArc.Authentication.Utilities
```

Replace the provider package with the one you are using (AzureB2C, Auth0B2C, Microsoft365, GoogleWorkspace, MagicLink, DevicePairing).

## Deployment Note (API Not Public)
The API may be internal-only. Public-facing interactions (magic link and device pairing) are handled by the client app, which exposes the user-facing endpoints and calls the API from the backend.

## Program.cs (API)
```csharp
var builder = WebApplication.CreateBuilder(args);
var auth = builder.Configuration.GetRequiredSection("Auth");
var googleWorkspace = auth.GetRequiredSection("Providers").GetRequiredSection("GoogleWorkspace");

builder.AddApiAuthentication(auth);
builder.Services.AddProviderGoogleWorkspace(googleWorkspace);

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapAuthentication();
app.Run();
```

## Program.cs (OIDC Web Client)
```csharp
var builder = WebApplication.CreateBuilder(args);
var auth = builder.Configuration.GetRequiredSection("Auth");
var googleWorkspace = auth.GetRequiredSection("Providers").GetRequiredSection("GoogleWorkspace");

builder.AddOidcClientAuthentication(auth);
builder.Services.AddProviderGoogleWorkspace(googleWorkspace);

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapClientAuthentication(); // sets up /login, /logout, /auth/dev-login, plus provider client endpoints
app.Run();
```

## Program.cs (Non-OIDC Client)
```csharp
var builder = WebApplication.CreateBuilder(args);
var auth = builder.Configuration.GetRequiredSection("Auth");
var magicLink = auth.GetRequiredSection("Providers").GetRequiredSection("MagicLink");

builder.AddClientAuthentication(auth);
builder.Services.AddProviderMagicLink(magicLink);

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapClientAuthentication();
app.Run();
```

Prefer the configuration-based bootstrap helpers for the standard setup path:
- Hosted API: `AddApiAuthentication(auth)` then explicit `AddProvider...(...)`
- OIDC web client: `AddOidcClientAuthentication(auth)` then explicit `AddProvider...(...)`
- Non-OIDC client: `AddClientAuthentication(auth)` then explicit `AddProvider...(...)`

The standard entry points are role- and flow-specific:
- `AddApiAuthentication(auth)` for token-issuing API hosts
- `AddOidcClientAuthentication(auth)` for OIDC web clients
- `AddClientAuthentication(auth)` for non-OIDC clients such as magic link and device pairing

For advanced composition, the `Action<...>` overloads of `AddApiAuthentication(...)` and `AddClientAuthentication(...)`, along with `AddApiTokenExchangeOnOidcSignIn(...)`, remain available.

Replace the provider and configuration section with the guide for your chosen provider below.

## Endpoints Created by `MapAuthentication` (API)
`MapAuthentication` is API-only. It registers exchange, refresh, and any provider-specific API endpoints.

Common API endpoints:
- `POST /auth/exchange/{providerKey}` – exchange external tokens for API tokens
- `POST /auth/refresh` – refresh API tokens (if enabled)

Source: API endpoint mappings created by `MapAuthentication`.

Provider API endpoints (examples):
- Device Pairing: `POST /auth/device-pairing/code`, `POST /auth/device-pairing/resolve`, `GET /auth/device-pairing/qr/{code}`
- Magic Link: `POST /auth/magic-link/request`, `POST /auth/magic-link/redeem`

Source: API endpoint mappings created by `MapAuthentication` plus provider-specific API modules.

## Client-Side Endpoints (OIDC Callbacks)
OIDC callbacks are handled by the client app, not the API. `MapAuthentication` is API-only; OIDC clients use `MapClientAuthentication` plus `AddOidcClientAuthentication(auth)`.

Client callback endpoints (examples):
- `GET /signin-oidc` – default OIDC callback path
- `GET /signout-callback-oidc` – optional sign-out callback

Source: OpenIdConnect handler registered by the OIDC provider package and configured for token exchange by `AddOidcClientAuthentication(auth)`.

## Client-Side Mapped Endpoints (All Clients)
`MapClientAuthentication` registers:
- `GET /login` – starts the OIDC challenge
- `GET|POST /logout` – clears local token store (and optional upstream sign-out)
- `GET|POST /auth/dev-login` – dev-only login helper (Development + DevBypass enabled)

Some providers require additional public client endpoints for user interaction:
- Magic Link routes (request + redeem)
- Device pairing routes (code entry, QR display)

Source: client endpoint mappings created by `MapClientAuthentication` in the client app.
Paths are based on the provider key, for example:
- Magic Link (default provider key `magic-link`): `POST /magic-link/request`, `POST /magic-link/redeem`
- Device Pairing (default provider key `device-pairing`): `POST /device-pairing/code`, `POST /device-pairing/resolve`, `GET /device-pairing/qr/{code}`

Use the provider client configuration to set the callback paths expected by the middleware and your IdP.

## Provider Scheme and Endpoint Key
Each `AddProvider...` must set a unique scheme/key used to:
- filter authentication to a specific provider
- namespace provider-specific endpoint names

This can be overridden to support multiple instances of the same provider (e.g., separate Auth0 tenants for staff and customers).
Override with `ProviderKey` and `Scheme` in the provider configuration section.

## Choose Your Provider
### OIDC Web Providers
- [Azure B2C](getting-started/azure-b2c.md)
- [Auth0 (B2C)](getting-started/auth0-b2c.md)
- [Google Workspace (SSO)](getting-started/google-workspace.md)
- [Microsoft 365 (SSO)](getting-started/microsoft-365.md)

### Non-OIDC Providers
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
            { "subject": "test-user", "name": "Test User", "email": "test@example.com", "roles": ["Staff"] }
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

Dev bypass for exchange (API):
- Provide `DevBypassUser` in the exchange request to mint tokens for a configured dev user.

Example request:
```json
{
  "DevBypassUser": "test@example.com"
}
```
