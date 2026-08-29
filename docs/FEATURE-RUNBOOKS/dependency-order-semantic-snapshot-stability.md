# Dependency ordering semantic snapshot stability

## Scope

This runbook covers the Core-only `DependencyGraph.TopologicalDirtyOrder(...)` caller-input boundary. It does not alter native BricsCAD behavior, quantity rules, persistence, or licensed runtime acceptance.

## Integrity contract

An admitted `ProjectElement` must be interpreted from semantic state captured when its `IEnumerator.Current` is accepted, not from mutable state that a later caller-controlled `MoveNext()` can change.

For each admitted element the ordering boundary captures its reference, semantic ID, `Dirty` flags, and canonical dependency sequence immediately after `Current`. Dependency validation therefore occurs at admission. Topological selection and DFS use those immutable snapshots rather than later live `Dirty` or `DependsOn` values.

After caller traversal/count validation and again before returning the ordering result, the live element must still match the captured snapshot. Mutation of dirty state or dependency membership/order fails closed with a retryable dependency-order input stability error.

Existing known-Count admission, 10,000-element bound, null/duplicate-id diagnostics, cycle detection, clean-element behavior, and streaming support remain authoritative.

## Deterministic regression

`DependencyOrderSemanticSnapshotStabilitySmoke` uses a hostile counted enumerable whose second `MoveNext()` mutates the first already-yielded element. Separate cases mutate its dirty state and dependency list. Both must fail closed instead of silently changing which elements are ordered or changing dependency topology. A stable dependency pair confirms normal topological order remains unchanged.

`scripts/preflight-dependency-order-semantic-snapshot-stability.py` is auto-discovered by aggregate feature guards and pins snapshot-based ordering, including the prohibition on live `frame.Element.DependsOn` and post-enumeration live `Dirty` reads.

## Validation boundary

Required remote validation is the discovered feature preflight, Core Release build/deterministic smoke, and applicable shared/protected CI. Licensed BricsCAD V25/V26 runtime is **NOT_APPLICABLE**; hosted validation must not be reported as `LOCAL_PASS`.
