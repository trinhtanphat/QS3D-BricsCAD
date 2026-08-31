# Rebar shape path point Count stability

Lane-Key: `issue-4572`

## Scope

This runbook covers the public `RebarShapePath(string, IReadOnlyList<RebarShapePoint>)` caller-owned list boundary. Runtime qualification is **NOT_APPLICABLE**: this is deterministic Core snapshot/value integrity and requires no licensed BricsCAD or private DWG.

## Defect

`IReadOnlyList<T>` is not immutable. The historical constructor passed the caller-owned list directly to `new List<RebarShapePoint>(points)`, which delegates traversal to the source enumerable. A hostile implementation could advertise one `Count` while its enumerator yielded extra or fewer points, throw from a tail beyond the declared cardinality, or change cardinality during construction. The constructor therefore did not bind its snapshot to the public list's admitted Count/index contract before publishing `Points`.

## Production contract

1. Read `points.Count` once as the admitted point Count.
2. Reject admitted Count below two before any caller-controlled indexer access.
3. Allocate the detached snapshot from the admitted Count, but never use caller enumeration to populate it.
4. Traverse exactly `0..admittedPointCount-1` through `points[index]`.
5. Immediately before every indexer read, require current Count to equal the admitted Count.
6. Revalidate Count after the admitted traversal and before publishing `Points`.
7. Preserve existing shape-code normalization, finite point semantics and detached read-only `Points` behavior.

## Deterministic regression

`RebarShapePathCountStabilitySmoke` uses hostile `IReadOnlyList<RebarShapePoint>` implementations and requires:

- growth before a later admitted index: reject before an unexpected/new-generation indexer read;
- shrink before a later admitted index: reject before a missing/new-generation read;
- Count drift visible only at final rebound: reject after exactly the admitted reads;
- fewer than two admitted points: reject before any indexer read;
- stable caller-owned points: read every admitted index exactly once, never request caller enumeration, normalize the shape code and publish the expected detached coordinates.

The auto-discovered `preflight-rebar-shape-path-count-stability.py` pins admission, fixed-index traversal, pre-index Count checks, final rebound and smoke registration, and rejects regression to `new List<RebarShapePoint>(points)`.

## Landing

Require the focused guard, aggregate discovered feature guards, Core build/smoke, trusted V25 compile-reference validation and protected exact-head `preflight + core`. If protected main advances, collision-scan all four reserved paths, reconcile non-force and obtain fresh exact-head checks. Merge only with expected-head protection and verify the exact task head in protected-main ancestry.
