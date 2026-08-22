# Work claim — Core schedule provenance

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-core-schedule-provenance`
- Registered: `2026-08-11T20:40:00+07:00`
- Baseline main SHA: `b0ebaa6043cc933cc4bf017ee9aa5ca50b1d4e07`
- Priority: make Core schedule rows retain enough semantic/CAD provenance for deterministic reverse review instead of exposing only aggregated quantities and element IDs.

## Reserved scope

Add read-only provenance to Material Usage, Curtain Wall, Door/Opening and Room Finish schedule rows without changing quantity formulas, grouping keys or mutation behavior. Each produced row should retain the project identity, drawing fingerprint, semantic element IDs and normalized/deduplicated source CAD handles that contributed to that row.

## Expected surfaces

- `src/QS3D.Core/Reporting/MaterialUsageSchedule.cs`
- `src/QS3D.Core/Reporting/CurtainWallSchedule.cs`
- `src/QS3D.Core/Reporting/DoorOpeningSchedule.cs`
- `src/QS3D.Core/Reporting/RoomFinishSchedule.cs`
- `src/QS3D.Core/Reporting/` one focused internal provenance helper if useful
- `tests/QS3D.Core.SmokeTests/` focused provenance regression + registration only as needed
- this claim file for close-out

## Explicit exclusions

- No BricsCAD/WPF/modeless schedule or BQ UI changes. Current BQ-detail and right-panel quantity claims explicitly own those UI surfaces and exclude Core schedule builders.
- No quantity arithmetic, grouping-key, geometry, persistence, mutation, Room Auto, Direct Draw/Create Similar or material-catalog mutation changes.
- No GitHub Actions/build/release dispatch and no native BricsCAD V25 runtime PASS claim.

## Validation plan

- Re-fetch target builders from current `main` after claim publication and preserve the just-merged schedule identity guard.
- Populate `ProjectId` and `DrawingFingerprint` on every created schedule row.
- Carry contributing `ProjectElement.SourceHandles` into rows using trimmed, case-insensitive first-seen deduplication; ignore blank handles.
- Add deterministic smoke coverage for all four builders, including duplicate/case-variant source handles and valid project/drawing identity.
- Compare against current `main` immediately before integration and merge only if target files remain non-overlapping.

## Coordination

This follows completed PR `#450` / merge `2b15723bc8670d9ce8e8ead967718ec1bd0eaea7` and preserves its fail-closed element-identity guard. Active BQ-detail/right-panel work is UI-only and explicitly excludes Core reporting builders. Room Auto and Core mutation-atomicity lanes are also excluded.

## Completion

- PR: `#452` — `feat(reporting): retain source provenance in schedule rows`
- Reviewed feature head before squash: `2bcc545e2464038e82868df52898d74a10817dda`
- Squash merge on `main`: `072b622e6c4dc26139de0448181a995004a557b6`
- Added `ProjectId`, `DrawingFingerprint` and `SourceHandles` provenance to Material Usage, Curtain Wall, Door/Opening and Room Finish schedule rows.
- Added `ReportingRowProvenance` for trimmed, case-insensitive, first-seen source Handle deduplication.
- Extended the already-registered `ScheduleReportingIdentitySmoke` with deterministic provenance coverage for all four builders; no additional test registration file was touched.
- Final reviewed feature diff was 6 files / 157 additions / 1 deletion; the one deletion was only the existing one-line Curtain row initializer expanded into a multiline initializer.
- Compared concurrent `main` changes twice before integration; neither comparison touched the six reserved source/test surfaces.
- Quantity formulas, grouping keys, geometry, persistence, mutation and BricsCAD/WPF UI were unchanged.
- GitHub Actions/build/release were not dispatched for this lane.
- No native BricsCAD V25/WPF runtime PASS is claimed.

## Completion condition

Satisfied by PR `#452` and merge `072b622e6c4dc26139de0448181a995004a557b6`: all four Core schedules now expose deterministic project/drawing/source-handle provenance with focused smoke coverage, existing quantity/grouping behavior is unchanged, and the change was integrated without overwriting concurrent work.
