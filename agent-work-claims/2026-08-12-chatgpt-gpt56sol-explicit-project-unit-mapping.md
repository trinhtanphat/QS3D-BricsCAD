# Work claim — explicit project unit mapping

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-unit-map`
- Registered: `2026-08-12T08:08:35+07:00`
- Completed: `2026-08-12T08:10:20+07:00`
- Baseline main SHA: `480fbe6e757a7880ea675cef6e21a75bd6180ac9`
- Claim commit: `628a390f2c3063fb1ad73910657e9a53da884691`
- Implementation commit: `fe1b0be2900c0e0feb477b5ff98983065930f0b4`
- Regression-test commit: `9dac58bda18c16733f7b2ede9f204cbd0359ecd0`
- Final pushed product/test SHA: `9dac58bda18c16733f7b2ede9f204cbd0359ecd0`
- Priority: `Correctness hardening discovered during requested full repository review; prevent silent unit semantic drift from ordinal enum coupling.`

## Reserved scope

Replace the ordinal cast in `ProjectUnitPolicy.ToDrawingUnit(LengthUnit)` with an explicit one-to-one mapping and add focused regression coverage proving every supported `LengthUnit` maps to the same-named `DrawingUnit` while undefined values still fail closed.

## Implemented

- Replaced `(DrawingUnit)(int)unit` with explicit fail-closed mapping for every currently supported `LengthUnit`.
- Preserved rejection of undefined values without relying on ordinal alignment between the two enum declarations.
- Extended `DrawingUnitResolutionSmoke` to cover undefined direct mapping and to enumerate every declared `LengthUnit`, asserting same-named `DrawingUnit` semantics.

## Changed surfaces

- `src/QS3D.Core/Units/ProjectUnitPolicy.cs`
- `tests/QS3D.Core.SmokeTests/DrawingUnitResolutionSmoke.cs`

## Excluded scope

- `UnitScale` conversion factors and numeric conversion behavior
- `DrawingUnitResolutionPolicy` and BricsCAD INSUNITS/runtime unit resolution
- UI, authoring prompts, exporters, persistence, quantity rules, rebar, Model Health, workspace and Preview Review
- renaming/reordering either enum or adding new unit kinds

## Validation performed

- Re-read the existing completed project-unit enum-integrity claim and reused its established smoke surface rather than creating a duplicate harness.
- Source write was SHA-guarded against the exact `ProjectUnitPolicy.cs` blob and pushed as `fe1b0be2900c0e0feb477b5ff98983065930f0b4`.
- Regression write was SHA-guarded against the exact `DrawingUnitResolutionSmoke.cs` blob and pushed as `9dac58bda18c16733f7b2ede9f204cbd0359ecd0`.
- The regression covers all declared unit names plus undefined-value failure, so future enum insertion/reordering cannot silently pass an ordinal cast contract.
- No GitHub Actions workflow was dispatched or re-run. This remote qualification does not claim licensed BricsCAD V25 runtime execution.

## Coordination

Recent completed project-unit enum-integrity work added undefined-value guards. This completed follow-up was limited to eliminating the remaining ordinal-cast coupling; it did not reopen conversion-factor or runtime unit-resolution lanes. No newer overlapping `claim unit` commit appeared after this reservation during implementation.

## Outcome

Project-level unit conversion no longer depends on two independent enum declarations retaining identical numeric ordinals. The explicit mapping and focused smoke regression are pushed to `main` without force-push or overwrite of concurrent work.
