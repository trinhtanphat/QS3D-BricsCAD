# TBQ workspace post-Current Count stability

## Scope

`TbqProjectWorkspaceState` accepts four caller-controlled enumerable inputs: bill items, build-up rates, rate references, and BQ library entries. Counted inputs are integrity-bearing commercial snapshots. This contract is deterministic Core behavior; licensed BricsCAD runtime is **NOT_APPLICABLE**.

## Integrity boundary

For every counted TBQ traversal, the admitted supported Count surfaces must remain stable across the entire traversal. Existing guards already bind Count at admission, before and after `MoveNext`, enforce maximum cardinality and overrun-before-`Current`, and verify final observed cardinality.

The stronger invariant is that Count is rebound immediately **post-`Current`** and before semantic acceptance of the returned value. A caller-controlled `Current` getter may itself mutate the source's Count. That Count drift must fail closed before:

- bill items are null/duplicate checked, snapshotted, or sorted;
- build-up rates are null/duplicate checked, snapshotted, or sorted;
- rate references are counted/yielded into `RateReferenceGraph`;
- BQ library entries are counted/yielded into `BqLibraryCatalog`.

No returned item from a Count-drifting `Current` boundary may enter published workspace state.

## Regression contract

`TbqProjectWorkspaceSmoke.CurrentCountDriftFailsBeforeItemAcceptance` uses counted collections whose `Current` changes Count from 1 to 2 while returning a null value. The required exception is the canonical known-count-changed failure for bill items, build-up rates, rate references, and BQ library entries; ordinary null/item validation must not win. An honest counted control with no Current-induced drift remains accepted.

The focused preflight pins all three production traversal shapes (`SnapshotBillItems`, `SnapshotBuildUpRates`, and generic `Bounded<T>`) so the post-`Current` Count rebound cannot move behind null/duplicate/snapshot/count/yield processing.

## Preservation requirements

Keep multi-interface Count agreement, negative and oversize Count rejection, pre/post-`MoveNext` stability, overrun-before-`Current`, per-input ceilings, final exact observed-cardinality checks, duplicate/null semantics, deterministic sorting, and pure-streaming behavior unchanged. Do not weaken the gate by accepting transient Count drift or by converting the hostile regression to a generic exception-only assertion.
