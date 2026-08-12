# Work claim — Beam Stirrup actual-spacing health integrity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-beam-stirrup-actual-spacing-health`
- Registered: `2026-08-12T11:39:00+07:00`
- Baseline main SHA: `72d81943726687163a9e0fb7e765ebd1e00c81af`
- Priority: P1 — writer-owned generated Beam Stirrup spacing metadata must not bypass health validation.
- Task Key: `CORE-BEAM-STIRRUP-ACTUAL-SPACING-HEALTH`

## Confirmed defect

`BeamStirrupSolidBuilder` always persists `GeneratedBeamStirrupActualSpacingM` from `BeamStirrupLayoutPlanner.ActualSpacingM` using `double.ToString("R", CultureInfo.InvariantCulture)`. `GeneratedBeamStirrupHealthService` currently never reads this persisted field. A generated stirrup can therefore carry malformed `NaN`, `Infinity`, negative spacing, or an alternate non-writer spelling without any spacing-specific health evidence.

`BeamStirrupLayoutPlanner` permits a single-stirrup layout, so zero actual spacing is valid; the domain bound is finite and nonnegative, not strictly positive.

## Non-overlap check

Recent commit/code searches found no Beam Stirrup actual-spacing health lane. Existing Beam Stirrup handle canonicality and related generated-handle work own different metadata.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedBeamStirrupHealthService.cs`
- one focused Core smoke regression for `GeneratedBeamStirrupActualSpacingM`
- this claim file

Do not modify the Beam Stirrup planner/builder, native CAD generation, handle ownership, other generated metadata, persistence format, command wrappers, or BricsCAD runtime code.

## Intended contract

- Present non-empty actual-spacing metadata must parse as an invariant finite number >= 0 or emit `BEAM_STIRRUP_ACTUAL_SPACING_INVALID`.
- After numeric/domain validity, raw text must equal `value.ToString("R", CultureInfo.InvariantCulture)` or emit `BEAM_STIRRUP_ACTUAL_SPACING_NON_CANONICAL`.
- Invalid values do not receive canonicality noise.
- Exact writer-owned `0` and positive round-trip values preserve existing behavior.
- Inspection remains read-only and deterministic.

## Completion condition

Malformed and noncanonical actual-spacing metadata is fail-visible, focused smoke coverage pins NaN/negative/noncanonical/zero/canonical controls, source + smoke are read back from merged `main`, ancestry is verified, and this claim is closed with exact commit SHAs.
