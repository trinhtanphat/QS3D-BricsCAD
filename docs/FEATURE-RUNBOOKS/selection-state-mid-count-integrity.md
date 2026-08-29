# SelectionState mid-traversal known-Count integrity

## Purpose

`SelectionState.Replace` accepts caller-owned enumerables. When an input exposes a known `Count`, that cardinality is part of the admitted snapshot contract, not merely a terminal consistency hint.

## Defect boundary

Before this package, known Count was resolved at admission and compared again after enumeration. Overrun was rejected before reading an extra `Current`, but a Count change after an admitted item could still allow the next caller-controlled `MoveNext` to execute. Likewise, Count could change inside a successful `MoveNext` and the new `Current` could be read before terminal validation. A hostile iterator could therefore perform side effects or throw ahead of the stronger Count-integrity failure.

## Production contract

For inputs exposing `ICollection<string>.Count`, `IReadOnlyCollection<string>.Count`, or non-generic `ICollection.Count`, `Replace` must:

1. resolve one non-negative, non-conflicting admitted Count within the 10,000-entry limit;
2. revalidate all exposed Count evidence immediately before every `MoveNext`;
3. after a successful `MoveNext`, revalidate Count again before reading `Current`;
4. preserve the existing overrun check before extra `Current` access;
5. revalidate Count after traversal and require exact admitted cardinality;
6. publish no partial selection and no `Changed` event on any integrity failure.

Pure streaming enumerables with no known Count remain supported. Existing trimming, case-insensitive deduplication, deterministic output ordering, change-version/reentrancy protection and input cap remain unchanged.

## Deterministic evidence

`SelectionStateMidCountIntegritySmoke` covers Count drift triggered after `Current` and proves rejection before the next `MoveNext`; Count drift triggered inside `MoveNext` and proves rejection before the corresponding `Current`; cross-interface Count conflict introduced mid-traversal; stable counted input; and pure streaming input. Existing SelectionState smoke coverage continues to cover overrun, under-yield, terminal rebound, invalid counts and publication semantics.

`python scripts/preflight-selection-state-mid-count-integrity.py` pins the production traversal ordering and focused smoke evidence.

## Runtime classification

`NOT_APPLICABLE` for licensed BricsCAD. This is deterministic Core state/integrity behavior and does not justify a remote `LOCAL_PASS` claim.
