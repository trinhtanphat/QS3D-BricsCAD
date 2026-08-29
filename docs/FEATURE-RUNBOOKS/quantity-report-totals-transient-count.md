# QuantityReportTotals transient known-Count stability

## Scope

This runbook covers deterministic Core validation for `QuantityReportTotals.FromRows`. Runtime BricsCAD evidence is not applicable.

## Defect contract

`FromRows` accepts arbitrary `IEnumerable<QuantityReportRow>` inputs and binds any available `ICollection<T>`, `IReadOnlyCollection<T>`, and non-generic `ICollection` Count evidence. Admission plus a final rebound is insufficient: a mutable/hostile collection can expose transient Count drift during enumeration and restore its original Count before the final check.

A report total must never be published from such unstable metadata. The traversal therefore rebinds all supported Count surfaces:

1. before every `MoveNext`;
2. after every successful `MoveNext`, before the N+1 check and before `IEnumerator.Current`;
3. immediately after terminal `MoveNext == false`;
4. once more before final numeric publication.

Any Count value drift, supported-interface source-set drift, negative Count, or conflicting simultaneous Count evidence fails closed.

## Preserved behavior

- known Count N+1 is rejected before N+1 `Current`;
- under-yield remains a Count/enumeration mismatch;
- stable multi-interface counted collections remain accepted;
- pure streaming enumerables with no supported Count surface remain accepted;
- compensated Gross/Deduction/Net/Formwork/Length/DoorArea totals are unchanged;
- null rows, non-finite values and checked Count overflow remain fail-closed.

## Deterministic regression

`QuantityReportTotalsTransientCountSmoke` uses a hostile collection implementing all three supported Count interfaces. Reading the first row arms a transient Count mutation. A legacy admission/final-only implementation proceeds into the next `MoveNext`, where the fixture restores the original Count and can hide the drift. The corrected traversal observes the transient state before that second `MoveNext`.

The smoke covers transient growth, shrink, negative Count, cross-interface conflict, and a stable counted control. Failure probes assert `MoveNextCalls == 1` and `CurrentReads == 1`, proving no second traversal step or affected `Current` read occurs after the metadata becomes unstable.

## Repository validation

Run:

```text
python scripts/preflight-quantity-report-totals-transient-count.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

Then require exact-head Shared CI, current-main reconciliation if needed, protected PR `preflight + core`, expected-head merge, and exact protected-main verification.
