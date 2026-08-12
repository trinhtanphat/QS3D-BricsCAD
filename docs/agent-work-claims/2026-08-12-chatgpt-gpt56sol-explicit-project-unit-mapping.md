# Work claim — explicit project unit mapping

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-unit-map`
- Registered: `2026-08-12T08:08:35+07:00`
- Baseline main SHA: `480fbe6e757a7880ea675cef6e21a75bd6180ac9`
- Priority: `Correctness hardening discovered during requested full repository review; prevent silent unit semantic drift from ordinal enum coupling.`

## Reserved scope

Replace the ordinal cast in `ProjectUnitPolicy.ToDrawingUnit(LengthUnit)` with an explicit one-to-one mapping and add focused regression coverage proving every supported `LengthUnit` maps to the same-named `DrawingUnit` while undefined values still fail closed.

## Expected surfaces

- `src/QS3D.Core/Units/ProjectUnitPolicy.cs`
- focused Core/unit smoke or regression test surface that covers `ProjectUnitPolicy.ToDrawingUnit`

## Excluded scope

- `UnitScale` conversion factors and numeric conversion behavior
- `DrawingUnitResolutionPolicy` and BricsCAD INSUNITS/runtime unit resolution
- UI, authoring prompts, exporters, persistence, quantity rules, rebar, Model Health, workspace and Preview Review
- renaming/reordering either enum or adding new unit kinds

## Validation plan

- Inspect existing unit test/preflight conventions and extend the narrowest existing surface.
- Assert all declared `LengthUnit` values map explicitly to the same semantic `DrawingUnit` value by name.
- Preserve undefined-enum rejection.
- Review resulting source/test diff against current `main`; do not dispatch GitHub Actions.

## Coordination

Recent completed project-unit enum-integrity work added undefined-value guards. This claim is a non-overlapping follow-up limited to eliminating the remaining ordinal-cast coupling; it does not reopen conversion-factor or runtime unit-resolution lanes.

## Completion condition

The claim commit is on `main`, implementation plus focused regression coverage is pushed without overwriting concurrent work, and this claim is updated to `COMPLETED` with exact commit/evidence notes.
