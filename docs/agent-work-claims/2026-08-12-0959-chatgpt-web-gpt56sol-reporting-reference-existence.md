# Work claim — Reporting reference existence integrity

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T09:59:00+07:00`
- Completed: `2026-08-12T10:01:00+07:00`
- Baseline main SHA: `bcc3d13fca83ee747cec362945883bc6686b3a08`
- Claim commit: `41349897d33edfbcdb374fe752d36e6fbb5909f5`
- Source fix: `d295ee32f8bdb56df3fd9a0db88b24640a5b6a81`
- Regression smoke: `ae11ec1b0224a884c4fd7e59e87e33de7b7ea377`
- Priority: P1 — reporting must fail closed on dangling semantic references instead of emitting fallback labels from invalid project state.

## Confirmed defect

The shared `ReportingProjectIdentityGuard` validated null/blank/duplicate/canonical primary IDs and canonical spelling of Element `FamilyId`/`FloorId`/`ZoneId`, but did not require a nonblank reference to resolve to a project-owned definition. Report builders such as `ProjectQuantityReportBuilder` and `MaterialUsageScheduleBuilder` then used `TryGetValue(...)` fallbacks: a missing Floor/Zone could be surfaced as the raw id and a missing Family could be treated as absent inheritance / raw family label. A malformed persisted project could therefore emit apparently valid reporting output instead of failing closed.

Recent reporting primary/reference canonicality lanes and the nullability build-blocker were completed first. They cover identity spelling/duplicates/null safety, not target existence.

## Implemented contract

- Existing primary-ID and relation-ID canonicality semantics are preserved.
- Blank/unassigned Family/Floor/Zone references remain valid.
- Every canonical nonblank `FloorId` must resolve to a project Floor.
- Every canonical nonblank `ZoneId` must resolve to a project Zone.
- Every canonical nonblank `FamilyId` must resolve to a project Family.
- Existence validation runs through the shared reporting identity boundary before report rows, totals or provenance are produced.
- Existing case-insensitive reference lookup behavior is preserved.
- Grouping keys, quantity math, report ordering, Room Finish identity rules, material inheritance and source-handle provenance are unchanged.
- Family/category compatibility remains excluded from this lane.

## Regression coverage

`ReportingReferenceExistenceSmoke` is auto-registered with a module initializer and covers:

- dangling Family reference rejected by Material Usage and Quantity Group/Detail builders;
- dangling Floor reference rejected by the same shared reporting paths;
- dangling Zone reference rejected by the same shared reporting paths;
- blank/unassigned references remain valid;
- existing Family/Floor/Zone references remain valid under existing `OrdinalIgnoreCase` identity lookup semantics.

## Validation

- Exact source diff readback confirmed the only source change is shared reference-existence validation in `ReportingProjectIdentityGuard.cs`.
- Exact regression commit readback confirmed focused dangling/blank/existing reference coverage.
- Compared source fix `d295ee32f8bdb56df3fd9a0db88b24640a5b6a81` to observed current `main` `578505f2d869d4996b535b8a0f9ff0c07f5657d8`: `ahead_by=6`, `behind_by=0`, with the source fix as merge base; no concurrent commit in that range modified `src/QS3D.Core/Reporting/ReportingProjectIdentityGuard.cs`.
- No GitHub Actions were dispatched. The smoke source was committed/read back but not executed from this connector-only session. No executable .NET/full build PASS and no licensed BricsCAD V25/V26 runtime PASS are claimed.

## Completion

`COMPLETED`: shared reporting now fails closed on dangling canonical Family/Floor/Zone references instead of converting malformed project state into fallback report labels or missing inheritance.
