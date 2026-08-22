# Work claim — Drawing-unit metadata whitespace canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-drawing-unit-whitespace-canonicality-20260812-0856`
- Registered: `2026-08-12T08:56:00+07:00`
- Completed: `2026-08-12T08:58:00+07:00`
- Baseline main SHA: `3436a5515b912db3cd9c9b59467ad48c4866fe1a`
- Claim commit: `e67c22582f8092ec31e4a55ffdc9a0c8d7854f49`
- Source fix commit: `5cf0ec9a59444ed9949904b49cf92d4f271d8f7d`
- Regression commit: `73fe36cd3c046421d12cc5215cdf29623b836488`
- Priority: P2 evidence-driven remote-safe unit metadata integrity

## Confirmed defect

`DrawingUnitResolutionPolicy.SetProjectOverride(...)` and `BindQuantityUnit(...)` persist unit metadata using canonical enum names such as `Meter`. The corresponding `TryParseNamedUnitToken(...)` trimmed persisted input before validation, so non-canonical metadata such as `" Meter "` was silently accepted by both override resolution and the method named `TryReadCanonical(...)`.

The previous named-token fix intentionally rejected numeric enum aliases while retaining case-insensitive named tokens; this claim is a narrower follow-up for leading/trailing whitespace only.

## Reserved scope

- `src/QS3D.Core/Units/DrawingUnitResolutionPolicy.cs`
- `tests/QS3D.Core.SmokeTests/DrawingUnitResolutionSmoke.cs`
- this claim file

## Implemented fix

`TryParseNamedUnitToken(...)` now validates the persisted token before enum parsing and rejects leading/trailing whitespace. Case-insensitive canonical names such as `meter` remain accepted; numeric aliases remain rejected; legacy effective-unit migration parsing is unchanged.

## Validation evidence

- `73fe36cd3c046421d12cc5215cdf29623b836488` adds focused smoke coverage proving padded override metadata and padded bound metadata fail closed.
- Existing lowercase named-token cases remain in the same smoke to preserve case-insensitive compatibility.
- Existing numeric alias regressions remain in place.
- Source/test changes were applied against re-read exact current blob SHAs.

## Excluded scope

- No changes to unit conversion factors or `UnitScale`.
- No redesign of legacy `QS3D.DrawingUnit` assumption parsing.
- No BricsCAD/native/runtime or GitHub Actions work.

## Completion condition

Completed: source and focused smoke regression are committed to `main`, exact integration SHAs are recorded, and no local BricsCAD/runtime PASS is claimed.
