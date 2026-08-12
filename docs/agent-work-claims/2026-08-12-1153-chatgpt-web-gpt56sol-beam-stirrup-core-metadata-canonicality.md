# Work claim — Beam Stirrup core generated metadata canonicality

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-beam-stirrup-core-metadata-canonicality`
- Registered: `2026-08-12T11:53:00+07:00`
- Baseline main SHA: `1bf530136cfff6a7ae8e99074d5089fa1b3662fd`
- Priority: P1 — generated Beam Stirrup core metadata must preserve writer-owned serialization.
- Task Key: `CORE-BEAM-STIRRUP-CORE-METADATA-CANONICALITY`

## Confirmed defect

`BeamStirrupSolidBuilder` persists `GeneratedBeamStirrupCount` with invariant `int.ToString`, `GeneratedBeamStirrupDiameterMm` with `double.ToString("R", CultureInfo.InvariantCulture)`, and `GeneratedBeamStirrupMode` as one of the planner-owned exact literals `Beam.Line.RectangularClosedLoop`, `Beam.Line.RectangularRoundedLoop`, or `Beam.Line.RectangularHookedPath`.

`GeneratedBeamStirrupHealthService` currently accepts count through `int.TryParse`, diameter through `double.TryParse`, and mode through trim + case-insensitive comparison. Alternate raw spellings such as `01`, `10.0`, or ` beam.line.rectangularclosedloop ` can therefore pass semantic health even though the writer never emits those spellings.

## Non-overlap check

Recent commit/PR checks found no Beam Stirrup count/diameter/mode canonicality lane. The completed actual-spacing lane owns only `GeneratedBeamStirrupActualSpacingM`; handle canonicality owns only generated handle tokens. Active DependencyImpact work does not overlap this provider scope.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedBeamStirrupHealthService.cs`
- one focused Core smoke regression for count/diameter/mode canonicality
- this claim file

Do not modify Beam Stirrup planner/builder, actual-spacing validation, advanced centerline/polyline/bend/hook/angle metadata, handle ownership, native CAD generation, persistence format, command wrappers, or BricsCAD runtime code.

## Intended contract

- A count that parses and equals the valid handle count must use exact invariant integer spelling or emit `BEAM_STIRRUP_GENERATED_COUNT_NON_CANONICAL` as Error.
- A finite positive diameter must use exact round-trip invariant spelling or emit `BEAM_STIRRUP_GENERATED_DIAMETER_NON_CANONICAL` as Error.
- A present recognized mode alias must use the exact writer-owned literal or emit `BEAM_STIRRUP_GENERATED_MODE_NON_CANONICAL` as Error.
- Existing count mismatch, diameter invalid, unsupported/missing-mode and advanced-mode semantics retain precedence.
- Exact writer-owned values preserve existing behavior.

## Completion condition

Count/diameter/mode aliases are fail-visible without changing invalid/mismatch behavior, focused smoke coverage pins aliases plus invalid/canonical controls, source + smoke are read back from merged `main`, ancestry is verified, and this claim is closed with exact commit SHAs.
