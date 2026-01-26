# Microsoft 365 (SSO) (Getting Started)

## Summary
Use Microsoft 365 (Entra ID) as the client IdP for workforce SSO. The API validates Entra ID tokens during exchange and issues API tokens.

## Endpoints
API:
- `POST /auth/exchange/microsoft-365`
- `POST /auth/refresh`
Source: API endpoint mappings created by `MapAuthentication`.

Client:
- `GET /signin-oidc`
- `GET /signout-callback-oidc` (optional)
Source: OpenIdConnect handler registered by `AddClientAuthentication`.

Note: no client endpoint mapping is required beyond the OIDC middleware.

## Scheme and Endpoint Key
Set a unique scheme/key for this provider to control auth filtering and endpoint naming. Override it if you need multiple Microsoft 365 instances in the same app.

## Program.cs (API)
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAppAuthentication(options =>
    {
        options.Issuer = builder.Configuration["Auth:Issuer"];
        options.Audience = builder.Configuration["Auth:Audience"];
    })
    .AddProviderMicrosoft365(builder.Configuration.GetSection("Auth:Providers:Microsoft365"));

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
        options.ProviderKey = "microsoft-365";
        options.ApiBaseUrl = builder.Configuration["Auth:ApiBaseUrl"];
    })
    .AddProviderMicrosoft365(builder.Configuration.GetSection("Auth:Providers:Microsoft365"));

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
      "Microsoft365": {
        "Authority": "https://login.microsoftonline.com/{tenantId}/v2.0",
        "ClientId": "...",
        "ClientSecret": "...",
        "AllowedTenants": [ "{tenantId}" ]
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
      "Microsoft365": {
        "Authority": "https://login.microsoftonline.com/{tenantId}/v2.0",
        "ClientId": "...",
        "ClientSecret": "...",
        "RedirectUris": [ "https://app.example.local/signin-oidc" ]
      }
    }
  }
}
```

## Development Bypass
```json
{
  "Auth": {
    "Providers": {
      "Microsoft365": {
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
