# Semantic schedule collection Current no-overread

Issue: #4486  
Lane-Key: `issue-4486`  
Runtime: `NOT_APPLICABLE` — deterministic Core documentation/data-integrity contract.

## Defect boundary

`SemanticScheduleDefinition` snapshots caller-provided category/id/column enumerables and may bind a deterministic `Count` from supported collection surfaces. The admitted Count is part of the semantic boundary: after `MoveNext()` reports an N+1 item, the implementation must reject before observing caller-controlled `Current` for that item.

`SemanticScheduleCatalog.Save` independently caps persisted schedules at 128 definitions. The 129th item may be detected with `MoveNext`, but its `Current` value must not be read before the capacity rejection.

## Required traversal order

For a counted definition snapshot:

1. bind/validate supported Count evidence;
2. `MoveNext()`;
3. reject capacity overflow;
4. reject known-Count overflow;
5. read `Current` and retain the admitted item;
6. after normal termination, require exact cardinality and re-read supported Count evidence before publishing the snapshot.

For catalog Save:

1. `MoveNext()`;
2. reject when 128 definitions are already retained;
3. only then read `Current` and retain the admitted definition.

The hard-cap and cardinality diagnostics remain stable. Count drift/conflict after exact traversal fails closed before a snapshot can escape.

## Deterministic regression evidence

`SemanticScheduleCollectionNoOverreadSmoke` independently records `MoveNext` and `Current` access. It covers:

- Count=1 / yield=2: `MoveNext=2`, `Current=1`;
- exact traversal followed by Count drift: terminal traversal occurs, then post-traversal Count rebind fails closed;
- catalog yield=129: `MoveNext=129`, `Current=128`, with no project metadata/version mutation;
- stable counted input: exactly one `Current` read per admitted value and original order retained.

`scripts/preflight-semantic-schedule-collection-no-overread.py` is auto-discovered by aggregate feature guards and prevents source-shape regression back to a `foreach` catalog boundary or a known-Count check after `Current`.

## Acceptance

Remote-safe acceptance requires the feature source guard, deterministic Core smoke, Core build, trusted V25 compile-reference validation, V25 plugin compile and final build to pass on the exact reconciled PR head. No licensed BricsCAD/private-DWG `LOCAL_PASS` is required or claimed for this Core-only contract.
