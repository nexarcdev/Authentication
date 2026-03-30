# Azure B2C (Getting Started)

## Summary
Flow type: OIDC web client.

Use Azure AD B2C as the external identity provider for a web client. The API validates Azure B2C tokens during exchange and issues first-party API tokens.

## Standard Setup
### Program.cs (API)
```csharp
var builder = WebApplication.CreateBuilder(args);
var auth = builder.Configuration.GetRequiredSection("Auth");
var azureB2C = auth.GetRequiredSection("Providers").GetRequiredSection("AzureB2C");

builder.AddApiAuthentication(auth);
builder.Services.AddProviderAzureB2C(azureB2C);

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapAuthentication();
app.Run();
```

### Program.cs (Client)
```csharp
var builder = WebApplication.CreateBuilder(args);
var auth = builder.Configuration.GetRequiredSection("Auth");
var azureB2C = auth.GetRequiredSection("Providers").GetRequiredSection("AzureB2C");

builder.AddOidcClientAuthentication(auth);
builder.Services.AddProviderAzureB2C(azureB2C);

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapClientAuthentication();
app.Run();
```

## Endpoints
API:
- `POST /auth/exchange/azure-b2c`
- `POST /auth/refresh`

Client:
- `GET /login`
- `GET|POST /logout`
- `GET|POST /auth/dev-login`
- `GET /signin-oidc`
- `GET /signout-callback-oidc` (optional)

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

Authority must be the B2C authority base URL, not the `/authorize` endpoint.

## Client Configuration
```json
{
  "Auth": {
    "ProviderKey": "azure-b2c",
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

## Advanced Composition
If you need custom composition, keep `AddProviderAzureB2C(...)` explicit and use `AddClientAuthentication(...)` plus `AddApiTokenExchangeOnOidcSignIn(...)` on the client, or the `Action<ApiAuthenticationOptions>` overload of `AddApiAuthentication(...)` on the API.
