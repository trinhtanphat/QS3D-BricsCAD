# Advanced-cost Current-induced Count stability

Lane-Key: `issue-4966`

## Defect boundary

Advanced cost collection traversal already binds an admitted known Count before traversal and rechecks it before/after `MoveNext`. `IEnumerator.Current` is caller-controlled code too. Before this carrier, rate build-up components, historical cost records, tender lines/requirements/bids, and progress contract/claim inputs could execute `Current`, then perform semantic validation or retain the returned item before checking whether `Current` changed the source's reported Count.

That ordering can make a hostile counted source surface a null/duplicate/reference error, or retain state, before the stronger collection-integrity failure. The deterministic contract is therefore incomplete even when a later traversal checkpoint eventually notices the drift.

## Production contract

For every admitted counted traversal in `AdvancedCostManagement.cs`, the ordering is:

`stable Count -> MoveNext -> stable Count -> process-limit check -> Current -> stable Count -> semantic validation/retention`.

The Count rebound is performed immediately after `Current` and before null, duplicate, reference, or retention semantics. A Count change observed at this boundary fails closed with the existing `known count changed during traversal` contract. Maximum-entry, known-count/traversal parity, canonical ordering, tender ranking, progress clipping/retention, and commercial arithmetic behavior are otherwise unchanged.

Sources without an admitted known Count remain streaming-compatible; the rebound helper is a no-op for those sources.

## Deterministic regression

`AdvancedCostCurrentCountStabilitySmoke.cs` uses an `ICollection<T>` whose `Current` arms a one-read Count drift while returning null. Each affected public advanced-cost surface must reject at the Count boundary before null validation, and the hostile enumerator must be read exactly once. The smoke also preserves stable counted and streaming controls for tender, progress, rate-build-up, and historical behavior.

`preflight-advanced-cost-current-count-stability.py` pins all seven immediate post-`Current` rebound sites plus the hostile and control coverage. It is auto-discovered by Shared CI through the repository feature-preflight sweep.

## Validation and runtime boundary

Run the focused preflight, Core build, and deterministic Core smoke suite. Protected PR validation requires fresh exact-head `preflight` and `core` SUCCESS before merge.

No licensed BricsCAD runtime or private DWG is required for this Core collection-integrity defect, and no `LOCAL_PASS` may be inferred from remote/static evidence.
