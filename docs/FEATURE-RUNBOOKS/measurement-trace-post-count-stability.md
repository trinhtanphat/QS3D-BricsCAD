# MeasurementTrace child post-traversal Count stability

Lane-Key: `issue-4414`

## Scope

This lane hardens the deterministic Core snapshots used by `MeasurementTrace` for input facts, adjustments, warnings and assumptions. Runtime classification is `NOT_APPLICABLE`; licensed BricsCAD/private-DWG execution is outside this source contract.

## Defect

Before this lane, child snapshot helpers sampled all supported deterministic Count surfaces before caller-controlled enumeration and enforced negative/conflicting/oversized admission, known-count overrun, under-yield and the independent 10,000-entry streaming cap. They did not re-read Count after an otherwise exact traversal. A counted enumerable could therefore advertise Count=N, yield exactly N valid entries, mutate Count metadata during enumeration and still publish a frozen measurement trace against stale cardinality evidence.

Historical #2223 established bounded child ingestion, #2728 established invalid/conflicting Count admission and #2839 bound the admitted Count to traversal length. This lane adds the missing post-traversal generation check; it does not alter `MeasurementSnapshot` or those historical contracts.

## Required contract

For facts, adjustments and messages:

1. bind `ICollection<T>`, `IReadOnlyCollection<T>` and non-generic `ICollection` Count evidence before traversal;
2. preserve negative/conflicting/>10,000 rejection before enumeration;
3. reject the first item beyond admitted Count before validating/retaining that unexpected item;
4. preserve under-yield rejection after traversal;
5. re-bind every supported Count surface after exact traversal and require the final canonical Count to equal the admitted Count before sorting, duplicate/conflict validation or publication;
6. route post-traversal negative/conflicting evidence through the same fail-closed Count validation;
7. preserve pure streaming inputs, canonical sorting, duplicate/conflicting evidence rules, unit/rule contracts, canonical serialization/equality/hash and numeric reconciliation.

## Deterministic regression

`MeasurementTracePostTraversalCountStabilitySmoke` covers fact, adjustment and message Count drift, post-traversal negative Count, stable counted sources and pure-streaming controls. Existing child-bound/count/traversal smokes continue to own pre-enumeration, overrun/no-overread, under-yield and 10,000-entry boundaries.

Run the auto-discovered guard with:

```text
python scripts/preflight-measurement-trace-post-count-stability.py
```

## Integration

Require automatic exact-head branch Shared CI (`preflight` + `core`). If protected `main` advances, reconcile non-force on the same carrier and require fresh branch evidence. Open one canonical PR carrying `Lane-Key: issue-4414`; require current protected PR `preflight` + `core`, mergeability and strict freshness, then merge with expected-head protection and verify exact `main` ancestry contains the final C02 head.
