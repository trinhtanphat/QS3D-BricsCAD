# Work claim — reporting element-reference normalization

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-reporting-reference-normalization`
- Registered: `2026-08-11T22:00:00+07:00`
- Baseline: newest `main` at claim branch creation
- Priority: normalize mutable `ProjectElement` Family/Floor/Zone reference strings at read-only reporting boundaries so whitespace/case-preserving setter writes cannot split groups or miss existing lookup identities.

## Confirmed defect

`ProjectElement` trims `FamilyId`, `FloorId` and `ZoneId` only in its constructor, but the three properties have public setters with no normalization. A later edit/import can assign values such as `" floor "` or `" family "`. Core reporting currently uses several of these raw strings directly in `TryGetValue(...)` lookups and grouping keys. The same semantic reference can therefore fail to resolve its Floor/Family/Zone display name and/or create a separate grouping key even though `ProjectState.Find*` identity semantics trim lookups and compare case-insensitively.

## Reserved scope

- `src/QS3D.Core/Reporting/ReportingProjectIdentityGuard.cs` — add read-only reference normalization helper only
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

## Completion condition

Core reporting consistently normalizes mutable semantic reference strings before lookup/grouping, focused regression coverage is merged without overwriting concurrent work, and this claim is closed with exact SHAs and truthful validation scope.
