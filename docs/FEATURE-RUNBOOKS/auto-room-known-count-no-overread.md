# Auto Room known-Count no-overread

Lane-Key: issue-4477

## Contract

`AutoRoomLifecycle.MarkStaleForSelection` must not observe caller-controlled `IEnumerator.Current` values that are outside the admitted semantic boundary.

For `activeRoomIds`, the required order is `MoveNext -> advertised Count overrun check -> Current -> normalize/retain`; known-Count N+1 fails immediately and must not read Current N+1.

For `selectedSourceHandles`, historical behavior intentionally traverses beyond an under-reported Count so final cardinality drift remains detectable and the independent 5,000-input hard ceiling can win. The required order is therefore `MoveNext -> hard ceiling -> advertised-Count discard decision -> Current -> normalize/retain`. Post-Count values contribute only to observed traversal cardinality and must not be read. Item 5,001 must be proven by `MoveNext` but rejected before Current 5,001.

Existing contracts remain authoritative: advertised selected Count >5,000 fails before enumeration; under-yield fails; exact-count normalization/deduplication remains unchanged; project ChangeVersion is revalidated before stale computation; invalid inputs fail before project mutation.

## Deterministic validation

Run the Core smoke project and `scripts/preflight-auto-room-known-count-no-overread.py`. The dedicated adversarial smoke independently records `MoveNext` and `Current` calls for active overrun, selected Count drift, and selected hard-cap precedence.

This is repository-safe Core/domain lifecycle work. Licensed BricsCAD, private DWGs, signing, and `LOCAL_PASS` evidence are not required and must not be inferred from hosted CI.
