# Work claim — Reporting reference canonicality fixture reconciliation

- Status: `COMPLETED`
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

## Completed implementation

- Claim PR: `#1285`; claim merge SHA: `308cb78f419c65b43c957e565187fc6404117a94`.
- Test commit: `668d7de830508107377bf008918db7ed0c596c83`.
- Test PR: `#1286`; implementation merge SHA: `73ec129c2df0094b9e83b77b4336eee8833ec4e7`.
- Public relation setters are asserted to canonicalize padded Family/Floor/Zone IDs before reporting.
- Test-local reflection injects exact raw padded backing-field values so `ReportingProjectIdentityGuard` remains covered as a fail-closed legacy/corrupt-state consumer.
- The public whitespace setter is asserted to normalize to unbound, then raw whitespace is injected and retained as an allowed blank/unbound reporting input.
- Production domain/reporting code, static gates, native/LOCAL surfaces and workflows were unchanged.

## Executed validation

- `dotnet build src/QS3D.Core/QS3D.Core.csproj -c Release`: PASS, 0 warnings / 0 errors.
- `dotnet build tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release`: PASS, 0 warnings / 0 errors.
- Full deterministic Core smoke progressed past `ReportingReferenceIdCanonicalitySmoke` and stopped at the next unrelated blocker: `RevisionCaptureXmlTextIntegritySmoke.InvalidProjectPayloadFailsAtCaptureBoundary`, where the current relation setter rejects a control character before the stale capture-boundary expectation is reached.
- Focused preflights PASS: `preflight-door-opening-schedule.py`, `preflight-door-opening-schedule-freshness.py`, `preflight-schedule-export-reporting-safety.py`, `preflight-door-schedule-project-safety.py`, `preflight-schedule-arithmetic.py`, and `preflight-schedule-hub.py`.
- Final implementation was read back from merged `main` at `73ec129c2df0094b9e83b77b4336eee8833ec4e7`.

No GitHub Actions or BricsCAD runtime were invoked.
