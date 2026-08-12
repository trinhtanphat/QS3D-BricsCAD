# Work claim — Drawing-unit bound/override integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-drawing-unit-bound-override-integrity-20260812-0951`
- Registered: `2026-08-12T09:51:00+07:00`
- Baseline main SHA: `873018e60020b09b7f907a4b3ed8c8bba7fe7544`
- Priority: P2 evidence-driven remote-safe unit metadata integrity

## Confirmed defect

`ValidateQuantityCompatibility(...)` explicitly rejects an effective drawing unit that differs from the canonical `QS3D.DrawingUnitBound.v1` quantity binding. However the public writer `SetProjectOverride(...)` currently writes `QS3D.DrawingUnitOverride.v1`, `QS3D.DrawingUnit`, and the binding source without checking that existing canonical quantity binding first.

A project bound to `Meter` can therefore be mutated by `SetProjectOverride(..., Millimeter)` into internally contradictory persisted metadata. The contradiction is detected only if a later caller happens to run quantity compatibility validation.

## Reserved scope

- `src/QS3D.Core/Units/DrawingUnitResolutionPolicy.cs`
- `tests/QS3D.Core.SmokeTests/DrawingUnitResolutionSmoke.cs`
- this claim file

## Expected fix

Preflight an existing canonical quantity-unit binding before mutating override metadata. Reject a differing unit without partial writes; allow a matching bound unit and preserve the current behavior when no bound key exists. Existing canonical-token, blank/padded/numeric, native INSUNITS and legacy migration behavior must remain unchanged.

## Excluded scope

- No redesign of remeasurement/rebinding workflows.
- No unit conversion-factor changes.
- No BricsCAD/native/runtime or GitHub Actions work.

## Validation plan

- Bound `Meter` + attempted override `Millimeter` fails closed.
- Failed override leaves existing override/effective/source metadata unchanged.
- Bound `Meter` + override `Meter` remains accepted.
- No-bound override behavior remains accepted.
- Existing metadata canonicality regressions remain intact.

## Completion condition

Source and focused smoke regression are committed to `main`, exact integration SHAs are recorded, and this claim is marked `COMPLETED` without claiming local BricsCAD/runtime PASS.
