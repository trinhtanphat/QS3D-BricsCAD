# Work claim — Curtain panel negative-zero fingerprint canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-curtain-panel-negative-zero-fingerprint-20260812-0919`
- Registered: `2026-08-12T09:19:00+07:00`
- Completed: `2026-08-12T09:22:00+07:00`
- Baseline main SHA: `42e6411578464173a55204a5803eda0930503704`
- Claim commit: `b39a991eba505cd079006abec76472d08eb82146`
- Source commit: `05caa11bdc067f1ba431a524b67b32ba6211ca08`
- Smoke commit: `64fa8482fbfe498dbbce2780638bd9e95ec5e7fc`
- Priority: deterministic panel fingerprint canonicality for numerically equivalent geometry.

## Completed defect

`CurtainWallPanelFingerprint.Compute(...)` formatted finite doubles with invariant round-trip (`"R"`) text before hashing. IEEE-754 `0d` and `-0d` are numerically equal, but round-trip formatting can preserve the negative-zero sign. Valid zero-capable values (`BottomOffsetM`, piece `X_M`, and piece `Z_M`) could therefore produce distinct SHA-256 fingerprints solely because their zero sign bit differed, creating false panel configuration changes/stale detection for geometrically identical inputs.

## Implemented scope

- `src/QS3D.Core/Geometry/CurtainWallPanelFingerprint.cs`
- `tests/QS3D.Core.SmokeTests/CurtainWallPanelFingerprintAreaFiniteSmoke.cs`
- this claim file

## Implemented contract

- Fingerprint formatting canonicalizes every numeric zero to positive `0d` before invariant round-trip serialization.
- Positive and negative zero hash identically for `BottomOffsetM`, piece `X_M`, and piece `Z_M`.
- Nonzero finite values retain the existing round-trip representation.
- Existing finite/positive/area-overflow validation, piece ordering, source-kind normalization and SHA-256 format remain unchanged.

## Validation actually performed

- Re-fetched moving `main`, the reserved source and existing panel-fingerprint smoke before each write.
- Source readback on `main` confirms signed-zero canonicalization is present in `R(...)` at blob `a091d5cedeaf0ce7c5ec054cec68a580bc91ba31`.
- Regression readback on `main` confirms `SignedZeroCoordinatesRemainCanonical()` covers `BottomOffsetM`, `X_M`, and `Z_M` at blob `56260b8096ac259b33b109f62396bc110fa5cf41`, while existing area-overflow and deterministic coverage remains present.
- No executable .NET build/smoke PASS is claimed from this connector-only environment.
- No GitHub Actions were dispatched, no force-push was used, and no BricsCAD V25/V26 runtime PASS is claimed.

## Excluded scope honored

No panel planning, native generation, ownership/stale policy, BricsCAD adapter/runtime, unrelated geometry behavior, active curtain claims, or release workflow was changed.

## Completion condition

Completed. Numerically equal signed-zero panel inputs no longer create distinct fingerprints, focused regression source is present on `main`, and this reservation is released by `COMPLETED` status.
