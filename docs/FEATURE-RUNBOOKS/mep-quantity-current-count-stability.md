# MEP quantity Current-induced Count stability

Issue: #4861
Lane-Key: `issue-4861`
Runtime: `NOT_APPLICABLE` — deterministic Core/MEP quantity input-integrity contract.

## Defect boundary

`MepQuantityService.Aggregate` already admitted supported known-Count surfaces and rebound them immediately before and after each caller-controlled `MoveNext()`. The remaining gap was the next caller-controlled boundary: `enumerator.Current`. A hostile counted enumerable could change its reported Count from `Current` and return an item that entered null, identity, grouping, or quantity aggregation before the following loop-edge Count check detected the drift.

## Required traversal contract

For counted inputs, preserve the admitted Count and require this ordering for every successful item:

1. rebind Count immediately before `MoveNext()`;
2. call `MoveNext()` exactly once;
3. rebind Count immediately after successful `MoveNext()` and enforce the admitted cardinality before `Current`;
4. read `Current` exactly once;
5. rebind Count immediately after `Current` and before any returned-item null/identity/grouping/quantity acceptance;
6. retain the existing terminal traversal/cardinality and final Count revalidation before publication.

The 10,000-element ceiling, conflicting/negative known-Count refusal, overrun-before-Current behavior, duplicate element IDs, deterministic grouping/sort, compensated length/area/volume aggregation, and pure-streaming behavior remain unchanged.

## Deterministic regression

`MepQuantityMidTraversalCountDriftSmoke` includes a hostile `IReadOnlyCollection<MepElement>` whose `Current` mutates Count and returns `null`. The required result is the canonical Count-stability `InvalidOperationException` before ordinary null-item validation. This proves the post-Current rebound is an acceptance boundary rather than merely a later loop-edge check.

`MepQuantityInputBoundSmoke` retains its existing phase-change controls and pins the stronger stable Count-observation budget: two successful items plus terminal/final validation observe Count nine times; three successful items observe it twelve times.

## Source guard

`scripts/preflight-mep-quantity-known-count-stability.py` pins the source ordering:

`Count -> MoveNext -> Count -> Current -> Count -> returned-item acceptance -> final Count -> publication`.

It also requires the Current-induced deterministic regression and the reconciled historical observation budgets.

## Validation

Repository-safe validation for this lane is:

- focused auto-discovered MEP quantity preflight;
- aggregate feature source guards;
- Core build and deterministic smoke suite;
- protected Shared CI `preflight + core` on the exact current candidate.

No licensed BricsCAD/private-DWG runtime evidence is required or claimed for this Core-only contract.
