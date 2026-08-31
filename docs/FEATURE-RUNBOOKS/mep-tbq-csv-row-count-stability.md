# MEP/TBQ CSV row Count stability

## Scope

This runbook covers the public `MepTbqProjectionService.SerializeCsv(IReadOnlyList<MepTbqReportRow>)` boundary. Runtime qualification is **NOT_APPLICABLE**: the contract is deterministic Core serialization integrity and requires no licensed BricsCAD or private DWG.

`BuildReport(IEnumerable<MepQuantityGroup>)` owns a separate enumerable Count-integrity contract. This package does not alter that boundary.

## Defect history

The original serializer used live `rows.Count` as the `for` condition. Earlier hardening fixed live-loop growth/shrink and then added an immediate post-index rebound so an `IReadOnlyList<T>` indexer could not change its primary Count and begin row processing before rejection.

Issue #5082 closes the remaining multi-interface gap. An object accepted through `IReadOnlyList<MepTbqReportRow>` may simultaneously implement `ICollection<MepTbqReportRow>` and/or non-generic `ICollection`. Before #5082 the serializer admitted and rebound only the read-only Count. Conflicting secondary Count evidence could therefore be accepted at admission, and a caller-controlled indexer could drift a secondary Count while keeping `IReadOnlyList.Count` stable. The generated CSV would then be based on a source whose advertised cardinality channels did not describe one coherent generation.

## Production contract

1. Capture the read-only Count and every secondary generic/non-generic collection Count channel exposed by the row object at admission.
2. Reject negative Count evidence, Count above the existing 10,000 row bound, and disagreement between any admitted Count channels before the first indexer access.
3. Keep the admitted channel set and values as the serialization cardinality contract; the fixed loop bound is the admitted read-only Count.
4. Immediately before every caller-controlled `rows[i]` read, revalidate every admitted Count channel against its admitted value.
5. Immediately after every `rows[i]` read, revalidate every admitted Count channel again before null validation, field access, escaping, formatting, or any row-semantic CSV processing.
6. Re-read the primary read-only Count after secondary Count getters so a secondary getter cannot change primary cardinality after its first check.
7. Preserve existing null-row validation, invariant numeric formatting, CSV escaping, source row order, exact one-indexer-read-per-admitted-row behavior, and the no-live-Count loop bound.
8. Revalidate the complete admitted Count-channel contract after traversal and fail before returning the local `StringBuilder` result if drift occurred.

Because the builder remains local, Count-integrity failure cannot publish a mixed-generation CSV string.

## Deterministic regression matrix

Historical `MepTbqCsvRowCountStabilitySmoke` remains unchanged and continues to cover primary read-only Count growth, shrink, indexer-induced drift, final rebound drift, negative/oversized Count, null rows, deterministic output, and no-overread behavior.

`MepTbqCsvMultiCountSmoke` adds the #5082 matrix:

- conflicting generic `ICollection<MepTbqReportRow>.Count` rejects before any indexer read;
- conflicting non-generic `ICollection.Count` rejects before any indexer read;
- an admitted generic Count that changes from the row indexer rejects immediately after exactly one admitted indexer read;
- a stable object exposing read-only, generic, and non-generic Count channels serializes byte-for-byte identically to the ordinary stable-list path and reads the row once.

## Source guard

Run:

```text
python scripts/preflight-mep-tbq-csv-row-count-stability.py
```

The auto-discovered guard now pins both historical one-channel protections and the stronger #5082 contract: multi-channel admission, generic/non-generic channel binding, fixed loop bound, pre-index rebound, post-index rebound before null/row processing, final rebound, hostile multi-interface smoke, and the stable three-channel control. It continues to reject regression to a live `i < rows.Count` loop.

## Acceptance

Repository-safe acceptance requires the focused guard, aggregate discovered feature guards, Core build and deterministic smoke to pass on the exact candidate. If protected main advances, collision-scan the four reserved #5082 paths and reconcile without force. Merge only a current, collision-clean exact head with fresh protected `preflight + core` success, then verify protected main contains the landed tree and release the reservation. No licensed BricsCAD `LOCAL_PASS` is applicable or claimed.

## Historical receipts

The original row-count source-ready carrier was reconciled onto protected `main@463f394b06454d6e200f03859c16e7ae2a050776`; pre-reconcile head `7df1f2e85b90ba464fb84d80880fdadfefe38a78` passed Shared CI run `33236386212`. Issue #4914 later strengthened the primary Count contract at the indexer boundary from protected baseline `0b00cf1512d49f0c8cd9305cace8cccfa89b196b`.

Issue #5082 is a distinct continuation: it preserves those contracts while binding every Count channel exposed by a multi-interface CSV row source.
