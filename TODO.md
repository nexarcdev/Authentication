# TODO (Build Checklist)

## Scaffold
- [x] Create solution and project layout
- [x] Create test projects for each module

## Abstractions
- [x] Define options models (core + provider)
- [x] Define interfaces for current user, token exchange, external validation
- [x] Define magic link + device pairing contracts
- [x] Add unit tests

## Utilities
- [x] Implement secure code generator (numeric + unambiguous)
- [x] Add unit tests

## API Core
- [x] Implement token exchange endpoints
- [x] Implement refresh endpoint and session model
- [x] Implement JWT issuance/validation
- [x] Implement claim normalization pipeline
- [x] Add unit + minimal integration tests

## Client Core
- [x] Implement OIDC wiring helpers
- [x] Implement token storage + refresh hooks
- [x] Implement MapClientAuthentication for public endpoints
- [x] Add unit tests

## Providers
- [x] Google Workspace provider + tests
- [x] Azure B2C provider + tests
- [x] Auth0 B2C provider + tests
- [x] Microsoft 365 provider + tests

## Magic Link
- [x] API endpoints + storage + verifier integration
- [x] Client helpers + notifier integration
- [x] Add unit tests

## Device Pairing
- [x] API endpoints + storage + verifier integration
- [x] Client helpers + UI route mapping
- [x] Add unit tests

## Dev Bypass (Internal)
- [x] Per-provider dev bypass rules
- [x] Guardrails for non-Development
- [x] Tests

## Docs
- [x] Align docs with final API surface
- [x] Update endpoint lists if needed
- [x] Add package install commands
