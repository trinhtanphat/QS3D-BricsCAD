# Work claim — License UTC timestamp token canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-license-utc-token-canonicality-20260812-0843`
- Registered: `2026-08-12T08:43:00+07:00`
- Completed: `2026-08-12T08:46:00+07:00`
- Baseline main SHA: `0970f1cb7779bcd95d2617c80e66dabb341c1b2a`
- Priority: P1 — persisted signed license timestamps must use the exact UTC representation bound by the canonical payload.
- Integration PR: `#658`
- Main integration commit: `bc0e229b9ad4dd86abf5409467ea09cc6d48a285`

## Confirmed defect

`LicenseVerifier.ParseUtc(...)` parsed the round-trip `O` format with `AssumeUniversal | AdjustToUniversal`, then normalized the result to `DateTimeKind.Utc`. Because `LicenseDocument.CanonicalPayload()` always serializes validity timestamps back to UTC `O` form, an XML token such as an equivalent offset/no-zone representation could be accepted and normalized before signature verification instead of being required to match the canonical UTC text. That permitted unsigned textual representation changes at a signed persisted boundary.

## Implemented scope

- `src/QS3D.Core/Licensing/LicenseVerifier.cs`
- `tests/QS3D.Core.SmokeTests/LicenseUtcTokenCanonicalitySmoke.cs`
- this claim file for close-out

## Completed contract

- `notBeforeUtc` and `expiresUtc` must parse as round-trip timestamps and already equal the exact `DateTimeKind.Utc` `O` representation used by `CanonicalPayload()`.
- Equivalent offset and missing-zone forms fail closed rather than being normalized during load.
- Existing canonical UTC `...0000000Z` licenses remain accepted.
- Existing signature/product/time/attribute/child/token protections remain unchanged.
- `LicenseVerifierSmoke.cs` was not modified; isolated module-initializer coverage pins the new persisted token boundary.

## Validation evidence

- Claim registration: `80f717c005a277b19f68f9e1a1b5aa3e85f48349`.
- Branch source commit: `5730a936a9963a4f33153de2726659c13c239137`.
- Branch smoke commit: `152bfce9c02cdca8b9758fff37bd0ce483da7919`.
- The branch was synchronized with moving `main` without force-push; PR `#658` squash-merged to `main` as `bc0e229b9ad4dd86abf5409467ea09cc6d48a285`.
- Post-merge readback confirms `ParseUtc(...)` compares the raw token to the exact UTC `O` representation and the isolated smoke is present on `main`.
- No GitHub Actions/build/release was dispatched and no local .NET/BricsCAD V25/V26 runtime PASS is claimed.

## Completion condition

`COMPLETED`: source fix plus deterministic isolated Core smoke coverage are integrated on current `main`, source was re-read, and exact integration SHA/evidence is recorded above.
