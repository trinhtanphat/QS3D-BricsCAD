# Work claim — Reporting reference existence integrity

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T09:59:00+07:00`
- Baseline main SHA: `bcc3d13fca83ee747cec362945883bc6686b3a08`
- Priority: P1 — reporting must fail closed on dangling semantic references instead of emitting fallback labels from invalid project state.

## Confirmed defect

The shared `ReportingProjectIdentityGuard` validates null/blank/duplicate/canonical primary IDs and canonical spelling of Element `FamilyId`/`FloorId`/`ZoneId`, but does not require a nonblank reference to resolve to a project-owned definition. Report builders such as `ProjectQuantityReportBuilder` and `MaterialUsageScheduleBuilder` then use `TryGetValue(...)` fallbacks: a missing Floor/Zone is surfaced as the raw id and a missing Family is treated as absent inheritance / raw family label. A malformed persisted project can therefore emit apparently valid reporting output instead of failing closed.

Recent reporting primary/reference canonicality lanes and the nullability build-blocker are completed. They cover identity spelling/duplicates/null safety, not target existence.

## Reserved scope

- `src/QS3D.Core/Reporting/ReportingProjectIdentityGuard.cs` — shared reference-existence validation only.
- `tests/QS3D.Core.SmokeTests/ReportingReferenceExistenceSmoke.cs` — focused auto-registered Core smoke.
- this claim file for close-out.

## Intended contract

- Preserve existing primary-ID and relation-ID canonicality semantics.
- Blank/unassigned Family/Floor/Zone references remain valid.
- Every canonical nonblank `FloorId` must resolve to a project Floor.
- Every canonical nonblank `ZoneId` must resolve to a project Zone.
- Every canonical nonblank `FamilyId` must resolve to a project Family.
- Reject dangling references before any report rows, totals or provenance are produced.
- Do not change grouping keys, quantity math, report ordering, Room Finish identity rules, material inheritance, or source-handle provenance.
- Family/category compatibility is excluded from this lane; only reference existence is reserved.

## Validation plan

- Re-fetch moving `main` and exact guard blob before source write.
- Add existence checks through the shared guard so all existing report builders inherit the fail-closed boundary.
- Add focused smoke coverage for missing Family/Floor/Zone plus blank/existing controls.
- Read back source/test diffs and verify ancestry on current `main`.
- No GitHub Actions dispatch; no executable .NET/full build or BricsCAD V25/V26 runtime PASS claim without actual execution.
