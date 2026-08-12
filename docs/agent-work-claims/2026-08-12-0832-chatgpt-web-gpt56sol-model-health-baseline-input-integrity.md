# Work claim — Model Health baseline malformed issue integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-model-health-baseline-input-integrity-20260812-0832`
- Registered: `2026-08-12T08:32:00+07:00`
- Baseline main SHA: `764a6ee6078af5267e19be376ebe5d9acf936a76`
- Priority: health-baseline false-clean prevention during owner-requested `continue all`

## Confirmed defect

`ModelHealthBaselineService.Capture()` delegates to `Index()`, which silently skips null diagnostic issues. `ModelHealthBaseline.Sort()` independently filters null entries as well. Undefined `HealthSeverity` values are also accepted and stored even though `ErrorCount`, `WarningCount` and `InfoCount` only count defined severities. A malformed diagnostic stream can therefore lose evidence or produce misleading aggregate counts instead of failing closed. This conflicts with the current `HealthSummary` and diagnostic-summary boundaries, which reject null and undefined severity input.

## Reserved scope

- `src/QS3D.Core/Diagnostics/ModelHealthBaselineService.cs`
- `tests/QS3D.Core.SmokeTests/ModelHealthBaselineSmoke.cs`
- this claim file for close-out

## Contract

- baseline capture rejects null diagnostic entries instead of dropping them;
- baseline capture rejects undefined `HealthSeverity` values before deduplication/counting;
- internal baseline sorting does not silently erase malformed entries;
- valid deduplication, stale-issue identity, deterministic ordering, cross-project refusal, diff semantics and semantic-capture read-only behavior remain unchanged;
- no CAD mutation, persistence schema, WPF/native BricsCAD, updater/release packaging or unrelated health-provider behavior changes.

## Validation plan

Extend the existing deterministic `ModelHealthBaselineSmoke` with malformed null and undefined-severity inputs while preserving existing duplicate/stale/diff/read-only assertions.

No GitHub Actions dispatch and no BricsCAD V25/V26 runtime PASS claim from this web session.
