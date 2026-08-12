# Work claim — Beam Stirrup advanced numeric metadata canonicality

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-beam-stirrup-advanced-numeric-canonicality`
- Registered: `2026-08-12T12:00:00+07:00`
- Baseline main SHA: `004cc684707c08ad9f41a179717ab39c38d17d90`
- Priority: P1 — advanced generated Beam Stirrup numeric snapshots must preserve writer-owned round-trip spelling.
- Task Key: `CORE-BEAM-STIRRUP-ADVANCED-NUMERIC-CANONICALITY`

## Confirmed defect

`BeamStirrupSolidBuilder.CommitSemanticUpdate(...)` persists all six advanced numeric snapshots with `double.ToString("R", CultureInfo.InvariantCulture)`: `GeneratedBeamStirrupCenterlineLengthM`, `GeneratedBeamStirrupTotalCenterlineLengthM`, `GeneratedBeamStirrupPolylineLengthM`, `GeneratedBeamStirrupBendRadiusM`, `GeneratedBeamStirrupHookLengthM`, and `GeneratedBeamStirrupHookTailAngleDeg`.

`GeneratedBeamStirrupHealthService` currently validates those fields through numeric parsing/domain relationships only. Alternate raw spellings such as `4.0` or `0.0` therefore pass health when they represent otherwise valid values, even though the writer never emits those spellings.

## Non-overlap check

Recent history search found no Beam Stirrup advanced-numeric canonicality lane. Completed Beam Stirrup actual-spacing and core count/diameter/mode canonicality lanes own different metadata.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedBeamStirrupHealthService.cs`
- one focused Core smoke regression for the six advanced numeric keys
- this claim file

Do not modify Beam Stirrup planner/builder, handles/count/diameter/actual-spacing/mode validation, advanced geometry relationships, native CAD generation, persistence format, command wrappers, or BricsCAD runtime code.

## Intended contract

- After an advanced numeric field passes its existing finite and standalone domain rule, its raw text must equal `value.ToString("R", CultureInfo.InvariantCulture)` or emit `BEAM_STIRRUP_GENERATED_METADATA_NON_CANONICAL` as Error.
- Existing invalid, length mismatch and mode mismatch diagnostics remain unchanged and continue to use parsed values.
- Invalid/nonfinite values do not receive canonicality evidence before numeric validity is established.
- Exact writer-owned round-trip strings preserve existing behavior.

## Completion condition

Aliases across positive, nonnegative and angle snapshots are fail-visible without changing existing relationship semantics, focused smoke coverage pins representative aliases plus invalid/canonical controls, source + smoke are read back from merged `main`, ancestry is verified, and this claim is closed with exact commit SHAs.
