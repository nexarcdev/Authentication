# Microsoft 365 (SSO) (Getting Started)

## Summary
Flow type: OIDC web client.

Use Microsoft 365 (Entra ID) as the external identity provider for workforce SSO. The API validates Entra ID tokens during exchange and issues first-party API tokens.

## Standard Setup
### Program.cs (API)
```csharp
var builder = WebApplication.CreateBuilder(args);
var auth = builder.Configuration.GetRequiredSection("Auth");
var microsoft365 = auth.GetRequiredSection("Providers").GetRequiredSection("Microsoft365");

builder.AddApiAuthentication(auth);
builder.Services.AddProviderMicrosoft365(microsoft365);

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
var microsoft365 = auth.GetRequiredSection("Providers").GetRequiredSection("Microsoft365");

builder.AddOidcClientAuthentication(auth);
builder.Services.AddProviderMicrosoft365(microsoft365);

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapClientAuthentication();
app.Run();
```

## Endpoints
API:
- `POST /auth/exchange/microsoft-365`
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

Authority must be the Entra authority base URL, not the `/oauth2/v2.0/authorize` endpoint.

## Client Configuration
```json
{
  "Auth": {
    "ProviderKey": "microsoft-365",
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

## Advanced Composition
If you need custom composition, keep `AddProviderMicrosoft365(...)` explicit and use `AddClientAuthentication(...)` plus `AddApiTokenExchangeOnOidcSignIn(...)` on the client, or the `Action<ApiAuthenticationOptions>` overload of `AddApiAuthentication(...)` on the API.
