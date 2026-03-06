[![Publish NuGet Packages](https://github.com/nexarcdev/Authentication/actions/workflows/nuget-publish.yml/badge.svg)](https://github.com/nexarcdev/Authentication/actions/workflows/nuget-publish.yml) [![NuGet Package](https://img.shields.io/nuget/v/NexArc.Authentication.Abstractions.svg)](https://www.nuget.org/packages/NexArc.Authentication.Abstractions)

# Authentication Toolkit for ASP.NET & Blazor

A set of NuGet packages that provides a clean, standards-based authentication model for ASP.NET and Blazor. The API is the single token issuer. Each client app uses only the identity provider (IdP) it needs, then exchanges external identities for API-issued tokens.

## Purpose and Capabilities
- Consistent authentication model across APIs + multiple client apps
- Standards-based OIDC/OAuth flows with opinionated defaults
- Token exchange flow that keeps API auth first-party and centralized
- Client helpers for login/logout, token storage, and API calls
- Extensible providers for enterprise and consumer IdPs
- Magic link and device pairing flows for non-traditional sign-in
- Development bypass with strict environment guardrails

## Core Principles
- **Single issuer for the API:** the API trusts only tokens it issues
- **Token exchange flow:** clients exchange external tokens for API-issued tokens
- **Opinionated defaults:** sensible choices with extension points
- **No branding in protocols:** no custom cookie names or branded claims

## Package Layout (NuGet)
- `NexArc.Authentication.Abstractions` - shared primitives, options, interfaces
- `NexArc.Authentication.Api` - token exchange endpoints, token issuance/validation
- `NexArc.Authentication.Client` - client auth state, token storage, API client helpers
- `NexArc.Authentication.DevBypass` - internal dev bypass guardrails
- `NexArc.Authentication.MagicLink` - magic link flow (API + client endpoints)
- `NexArc.Authentication.DevicePairing` - device pairing flow (API + client endpoints)
- `NexArc.Authentication.Utilities` - secure code generator
- Provider packages (one per IdP) - client wiring + API validation

## Supported Providers
- Google Workspace (SSO)
- Microsoft 365 (Entra ID)
- Azure AD B2C
- Auth0 (B2C)
- Magic link (code + link)
- Device pairing (short code + optional QR)

## Quick Start

### Install packages
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

Replace the provider package with the one you are using (AzureB2C, Auth0B2C, Microsoft365, GoogleWorkspace, MagicLink, DevicePairing).

### 1) API
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAppAuthentication(options =>
    {
        options.Issuer = builder.Configuration["Auth:Issuer"];
        options.Audience = builder.Configuration["Auth:Audience"];
    })
    .AddProviderGoogleWorkspace(builder.Configuration.GetSection("Auth:Providers:GoogleWorkspace"))
    .AddProviderAzureB2C(builder.Configuration.GetSection("Auth:Providers:AzureB2C"));

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapAuthentication();
app.Run();
```

### 2) Client App (Blazor or ASP.NET UI)
```csharp
var builder = WebApplication.CreateBuilder(args);

var providerKey = builder.Configuration["Auth:ProviderKey"] ?? "google-workspace";
var providerScheme = builder.Configuration["Auth:Providers:GoogleWorkspace:Scheme"] ?? providerKey;

builder.Services
    .AddClientAuthentication(options =>
    {
        options.ProviderKey = providerKey;
        options.ApiBaseUrl = builder.Configuration["Auth:ApiBaseUrl"];
    })
    .AddProviderGoogleWorkspace(builder.Configuration.GetSection("Auth:Providers:GoogleWorkspace"));

builder.Services.AddAuthorization();
builder.Services.AddApiTokenExchangeOnOidcSignIn(providerScheme, providerKey);

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapClientAuthentication(); // adds /login, /logout, /auth/dev-login, plus provider client endpoints
app.Run();
```

## How It Works (Token Exchange)
1. Client signs in with its configured IdP using OIDC (Auth Code + PKCE)
2. Client exchanges external tokens with the API (`POST /auth/exchange/{providerKey}`)
3. API validates the external token, normalizes identity, and issues API tokens
4. Client uses API-issued access token on all API calls
5. Automatic refresh keeps sessions alive without frequent IdP prompts

## Default Session Policy
- Access token lifetime: `16 hours`
- Refresh tokens: enabled by default
- Refresh token lifetime (sliding idle window): `16 hours`
- Absolute session lifetime cap: `7 days`
- Client automatic refresh: enabled by default (`RefreshBeforeExpiry = 1 minute`)

You can override these defaults in the API setup:
```csharp
builder.Services.AddAppAuthentication(options =>
{
    options.Issuer = builder.Configuration["Auth:Issuer"];
    options.Audience = builder.Configuration["Auth:Audience"];
    options.AccessTokenLifetime = TimeSpan.FromHours(16);
    options.RefreshTokensEnabled = true;
    options.RefreshTokenLifetime = TimeSpan.FromHours(16);
    options.SessionAbsoluteLifetime = TimeSpan.FromDays(7);
});
```

You can also tune client refresh behavior:
```csharp
builder.Services.AddClientAuthentication(options =>
{
    options.ProviderKey = builder.Configuration["Auth:ProviderKey"] ?? "google-workspace";
    options.ApiBaseUrl = builder.Configuration["Auth:ApiBaseUrl"];
    options.AutomaticTokenRefreshEnabled = true;
    options.RefreshBeforeExpiry = TimeSpan.FromMinutes(1);
});
```

## Magic Link and Device Pairing Requirements
- API must register session storage + verification services via DI
- Client must provide a notifier for delivering magic links (email/SMS/push)
- Device pairing requires no client-side services beyond auth configuration
- Client endpoints are mapped under the provider key (default `magic-link` / `device-pairing`)

## Development Bypass
- Development bypass is automatic and driven by per-provider config
- Enable it under `Auth:Providers:<Provider>:DevBypass:Enabled`
- Provide test users under `Auth:Providers:<Provider>:DevBypass:Users` (IdP providers)
- Magic link uses `Auth:Providers:MagicLink:DevBypass:Destinations`
- Device pairing uses `Auth:Providers:DevicePairing:DevBypass:Devices`
- Magic link auto-approves configured destinations during redeem in Development
- Device pairing auto-approves configured devices during resolve in Development
- Clients must implement a notifier interface for user delivery (email/SMS)
- Hard guardrail: if enabled outside Development, startup fails
- Dev bypass exchange supports `DevBypassUser` to mint tokens for configured users

## Provider Notes
- Google Workspace can restrict sign-in to a hosted domain allowlist
- Configure `AllowedDomains` as an array; empty means allow all Workspace domains

## Docs and Examples
- [Getting Started](docs/getting-started.md)
- Examples: `examples/Authentication.Examples.*`

## Environment Variables (Production)
ASP.NET configuration supports environment variables using `__` as the section separator (example: `Auth__Issuer` maps to `Auth:Issuer`).

### API (common)
- `Auth__Issuer` (required)
- `Auth__Audience` (required)
- `Auth__AccessTokenLifetime` (optional)
- `Auth__RefreshTokensEnabled` (optional)
- `Auth__RefreshTokenLifetime` (optional)
- `Auth__SessionAbsoluteLifetime` (optional)

### API (provider-specific)
Google Workspace:
- `Auth__Providers__GoogleWorkspace__Authority` (required)
- `Auth__Providers__GoogleWorkspace__ClientId` (required)
- `Auth__Providers__GoogleWorkspace__ClientSecret` (required)
- `Auth__Providers__GoogleWorkspace__AllowedDomains__0`, `__1`, ... (optional)

Microsoft 365 (Entra ID):
- `Auth__Providers__Microsoft365__Authority` (required)
- `Auth__Providers__Microsoft365__ClientId` (required)
- `Auth__Providers__Microsoft365__ClientSecret` (required)
- `Auth__Providers__Microsoft365__AllowedTenants__0`, `__1`, ... (optional)

Azure AD B2C:
- `Auth__Providers__AzureB2C__Authority` (required)
- `Auth__Providers__AzureB2C__ClientId` (required)
- `Auth__Providers__AzureB2C__ClientSecret` (required)
- `Auth__Providers__AzureB2C__AllowedTenants__0`, `__1`, ... (optional)

Auth0 (B2C):
- `Auth__Providers__Auth0B2C__Authority` (required)
- `Auth__Providers__Auth0B2C__ClientId` (required)
- `Auth__Providers__Auth0B2C__ClientSecret` (required)
- `Auth__Providers__Auth0B2C__AllowedTenants__0`, `__1`, ... (optional)

Magic link (API):
- `Auth__Providers__MagicLink__RedeemUrl` (recommended in production)
- `Auth__Providers__MagicLink__CodeLength` (optional)
- `Auth__Providers__MagicLink__CodeAlphabet` (optional)
- `Auth__Providers__MagicLink__CodeLifetimeSeconds` (optional)

Device pairing (API):
- `Auth__Providers__DevicePairing__PairingUrl` (recommended in production)
- `Auth__Providers__DevicePairing__CodeLength` (optional)
- `Auth__Providers__DevicePairing__CodeAlphabet` (optional)
- `Auth__Providers__DevicePairing__CodeLifetimeSeconds` (optional)

### Client (common)
- `Auth__ApiBaseUrl` (required)
- `Auth__ProviderKey` (recommended; defaults per provider)
- `Auth__AuthApiClientName` (optional)
- `Auth__AutomaticTokenRefreshEnabled` (optional)
- `Auth__RefreshBeforeExpiry` (optional)

### Client (provider-specific)
Google Workspace:
- `Auth__Providers__GoogleWorkspace__Authority` (required)
- `Auth__Providers__GoogleWorkspace__ClientId` (required)
- `Auth__Providers__GoogleWorkspace__ClientSecret` (required)
- `Auth__Providers__GoogleWorkspace__RedirectUris__0`, `__1`, ... (required)
- `Auth__Providers__GoogleWorkspace__AllowedDomains__0`, `__1`, ... (optional)

Microsoft 365 (Entra ID):
- `Auth__Providers__Microsoft365__Authority` (required)
- `Auth__Providers__Microsoft365__ClientId` (required)
- `Auth__Providers__Microsoft365__ClientSecret` (required)
- `Auth__Providers__Microsoft365__RedirectUris__0`, `__1`, ... (required)

Azure AD B2C:
- `Auth__Providers__AzureB2C__Authority` (required)
- `Auth__Providers__AzureB2C__ClientId` (required)
- `Auth__Providers__AzureB2C__ClientSecret` (required)
- `Auth__Providers__AzureB2C__RedirectUris__0`, `__1`, ... (required)

Auth0 (B2C):
- `Auth__Providers__Auth0B2C__Authority` (required)
- `Auth__Providers__Auth0B2C__ClientId` (required)
- `Auth__Providers__Auth0B2C__ClientSecret` (required)
- `Auth__Providers__Auth0B2C__RedirectUris__0`, `__1`, ... (required)

Magic link (Client):
- `Auth__Providers__MagicLink__RedeemUrl` (optional; used to build links if API does not set one)

Device pairing (Client):
- `Auth__Providers__DevicePairing__PairingUrl` (optional; used by API for QR payloads)

Notes:
- Use your hosting platform's secret store for `ClientSecret` values.
- Do not enable `DevBypass` in production; startup fails outside `Development`.
- For OIDC providers, set `Authority` to the provider authority/base URL (not the `/authorize` endpoint).

## Status
- This repo is scaffolding for the packages and docs. The goal is a clean, standards-based auth stack that feels native to ASP.NET and Blazor.
