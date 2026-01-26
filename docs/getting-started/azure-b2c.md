# Azure B2C (Getting Started)

## Summary
Use Azure AD B2C as the client IdP. The API validates B2C tokens during exchange and issues API tokens.

## Endpoints
API:
- `POST /auth/exchange/azure-b2c`
- `POST /auth/refresh`
Source: API endpoint mappings created by `MapAuthentication`.

Client:
- `GET /signin-oidc`
- `GET /signout-callback-oidc` (optional)
Source: OpenIdConnect handler registered by `AddClientAuthentication`.

Note: no client endpoint mapping is required beyond the OIDC middleware.

## Scheme and Endpoint Key
Set a unique scheme/key for this provider to control auth filtering and endpoint naming. Override it if you need multiple Azure B2C instances in the same app.

## Program.cs (API)
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAppAuthentication(options =>
    {
        options.Issuer = builder.Configuration["Auth:Issuer"];
        options.Audience = builder.Configuration["Auth:Audience"];
    })
    .AddProviderAzureB2C(builder.Configuration.GetSection("Auth:Providers:AzureB2C"));

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
        options.ProviderKey = "azure-b2c";
        options.ApiBaseUrl = builder.Configuration["Auth:ApiBaseUrl"];
    })
    .AddProviderAzureB2C(builder.Configuration.GetSection("Auth:Providers:AzureB2C"));

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
      "AzureB2C": {
        "Authority": "https://{tenant}.b2clogin.com/{tenant}.onmicrosoft.com/{policy}",
        "ClientId": "...",
        "ClientSecret": "...",
        "AllowedTenants": [ "{tenant}.onmicrosoft.com" ]
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
      "AzureB2C": {
        "Authority": "https://{tenant}.b2clogin.com/{tenant}.onmicrosoft.com/{policy}",
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
      "AzureB2C": {
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
