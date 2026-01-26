# Secure Code Generator (Getting Started)

## Summary
This utility generates short-lived codes suitable for device pairing and magic link flows. It supports numeric codes and unambiguous alphanumeric codes that avoid confusing characters.

## Supported Alphabets
- Numeric: `0-9`
- Unambiguous: `ABCDEFGHJKLMNPQRSTUVWXYZ23456789`

## Configuration
```json
{
  "Auth": {
    "Providers": {
      "DevicePairing": {
        "CodeLength": 6,
        "CodeAlphabet": "Numeric",
        "CodeLifetimeSeconds": 300
      },
      "MagicLink": {
        "CodeLength": 4,
        "CodeAlphabet": "Unambiguous",
        "CodeLifetimeSeconds": 600
      }
    }
  }
}
```

## QR Codes
- Device pairing and magic link can both emit a QR payload
- QR payload includes the code and a redeem URL
- Code and URL are short-lived and must be validated by the API

## Security Notes
- Codes are random, time-limited, and single-use
- Hash codes at rest if persisted
- Always validate code expiration and attempt limits
