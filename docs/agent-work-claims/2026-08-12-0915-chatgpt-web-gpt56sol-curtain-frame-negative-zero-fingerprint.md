# Work claim — Curtain frame negative-zero fingerprint canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-curtain-frame-negative-zero-fingerprint-20260812-0915`
- Registered: `2026-08-12T09:15:00+07:00`
- Baseline main SHA: `6257c714d1acbaec56b86b8729bad311a3c7ad34`
- Priority: deterministic semantic fingerprint canonicality for numerically equivalent frame configuration.

## Confirmed defect

`CurtainWallFrameFingerprint.Compute(...)` validates all numeric inputs and serializes them with round-trip (`"R"`) invariant formatting before hashing. IEEE-754 positive zero and negative zero compare equal and describe the same geometric configuration, but round-trip formatting preserves the sign of negative zero. A valid zero-valued field such as `BottomOffsetM`, `PerimeterFrameWidthM`, `MullionWidthM`, or `TransomWidthM` can therefore produce a different SHA-256 fingerprint solely because its zero carries a negative sign bit.

That creates false configuration changes/stale detection for numerically identical curtain-frame geometry.

## Reserved scope

- `src/QS3D.Core/Geometry/CurtainWallFrameFingerprint.cs` — numeric canonicalization only.
- `tests/QS3D.Core.SmokeTests/CurtainFrameFingerprintSmoke.cs` — focused negative-zero regression only.
- this claim file.

## Intended contract

- Positive zero and negative zero hash identically for every zero-allowed fingerprint input.
- All nonzero finite values retain existing round-trip representation and hashes.
- Existing positivity/non-negativity/finite validation remains unchanged.
- Do not modify curtain path planning, native generation, ownership/stale policy, or BricsCAD adapter/runtime code.

## Validation plan

- Re-fetch `main`, reserved source/test and recent curtain claims immediately after reservation.
- Add focused smoke coverage proving `0d` and `-0d` produce the same fingerprint for zero-allowed fields while genuine nonzero configuration changes still change the hash.
- Review exact diff/source readback after each write.
- No GitHub Actions dispatch and no BricsCAD V25/V26 runtime PASS claim.

## Completion condition

Numerically equal signed-zero curtain-frame inputs no longer create distinct fingerprints, focused regression source is merged, and this claim is closed with exact implementation commits and truthful validation scope.
