# Device Pairing (Getting Started)

## Summary
Flow type: non-OIDC client.

Device pairing allows a constrained device to authenticate by pairing with a signed-in client. It uses a short-lived code plus optional QR payload to complete the pairing flow and obtain API-issued tokens.

## Standard Setup
### Program.cs (API)
```csharp
var builder = WebApplication.CreateBuilder(args);
var auth = builder.Configuration.GetRequiredSection("Auth");
var devicePairing = auth.GetRequiredSection("Providers").GetRequiredSection("DevicePairing");

builder.AddApiAuthentication(auth);
builder.Services.AddProviderDevicePairing(devicePairing);

builder.Services.AddScoped<IDevicePairingSessionStore, DevicePairingSessionStore>();
builder.Services.AddScoped<IDevicePairingVerifier, DevicePairingVerifier>();

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
var devicePairing = auth.GetRequiredSection("Providers").GetRequiredSection("DevicePairing");

builder.AddClientAuthentication(auth);
builder.Services.AddProviderDevicePairing(devicePairing);

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapClientAuthentication();
app.Run();
```

## Endpoints
API:
- `POST /auth/device-pairing/code`
- `POST /auth/device-pairing/resolve`
- `GET /auth/device-pairing/qr/{code}` (optional)

Client:
- `GET /login`
- `GET|POST /logout`
- `GET|POST /auth/dev-login`
- `POST /device-pairing/code`
- `POST /device-pairing/resolve`
- `GET /device-pairing/qr/{code}`

Paths assume the default provider key `device-pairing`. If you override `ProviderKey`, replace the prefix accordingly.

## Required API Services
```csharp
public interface IDevicePairingSessionStore
{
    Task SaveAsync(DevicePairingSession session, CancellationToken ct);
    Task<DevicePairingSession?> FindByCodeAsync(string code, CancellationToken ct);
    Task CompleteAsync(string sessionId, CancellationToken ct);
}

public interface IDevicePairingVerifier
{
    Task<DevicePairingApproval> ApproveAsync(DevicePairingSession session, CancellationToken ct);
}
```

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
        "CodeLifetimeSeconds": 300,
        "PairingUrl": "https://app.example.local/pair"
      }
    }
  }
}
```

## Client Configuration
```json
{
  "Auth": {
    "ProviderKey": "device-pairing",
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

## Advanced Composition
If you need custom composition, keep `AddProviderDevicePairing(...)` explicit and use the `Action<ApiAuthenticationOptions>` overload of `AddApiAuthentication(...)` on the API or the `Action<ClientAuthenticationOptions>` overload of `AddClientAuthentication(...)` on the client.
