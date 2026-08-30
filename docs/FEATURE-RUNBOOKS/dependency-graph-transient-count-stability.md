# DependencyGraph transient known-Count stability

## Scope

This runbook covers deterministic Core validation for the `DependencyGraph.Rebuild(...)` and `DependencyGraph.TopologicalDirtyOrder(...)` collection-admission contract. It does not require licensed BricsCAD runtime execution.

## Failure model

A supported counted enumerable can execute caller-controlled logic inside `MoveNext()`. If the graph only snapshots Count before enumeration and validates it after traversal, a hostile source can temporarily change Count during `MoveNext()`, expose an element, then restore Count from `Current` before final parity validation. The graph would then admit semantic input while its cardinality evidence was transiently invalid.

## Required traversal invariant

For both graph entry points:

1. Snapshot and validate all supported known Count surfaces before traversal.
2. Revalidate the exact admitted Count/source set immediately before each `MoveNext()`.
3. After every successful `MoveNext()`, revalidate Count again before capacity checks and before `Current`.
4. Revalidate Count on the terminating `MoveNext()` path.
5. Keep the existing known-count overrun check before `Current` and preserve the maximum input bound.
6. Preserve final exact observed-count parity and final Count/source stability.
7. Preserve dependency snapshots, duplicate/missing dependency rejection, rebuild-version protection and deterministic topological ordering.
8. Pure-streaming enumerables with no known Count must retain their existing behavior and remain bounded by `MaxElementInputCount`.

## Deterministic regression

`DependencyGraphTransientCountStabilitySmoke` uses counted enumerables that mutate Count during their first `MoveNext()` and restore Count only from `Current`. The hardened implementation must reject after that first `MoveNext()` with `CurrentReads == 0` for:

- transient Count growth;
- transient Count shrink;
- transient negative Count;
- transient disagreement between generic and read-only Count surfaces.

The smoke also validates a stable counted rebuild and a pure-streaming dirty-order input.

## Source guard

Run:

```text
python scripts/preflight-dependency-graph-transient-count-stability.py
```

The guard requires explicit `while (true)` traversal, forbids `while (enumerator.MoveNext())` for these entry points, checks Count rebound ordering before `Current`, and pins the hostile regression cases.

## Repository validation

Run the normal deterministic Core validation used by CI:

```text
dotnet build src/QS3D.Core/QS3D.Core.csproj -c Release
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

Then use the repository Shared CI and protected PR `preflight + core` gates on the exact current candidate. If protected `main` advances, collision-scan the reserved paths and ancestry-reconcile without force before relying on candidate evidence.

## Acceptance boundary

This work is `REMOTE_SAFE` / deterministic Core correctness. CI build/smoke evidence is sufficient for this source contract. Do not claim licensed BricsCAD or private-DWG `LOCAL_PASS` from this runbook.
