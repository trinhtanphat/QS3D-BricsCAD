# MEP/TBQ CSV row Count stability

## Scope

This runbook covers the public `MepTbqProjectionService.SerializeCsv(IReadOnlyList<MepTbqReportRow>)` boundary. Runtime qualification is **NOT_APPLICABLE**: the contract is deterministic Core serialization integrity and requires no licensed BricsCAD or private DWG.

## Defect

`IReadOnlyList<T>` does not imply immutability. The historical serializer used `rows.Count` as the live `for` condition and then read `rows[i]`. A caller-controlled list could grow after an admitted row was read, causing an extra row to be indexed and serialized, shrink before a later indexer read, or change Count after an otherwise valid traversal without detection.

`BuildReport(IEnumerable<MepQuantityGroup>)` already owns a separate enumerable Count-integrity contract. This package does not alter that boundary.

## Production contract

1. Read `rows.Count` once as the admitted CSV row Count.
2. Reject a negative Count and reject Count above the existing 10,000 MEP/TBQ report bound before any indexer access.
3. Traverse only `0..admittedCount-1`; never use live `rows.Count` as the loop bound.
4. Immediately before every caller-controlled `rows[i]` read, require current Count to equal the admitted Count. This makes growth or shrink win before an index from another source generation is observed.
5. Preserve existing null-row validation, CSV escaping, invariant numeric formatting and row order.
6. Re-check Count after the admitted traversal and fail closed before returning the built CSV if Count drifted.

Because the `StringBuilder` remains local, any Count-integrity failure occurs before publication of a mixed-generation CSV string.

## Deterministic regression matrix

`MepTbqCsvRowCountStabilitySmoke` is module-initialized and covers:

- Count growth triggered by the first admitted indexer read: only that first row may be read; the unexpected row must never be indexed.
- Count shrink triggered by the first admitted indexer read: the second indexer read must never occur.
- Count drift visible only on the post-traversal rebound.
- negative Count rejection before any indexer read.
- Count 10,001 rejection before any indexer read.
- null row validation inside an admitted stable Count.
- stable rows serialize byte-for-byte identically to the ordinary stable-list path and are each read exactly once.

## Source guard

Run:

```text
python scripts/preflight-mep-tbq-csv-row-count-stability.py
```

The guard pins admission, fixed loop bound, pre-index Count revalidation, post-traversal revalidation and hostile-list smoke evidence. It rejects regression to a live `i < rows.Count` serialization loop.

## Acceptance

Repository-safe acceptance requires the focused guard, aggregate discovered feature guards, Core build and deterministic smoke to pass on the exact candidate. If protected main advances, collision-scan the four reserved paths and reconcile non-force before obtaining fresh protected checks and merging.
