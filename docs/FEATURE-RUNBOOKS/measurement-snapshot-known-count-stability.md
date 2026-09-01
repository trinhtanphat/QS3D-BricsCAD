# Measurement Snapshot Known-Count Stability

## Scope

Lane-Key: `issue-5066`

This runbook covers the deterministic Core boundary in `MeasurementSnapshot` that materializes caller-supplied canonical `MeasurementTrace` instances. Runtime classification is `NOT_APPLICABLE`; no licensed BricsCAD or private-DWG execution is needed for this source contract.

## Defect history

The original lane (`issue-4395`) added supported Count admission, known-count overrun/under-yield handling, and a post-traversal Count rebound. That closed persistent/final cardinality drift, but current main still used implicit `foreach` traversal. A hostile counted enumerable could transiently change an admitted Count during caller-controlled `MoveNext` or `Current`, restore it before the existing final rebound, and have the returned trace accepted against stale cardinality evidence.

Lane `issue-5066` closes that callback window without changing the public snapshot model or pure-streaming support.

## Contract

For the outer trace collection:

1. Inspect `ICollection<MeasurementTrace>`, `IReadOnlyCollection<MeasurementTrace>`, and non-generic `ICollection` Count surfaces before acquiring traversal data.
2. Reject negative, conflicting, and counts above 10,000 before enumeration.
3. Traverse once with an explicit enumerator. Re-bind every admitted Count surface immediately before and after caller-controlled `MoveNext`.
4. When `MoveNext` yields an item, enforce admitted-count overrun and the independent 10,000 streaming ceiling before reading `Current`.
5. Re-bind every admitted Count surface immediately after `Current`, before null validation, measurement identity/duplicate acceptance, or retention.
6. Reject under-yield after traversal and perform a final Count rebound before canonical sorting/publication.
7. Preserve null rejection, exact duplicate measurement-identity rejection, ordinal canonical ordering, canonical serialization, and pure-streaming behavior.

Nested `MeasurementTrace` fact/adjustment/message collection contracts remain outside this lane.

## Deterministic regression

`MeasurementSnapshotKnownCountStabilitySmoke` covers:

- transient `MoveNext` Count drift, with zero unexpected `Current` reads;
- transient `Current` Count drift before trace retention;
- exact traversal followed by persistent Count drift;
- post-traversal negative Count;
- post-traversal multi-interface Count disagreement;
- stable multi-interface Count with exact callback-boundary read budgets;
- stable single-interface counted input with exact callback-boundary read budget;
- known-Count overrun precedence over invalid extra trace payload;
- known-Count under-yield;
- pure-streaming canonical ordering.

`scripts/preflight-measurement-snapshot-known-count-stability.py` is auto-discovered by aggregate feature guards and pins the production ordering `Count -> MoveNext -> Count -> overrun/ceiling -> Current -> Count -> payload acceptance -> retention`, plus final rebound before publication.

## Integration

Require automatic exact-head Shared CI `preflight` + `core`, reconcile current `main` non-force if it advances, then require fresh protected PR `preflight` + `core` on the current candidate. Merge only with expected-head protection when strict freshness and mergeability are satisfied, then verify exact protected `main` contains the final task head and release the reservation.
