# Bulk edit transient known-Count stability

Lane: `issue-4666`

## Scope

This package hardens caller-controlled target traversal in `BulkEditService` for both semantic object targets and target IDs. It is deterministic Core correctness; licensed BricsCAD runtime is `NOT_APPLICABLE`.

## Defect contract

Historical known-Count protection rejected an item beyond advertised Count inside a C# `foreach` body. `foreach` reads `IEnumerator.Current` before entering that body, so Count=N could still expose N+1 `Current`. The same code bound supported Count metadata only once at admission, which allowed transient Count drift to be hidden if the collection restored its original Count before traversal completed.

The corrected traversal binds all supported Count surfaces (`ICollection<T>`, `IReadOnlyCollection<T>`, and non-generic `ICollection`) and revalidates both the value and supported-source set:

1. before every MoveNext;
2. after every successful MoveNext;
3. before the known-Count overrun/hard-cap admission and before IEnumerator.Current;
4. after terminal MoveNext and again before returning the materialized result.

Transient growth, transient shrink, negative Count, conflicting Count surfaces, Count-source drift, over-limit metadata, over-yield and under-yield fail closed.

## Preserved behavior

- Advertised Count=1 with a second successful MoveNext is rejected with only one Current read.
- The independent 10,000-entry limit remains authoritative for streaming sources without Count evidence.
- Honest stable multi-interface counted collections remain accepted.
- Object target null/canonical-id/project-ownership rules remain unchanged.
- Target-ID canonicalization, duplicate detection and project lookup remain unchanged.
- Project freshness/current-ownership checks remain after target materialization.
- Property edits, numeric multiplication and Family assignment remain all-or-nothing semantic mutations.

## Deterministic regressions

`BulkEditKnownCountEarlyDriftSmoke` now records both `MoveNextCalls` and `CurrentReads`, preserving the historical overrun/under-yield precedence contract while proving N+1 `Current` is not observed.

`BulkEditTransientCountStabilitySmoke` uses hostile collections implementing all three supported Count interfaces. Reading the first Current arms transient metadata which a legacy implementation could hide by restoring inside the next MoveNext. The corrected implementation rejects before that second MoveNext. Coverage includes transient growth, transient shrink, negative Count, conflicting Count surfaces, stable multi-interface object targets and stable multi-interface target IDs.

## Repository validation

Run the focused guards and deterministic smoke through normal Shared CI. After any source SHA change, require fresh exact-head branch evidence, reconcile latest protected main non-force if it advances, then require protected PR `preflight + core` SUCCESS before expected-head merge and exact-main verification.

Hosted CI is authoritative for this Core package. No licensed BricsCAD `LOCAL_PASS` is required or claimed.
