# Auth0 (B2C) (Getting Started)

## Summary
Use Auth0 as the client IdP in a consumer (B2C) scenario. The API validates Auth0 tokens during exchange and issues API tokens.

## Endpoints
API:
- `POST /auth/exchange/auth0-b2c`
- `POST /auth/refresh`
Source: API endpoint mappings created by `MapAuthentication`.

Client:
- `GET /signin-oidc`
- `GET /signout-callback-oidc` (optional)
Source: OpenIdConnect handler registered by `AddClientAuthentication`.

Note: no client endpoint mapping is required beyond the OIDC middleware.

## Scheme and Endpoint Key
Set a unique scheme/key for this provider to control auth filtering and endpoint naming. Override it if you need multiple Auth0 instances in the same app.

## Program.cs (API)
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAppAuthentication(options =>
    {
        options.Issuer = builder.Configuration["Auth:Issuer"];
        options.Audience = builder.Configuration["Auth:Audience"];
    })
    .AddProviderAuth0B2C(builder.Configuration.GetSection("Auth:Providers:Auth0B2C"));

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
        options.ProviderKey = "auth0-b2c";
        options.ApiBaseUrl = builder.Configuration["Auth:ApiBaseUrl"];
    })
    .AddProviderAuth0B2C(builder.Configuration.GetSection("Auth:Providers:Auth0B2C"));

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
      "Auth0B2C": {
        "Authority": "https://{your-domain}.auth0.com",
        "ClientId": "...",
        "ClientSecret": "...",
        "AllowedTenants": [ "{your-domain}.auth0.com" ]
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
      "Auth0B2C": {
        "Authority": "https://{your-domain}.auth0.com",
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
      "Auth0B2C": {
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
