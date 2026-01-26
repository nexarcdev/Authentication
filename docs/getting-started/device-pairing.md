# Device Pairing (Getting Started)

## Summary
Device pairing allows a constrained device to authenticate by pairing with a signed-in client. It uses a short-lived code plus optional QR code to complete pairing and exchange for API tokens.

## Endpoints
API:
- `POST /auth/device-pairing/code`
- `POST /auth/device-pairing/resolve`
- `GET /auth/qr/device-pairing/{code}` (optional)
Source: API endpoint mappings created by `MapAuthentication` plus the device pairing module.

Client:
- `GET /pair` (example pairing UI route)
Source: client endpoint mappings created by `MapClientAuthentication` plus host app UI routes.

## Required Client Services
The library runs the pairing flow, but the client app supplies storage and verification via DI.

Example interfaces:
```csharp
public interface IDevicePairingSessionStore
{
    Task SaveAsync(DevicePairingSession session, CancellationToken ct);
    Task<DevicePairingSession?> FindByCodeAsync(string code, CancellationToken ct);
    Task CompleteAsync(string sessionId, CancellationToken ct);
}

public interface IDevicePairingVerifier
{
    Task<DevicePairingApproval> ApproveAsync(DevicePairingRequest request, CancellationToken ct);
}
```

Registration example:
```csharp
builder.Services.AddScoped<IDevicePairingSessionStore, DevicePairingSessionStore>();
builder.Services.AddScoped<IDevicePairingVerifier, DevicePairingVerifier>();
```

How the library finds these:
- The provider resolves them from DI at runtime
- Missing registrations are treated as startup errors

## Scheme and Endpoint Key
Set a unique scheme/key for this provider to control auth filtering and endpoint naming. Override it if you need multiple device pairing experiences in the same app.

## Program.cs (API)
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAppAuthentication(options =>
    {
        options.Issuer = builder.Configuration["Auth:Issuer"];
        options.Audience = builder.Configuration["Auth:Audience"];
    })
    .AddProviderDevicePairing(builder.Configuration.GetSection("Auth:Providers:DevicePairing"));

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
        options.ProviderKey = "device-pairing";
        options.ApiBaseUrl = builder.Configuration["Auth:ApiBaseUrl"];
    })
    .AddProviderDevicePairing(builder.Configuration.GetSection("Auth:Providers:DevicePairing"));

var app = builder.Build();
app.MapClientAuthentication();
app.Run();
```

## Flow
1. Device requests a pairing code from the API
2. API generates a short-lived code (and optional QR payload)
3. User enters the code on a signed-in client
4. API validates the pairing and issues tokens for the device

## API Configuration
```json
{
  "Auth": {
    "Issuer": "https://auth.example.local",
    "Audience": "api",
    "Providers": {
      "DevicePairing": {
        "CodeLength": 8,
        "CodeAlphabet": "Numeric",
        "CodeLifetimeSeconds": 300
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
      "DevicePairing": {
        "PairingUrl": "https://app.example.local/pair"
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
      "DevicePairing": {
        "DevBypass": {
          "Enabled": true,
          "Devices": [
            {
              "deviceId": "dev-device-1",
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
- When enabled in Development, pairing requests are auto-approved for configured devices
- The API still enforces code lifetime and single-use semantics

## Uses Secure Code Generator
See [Secure Code Generator](secure-code-generator.md) for details on allowed alphabets and QR payloads.
