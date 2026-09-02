# Revision snapshot detacher indexer Count stability

## Scope

`RevisionSnapshotDetacher` freezes mutable revision snapshots before public comparison. This package closes caller-controlled `IList<T>` indexer gaps in both the top-level element list and nested source-handle/dependency lists.

## Fail-closed contract

After an admitted Count is read, every caller-controlled indexer getter is followed immediately by a Count rebound before the returned value may be published or used for nested copying. A nested list whose getter changes Count therefore fails before publication into the detached destination. A top-level element-list getter that changes Count fails before properties, quantities, source handles or dependencies are copied from that returned element.

The existing pre-indexer checks and final Count checks remain defense in depth. Negative Count and the 100,000-entry ceilings remain unchanged. There is no retry or normalization of a drifting collection.

## Compatibility

Stable lists retain their existing order and values. Null-element fidelity remains unchanged. The map/enumerator Count stability contract landed in #5359 remains intact and independent; this package adds the corresponding indexer boundary without weakening those `GetEnumerator`, `MoveNext` or `Current` protections.

## Evidence boundary

The deterministic smoke invokes the private list-copy boundary with a tracking destination to prove indexer-induced drift is rejected before publication, and uses a hostile top-level element list plus a nested probe to prove rejection occurs before nested copying. Licensed BricsCAD runtime is not applicable.
