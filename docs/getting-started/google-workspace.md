# Google Workspace (SSO) (Getting Started)

## Summary
Flow type: OIDC web client.

Use Google Workspace as the external identity provider for workforce SSO. The API validates Google tokens during exchange and issues first-party API tokens.

## Standard Setup
### Program.cs (API)
```csharp
var builder = WebApplication.CreateBuilder(args);
var auth = builder.Configuration.GetRequiredSection("Auth");
var googleWorkspace = auth.GetRequiredSection("Providers").GetRequiredSection("GoogleWorkspace");

builder.AddApiAuthentication(auth);
builder.Services.AddProviderGoogleWorkspace(googleWorkspace);

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
var googleWorkspace = auth.GetRequiredSection("Providers").GetRequiredSection("GoogleWorkspace");

builder.AddOidcClientAuthentication(auth);
builder.Services.AddProviderGoogleWorkspace(googleWorkspace);

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapClientAuthentication();
app.Run();
```

## Endpoints
API:
- `POST /auth/exchange/google-workspace`
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
    "ProviderKey": "google-workspace",
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

## Advanced Composition
If you need custom composition, keep `AddProviderGoogleWorkspace(...)` explicit and use `AddClientAuthentication(...)` plus `AddApiTokenExchangeOnOidcSignIn(...)` on the client, or the `Action<ApiAuthenticationOptions>` overload of `AddApiAuthentication(...)` on the API.
