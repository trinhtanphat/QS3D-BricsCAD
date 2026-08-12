# Work claim — License UTC timestamp token canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-license-utc-token-canonicality-20260812-0843`
- Registered: `2026-08-12T08:43:00+07:00`
- Baseline main SHA: `0970f1cb7779bcd95d2617c80e66dabb341c1b2a`
- Priority: P1 — persisted signed license timestamps must use the exact UTC representation bound by the canonical payload.

## Confirmed defect

`LicenseVerifier.ParseUtc(...)` parses the round-trip `O` format with `AssumeUniversal | AdjustToUniversal`, then normalizes the result to `DateTimeKind.Utc`. Because `LicenseDocument.CanonicalPayload()` always serializes validity timestamps back to UTC `O` form, an XML token such as an equivalent offset/no-zone representation can be accepted and normalized before signature verification instead of being required to match the canonical UTC text. That permits unsigned textual representation changes at a signed persisted boundary.

## Reserved scope

- `src/QS3D.Core/Licensing/LicenseVerifier.cs`
- one isolated Core smoke file for UTC timestamp token canonicality
- this claim file for close-out

## Contract

- `notBeforeUtc` and `expiresUtc` must parse as round-trip timestamps and already equal the exact `DateTimeKind.Utc` `O` representation used by `CanonicalPayload()`.
- Reject offset, missing-zone, shortened-fraction, or otherwise parseable-but-noncanonical representations rather than normalizing them during load.
- Preserve existing UTC `...0000000Z` license compatibility and all signature/product/time/attribute/child/token protections.
- Do not modify `LicenseVerifierSmoke.cs`; use isolated module-initializer coverage.
- No GitHub Actions/build/release dispatch and no BricsCAD runtime PASS claim from this remote lane.

## Completion condition

Source fix plus deterministic isolated Core smoke coverage are integrated on current `main`, source is re-read, and this claim is marked `COMPLETED` with exact integration SHA/evidence.
