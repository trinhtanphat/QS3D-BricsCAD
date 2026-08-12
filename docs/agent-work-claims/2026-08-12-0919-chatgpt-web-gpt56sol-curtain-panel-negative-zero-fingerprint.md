# Work claim — Curtain panel negative-zero fingerprint canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-curtain-panel-negative-zero-fingerprint-20260812-0919`
- Registered: `2026-08-12T09:19:00+07:00`
- Baseline main SHA: `42e6411578464173a55204a5803eda0930503704`
- Priority: deterministic panel fingerprint canonicality for numerically equivalent geometry.

## Confirmed defect

`CurtainWallPanelFingerprint.Compute(...)` formats finite doubles with invariant round-trip (`"R"`) text before hashing. IEEE-754 `0d` and `-0d` are numerically equal, but round-trip formatting can preserve the negative-zero sign. Valid zero-capable values (`BottomOffsetM`, piece `X_M`, and piece `Z_M`) can therefore produce distinct SHA-256 fingerprints solely because their zero sign bit differs.

That creates false panel configuration changes/stale detection for geometrically identical inputs.

## Reserved scope

- `src/QS3D.Core/Geometry/CurtainWallPanelFingerprint.cs` — signed-zero numeric canonicalization only.
- `tests/QS3D.Core.SmokeTests/CurtainWallPanelFingerprintAreaFiniteSmoke.cs` — focused signed-zero regression only.
- this claim file.

## Intended contract

- Positive and negative zero hash identically for every zero-capable panel fingerprint coordinate/offset.
- Nonzero finite values retain existing round-trip representation and hashes.
- Existing finite/positive/area-overflow validation, piece ordering, source-kind normalization and SHA-256 format stay unchanged.
- Do not modify panel planning, native generation, ownership/stale policy, adapter/runtime code, or other active curtain claims.

## Validation plan

- Re-fetch `main`, reserved source/test and recent panel claims after reservation.
- Add focused smoke assertions for signed zero in `BottomOffsetM`, `X_M`, and `Z_M`, while preserving area-overflow and deterministic coverage.
- Review exact source/test readback on `main` after writes.
- No GitHub Actions dispatch and no BricsCAD V25/V26 runtime PASS claim.

## Completion condition

Numerically equal signed-zero panel inputs no longer create distinct fingerprints, focused regression source is merged, and this claim is closed with exact commit SHAs and truthful validation scope.
