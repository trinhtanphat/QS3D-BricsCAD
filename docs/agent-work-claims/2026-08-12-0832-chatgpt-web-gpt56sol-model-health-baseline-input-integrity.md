# Work claim — Model Health baseline malformed issue integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-model-health-baseline-input-integrity-20260812-0832`
- Registered: `2026-08-12T08:32:00+07:00`
- Completed: `2026-08-12T08:36:00+07:00`
- Baseline main SHA: `764a6ee6078af5267e19be376ebe5d9acf936a76`
- Source commit on implementation branch: `937f15c4b4a7a42dc4cf25a5b502ce66b4349fa3`
- Smoke commit on implementation branch: `a075fbc79aa975b3c474fc9958153e75cf8949b8`
- Merged PR: `#654`
- Main squash SHA: `1244227eece503186c5a69c45bff087afdc9670c`
- Priority: health-baseline false-clean prevention during owner-requested `continue all`

## Confirmed defect

`ModelHealthBaselineService.Capture()` delegated to `Index()`, which silently skipped null diagnostic issues. `ModelHealthBaseline.Sort()` independently filtered null entries as well. Undefined `HealthSeverity` values were also accepted and stored even though `ErrorCount`, `WarningCount` and `InfoCount` only count defined severities. A malformed diagnostic stream could therefore lose evidence or produce misleading aggregate counts instead of failing closed. This conflicted with the current `HealthSummary` and diagnostic-summary boundaries, which reject null and undefined severity input.

## Reserved scope

- `src/QS3D.Core/Diagnostics/ModelHealthBaselineService.cs`
- `tests/QS3D.Core.SmokeTests/ModelHealthBaselineSmoke.cs`
- this claim file

## Completed contract

- baseline capture now rejects null diagnostic entries instead of dropping them;
- baseline capture now rejects undefined `HealthSeverity` values before deduplication/counting;
- internal baseline sorting no longer silently erases malformed entries and applies the same null/severity integrity checks;
- valid deduplication, stale-issue identity, deterministic ordering, cross-project refusal, diff semantics and semantic-capture read-only behavior remain unchanged;
- no CAD mutation, persistence schema, WPF/native BricsCAD, updater/release packaging or unrelated health-provider behavior changed.

## Validation evidence

- Claim commit was visible on `main` before implementation: `aae8efe9c8181899feb2a8cf6780852686b4671f`.
- Compared implementation base to moving `main` before PR; concurrent changes touched Audit/XLSX/claim files, not the reserved baseline source/test files.
- PR `#654` squash-merged as `1244227eece503186c5a69c45bff087afdc9670c`.
- Re-fetched merged `ModelHealthBaselineService.cs` from `main` and confirmed both `Sort()` and `Index()` reject null issues and undefined severities.
- Re-fetched merged `ModelHealthBaselineSmoke.cs` from `main` and confirmed focused null + `(HealthSeverity)999` capture regression coverage is present alongside existing duplicate/stale/diff/read-only coverage.
- GitHub Actions were not manually dispatched.
- Smoke source was not executed from this web session, and no BricsCAD V25/V26 runtime PASS is claimed.
