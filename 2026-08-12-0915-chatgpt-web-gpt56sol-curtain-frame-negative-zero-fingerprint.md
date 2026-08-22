# Work claim — Curtain frame negative-zero fingerprint canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-curtain-frame-negative-zero-fingerprint-20260812-0915`
- Registered: `2026-08-12T09:15:00+07:00`
- Completed: `2026-08-12T09:18:00+07:00`
- Baseline main SHA: `6257c714d1acbaec56b86b8729bad311a3c7ad34`
- Claim commit: `3a67566bed6dddaab9ce43bce59fb8c4a1f92722`
- Source commit: `4e085af82364079d0569844e9b03a7dfeb1651ec`
- Smoke commit: `f734a3c14e517132aae9f597a17cb8a426c1898f`
- Priority: deterministic semantic fingerprint canonicality for numerically equivalent frame configuration.

## Completed defect

`CurtainWallFrameFingerprint.Compute(...)` validated all numeric inputs and serialized them with round-trip (`"R"`) invariant formatting before hashing. IEEE-754 positive zero and negative zero compare equal and describe the same geometric configuration, but round-trip formatting can preserve the sign of negative zero. Valid zero-valued fields could therefore produce a different SHA-256 fingerprint solely because zero carried a negative sign bit, creating false configuration changes/stale detection.

## Implemented scope

- `src/QS3D.Core/Geometry/CurtainWallFrameFingerprint.cs`
- `tests/QS3D.Core.SmokeTests/CurtainFrameFingerprintSmoke.cs`
- this claim file

## Implemented contract

- Fingerprint formatting now canonicalizes every numeric zero to positive `0d` before invariant round-trip serialization.
- Positive zero and negative zero hash identically for every zero-allowed input: `BottomOffsetM`, `PerimeterFrameWidthM`, `MullionWidthM`, and `TransomWidthM`.
- Nonzero finite values retain the existing round-trip representation.
- Existing positivity, non-negativity, finite validation and SHA-256 format are unchanged.

## Validation actually performed

- Re-fetched the reserved source on moving `main` after the claim became visible and confirmed no concurrent overlap before the source write.
- Source readback on `main` confirms signed-zero canonicalization is present in `R(...)` at blob `2c1715683737e179786d40b9f9d1728e3d68fc28`.
- Regression readback on `main` confirms `CanonicalizesSignedZero()` covers all four zero-allowed fields at blob `0788f1a8fcff6d8cea35164ea2bf047507ff5352` while the existing genuine-change and invalid-input assertions remain present.
- A first smoke-file update was rejected by GitHub with a concurrency conflict; the file was re-fetched and the update was retried against the current blob rather than overwriting moving-main work.
- No executable .NET build/smoke PASS is claimed from this connector-only environment.
- No GitHub Actions were dispatched, no force-push was used, and no BricsCAD V25/V26 runtime PASS is claimed.

## Excluded scope honored

No curtain path planning, native generation, ownership/stale policy, BricsCAD adapter/runtime, unrelated geometry behavior, or release workflow was changed.

## Completion condition

Completed. Numerically equal signed-zero curtain-frame inputs no longer create distinct fingerprints, focused regression source is present on `main`, and this reservation is released by `COMPLETED` status.
