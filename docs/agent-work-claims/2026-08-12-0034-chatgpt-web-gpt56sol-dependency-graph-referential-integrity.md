# Work claim — DependencyGraph full-graph referential integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-dependency-graph-referential-integrity`
- Registered: `2026-08-12T00:34:00+07:00`
- Baseline main SHA: `a188e13e996d9496d4dc9a1caed38cd5446fa8a6`
- Priority: concrete CAD-independent semantic graph defect found during owner-requested continue-all audit

## Confirmed defect

`DependencyGraph.Rebuild(IEnumerable<ProjectElement>)` is the full-project graph construction path. It now rejects blank, padded and duplicate `DependsOn` entries, but it still accepts a canonical dependency ID that does not resolve to any element in the supplied full graph. The missing ID is indexed as a source-only key. This conflicts with `ModelHealthService`, which classifies a missing semantic dependency as `MISSING_DEPENDENCY` with Error severity, and with Build3D's upstream closure, which explicitly fails closed on a missing dependency.

A dangling full-graph edge can therefore survive graph rebuild and be treated differently by graph consumers even though the project relation is invalid.

## Reserved scope

After collecting all semantic element IDs into the staged rebuild state, require every canonical `DependsOn` entry to resolve to an element in that same staged full graph before committing `_dependents` / `_elementsById`.

Preserve `TopologicalDirtyOrder(...)` subset semantics: that method may receive a candidate subset and must continue ignoring canonical dependencies that are outside the candidate set. Do not change subset regeneration semantics.

## Expected surfaces

- `src/QS3D.Core/Services/DependencyGraph.cs` (`Rebuild` full-graph validation only)
- `tests/QS3D.Core.SmokeTests/DependencyGraphReferentialIntegritySmoke.cs`
- module-initializer registration in that new smoke file
- this claim file

## Excluded scope

- No `DependencyImpactPlanner`, `RegenerationEngine`, Build3D, HostLink, Health or persistence changes.
- No topological subset behavior changes.
- No relation auto-repair, implicit dependency removal or synthetic external nodes.
- No GitHub Actions dispatch or V25/native runtime work.

## Validation plan

- Full `Rebuild` rejects a canonical dependency that is absent from the supplied element set.
- Failure preserves the previously committed graph/index state.
- Forward references to an element appearing later in the input remain valid because existence validation runs after full ID collection.
- Canonical valid graph rebuild remains deterministic.
- `TopologicalDirtyOrder` on a subset still permits a dependency outside that subset and orders the supplied candidates normally.
- Existing blank/padded/duplicate relation validation remains unchanged.

## Coordination

The immediately preceding dependency-graph canonical-relations lane (`9de690b278db71664218d2ea9360d0d3a84993e6`) is completed and covered relation text/duplicates only. Its claim explicitly did not alter DependencyImpactPlanner or broader graph semantics. Recent commit search found no separate active claim for full-graph missing dependency referential integrity.

## Completion condition

Current `main` rejects dangling semantic dependencies when constructing a full DependencyGraph, keeps subset topological ordering semantics unchanged, preserves previous graph state on invalid rebuild, includes focused deterministic smoke coverage, and this claim is closed `COMPLETED`.
