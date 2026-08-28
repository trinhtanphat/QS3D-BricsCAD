# Measurement Snapshot Known-Count Stability

## Scope

Lane-Key: `issue-4395`

This runbook covers the deterministic Core boundary in `MeasurementSnapshot` that materializes caller-supplied canonical `MeasurementTrace` instances. Runtime classification is `NOT_APPLICABLE`; no licensed BricsCAD or private-DWG execution is needed for this source contract.

## Defect

Before this lane, the snapshot inspected supported deterministic Count surfaces before traversal and enforced initial negative/conflicting/oversized evidence plus known-Count overrun and under-yield. It did not re-read those same Count surfaces after caller-controlled enumeration. A source could therefore advertise Count=N, yield exactly N valid traces, change its Count while enumerating, and still publish an immutable canonical snapshot against stale cardinality evidence.

## Contract

For the outer trace collection:

1. Inspect `ICollection<MeasurementTrace>`, `IReadOnlyCollection<MeasurementTrace>`, and non-generic `ICollection` Count surfaces before acquiring traversal data.
2. Reject negative, conflicting, and counts above 10,000 before enumeration.
3. During enumeration, reject the first item beyond an admitted known Count before null/identity validation or materialization of that extra trace; pure streaming sources continue to use the independent 10,000 cap.
4. Reject under-yield after traversal.
5. Re-bind every supported deterministic Count surface after exact traversal and require the final canonical Count to equal the admitted Count before sorting or snapshot publication. Post-traversal negative/conflicting evidence fails through the same canonical validation path.
6. Preserve null rejection, exact duplicate measurement-identity rejection, ordinal canonical ordering, canonical serialization and pure-streaming behavior.

Nested `MeasurementTrace` fact/adjustment/message collection contracts are explicitly outside this lane.

## Deterministic regression

`MeasurementSnapshotKnownCountStabilitySmoke` must cover:

- exact traversal followed by Count drift;
- post-traversal negative Count;
- post-traversal multi-interface Count disagreement;
- stable multi-interface Count with two-phase reads;
- stable single-interface counted input;
- known-Count overrun precedence over invalid extra trace payload;
- known-Count under-yield;
- pure-streaming canonical ordering.

`scripts/preflight-measurement-snapshot-known-count-stability.py` is auto-discovered by aggregate feature guards and pins the production/test ordering contract.

## Integration

Require automatic exact-head Shared CI `preflight` + `core`, reconcile current `main` non-force if it advances, then require fresh protected PR `preflight` + `core` on the current candidate. Merge only with expected-head protection when strict freshness and mergeability are satisfied, then verify exact protected `main` ancestry contains the final task head.
