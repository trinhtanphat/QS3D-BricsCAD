# Advanced cost known-Count Current integrity

Lane-Key: `issue-4544`

## Boundary

The Advanced Cost collection boundaries accept generic/read-only/non-generic `Count` evidence when available and retain an independent 10,000-entry streaming ceiling. A known Count is an admission contract, not merely a post-traversal diagnostic.

For caller-controlled collection traversal the required order is:

1. `MoveNext()` establishes whether another item exists.
2. `AdvancedCostCollectionContract.RequireCanProcessNext(...)` rejects known-Count overrun and the independent capacity ceiling.
3. Only after admission may `IEnumerator.Current` be observed.
4. Existing null/duplicate/reference/domain/commercial processing executes.
5. After traversal, exact cardinality and rebound known-Count stability are revalidated before result publication.

A C# `foreach` over these caller-controlled inputs is insufficient because `foreach` evaluates `Current` before the loop body. Therefore a Count=N source yielding N+1 can expose caller-owned N+1 state before an in-body guard runs.

## Deterministic acceptance

`AdvancedCostKnownCountCurrentIntegritySmoke` uses hostile `IReadOnlyCollection<T>` sources with independent `MoveNextCalls` and `CurrentReads`. For Count=1 with two available items, the consumer must execute the second successful `MoveNext` but reject before reading the second `Current`: `MoveNextCalls == 2`, `CurrentReads == 1`.

The existing Advanced Cost Count regression suite remains authoritative for negative/conflicting/oversized admission, under-yield rejection, post-traversal Count drift, duplicate/null semantics, exact-bound behavior, pure streaming behavior, deterministic ordering, ranking/progress semantics and commercial arithmetic.

## Runtime boundary

Core-only deterministic source/test behavior. Licensed BricsCAD runtime is `NOT_APPLICABLE`; this runbook does not authorize or claim `LOCAL_PASS`.
