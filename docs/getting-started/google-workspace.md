# Google Workspace (SSO) (Getting Started)

## Summary
Use Google Workspace as the client IdP for workforce SSO. The API validates Google tokens during exchange and issues API tokens.

## Endpoints
API:
- `POST /auth/exchange/google-workspace`
- `POST /auth/refresh`
Source: API endpoint mappings created by `MapAuthentication`.

Client:
- `GET /signin-oidc`
- `GET /signout-callback-oidc` (optional)
Source: OpenIdConnect handler registered by `AddClientAuthentication`.

Note: no client endpoint mapping is required beyond the OIDC middleware.

## Scheme and Endpoint Key
Set a unique scheme/key for this provider to control auth filtering and endpoint naming. Override it if you need multiple Google Workspace instances in the same app.

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

## API Configuration
```json
{
  "Auth": {
    "Issuer": "https://auth.example.local",
    "Audience": "api",
    "Providers": {
      "GoogleWorkspace": {
        "Authority": "https://accounts.google.com",
        "ClientId": "...",
        "ClientSecret": "...",
        "AllowedDomains": [ "example.com" ]
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
      "GoogleWorkspace": {
        "Authority": "https://accounts.google.com",
        "ClientId": "...",
        "ClientSecret": "...",
        "RedirectUris": [ "https://app.example.local/signin-oidc" ],
        "AllowedDomains": [ "example.com" ]
      }
    }
  }
}
```

## Hosted Domain Filter
- `AllowedDomains` is optional
- If empty or omitted, all Workspace domains are allowed

## Development Bypass
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
