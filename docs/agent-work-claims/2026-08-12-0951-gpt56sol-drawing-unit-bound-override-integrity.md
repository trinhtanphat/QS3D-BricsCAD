# Work claim — Drawing-unit bound/override integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-drawing-unit-bound-override-integrity-20260812-0951`
- Registered: `2026-08-12T09:51:00+07:00`
- Completed: `2026-08-12T09:54:00+07:00`
- Baseline main SHA: `873018e60020b09b7f907a4b3ed8c8bba7fe7544`
- Claim commit: `495cc3660659aa2cd891dc6392322d1215473449`
- Source fix commit: `790af584a2b356c04303913cfd750991a0f13961`
- Regression commit: `b3169198fcae5703b871151b832cc38f817d715d`
- Priority: P2 evidence-driven remote-safe unit metadata integrity

## Confirmed defect

`ValidateQuantityCompatibility(...)` explicitly rejects an effective drawing unit that differs from the canonical `QS3D.DrawingUnitBound.v1` quantity binding. The public writer `SetProjectOverride(...)` previously wrote `QS3D.DrawingUnitOverride.v1`, `QS3D.DrawingUnit`, and the binding source without checking that existing canonical quantity binding first.

A project bound to `Meter` could therefore be mutated by `SetProjectOverride(..., Millimeter)` into internally contradictory persisted metadata. The contradiction was detected only if a later caller happened to run quantity compatibility validation.

## Reserved scope

- `src/QS3D.Core/Units/DrawingUnitResolutionPolicy.cs`
- `tests/QS3D.Core.SmokeTests/DrawingUnitResolutionSmoke.cs`
- this claim file

## Implemented fix

`SetProjectOverride(...)` now preflights any existing canonical bound unit before mutating metadata. A differing bound unit fails with the same remeasurement guidance used by quantity compatibility validation. Matching bound units and projects without a bound key retain existing override behavior.

The preflight occurs before all override/effective/source writes, so a rejected request cannot leave partial metadata changes.

## Validation evidence

- `b3169198fcae5703b871151b832cc38f817d715d` covers bound `Meter` + attempted `Millimeter` override failing closed.
- The regression proves the pre-existing override/effective/source values remain unchanged after failure.
- The same regression proves a matching `Meter` override remains accepted and preserves the bound metadata.
- Existing no-bound, canonical-token, blank/padded/numeric and QSDB round-trip cases remain in the same smoke source.
- Current source and smoke were re-read after integration and contain the expected preflight/regression.

## Excluded scope

- No redesign of remeasurement/rebinding workflows.
- No unit conversion-factor changes.
- No BricsCAD/native/runtime or GitHub Actions work.

## Completion condition

Completed: source and focused smoke regression are committed to `main`, exact integration SHAs are recorded, and no local BricsCAD/runtime PASS is claimed.
