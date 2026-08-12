# Work claim — Tie Rebar core generated metadata canonicality

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-tie-rebar-core-metadata-canonicality`
- Registered: `2026-08-12T12:13:00+07:00`
- Baseline main SHA: `6d93e196d07b02afc59d71aa42f83ac283a7a706`
- Priority: P1 — generated Tie Rebar count/diameter/actual-spacing metadata must preserve writer-owned serialization.
- Task Key: `CORE-TIE-REBAR-CORE-METADATA-CANONICALITY`

## Confirmed defect

`ColumnTieSolidBuilder.CommitSemanticUpdate(...)` persists `GeneratedTieRebarCount` with invariant `int.ToString`, `GeneratedTieRebarDiameterMm` with `double.ToString("R", CultureInfo.InvariantCulture)`, and `GeneratedTieRebarActualSpacingM` with `double.ToString("R", CultureInfo.InvariantCulture)`.

`GeneratedTieRebarHealthService` currently accepts count through integer parsing/count equality and diameter/actual-spacing through numeric domain checks only. Alternate raw spellings such as `01`, `10.0`, or `0.200` can therefore pass health even though the writer never emits those spellings.

## Non-overlap check

Recent commit/PR checks found no Tie Rebar count/diameter/spacing canonicality lane. The completed Tie Rebar cover/mode lane owns different metadata; handle canonicality owns only generated handle tokens.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedTieRebarHealthService.cs`
- one focused Core smoke regression for count/diameter/actual-spacing canonicality
- this claim file

Do not modify Column Tie planner/builder, cover/mode validation, handle ownership/native CAD generation, persistence format, command wrappers, or BricsCAD runtime code.

## Intended contract

- A count that parses and equals the valid handle count must use exact invariant integer spelling or emit `TIE_REBAR_GENERATED_COUNT_NON_CANONICAL` as Error.
- A finite positive diameter must use exact round-trip invariant spelling or emit `TIE_REBAR_GENERATED_DIAMETER_NON_CANONICAL` as Error.
- A finite nonnegative actual spacing must use exact round-trip invariant spelling or emit `TIE_REBAR_GENERATED_SPACING_NON_CANONICAL` as Error.
- Existing mismatch/invalid precedence remains unchanged; invalid values do not receive canonicality noise.
- Exact writer-owned values, including zero actual spacing, preserve existing behavior.

## Completion condition

Count/diameter/spacing aliases are fail-visible without changing existing invalid/mismatch semantics, focused smoke coverage pins aliases plus invalid/zero/canonical controls, source + smoke are read back from merged `main`, ancestry is verified, and this claim is closed with exact commit SHAs.
