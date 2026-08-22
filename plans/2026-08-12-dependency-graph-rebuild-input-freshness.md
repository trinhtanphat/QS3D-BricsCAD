# DependencyGraph rebuild input freshness

## Problem

`DependencyGraph.Rebuild(IEnumerable<ProjectElement>)` executes caller-controlled lazy enumeration while building local graph dictionaries. The enumerable can reentrantly call `Rebuild()` on the same graph. The inner rebuild can install a newer graph, but the outer rebuild currently resumes and overwrites it with stale materialized state.

## Invariant

- Maintain a private monotonic rebuild revision.
- Capture the revision immediately before enumerating caller-supplied elements.
- Preserve all existing element/dependency validation and missing-dependency checks.
- Before applying local graph dictionaries, reject if the revision changed.
- Prepare the checked next revision before clearing either current graph dictionary.
- Apply the revision only after both `_dependents` and `_elementsById` have been replaced.
- Every successful rebuild advances the revision, including content-equivalent rebuilds, so a successful inner rebuild always invalidates an outer reentrant rebuild.

## Regression

Add deterministic Core smoke coverage for:

1. stable lazy rebuild still exposes direct/transitive dependency lookup;
2. an inner successful rebuild during outer enumeration causes the outer rebuild to fail and preserves the inner graph;
3. an inner successful rebuild followed by an empty outer enumeration still fails before an empty graph can overwrite the inner graph.

Register with `ModuleInitializer` and add a static preflight that locks version capture/enumeration/freshness/apply ordering, checked revision preparation, smoke cases, and registration.

## Exclusions

No thread-safety guarantee, `ProjectState.ChangeVersion` integration, dependency-validation changes, topological-order algorithm changes, persistence changes, or BricsCAD runtime behavior changes.
