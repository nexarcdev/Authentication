# Magic Link (Getting Started)

## Summary
Flow type: non-OIDC client.

Magic link uses a short-lived code delivered to the user. The client redeems the code through provider-specific endpoints and stores the API-issued tokens.

## Standard Setup
### Program.cs (API)
```csharp
var builder = WebApplication.CreateBuilder(args);
var auth = builder.Configuration.GetRequiredSection("Auth");
var magicLink = auth.GetRequiredSection("Providers").GetRequiredSection("MagicLink");

builder.AddApiAuthentication(auth);
builder.Services.AddProviderMagicLink(magicLink);

builder.Services.AddScoped<IMagicLinkSessionStore, MagicLinkSessionStore>();
builder.Services.AddScoped<IMagicLinkVerifier, MagicLinkVerifier>();

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
var magicLink = auth.GetRequiredSection("Providers").GetRequiredSection("MagicLink");

builder.AddClientAuthentication(auth);
builder.Services.AddProviderMagicLink(magicLink);
builder.Services.AddScoped<IMagicLinkNotifier, MagicLinkNotifier>();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapClientAuthentication();
app.Run();
```

## Endpoints
API:
- `POST /auth/magic-link/request`
- `POST /auth/magic-link/redeem`

Client:
- `GET /login`
- `GET|POST /logout`
- `GET|POST /auth/dev-login`
- `POST /magic-link/request`
- `POST /magic-link/redeem`

Paths assume the default provider key `magic-link`. If you override `ProviderKey`, replace the prefix accordingly.

## Required API Services
```csharp
public interface IMagicLinkSessionStore
{
    Task SaveAsync(MagicLinkSession session, CancellationToken ct);
    Task<MagicLinkSession?> FindByCodeAsync(string code, CancellationToken ct);
    Task CompleteAsync(string sessionId, CancellationToken ct);
}

public interface IMagicLinkVerifier
{
    Task<MagicLinkApproval> ApproveAsync(MagicLinkSession session, CancellationToken ct);
}
```

## Required Client Services
```csharp
public interface IMagicLinkNotifier
{
    Task SendAsync(string destination, string code, string link, CancellationToken ct);
}
```

## API Configuration
```json
{
  "Auth": {
    "Issuer": "https://auth.example.local",
    "Audience": "api",
    "Providers": {
      "MagicLink": {
        "CodeLength": 8,
        "CodeAlphabet": "Unambiguous",
        "CodeLifetimeSeconds": 600,
        "RedeemUrl": "https://app.example.local/magic"
      }
    }
  }
}
```

## Client Configuration
```json
{
  "Auth": {
    "ProviderKey": "magic-link",
    "ApiBaseUrl": "https://api.example.local",
    "Providers": {
      "MagicLink": {
        "RedeemUrl": "https://app.example.local/magic"
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
      "MagicLink": {
        "DevBypass": {
          "Enabled": true,
          "Destinations": [
            {
              "destination": "test@example.com",
              "user": { "name": "Test User", "email": "test@example.com", "roles": ["Staff"] }
            }
          ]
        }
      }
    }
  }
}
```

Behavior:
- When enabled in Development, configured destinations auto-approve on redeem
- The API still enforces code lifetime and single-use semantics

## Advanced Composition
If you need custom composition, keep `AddProviderMagicLink(...)` explicit and use the `Action<ApiAuthenticationOptions>` overload of `AddApiAuthentication(...)` on the API or the `Action<ClientAuthenticationOptions>` overload of `AddClientAuthentication(...)` on the client.
