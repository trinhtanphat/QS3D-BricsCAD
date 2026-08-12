# Work claim — Level Reference canonical IDs

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-level-reference-canonical-ids`
- Registered: `2026-08-12T08:38:00+07:00`
- Baseline main SHA: `0abbd050176357572a4b165bf7cec408326bca16`
- Priority: P1 — persisted Bottom/Top Level references must not be silently normalized by diagnostics.
- Task Key: `CORE-LEVEL-REFERENCE-CANONICAL-IDS`

## Confirmed defect

`ProjectFloorService.AssignBottomLevel(...)` / `AssignTopLevel(...)` persist exact canonical `floor.Id` values. `LevelReferenceHealthService`, however, reads `BottomLevelId` and `TopLevelId` through a helper that trims the stored text before validation. Directly mutated or malformed persisted values such as `" L1 "` therefore resolve as valid `L1` instead of producing health evidence. A whitespace-only stored reference is similarly normalized to empty/missing.

## Non-overlap check

The existing Level Reference health lane for null Floor/Level and null element entries is completed. Existing work also covers duplicate floor IDs and invalid/missing references. No recent claim/commit was found for canonical spelling of persisted Bottom/Top Level ID properties.

## Reserved scope

- `src/QS3D.Core/Diagnostics/LevelReferenceHealthService.cs`
- one focused Core smoke regression for non-canonical stored level references
- this claim file

Do not modify ProjectFloor mutation APIs, `ElementVerticalPlacementService`, persistence formats, interchange remap, native placement or BricsCAD runtime code.

## Intended contract

- Persisted Bottom/Top Level ID properties with leading/trailing whitespace fail visible as dedicated `HealthSeverity.Error` diagnostics.
- Whitespace-only stored level reference values also fail visible instead of being treated as absent.
- Canonical `L1`-style references preserve existing missing/ambiguous/range/native-integration behavior.
- Inspection remains read-only and deterministic.
- No GitHub Actions/build/release dispatch and no BricsCAD V25 runtime PASS claim from this remote lane.

## Completion condition

Non-canonical persisted Bottom/Top Level references are fail-visible, focused Core smoke coverage pins padded and whitespace-only cases plus canonical control behavior, source + smoke are read back from merged `main`, ancestry is verified, and this claim is closed with exact commit SHAs.
