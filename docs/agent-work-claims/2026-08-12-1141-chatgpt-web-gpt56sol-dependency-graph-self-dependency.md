# Work claim — DependencyGraph self-dependency integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-dependency-graph-self-dependency`
- Registered: `2026-08-12T11:41:00+07:00`
- Baseline main SHA: `6909c4f9c15286d36666bdb407b04b1adaddfcee`
- Priority: P1 — reject self-dependency before graph consumers can treat an invalid semantic relation as an empty/no-external-dependent graph.

## Confirmed defect

`DependencyHealthService` already classifies an element that depends on itself as a blocking `DEPENDENCY_SELF` Error. `DependencyGraph.Rebuild()` nevertheless accepts the same self-edge because `ValidateDependencies()` currently rejects blank, padded, and duplicate dependency IDs but never compares a dependency with the owning element ID. `GetDependentsTransitive(sourceId)` seeds its visited set with `sourceId`, so a self-edge is then hidden from the returned dependent set.

This is reachable from production mutation code: `SemanticUntrackService.EnsureNoExternalDependents()` rebuilds the graph and uses `GetDependentsTransitive()` before removing targets. A self-dependent target can therefore pass dependency planning and be untracked instead of failing closed on the malformed relation.

## Reserved scope

- `src/QS3D.Core/Services/DependencyGraph.cs`, limited to self-dependency validation
- focused Core smoke regression under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Intended contract

- Reject a dependency whose canonical ID equals the owning element ID, case-insensitively, during graph validation.
- Apply the same fail-closed validation to both `Rebuild()` and `TopologicalDirtyOrder()` through their existing `ValidateDependencies()` call.
- Preserve blank/padded/duplicate/missing dependency validation, stable-input checks, valid acyclic ordering, transitive traversal semantics, and graph rebuild atomicity.
- Regression must prove a self-edge is rejected and that a normal dependency edge still rebuilds/traverses correctly.

## Excluded scope

- `DependencyHealthService` diagnostics or severity changes.
- General cycle-detection algorithm changes beyond the direct self-edge boundary.
- Semantic handle ownership, persistence, UI, BricsCAD runtime, exporter, or GitHub Actions changes.
