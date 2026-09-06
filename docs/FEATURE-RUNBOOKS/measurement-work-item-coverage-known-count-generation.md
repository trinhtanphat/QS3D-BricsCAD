# Coverage report known-Count generation affinity

## Scope

`MeasurementWorkItemCoverageReport.Create` accepts both ordinary counted collections and pure `IEnumerable<MeasurementWorkItemCoverageFinding>` streams. Counted inputs expose a stronger contract than streams: once a supported `Count` has been admitted, the report must not combine findings from a traversal whose advertised cardinality changes while an item is being read.

## Defect boundary

The historical coverage-report contract already rejects oversized, negative or conflicting supported Count values and rejects a final Count/traversal mismatch. That final comparison alone cannot prove generation stability. A mutable or hostile collection can change Count inside `MoveNext` or `Current`, return an item from that unstable generation, and later restore the original Count while still yielding the originally advertised number of items.

Such a report would be deterministic in row ordering but not in source-generation identity.

## Contract

For an input that exposes a supported known Count:

1. validate and admit the initial Count using the existing capacity/negative/conflict contract;
2. call `MoveNext`, then immediately re-read the supported Count before acting on the enumerator result;
3. after a successful move, enforce the 10,000-finding streaming capacity boundary before reading `Current`;
4. read `Current`, then immediately re-read Count before null validation or row materialization;
5. after terminal traversal/disposal, re-read Count again before the existing final Count/traversal equality check.

Any rebound that differs from the admitted Count fails closed. Existing oversized, negative and conflicting rebound evidence continues to use the canonical Count validation errors. Stable counted input remains supported, and pure streaming input does not acquire a synthetic Count requirement.

## Deterministic regression matrix

`MeasurementWorkItemCoverageKnownCountGenerationSmoke` uses hostile counted enumerators to prove:

- MoveNext-induced Count drift is rejected before `Current` is read;
- Current-induced Count drift is rejected before the returned finding is accepted;
- an honest stable counted source remains accepted;
- a pure streaming source remains accepted.

The smoke is module-initialized so it runs in the deterministic Core smoke phase without changing shared smoke registration owned by another active lane.

`scripts/preflight-measurement-work-item-coverage-known-count-generation.py` pins the explicit enumerator boundaries, post-MoveNext/post-Current/terminal Count rebounds, admission ordering and smoke execution contract.

## Compatibility

This changes only malformed or concurrently unstable counted-source behavior. Valid reports preserve row sorting, issue aggregation, null rejection, finding capacity and final Count/traversal agreement semantics.

## Runtime boundary

REMOTE_SAFE managed Core only. No licensed BricsCAD, Windows UI, private DWG, release publication or `LOCAL_PASS` evidence is required or claimed.
