# Work claim — Full regeneration dependency integrity

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T10:04:00+07:00`
- Baseline main SHA observed before claim: `17717bdb444d385a8954dbe09e16638bddf34e4b`
- Priority: evidence-driven Core full-project execution integrity

## Confirmed defect

`RegenerationEngine.MarkChanged(...)` rebuilds `DependencyGraph` before mutation, and `DependencyGraph.Rebuild(...)` rejects blank, non-canonical, duplicate and dangling semantic dependency references across the full project. `RegenerationEngine.RegenerateDirty(...)`, however, currently validates only null/duplicate project element identities before passing the full `project.Elements` collection to `TopologicalDirtyOrder(...)`.

`TopologicalDirtyOrder(...)` intentionally permits dependencies outside the dirty subset, so it cannot distinguish a clean in-project dependency from a dependency whose target is missing from the project. As a result, full-project `RegenerateDirty()` can silently regenerate a dirty dependent even when its semantic dependency target does not exist, diverging from the full-graph integrity contract already enforced by `MarkChanged` / `DependencyGraph.Rebuild`.

## Intended scope

- preflight the full dependency graph once at the `RegenerateDirty()` boundary before transactional regeneration;
- preserve intentional `TopologicalDirtyOrder` subset semantics and do not change `RegenerateDirtySubset(...)` behavior;
- preserve existing null/duplicate element validation, regeneration ordering, rollback, quantity formulas and project-touch semantics;
- add focused Core smoke coverage for dangling full-project dependency rejection plus a valid clean-dependency control.

## Reserved surfaces

- `src/QS3D.Core/Services/RegenerationEngine.cs`
- `tests/QS3D.Core.SmokeTests/RegenerationDirtyDependencyIntegritySmoke.cs`
- this claim file

## Excluded scope

Do not modify `DependencyGraph.TopologicalDirtyOrder`, dependency health/reporting, subset regeneration semantics, generated rebar, curtain geometry, ED2, Grid/Zone lanes, UI/CAD adapters, build/release workflows, or other concurrently claimed files.

## Validation boundary

Remote/static source + regression review only. Do not dispatch/rerun GitHub Actions and do not claim BricsCAD V25/V26 or local .NET runtime PASS without actual execution.
