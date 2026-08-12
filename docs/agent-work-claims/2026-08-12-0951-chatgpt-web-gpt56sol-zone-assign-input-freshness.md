# Work claim — Zone assignment input freshness

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-zone-assign-input-freshness`
- Registered: `2026-08-12T09:51:00+07:00`
- Completed: `2026-08-12`
- Baseline main SHA: `edc009584887e74e8d54fb658f9a4f3482f715c2`
- Priority: P1 — fail-closed Core Zone mutation freshness at a caller-controlled enumeration boundary.

## Confirmed defect

`ProjectZoneService.Assign(ProjectState, string, IEnumerable<ProjectElement>)` resolved the project and then enumerated caller-controlled target elements without checking whether that enumeration changed the same `ProjectState`. A lazy target enumerable could call `project.Touch()` while yielding an otherwise-owned element; assignment then continued, called `project.Touch()` again, and mutated `ZoneId` against a newer project state than the target-enumeration baseline.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectZoneService.cs`
- focused Core smoke regression and registration under `tests/QS3D.Core.SmokeTests/`
- focused static preflight under `scripts/`
- `docs/plans/2026-08-12-zone-assign-input-freshness.md`
- this claim file

## Implemented contract

- Capture `project.ChangeVersion` immediately before enumerating `elements` in `Assign(...)`.
- Preserve existing null/ownership validation and duplicate-target collapse during enumeration.
- Immediately after target enumeration, fail closed with `InvalidOperationException` if the project version changed.
- Reject freshness drift before changed-target calculation, assignment no-op return, `project.Touch()`, or any `ZoneId`/dirty mutation.
- Preserve canonical no-op semantics and normal stable-input assignment behavior.

## Evidence

- Claim: `b06b456aff1603516757ba20713fbf6292f9641f`
- Plan: `7529121f311c2b2a092b911abd2aaae3bdf27a68`
- Source fix: `c8c880b8f43985d0b7155feea79bf8797ab10287`
- Deterministic smoke regression: `cbac1faf4f0580b92b9b68be65c9318e3fd0e389`
- Smoke registration: `98f124fe36dc5338fe18fcaacee6ea9bf5a6f563`
- Static preflight: `b10d5d55d1a9addeacddef1903240e3c28e643fe`

## Validation evidence

- Current `main` readback confirmed version capture → caller enumeration → freshness rejection → changed-target calculation ordering remains present.
- Deterministic smoke source covers stable lazy assignment, mutating lazy assignment, and mutating empty input; ModuleInitializer registration is committed.
- Static preflight is committed and locks the source ordering plus smoke/registration presence.
- This connector-only session did not execute the full Core smoke executable, the Python preflight, GitHub Actions, or licensed BricsCAD V25/V26 runtime; no PASS claim is made for those environments.

## Coordination

The completed Floor/Zone UI audit lane explicitly excluded `ProjectFloorService` / `ProjectZoneService` domain assignment semantics. `ProjectFloorService` remained excluded here to avoid overlap with concurrent vertical-level/floor work.

## Excluded scope

- `ProjectFloorService` and vertical level behavior.
- Zone UI audit wrappers and active-zone behavior.
- Zone create/update/delete semantics.
- GitHub Actions/build/release dispatch or licensed BricsCAD runtime qualification.

## Completion condition

`COMPLETED`: Zone assignment now fails closed when caller-controlled target enumeration changes the project, focused regression/preflight coverage is committed, exact integration SHAs are recorded, and remote validation limitations are explicit.
