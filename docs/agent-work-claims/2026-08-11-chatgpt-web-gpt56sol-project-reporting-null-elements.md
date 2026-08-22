# Work claim — project reporting null-element identity integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-project-reporting-null-elements`
- Registered: `2026-08-11T21:40:00+07:00`
- Baseline: newest `main` at claim branch creation
- Priority: make project-backed BQ and Core schedules fail closed deterministically when `ProjectState.Elements` contains a null entry.

## Confirmed defect

`ProjectState.Elements` is an exposed mutable `IList<ProjectElement>`, so a malformed caller can insert `null`. `ReportingProjectIdentityGuard.RequireUniqueElementIds(...)` dereferenced `element.Id` without a null check, causing an incidental `NullReferenceException` before schedule context was reported. `ProjectQuantityReportBuilder.Build(...)` had a separate direct `element.Id` loop and could fail the same way for unscoped BQ/ED2. This was inconsistent with the explicit null-member integrity already enforced by legacy reporting.

## Reserved scope

- `src/QS3D.Core/Reporting/ReportingProjectIdentityGuard.cs`
- `src/QS3D.Core/Reporting/ProjectQuantityReportBuilder.cs`
- `tests/QS3D.Core.SmokeTests/ScheduleReportingIdentitySmoke.cs`
- this claim file for close-out

## Intended contract

- shared project reporting identity guard rejects the first null project element with `InvalidOperationException` and its zero-based index;
- project-backed BQ/ED2 invokes the shared guard before Room Finish validation, selection resolution, grouping or quantity calculation;
- remove duplicated blank/duplicate element-ID validation from the BQ loop once the shared guard owns that boundary;
- preserve existing case-insensitive duplicate/blank ID fail-closed behavior and all quantity/provenance/grouping semantics;
- all four schedule builders and project-backed Group/Detail reject null project elements deterministically.

## Explicit exclusions

- No `ProjectState` collection/domain mutation changes.
- No schedule grouping/formulas/rows, XLSX/UI/quantity-settings/persistence/core-mutation/geometry/rebar/release changes.
- No `SmokeTestRegistration.cs` edit; `ScheduleReportingIdentitySmoke` is already registered.
- No GitHub Actions/build/release dispatch and no native BricsCAD V25/WPF runtime PASS claim.

## Validation plan

- re-fetch all three reserved files after this claim is merged;
- add a null-element fixture proving all four schedule builders plus project-backed Group/Detail reject the malformed project;
- preserve existing exact/case-variant duplicate identity and provenance regressions;
- compare newest `main` before integration and structurally rebase only reviewed blobs if concurrent work is disjoint.

## Coordination

Previous reporting identity/provenance/null/non-negative/material claims are completed. The active Core mutation atomicity claim excludes reporting. Current Quantity Settings/UI/updater/rebar claims reserve disjoint surfaces.

## Completion

- Claim-only PR: `#481`, squash merge on `main`: `871d5f62537e2fd50477a59226afa64cab869c5c`.
- Implementation PR: `#484` — `fix(reporting): reject null project elements`.
- Reviewed implementation head before squash: `d3f763979d79029ea88304bb637661826af96b9f`.
- Squash merge on `main`: `ba0e79896a0edebe8be35290ff4422e02d43c3cd`.
- `ReportingProjectIdentityGuard` now rejects the first null project element with its zero-based index before dereferencing identity.
- `ProjectQuantityReportBuilder` now invokes the shared identity guard before Room Finish validation, selection resolution or quantity calculation and no longer carries a duplicate blank/duplicate ID scan in its loop.
- The already-registered `ScheduleReportingIdentitySmoke` now covers null project elements across Material Usage, Curtain Wall, Door/Opening, Room Finish and project-backed Group/Detail while preserving prior duplicate/provenance regressions.
- Final implementation PR diff: 3 files / 33 additions / 4 deletions.
- Concurrent-main comparison before integration showed no overlap with the three reserved files.
- GitHub Actions/build/release were not dispatched.
- No native BricsCAD V25/WPF runtime PASS is claimed.

## Completion condition

Satisfied by PR `#484` and merge `ba0e79896a0edebe8be35290ff4422e02d43c3cd`: project-backed BQ/ED2 and all guarded Core schedules now fail closed on null project elements through one shared reporting identity boundary with focused regression coverage.
