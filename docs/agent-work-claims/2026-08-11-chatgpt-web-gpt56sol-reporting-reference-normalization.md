# Work claim — reporting element-reference normalization

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-reporting-reference-normalization`
- Registered: `2026-08-11T22:00:00+07:00`
- Baseline: newest `main` at claim branch creation
- Priority: normalize mutable `ProjectElement` Family/Floor/Zone reference strings at read-only reporting boundaries so whitespace/case-preserving setter writes cannot split groups or miss existing lookup identities.

## Confirmed defect

`ProjectElement` trims `FamilyId`, `FloorId` and `ZoneId` only in its constructor, but the three properties have public setters with no normalization. A later edit/import can assign values such as `" floor "` or `" family "`. Core reporting used several of these raw strings directly in `TryGetValue(...)` lookups and grouping keys. The same semantic reference could therefore fail to resolve its Floor/Family/Zone display name and/or create a separate grouping key even though `ProjectState.Find*` identity semantics trim lookups and compare case-insensitively.

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

- normalize FamilyId/FloorId/ZoneId with null-safe trim at reporting read time, without mutating the `ProjectElement`;
- use normalized reference IDs consistently for dictionary lookup and grouping keys;
- preserve blank references as blank and preserve existing fallback/display behavior when an ID is genuinely unresolved;
- case-insensitive reference identity remains unchanged;
- all prior project identity, quantity, provenance, Room Finish and ordering rules remain intact.

## Explicit exclusions

- No `ProjectElement` setter/domain mutation change; Core mutation atomicity owns mutation hardening.
- No requirement that every nonblank reference must resolve; this lane only normalizes reporting reads and preserves existing unresolved-reference behavior.
- No schedule formula/quantity arithmetic/XLSX/UI/quantity-settings/persistence/geometry/rebar/updater/release changes.
- No `SmokeTestRegistration.cs` edit; `ScheduleReportingIdentitySmoke` is already registered.
- No GitHub Actions/build/release dispatch and no native BricsCAD V25/WPF runtime PASS claim.

## Validation plan

- re-fetch all reserved files after claim merge;
- mutate constructed elements after creation to padded Family/Floor/Zone IDs and prove project-backed BQ plus schedules resolve/group them exactly like canonical IDs;
- preserve existing malformed collection identity and provenance regressions;
- compare newest `main` before integration and structurally rebase only reviewed blobs if concurrent changes remain disjoint.

## Coordination

The previous project reporting reference-identity claim is completed. Current Core mutation, Quantity Settings, updater, UI, rebar, interchange and release lanes reserve disjoint surfaces or explicitly exclude reporting read normalization.

## Completion

- Claim-only PR: `#490`, squash merge on `main`: `b9718ba36505e465e64515c2586b86b8b1408e02`.
- Implementation PR: `#492` — `fix(reporting): normalize mutable reference ids`.
- Final reviewed implementation head: `77aaf9d1f6d9dfc4efa1a7a90e9f6d34c7fd5c4f`.
- Squash merge on `main`: `31765bef16cac17e73f029c6b17030c9e01e48cb`.
- Added `ReportingProjectIdentityGuard.NormalizeReferenceId(...)` as a read-only null-safe trim helper.
- Material Usage, Curtain Wall, Door/Opening and Room Finish schedules now use normalized Floor/Family references consistently for lookup, fallback display and grouping keys.
- Project-backed BQ/ED2 now normalizes Floor/Zone/Family references before lookup and grouping, while detail identity remains based on semantic ElementId.
- Regression coverage mutates `ProjectElement` references after construction and proves padded/case-varied references resolve and group like canonical project identities.
- Final implementation PR diff: 7 files / 73 additions / 20 deletions.
- Repeated concurrent-main comparisons showed no overlap with the seven implementation files before integration.
- GitHub Actions/build/release were not dispatched.
- No native BricsCAD V25/WPF runtime PASS is claimed.

## Completion condition

Satisfied by PR `#492` and merge `31765bef16cac17e73f029c6b17030c9e01e48cb`: Core reporting now consistently normalizes mutable semantic reference strings before lookup/grouping without mutating domain state or overwriting concurrent work.
