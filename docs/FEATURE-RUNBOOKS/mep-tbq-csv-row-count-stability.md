# MEP/TBQ CSV row Count stability

## Scope

This runbook covers the public `MepTbqProjectionService.SerializeCsv(IReadOnlyList<MepTbqReportRow>)` boundary. Runtime qualification is **NOT_APPLICABLE**: the contract is deterministic Core serialization integrity and requires no licensed BricsCAD or private DWG.

## Defect

`IReadOnlyList<T>` does not imply immutability. The historical serializer used `rows.Count` as the live `for` condition and then read `rows[i]`. A caller-controlled list could grow after an admitted row was read, causing an extra row to be indexed and serialized, shrink before a later indexer read, or change Count after an otherwise valid traversal without detection.

A later hardening fixed the live-loop problem by admitting Count and rebinding before each indexer read plus after traversal. That still left the indexer itself as a hostile boundary: `rows[i]` can change Count before returning a row. Without an immediate post-index rebound, null validation or row-field CSV processing could begin before the cardinality change was detected on the next loop edge or final check.

`BuildReport(IEnumerable<MepQuantityGroup>)` owns a separate enumerable Count-integrity contract. This package does not alter that boundary.

## Production contract

1. Read `rows.Count` once as the admitted CSV row Count.
2. Reject a negative Count and reject Count above the existing 10,000 MEP/TBQ report bound before any indexer access.
3. Traverse only `0..admittedCount-1`; never use live `rows.Count` as the loop bound.
4. Immediately before every caller-controlled `rows[i]` read, require current Count to equal the admitted Count.
5. Immediately after every `rows[i]` read, rebind Count again before null validation, field access, escaping, formatting, or any other row-semantic CSV processing. Indexer-induced growth/shrink therefore fails closed at the hostile boundary itself.
6. Preserve existing null-row validation for a stable Count, CSV escaping, invariant numeric formatting and row order.
7. Re-check Count after the admitted traversal and fail closed before returning the built CSV if Count drifted.

Because the `StringBuilder` remains local, any Count-integrity failure occurs before publication of a mixed-generation CSV string.

## Deterministic regression matrix

`MepTbqCsvRowCountStabilitySmoke` is module-initialized and covers:

- Count growth triggered by the first admitted indexer read: only that first row may be read; the unexpected row must never be indexed.
- Count shrink triggered by the first admitted indexer read: the second indexer read must never occur.
- indexer-induced Count drift while returning `null`: canonical CSV Count-integrity failure must win before ordinary null-row validation.
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

The guard pins admission, fixed loop bound, pre-index Count revalidation, **post-index Count revalidation before null/row processing**, post-traversal revalidation, and hostile-list smoke evidence. It rejects regression to a live `i < rows.Count` serialization loop or moving the post-index rebound behind semantic processing.

## Acceptance

Repository-safe acceptance requires the focused guard, aggregate discovered feature guards, Core build and deterministic smoke to pass on the exact candidate. If protected main advances, collision-scan the four reserved paths and reconcile non-force before obtaining fresh protected checks and merging.

## Reconciliation receipt

The original row-count source-ready carrier was reconciled non-force onto protected `main@463f394b06454d6e200f03859c16e7ae2a050776` after a zero-overlap scan of all four reserved paths. Pre-reconcile exact head `7df1f2e85b90ba464fb84d80880fdadfefe38a78` passed Shared CI run `33236386212`, including deterministic smoke and V25 compile/final build.

Issue #4914 strengthens that contract at the indexer boundary from protected baseline `0b00cf1512d49f0c8cd9305cace8cccfa89b196b`. Its candidate must obtain fresh exact-head branch CI and current protected PR checks; older evidence is not merge evidence.
