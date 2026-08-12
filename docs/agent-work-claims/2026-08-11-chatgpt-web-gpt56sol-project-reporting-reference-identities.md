# Work claim — project reporting reference identity integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-project-reporting-reference-identities`
- Registered: `2026-08-11T21:51:00+07:00`
- Baseline: newest `main` at claim branch creation
- Priority: fail closed on malformed Floor/Zone/Family identity collections before Core reporting constructs case-insensitive lookup dictionaries.

## Confirmed defect

`ProjectState.Floors`, `Zones` and `Families` are exposed mutable `IList` collections. Project-backed BQ and Core schedules built `ToDictionary(..., StringComparer.OrdinalIgnoreCase)` maps directly. A null collection entry could cause incidental `NullReferenceException`; duplicate case-insensitive IDs could cause generic duplicate-key exceptions. This was less deterministic than the shared element identity boundary and could surface after unrelated validation rather than as an explicit reporting-project integrity error.

## Reserved scope

- `src/QS3D.Core/Reporting/ReportingProjectIdentityGuard.cs`
- `src/QS3D.Core/Reporting/ProjectQuantityReportBuilder.cs` (reviewed; no edit required)
- `src/QS3D.Core/Reporting/MaterialUsageSchedule.cs` (reviewed; no edit required)
- `src/QS3D.Core/Reporting/CurtainWallSchedule.cs` (reviewed; no edit required)
- `src/QS3D.Core/Reporting/DoorOpeningSchedule.cs` (reviewed; no edit required)
- `src/QS3D.Core/Reporting/RoomFinishSchedule.cs` (reviewed; no edit required)
- `tests/QS3D.Core.SmokeTests/ScheduleReportingIdentitySmoke.cs`
- this claim file for close-out

## Intended contract

- expand the shared reporting identity guard to validate Elements, Floors, Zones and Families through one case-insensitive fail-closed boundary;
- reject null entries with collection name + zero-based index;
- reject blank IDs and duplicate/case-variant IDs with explicit collection identity context;
- all four schedule builders and project-backed BQ/ED2 invoke this guard before `ToDictionary`, Room Finish validation, selection resolution or quantity calculations;
- preserve all existing element identity, quantity, grouping and provenance behavior.

## Explicit exclusions

- No mutation or setter changes to `ProjectState`, `FloorDefinition`, `ZoneDefinition`, `ProjectFamily` or `ProjectElement`.
- No schedule formula/grouping/row changes, XLSX/UI/quantity-settings/persistence/core-mutation/geometry/rebar/updater/release changes.
- No `SmokeTestRegistration.cs` edit; `ScheduleReportingIdentitySmoke` is already registered.
- No GitHub Actions/build/release dispatch and no native BricsCAD V25/WPF runtime PASS claim.

## Validation plan

- re-fetch all reserved files after this claim is merged;
- extend the existing schedule identity smoke with null and duplicate/case-variant Floor/Zone/Family fixtures;
- prove schedules plus project-backed Group/Detail reject malformed references before lookup construction;
- preserve existing null/duplicate Element and schedule provenance cases;
- compare newest `main` before integration and structurally rebase only reviewed blobs if concurrent changes remain disjoint.

## Coordination

The preceding project-reporting null-element claim is completed. Current Core mutation, Quantity Settings, updater, UI, rebar and release lanes reserve disjoint surfaces or explicitly exclude reporting.

## Completion

- Claim-only PR: `#486`, squash merge on `main`: `965f2b7ca620a19cb04027b7b9724c8a40f45797`.
- Implementation PR: `#488` — `fix(reporting): validate project reference identities`.
- Reviewed implementation head before squash: `a0b1a72e39889c18598c8bca09080776cd6aef32`.
- Squash merge on `main`: `ae296592e4eb6cd6211ec5787d7f4bd3dbcb698c`.
- Kept the existing shared `RequireUniqueElementIds(...)` entrypoint to minimize call-site churn; its implementation now validates Elements, Floors, Zones and Families with one generic case-insensitive identity helper.
- Null entries fail closed with collection + zero-based index; blank and duplicate/case-variant IDs fail closed with collection identity context.
- No schedule/BQ call-site edit was needed because all six reporting entry points already invoke the shared guard before lookup/calculation.
- The already-registered `ScheduleReportingIdentitySmoke` now covers null and duplicate/case-variant Floor/Zone/Family collections across Material Usage, Curtain Wall, Door/Opening, Room Finish and project-backed Group/Detail, while preserving previous element/provenance regressions.
- Final implementation PR diff: 2 files / 66 additions / 17 deletions.
- Concurrent-main comparison before integration showed no overlap with the two implementation files.
- GitHub Actions/build/release were not dispatched.
- No native BricsCAD V25/WPF runtime PASS is claimed.

## Completion condition

Satisfied by PR `#488` and merge `ae296592e4eb6cd6211ec5787d7f4bd3dbcb698c`: Core reporting now has one deterministic project identity boundary across Elements/Floors/Zones/Families with focused regression coverage and no concurrent-work overwrite.
