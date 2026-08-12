# Work claim — SelectionState replacement input freshness

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-selection-state-replace-input-freshness`
- Registered: `2026-08-12T10:26:00+07:00`
- Baseline main SHA: `7c43babfd7063b9d84dd0c097f72af4c8a2dd49f`
- Priority: P1 — fail-closed semantic selection replacement at a caller-controlled reentrant enumeration boundary.

## Confirmed defect

`SelectionState.Replace(IEnumerable<string>)` materializes caller-controlled lazy input before applying it. During enumeration, the producer can reentrantly call `Clear()` or `Replace()` on the same `SelectionState`. The outer `Replace()` does not detect that effective selection mutation and can overwrite the newer inner state using stale materialized input.

## Reserved scope

- `src/QS3D.Core/Services/SelectionState.cs`
- focused Core smoke regression and ModuleInitializer registration under `tests/QS3D.Core.SmokeTests/`
- focused static preflight under `scripts/`
- `docs/plans/2026-08-12-selection-state-replace-input-freshness.md`
- this claim file

## Intended contract

- Track effective selection mutations with a private monotonic revision.
- Capture the revision immediately before enumerating caller-supplied replacement IDs.
- Immediately after materialization, reject if the revision changed, before no-op comparison or replacement apply.
- Advance the revision only for effective `Replace()` / `Clear()` mutations while preserving current no-op and `Changed` event semantics.
- Preserve the 10,000 target cap, blank/null skipping, trimming, case-insensitive de-duplication, and stable lazy-input behavior.
- Keep revision overflow atomic by computing the next revision before mutating `_ids`.

## Excluded scope

- Full thread-safety or cross-thread synchronization guarantees.
- BricsCAD implied-selection bridge/UI behavior.
- Selection Inspector and unrelated selection diagnostics.
- GitHub Actions/build/release dispatch or licensed BricsCAD runtime qualification.
