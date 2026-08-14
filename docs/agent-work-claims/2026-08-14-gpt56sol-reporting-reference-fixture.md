# Work claim — Reporting reference canonicality fixture reconciliation

- Status: `ACTIVE`
- Agent: `gpt56sol-reporting-reference-fixture-20260814`
- Task: `/root/fix_source_reconcile_desync`
- Registered: `2026-08-14T16:27:30+07:00`
- Baseline main SHA: `22cca51cd40fbccf1e3b0ee72cddd9e6de0c9d88`
- Priority: next deterministic Core full-smoke blocker after the Quantity Rule raw-FamilyId fixture reconciliation

## Reserved scope

Reconcile `ReportingReferenceIdCanonicalitySmoke` with the current canonical `ProjectElement` relation setters. Preserve public-writer normalization while using test-local reflection to exercise the reporting guard's fail-closed handling of raw padded Family/Floor/Zone references and its successful raw-whitespace blank/unbound allowance.

## Expected surfaces

- `tests/QS3D.Core.SmokeTests/ReportingReferenceIdCanonicalitySmoke.cs`
- this claim file for close-out

## Excluded scope

- production `ProjectElement` or `ReportingProjectIdentityGuard` behavior;
- other Core smokes, static gates, documentation, native adapters, BricsCAD runtime, LOCAL runners/probes, private data, GitHub Actions and release work.

## Validation plan

- build `QS3D.Core` and the Core smoke-test project;
- run the complete deterministic Core smoke suite;
- run relevant reporting and Door/Opening focused preflights/gates already present in the repository;
- read back the final one-smoke diff and report the first unrelated full-smoke blocker without expanding this claim.

## Coordination

No current `ACTIVE` / `BLOCKED` claim or open pull request owns this smoke or reporting-reference canonicality contract. The active Preview Review CDATA, QSDB stale-backup and V25 preview-dispatch claims are disjoint.

## Completion condition

Merge the one-smoke fixture repair, record executed evidence and final merge SHA here, and mark this claim `COMPLETED` without changing production reporting behavior.
