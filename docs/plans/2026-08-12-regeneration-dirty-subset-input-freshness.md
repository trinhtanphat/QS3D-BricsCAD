# Plan — Regeneration dirty-subset input freshness

Date: 2026-08-12
Agent: `chatgpt-web-gpt56sol-regeneration-dirty-subset-input-freshness`
Claim: `docs/agent-work-claims/2026-08-12-0758-chatgpt-web-gpt56sol-regeneration-dirty-subset-input-freshness.md`

## Problem

`RegenerationEngine.RegenerateDirtySubset(ProjectState, IEnumerable<string>)` executes caller-controlled lazy enumeration inside `CanonicalTargetIds(...)`. The current implementation reads the project element count before that enumeration, then continues resolving and regenerating against `project.Elements` without proving that the project version stayed unchanged while the enumerable ran.

A lazy enumerable can therefore mutate the same `ProjectState` and call `Touch()` while yielding IDs. The method then proceeds using input derived across two project versions. Even an enumerable that yields no IDs can currently mutate the project and be accepted as a normal zero-target no-op.

## Contract

1. Capture `ProjectState.ChangeVersion` immediately before materializing caller-controlled target IDs.
2. Keep the existing single materialization/normalization path and existing target-count bound.
3. Immediately after materialization, compare the canonical project version with the captured version.
4. If it changed, throw `InvalidOperationException` before the empty-set early return, project-element scan, snapshot capture, regenerator invocation, dirty clearing, generated-artifact updates, or regeneration `Touch()`.
5. Preserve the existing zero-target no-op when enumeration is side-effect free.
6. Preserve current canonical-ID, duplicate-ID, missing-target, project-order, transaction rollback and whole-project regeneration behavior.

## Implementation steps

1. Refresh `main`, claim state and `RegenerationEngine.cs`; abort or rebase content-wise if another agent has taken or modified the reserved lane.
2. Add the minimal version capture/check around `CanonicalTargetIds(...)`.
3. Add deterministic Core smoke coverage with a lazy enumerable that mutates the same project during enumeration; assert fail-closed before regeneration-side mutations. Include a side-effect-free path to prevent accidental loss of valid subset behavior.
4. Add/update a focused auto-discovered Python preflight under `scripts/` to pin the freshness guard ordering and regression registration, following current repository conventions.
5. Re-fetch every touched blob before each write; on a 409, merge the concurrent content instead of overwriting it.
6. Mark the claim `COMPLETED` only after source, regression and preflight commits are present on `main`, recording exact SHAs and validation limits.

## Validation boundaries

Remote-safe validation is limited to source/test/preflight inspection and any GitHub-hosted status that is actually observable. This plan does not claim a local .NET/BricsCAD build, GitHub Actions run, or BricsCAD V25 runtime PASS unless such evidence is explicitly obtained.