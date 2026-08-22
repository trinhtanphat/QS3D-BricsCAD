# Work claim — DependencyGraph self-dependency integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-dependency-graph-self-dependency`
- Registered: `2026-08-12T11:41:00+07:00`
- Completed: `2026-08-12T11:46:00+07:00`
- Baseline main SHA: `6909c4f9c15286d36666bdb407b04b1adaddfcee`
- Claim merge SHA: `3d33cca305e52f75fadb2de215296c6e73044a50`
- Implementation SHA: `7d3cc0fb128ae47e0c3d09ced4804e241213e1c4`
- Implementation PR: `#832`
- Superseded unmerged PR: `#829`
- Priority: P1 — reject self-dependency before graph consumers can treat an invalid semantic relation as an empty/no-external-dependent graph.

## Confirmed defect

`DependencyHealthService` already classifies an element that depends on itself as a blocking `DEPENDENCY_SELF` Error. `DependencyGraph.Rebuild()` nevertheless accepted the same self-edge because `ValidateDependencies()` rejected blank, padded, and duplicate dependency IDs but never compared a dependency with the owning element ID. `GetDependentsTransitive(sourceId)` seeds its visited set with `sourceId`, so a self-edge was then hidden from the returned dependent set.

This was reachable from production mutation code: `SemanticUntrackService.EnsureNoExternalDependents()` rebuilds the graph and uses `GetDependentsTransitive()` before removing targets. A self-dependent target could therefore pass dependency planning and be untracked instead of failing closed on the malformed relation.

## Implemented contract

- `ValidateDependencies()` now rejects a dependency whose ID equals the owning element ID, case-insensitively.
- The shared validation makes both `Rebuild()` and `TopologicalDirtyOrder()` fail closed on direct self-dependency.
- Blank/padded/duplicate/missing dependency validation, rebuild input-freshness checks, valid acyclic ordering, transitive traversal semantics, and graph rebuild atomicity remain unchanged.
- `DependencyGraphSelfDependencySmoke` covers case-insensitive self-edge rejection in `Rebuild()`, self-edge rejection in dirty ordering, and a normal `ROOT -> CHILD` rebuild/direct/transitive path.

## Validation

- Claim-only PR `#827` squash-merged as `3d33cca305e52f75fadb2de215296c6e73044a50` before source changes.
- The first implementation PR `#829` was closed unmerged because its pre-squash claim ancestry polluted the three-dot diff; no source from that PR entered `main`.
- Clean implementation PR `#832` changed exactly two files and squash-merged to `main` as `7d3cc0fb128ae47e0c3d09ced4804e241213e1c4`.
- Commit readback confirms the source guard and focused smoke file are present in the merged commit.
- GitHub combined status returned no status checks for the implementation commit (`statuses=[]`). No GitHub Actions or BricsCAD runtime/build PASS is claimed.

## Reserved scope

- `src/QS3D.Core/Services/DependencyGraph.cs`, limited to self-dependency validation
- `tests/QS3D.Core.SmokeTests/DependencyGraphSelfDependencySmoke.cs`
- this claim file

## Excluded scope

- `DependencyHealthService` diagnostics or severity changes.
- General cycle-detection algorithm changes beyond the direct self-edge boundary.
- Semantic handle ownership, persistence, UI, BricsCAD runtime, exporter, or GitHub Actions changes.
