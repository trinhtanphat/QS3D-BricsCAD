# Work claim — ElementInstance finite stored measurements

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:23:00+07:00`
- Baseline main SHA: `42ad446c6d70ba4462e4c830e83d16733aa368e1`
- Priority: evidence-driven remote-safe Core domain integrity

## Confirmed defect

`ElementInstance` exposes thirteen public stored measurement `double` auto-properties (length, areas and volumes). All currently accept `double.NaN` and infinities, allowing non-finite measurement state to persist in a public Core domain instance even though downstream quantity APIs increasingly fail closed on non-finite values.

## Reserved scope

Require every stored numeric measurement assignment on `ElementInstance` to be finite. Preserve current default zero values and every finite value, including negative values; this lane does not introduce nonnegative quantity or engineering rules.

Covered stored properties:

- `LengthM`
- `AreaM2`
- `VolumeM3`
- `GrossConcreteM3`
- `DeductionM3`
- `FormworkM2`
- `DoorAreaM2`
- `OuterPerimeterM`
- `InnerPerimeterM`
- `SideAreaM2`
- `BottomAreaM2`
- `TopAreaM2`
- `OtherAreaM2`

## Expected surfaces

- `src/QS3D.Core/Domain/ElementInstance.cs`
- `tests/QS3D.Core.SmokeTests/ElementInstanceFiniteMeasurementsSmoke.cs`
- `tests/QS3D.Core.SmokeTests/ElementInstanceFiniteMeasurementsRegistration.cs`
- this claim file

## Excluded scope

- No `ProjectElement.cs` changes; another active lane currently reserves `SetQuantity` there.
- No reporting nonnegative policy, quantity formulas, rounding/tolerance or unit semantics.
- No change to `NetConcreteM3` derived subtraction/overflow behavior in this lane.
- No family/floor/source-handle behavior changes.
- No GitHub Actions dispatch.

## Validation plan

- Preserve all zero defaults.
- Preserve representative zero, negative and positive finite assignments.
- Reject NaN, +Infinity and -Infinity across all thirteen stored measurement properties.
- Verify rejected assignments retain the previous finite stored value.
- Use a dedicated module initializer to avoid shared smoke registry contention.
- Re-fetch target blob immediately before product write and inspect exact pushed diffs/ancestry.
- No .NET/V25 runtime PASS will be claimed unless actually executed.

## Coordination

Recent commit/claim search found no active or recent reservation for `ElementInstance` finite measurement setters. This is intentionally narrower than reporting nonnegative-integrity work and does not touch reporting surfaces.

## Completion condition

`ElementInstance` cannot retain non-finite stored measurements through its public setters, focused smoke coverage is on current `main`, concurrent work is preserved, and this claim is closed with exact commit SHAs.