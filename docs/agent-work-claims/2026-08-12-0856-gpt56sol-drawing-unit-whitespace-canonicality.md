# Work claim — Drawing-unit metadata whitespace canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-drawing-unit-whitespace-canonicality-20260812-0856`
- Registered: `2026-08-12T08:56:00+07:00`
- Baseline main SHA: `3436a5515b912db3cd9c9b59467ad48c4866fe1a`
- Priority: P2 evidence-driven remote-safe unit metadata integrity

## Confirmed defect

`DrawingUnitResolutionPolicy.SetProjectOverride(...)` and `BindQuantityUnit(...)` persist unit metadata using canonical enum names such as `Meter`. The corresponding `TryParseNamedUnitToken(...)` currently trims persisted input before validation, so non-canonical metadata such as `" Meter "` is silently accepted by both override resolution and the method named `TryReadCanonical(...)`.

The previous named-token fix intentionally rejected numeric enum aliases while retaining case-insensitive named tokens; this claim is a narrower follow-up for leading/trailing whitespace only.

## Reserved scope

- `src/QS3D.Core/Units/DrawingUnitResolutionPolicy.cs`
- `tests/QS3D.Core.SmokeTests/DrawingUnitResolutionSmoke.cs`
- this claim file

## Expected fix

Reject leading/trailing whitespace in persisted named unit tokens while preserving case-insensitive canonical names (`meter` remains accepted), defined-enum validation, legacy effective-unit migration behavior, and writer output.

## Excluded scope

- No changes to unit conversion factors or `UnitScale`.
- No redesign of legacy `QS3D.DrawingUnit` assumption parsing.
- No BricsCAD/native/runtime or GitHub Actions work.

## Validation plan

- Padded override metadata must fail closed.
- Padded bound metadata must fail closed.
- Lowercase named tokens remain compatible.
- Numeric aliases remain rejected.
- Re-read exact current source/test blobs before each SHA-guarded write.

## Completion condition

Source and focused smoke regression are committed to `main`, exact integration SHAs are recorded, and this claim is marked `COMPLETED` without claiming local BricsCAD/runtime PASS.
