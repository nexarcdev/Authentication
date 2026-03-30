# Auth0 (B2C) (Getting Started)

## Summary
Flow type: OIDC web client.

Use Auth0 as the external identity provider for a consumer web client. The API validates Auth0 tokens during exchange and issues first-party API tokens.

## Standard Setup
### Program.cs (API)
```csharp
var builder = WebApplication.CreateBuilder(args);
var auth = builder.Configuration.GetRequiredSection("Auth");
var auth0B2C = auth.GetRequiredSection("Providers").GetRequiredSection("Auth0B2C");

builder.AddApiAuthentication(auth);
builder.Services.AddProviderAuth0B2C(auth0B2C);

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
var auth0B2C = auth.GetRequiredSection("Providers").GetRequiredSection("Auth0B2C");

builder.AddOidcClientAuthentication(auth);
builder.Services.AddProviderAuth0B2C(auth0B2C);

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapClientAuthentication();
app.Run();
```

## Endpoints
API:
- `POST /auth/exchange/auth0-b2c`
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
    "ProviderKey": "auth0-b2c",
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

## Advanced Composition
If you need custom composition, keep `AddProviderAuth0B2C(...)` explicit and use `AddClientAuthentication(...)` plus `AddApiTokenExchangeOnOidcSignIn(...)` on the client, or the `Action<ApiAuthenticationOptions>` overload of `AddApiAuthentication(...)` on the API.
