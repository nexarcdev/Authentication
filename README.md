# Authentication Toolkit for ASP.NET & Blazor

A zero-friction, plug-and-play set of NuGet packages that gives ASP.NET and Blazor apps a consistent, standards-based authentication model with minimal configuration and a clear path to extension. The API is the central authority. Each client app uses only the identity provider (IdP) it needs, then exchanges external identities for first-party tokens issued by the API.

## Goals
- One-stop setup for APIs + multiple client apps
- Centralized IdP configuration in the API
- Simple client setup per app (only the IdP it needs)
- Standards-based OIDC/OAuth flows, clean defaults
- Minimal config now, extensible hooks later
- Development bypass for fast local workflows

## Core Principles
- **Single issuer for the API:** the API trusts only tokens it issues
- **Token exchange (Flow B):** clients exchange external tokens for API-issued tokens
- **Opinionated defaults:** sensible choices with extension points
- **No branding in protocols:** no custom cookie names or branded claims

## Package Layout (NuGet)
- `NexArc.Authentication.Abstractions` – shared primitives, options, interfaces
- `NexArc.Authentication.Api` – token exchange endpoints, token issuance/validation
- `NexArc.Authentication.Client` – client auth state, token storage, API client helpers
- `NexArc.Authentication.DevBypass` – dev portal + guardrails
- Provider packages (one per IdP) – client wiring + API validation

## Quick Start

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
app.MapAuthentication();
app.Run();
```

### 2) Client App (Blazor or ASP.NET UI)
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

## How It Works (Flow B)
1. Client signs in with its configured IdP using OIDC (Auth Code + PKCE)
2. Client exchanges external tokens with the API (`POST /auth/exchange/{providerKey}`)
3. API validates the external token, normalizes identity, and issues API tokens
4. Client uses API-issued access token on all API calls
5. Optional refresh flow keeps sessions alive without frequent IdP prompts

## Development Bypass
- Development bypass is automatic and driven by per-provider config
- Enable it under `Auth:Providers:<Provider>:DevBypass:Enabled`
- Provide test users under `Auth:Providers:<Provider>:DevBypass:Users`
- Magic Link returns the code immediately to the client
- Clients must implement a notifier interface for user delivery (SMS/email)
- Hard guardrail: if enabled outside Development, startup fails

## Provider Notes
- Google Workspace can restrict sign-in to a hosted domain allowlist
- Configure `AllowedDomains` as an array; empty means allow all Workspace domains

## Docs
- [Getting Started](docs/getting-started.md)

## Status
- This repo is scaffolding for the packages and docs. The goal is a clean, standards-based auth stack that feels native to ASP.NET and Blazor.
