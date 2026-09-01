# RateBook Current-induced Count integrity

Lane-Key: `issue-5187`
Reservation-Protocol: `v2`
Ownership-Key: `core.cost.ratebook-current-count-integrity-v1`

## Scope

`RateBook` materializes commercial rate items from an `IEnumerable<RateItem>`. When the source exposes deterministic Count metadata through `ICollection<RateItem>`, `IReadOnlyCollection<RateItem>`, or non-generic `ICollection`, the admitted Count is part of the input integrity contract for the entire traversal.

## Correctness contract

For counted sources, traversal must fail closed in this order:

1. bind all available Count channels at admission and reject negative/conflicting values;
2. reject an admitted Count above `MaxItems` before enumeration;
3. rebind Count before `MoveNext()`;
4. after a successful `MoveNext()`, rebind Count again before overrun/bound checks and before observing `Current`;
5. reject known-count overrun and the `MaxItems` hard ceiling before `Current`;
6. read `enumerator.Current` exactly once for the admitted item;
7. immediately rebind the exact admitted Count after `Current` and before null validation, duplicate-ID detection, scope/effective-time mutation, snapshot retention, or index publication;
8. after traversal, reject under-yield and perform the final Count rebound before sorting/publication.

The post-`Current` rebound is required because `IEnumerator<T>.Current` is caller-controlled code for arbitrary enumerable implementations. A hostile counted source can mutate one or more Count channels inside the getter. Such metadata drift must be rejected as Count-integrity failure before ordinary item semantics or mutable RateBook staging can run.

Pure streaming sources without deterministic Count metadata retain the existing bounded traversal behavior. Existing duplicate ID, ambiguous effective timestamp, sorting, scope, maximum-item, over-yield, under-yield, multi-interface Count conflict, and final Count stability semantics remain unchanged.

## Deterministic regression

`RateBookKnownCountTraversalSmoke.CurrentInducedCountDriftFailsBeforeItemSemantics` uses a custom `IReadOnlyCollection<RateItem>` whose `Current` getter changes Count from the admitted value. The regression proves:

- `Current` is read exactly once;
- the Count mutation becomes visible immediately;
- `RateBook` throws the known-count-changed failure before accepting/publishing the returned item.

The historical over-yield, under-yield, exact-count, post-traversal drift, multi-interface conflict, honest multi-interface, and pure-streaming controls continue to run.

`scripts/preflight-ratebook-known-count-stability.py` pins the production ordering `admission -> pre-Move rebound -> MoveNext -> post-Move rebound -> overrun/bound -> Current -> post-Current rebound -> item semantics/retention -> under-yield -> final rebound -> publication` and requires the hostile smoke.

## Validation boundary

Runtime: `NOT_APPLICABLE`.

This is deterministic Core cost/rate-book state and commercial-integrity behavior. Protected exact-head `preflight` + `core` SUCCESS is the acceptance evidence. Licensed BricsCAD runtime execution is not required and must not be claimed as `LOCAL_PASS` for this carrier.
