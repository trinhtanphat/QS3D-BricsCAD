# Work claim — project reporting reference identity integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-project-reporting-reference-identities`
- Registered: `2026-08-11T21:51:00+07:00`
- Baseline: newest `main` at claim branch creation
- Priority: fail closed on malformed Floor/Zone/Family identity collections before Core reporting constructs case-insensitive lookup dictionaries.

## Confirmed defect

`ProjectState.Floors`, `Zones` and `Families` are exposed mutable `IList` collections. Project-backed BQ and Core schedules currently build `ToDictionary(..., StringComparer.OrdinalIgnoreCase)` maps directly. A null collection entry causes incidental `NullReferenceException`; duplicate case-insensitive IDs cause generic duplicate-key exceptions. This is less deterministic than the shared element identity boundary and can surface after unrelated validation rather than as an explicit reporting-project integrity error.

## Reserved scope

- `src/QS3D.Core/Reporting/ReportingProjectIdentityGuard.cs`
- `src/QS3D.Core/Reporting/ProjectQuantityReportBuilder.cs`
- `src/QS3D.Core/Reporting/MaterialUsageSchedule.cs`
- `src/QS3D.Core/Reporting/CurtainWallSchedule.cs`
- `src/QS3D.Core/Reporting/DoorOpeningSchedule.cs`
- `src/QS3D.Core/Reporting/RoomFinishSchedule.cs`
- `tests/QS3D.Core.SmokeTests/ScheduleReportingIdentitySmoke.cs`
- this claim file for close-out

## Intended contract

- rename/expand the shared reporting identity guard to validate Elements, Floors, Zones and Families through one case-insensitive fail-closed boundary;
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

## Completion condition

Core reporting has one shared deterministic project identity boundary across Elements/Floors/Zones/Families, focused regression coverage is merged onto current `main` without overwriting concurrent work, and this claim is closed with exact SHAs and truthful validation scope.
