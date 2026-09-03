# RateBook Current-induced Count stability

Issue: #4863
Lane-Key: `issue-4863`
Runtime: `NOT_APPLICABLE` — deterministic Core/commercial rate-book integrity.

## Defect boundary

`RateBook` already admitted supported deterministic Count surfaces and rebound the admitted Count immediately before and after every caller-controlled `MoveNext()`. The remaining uncontrolled boundary was `enumerator.Current`: a hostile counted enumerable could change its Count while returning an item, and that returned item could reach null/duplicate ID/scope/effective-time/snapshot acceptance before the next loop-edge Count check.

## Required traversal contract

For counted inputs, preserve the admitted Count and require this ordering for every successful item:

1. rebind Count before `MoveNext()`;
2. call `MoveNext()` exactly once;
3. rebind Count after successful `MoveNext()`;
4. enforce admitted Count overrun and the 10,000-item ceiling before `Current`;
5. read `Current` exactly once;
6. rebind Count immediately after `Current` and before any returned-item validation or commercial state mutation;
7. preserve terminal under-yield validation and the final Count rebound before sorting/publication.

Negative/conflicting Count refusal, duplicate rate-item identity, scope/effective-time ambiguity rejection, deterministic ordering/resolution and pure-streaming behavior remain unchanged.

## Deterministic regression

`RateBookKnownCountTraversalSmoke` includes a hostile `IReadOnlyCollection<RateItem>`/enumerator whose `Current` changes Count and returns `null`. The required outcome is the canonical `Rate book item source known count changed during traversal.` failure before ordinary null-item validation. This proves the new rebound protects the item-acceptance boundary rather than merely detecting drift on a later iteration.

Historical over-yield, under-yield, end-of-traversal drift, multi-interface conflict, stable multi-interface and pure-streaming controls remain active.

## Source guard

`scripts/preflight-ratebook-known-count-stability.py` pins the production ordering:

`Count -> MoveNext -> Count -> overrun/ceiling -> Current -> Count -> item acceptance -> final Count -> publication`.

It also requires the Current-induced regression while retaining the historical Count-stability controls.

## Validation

Repository-safe acceptance requires the focused auto-discovered guard, aggregate feature guards, Core build/deterministic smoke, and exact-current protected Shared CI `preflight + core`. No licensed BricsCAD/private-DWG runtime evidence is required or claimed for this Core-only package.
