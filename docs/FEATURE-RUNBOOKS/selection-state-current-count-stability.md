# SelectionState Current-induced Count stability

## Scope

`SelectionState.Replace(IEnumerable<string>)` treats supported collection `Count` surfaces as an admitted traversal-integrity contract. A hostile enumerable may execute caller-controlled code from `MoveNext`, `Current`, or a Count getter, so every successful `Current` read must be followed by a Count rebound before the item is accepted into replacement state.

## Required ordering

For each item, the traversal must preserve:

1. known-Count stability before `MoveNext`;
2. `MoveNext`;
3. known-Count stability after `MoveNext`;
4. known-count overrun and absolute input-cap rejection before `Current`;
5. exactly one `Current` read;
6. known-Count stability immediately after `Current`;
7. only then increment/normalize/accept the item.

The final known-Count rebound, under-yield check, selection ChangeVersion freshness, case-insensitive dedupe, trimming, no-partial-publication semantics, and streaming-input support remain unchanged.

## Deterministic evidence

`SelectionStateMidCountIntegritySmoke` exercises hostile Count drift and confirms failure without publishing partial selection changes. `scripts/preflight-selection-state-current-count-stability.py` pins the exact source ordering so a later refactor cannot silently move acceptance ahead of the post-Current rebound.

Licensed BricsCAD runtime is not applicable to this Core-only integrity boundary.
