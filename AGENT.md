# Agent Guide

This repo aims for a clean, standards-based authentication toolkit that feels native to ASP.NET and Blazor. Keep everything generic in behavior and naming, except for the required package/namespace prefix.

## Product Intent
- Opinionated defaults with minimal configuration
- API as the central authority (single issuer)
- Client apps configure only the IdP they use
- Token exchange model (OIDC on the client, API-issued tokens for the API)
- Development bypass for fast local workflows with strict safety guardrails

## Naming & Branding
- Absolutely no branded ("NexArc") class names, method names, protocol names, claims, cookie names, or endpoints
- Use neutral, standards-based terminology (OIDC, OAuth, JWT)
- Use `/auth/*` endpoints in docs and samples
- The only non-generic element should be the `NexArc.Authentication.*` namespace/package prefix
- Do not use the project name in method names, option names, or endpoint names in docs or samples

## Documentation Expectations
- Prefer simple, copy-pasteable code snippets
- Default to minimal configuration with clear extension points
- Document what is required vs optional
- Use short lifetimes and strong defaults

## Architecture Constraints
- The API validates external IdP tokens only during exchange
- The API issues all access tokens used to call protected endpoints
- Clients attach API-issued bearer tokens to all API calls
- Refresh tokens are optional and should be revocable

## Development Bypass Rules
- Development bypass must throw on startup in non-Development environments
- Bypass is opt-in and explicit
- Bypass is configured per provider
- Mock user IDs come from `appsettings.Development.json` arrays
- Magic Link returns the code immediately to the client
- Clients implement a notifier interface for user delivery (SMS/email)

## Engineering Standards
- Fix root causes, never patch symptoms; avoid hacks
- Production-ready, tested, secure; use xUnit for unit tests
- If uncertain, add detailed debugging output and ask the user to run tests
- Never manually edit `.csproj`, `.sln`, or `.slnx` files; use `dotnet` CLI
- Keep code DRY, KISS, SOLID; aim for Microsoft-level quality

## Files to Keep in Sync
- `README.md` for overview and intent
- `docs/getting-started.md` for setup steps
