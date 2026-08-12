# Work claim — Takeoff count drawing-unit definedness

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T09:08:00+07:00`
- Baseline main SHA: `398474ddea51243179b80eb9b0b54e051b515970`
- Priority: evidence-driven remote-safe Core input validation

## Reason

`QuantityEngine.Calculate` accepts a public `DrawingUnit` argument for every takeoff kind. Length, area, and volume reach `UnitScale`, whose switch rejects undefined `DrawingUnit` enum values, but `TakeoffKind.Count` returns `1 ea` before any drawing-unit validation. An undefined enum value is therefore silently accepted only on the Count early-return path.

## Reserved scope

Validate that the supplied `DrawingUnit` is defined before branching on `TakeoffKind`, so every public `QuantityEngine.Calculate` path fails closed for unsupported drawing units. Preserve quantity math, Count value/unit, metric availability behavior, supported unit conversions, `TakeoffResult`, and all BricsCAD-facing behavior.

## Expected surfaces

- `src/QS3D.Core/Takeoff/QuantityEngine.cs`
- `tests/QS3D.Core.SmokeTests/QuantityEngineDrawingUnitValidationSmoke.cs`
- `tests/QS3D.Core.SmokeTests/QuantityEngineDrawingUnitValidationRegistration.cs`
- this claim file

## Excluded scope

- No changes to `TakeoffResult`, wall quantity/reporting, exporters, project persistence, UI, or BricsCAD V25 runtime.
- No unit remapping or conversion-policy changes.
- No GitHub Actions dispatch.

## Validation plan

- Assert a supported drawing unit still returns Count = `1` with unit `ea`.
- Assert an undefined `DrawingUnit` throws `ArgumentOutOfRangeException` even for `TakeoffKind.Count`.
- Assert the metric branches retain existing conversion behavior by keeping the change before the existing switch only.
- Re-fetch current `main` and target blobs after this claim lands and before every write; never force-push.
- Record static/exact-diff/ancestry verification only; do not claim an executed repository `dotnet` or BricsCAD V25 run in this hosted session.

## Coordination

The recent `takeoff-result-token-canonicalization` claim explicitly excludes `QuantityEngine` and drawing-unit conversion. Recent claim search found no active reservation for this validation-ordering defect.

## Completion condition

Current `main` rejects undefined drawing-unit enum values consistently across all takeoff kinds, includes focused CAD-independent smoke coverage, and this claim is marked `COMPLETED`.
