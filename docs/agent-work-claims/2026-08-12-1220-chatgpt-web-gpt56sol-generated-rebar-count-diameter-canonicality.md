# Work claim — Generated Rebar count/diameter canonicality

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-generated-rebar-count-diameter-canonicality`
- Registered: `2026-08-12T12:20:00+07:00`
- Baseline main SHA: `e7f78bed2809247057ce790b4c2d1c3133b0603b`
- Priority: P1 — writer-owned generated longitudinal/shape rebar core metadata must preserve exact serialization.
- Task Key: `CORE-GENERATED-REBAR-COUNT-DIAMETER-CANONICALITY`

## Confirmed defect

`ColumnRebarSolidBuilder` and `BeamRebarSolidBuilder` both persist `GeneratedRebarCount` with invariant `int.ToString(...)` and `GeneratedRebarDiameterMm` with `double.ToString("R", CultureInfo.InvariantCulture)`. `ShapeRebarSolidBuilder` persists `GeneratedShapeRebarCount` with invariant `int.ToString(...)`.

`GeneratedRebarHealthService` currently validates longitudinal/shape counts through integer parsing plus expected-count equality and validates longitudinal diameter through finite positive numeric parsing only. Alternate raw spellings such as `01` or `10.0` can therefore pass health even though the writers never emit them.

## Non-overlap check

Recent commit searches found no generated-rebar count canonicality, shape-rebar count canonicality, or generated-rebar diameter canonicality lane. Generated rebar mode semantics/null-health already has its own completed history and is explicitly excluded from this claim.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedRebarHealthService.cs`
- one focused Core smoke regression for longitudinal count, shape count, and longitudinal diameter canonicality
- this claim file

Do not modify generated-rebar mode semantics, cover/beam-specific metadata, builders/planners, handle ownership/native CAD generation, persistence format, command wrappers, or BricsCAD runtime code.

## Intended contract

- A longitudinal count that parses and equals the valid generated-handle count must use exact invariant integer spelling or emit `REBAR_GENERATED_COUNT_NON_CANONICAL` as Error.
- A shape count that parses and equals the valid generated-shape handle count must use exact invariant integer spelling or emit `SHAPE_REBAR_GENERATED_COUNT_NON_CANONICAL` as Error.
- A finite positive longitudinal generated diameter must use exact round-trip invariant spelling or emit `REBAR_GENERATED_DIAMETER_NON_CANONICAL` as Error.
- Existing count mismatch and diameter invalid precedence remains unchanged; invalid/mismatched values do not receive canonicality noise.
- Exact writer-owned values preserve existing behavior.

## Completion condition

Longitudinal/shape count aliases and diameter aliases are fail-visible without changing invalid/mismatch semantics, focused smoke coverage pins aliases plus invalid/mismatch/canonical controls, source + smoke are read back from merged `main`, ancestry is verified, and this claim is closed with exact commit SHAs.
