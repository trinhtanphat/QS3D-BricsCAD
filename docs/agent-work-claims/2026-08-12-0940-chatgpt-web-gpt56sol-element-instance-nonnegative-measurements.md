# Work claim — Element instance non-negative measurements

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T09:40:00+07:00`
- Baseline main SHA: `54ccdc640cdc28c871d42040b9c1858108ef83cc`
- Priority: evidence-driven remote-safe domain quantity invariant

## Reason

`ElementInstance` currently rejects only NaN/Infinity for physical quantity fields, so negative length, area, volume, deduction, formwork and perimeter values can be stored in the public domain model. Downstream quantity math requires non-negative physical measurements, making these values guaranteed-invalid delayed inputs rather than meaningful signed geometry offsets.

## Intended scope

Require all physical measurement properties on `ElementInstance` to be finite and non-negative while preserving zero as a valid neutral/default value, existing property names, floor normalization, source-handle behavior and the existing `NetConcreteM3` derived calculation.

## Changed surfaces

- `src/QS3D.Core/Domain/ElementInstance.cs`
- `tests/QS3D.Core.SmokeTests/ElementInstanceNonNegativeMeasurementsSmoke.cs`
- this claim file

## Validation boundary

Remote/static validation only in this hosted session. Do not dispatch/rerun GitHub Actions and do not claim BricsCAD V25/V26 or local .NET runtime PASS without actual supported runtime execution.