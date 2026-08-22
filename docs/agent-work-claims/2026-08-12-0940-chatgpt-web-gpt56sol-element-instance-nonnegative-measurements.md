# Work claim — Element instance non-negative measurements

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T09:40:00+07:00`
- Baseline main SHA: `54ccdc640cdc28c871d42040b9c1858108ef83cc`
- Merge SHA: `47dff0f9b81fc3145f77e680d8ae70d3518ad6e9`
- Priority: evidence-driven remote-safe domain quantity invariant

## Reason

`ElementInstance` previously rejected only NaN/Infinity for physical quantity fields, so negative length, area, volume, deduction, formwork and perimeter values could be stored in the public domain model. Downstream quantity math requires non-negative physical measurements, making these values guaranteed-invalid delayed inputs rather than meaningful signed geometry offsets.

## Completed scope

All physical measurement properties on `ElementInstance` now require finite, non-negative values while preserving zero as a valid neutral/default value, existing property names, floor normalization, source-handle behavior and the existing `NetConcreteM3` derived calculation. Focused module-initializer smoke coverage guards all 13 measurement setters against negative and non-finite inputs and confirms zero/positive values remain accepted.

## Changed surfaces

- `src/QS3D.Core/Domain/ElementInstance.cs`
- `tests/QS3D.Core.SmokeTests/ElementInstanceNonNegativeMeasurementsSmoke.cs`
- this claim file

## Validation boundary

Remote/static validation only in this hosted session. GitHub `main` readback confirmed the source and smoke after merge. No GitHub Actions were dispatched/rerun and no BricsCAD V25/V26 or local .NET runtime PASS is claimed.