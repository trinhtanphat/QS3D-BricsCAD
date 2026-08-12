# Work claim — Takeoff count drawing-unit definedness

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T09:08:00+07:00`
- Completed: `2026-08-12T09:13:00+07:00`
- Baseline main SHA: `398474ddea51243179b80eb9b0b54e051b515970`
- Claim commit: `bfe52704a50a8db13f811598dbb81cdf63c13c82`
- Fix commit: `c120518001a11075194b563a4043973bb27763f4`
- Smoke commit: `fa18214d48a1e7d92f26c345f11ad5ff4a14f5af`
- Registration commit: `8cd7f3438de2b67cad51b9f66ea8749cece2b38e`
- Priority: evidence-driven remote-safe Core input validation

## Reason

`QuantityEngine.Calculate` accepts a public `DrawingUnit` argument for every takeoff kind. Length, area, and volume reach `UnitScale`, whose switch rejects undefined `DrawingUnit` enum values, but `TakeoffKind.Count` returned `1 ea` before any drawing-unit validation. An undefined enum value was therefore silently accepted only on the Count early-return path.

## Implemented

`QuantityEngine.Calculate` now validates `TakeoffKind` first and `DrawingUnit` second, before entering the existing switch. This preserves the existing invalid-kind precedence while making `TakeoffKind.Count` fail closed for undefined drawing units. The Count value/unit and metric conversion branches are otherwise unchanged.

Focused CAD-independent smoke coverage now asserts:

- supported Count remains `1 ea`;
- undefined Count drawing unit throws `ArgumentOutOfRangeException` for `drawingUnit`;
- undefined takeoff kind retains precedence when both enums are malformed;
- supported `1000 mm` length conversion remains `1 m`.

A dedicated module-initializer registration invokes the new smoke without modifying shared registration surfaces.

## Reserved scope

- `src/QS3D.Core/Takeoff/QuantityEngine.cs`
- `tests/QS3D.Core.SmokeTests/QuantityEngineDrawingUnitValidationSmoke.cs`
- `tests/QS3D.Core.SmokeTests/QuantityEngineDrawingUnitValidationRegistration.cs`
- this claim file

## Excluded scope

- No changes to `TakeoffResult`, wall quantity/reporting, exporters, project persistence, UI, or BricsCAD V25 runtime.
- No unit remapping or conversion-policy changes.
- No GitHub Actions dispatch.

## Validation

- Exact fix diff: three inserted lines in `QuantityEngine.cs`; no unrelated product edits.
- Exact smoke diff: one new focused smoke source.
- Exact registration diff: one new dedicated module-initializer source.
- `8cd7f3438de2b67cad51b9f66ea8749cece2b38e` was verified as an ancestor of observed current `main`; intervening commits modified only disjoint surfaces.
- Static/exact-diff/ancestry verification only. No repository `dotnet` or licensed BricsCAD V25 runtime PASS is claimed from this hosted session.

## Coordination

The recent `takeoff-result-token-canonicalization` claim explicitly excluded `QuantityEngine` and drawing-unit conversion. No competing active reservation for this defect was found before the claim was created.

## Completion condition

Satisfied: current `main` rejects undefined drawing-unit enum values consistently across all takeoff kinds, includes focused CAD-independent smoke coverage, and this claim is `COMPLETED`.
