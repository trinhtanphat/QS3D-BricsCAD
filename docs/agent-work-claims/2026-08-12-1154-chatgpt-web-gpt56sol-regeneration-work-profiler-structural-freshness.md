# Work claim — Regeneration work profiler structural freshness

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-regeneration-work-profiler-structural-freshness`
- Registered: `2026-08-12T11:54:00+07:00`
- Baseline main SHA: `d1abc79155f9c4459791bca4103e236fc96e95c2`
- Priority: P2 — reject caller-controlled subset enumeration that structurally changes project element ownership without advancing `ChangeVersion`.

## Confirmed defect

`RegenerationWorkProfiler.ProfileSubset(...)` captures `ProjectState.ChangeVersion` before enumerating caller-controlled `elementIds` and rechecks it afterward. Direct mutations of `project.Elements` do not necessarily advance `ChangeVersion`. A lazy target enumerable can remove or replace a project element (including same-ID replacement) while yielding target ids; the existing version check passes and the profiler continues using the mutated ownership set.

## Reserved scope

- `src/QS3D.Core/Services/RegenerationWorkProfiler.cs`, limited to structural ownership freshness around `ProfileSubset(...)` target enumeration
- focused Core smoke regression + ModuleInitializer registration under `tests/QS3D.Core.SmokeTests/`
- focused static preflight under `scripts/`
- `docs/plans/2026-08-12-regeneration-work-profiler-structural-freshness.md`
- this claim file

## Intended contract

- Snapshot unique project element ID -> instance ownership immediately before caller-controlled target enumeration.
- Keep the existing `ChangeVersion` freshness check.
- After enumeration and semantic-version validation, reject count/null/duplicate/remove/replace drift before empty-subset handling or candidate planning.
- Stable lazy subset profiling remains unchanged.
- No public API signature changes.

## Excluded scope

- `DependencyImpactPlanner` structural freshness, already completed separately.
- `RegenerationEngine` / `RegenerationPreviewService` target freshness.
- Regeneration execution, UI, GitHub Actions, or licensed BricsCAD runtime qualification.
