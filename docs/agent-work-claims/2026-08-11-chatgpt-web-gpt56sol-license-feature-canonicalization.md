# Agent work claim — license feature canonicalization integrity

- Status: `ACTIVE`
- Owner: ChatGPT Web / GPT-5.6 Sol
- Track: Core licensing signature canonicalization
- Mode: Remote source-safe
- Started: 2026-08-11 21:28 +07
- Baseline main SHA observed before reservation attempt: `6dc93895243bef0f26f7b5c22113977216ef38fb`

## Claimed paths

- `src/QS3D.Core/Licensing/LicenseVerifier.cs`
- `tests/QS3D.Core.SmokeTests/LicenseVerifierSmoke.cs`
- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs`
- `docs/agent-work-claims/2026-08-11-chatgpt-web-gpt56sol-license-feature-canonicalization.md`

## Defect

License feature names are joined with `,` in the signed canonical payload, but validation currently permits commas inside individual feature names. Distinct feature sets such as `["A,B", "C"]` and `["A", "B,C"]` can therefore serialize to the same signed feature line. The verifier must reject delimiter-bearing feature names so signed entitlements have an unambiguous canonical representation.

## Validation plan

- Extend `LicenseVerifierSmoke` with a deterministic delimiter-canonicalization regression.
- Register the existing licensing smoke suite in `SmokeTestRegistration` so it runs with the main smoke harness.
- Do not dispatch GitHub Actions; review the exact source/test diff through the connector.

## Blocked dependencies

None.
