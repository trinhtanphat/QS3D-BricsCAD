# Project quantity aggregation precision

Lane-Key: `issue-4652`

## Scope

`ProjectQuantityReportBuilder` groups project elements into commercial quantity rows. Continuous grouped quantities must retain finite small contributions across high dynamic range without weakening the existing fail-closed requirement for a final binary64 value that is not representable.

## Invariant

For each grouped row and continuous metric such as concrete volume, formwork area, dimensions, perimeters and measured areas:

1. validate every incoming quantity with the existing metric semantics;
2. accumulate into isolated compensated state rather than publishing strict pairwise row totals;
3. reject sum/compensation overflow immediately;
4. preserve evidence flags, counts, grouping/order, notes, project-revision checks and provenance independently;
5. finalize only after traversal;
6. fail closed if a material non-zero compensation cannot be represented in the final binary64 result.

The final representability boundary remains strict: `2^53 + 1` must not silently publish as `2^53`, while `1e16 + 1 + 1` must publish the representable `10000000000000002`.

`Detail(...)` remains element-isolated: each detail key has exactly one element and must retain existing values and provenance.

## Deterministic regression

`ProjectQuantityAggregationPrecisionSmoke` covers large-first and small-first high-dynamic-range inputs, metric/group isolation, detail isolation, final-unrepresentable refusal and non-finite refusal. The smoke self-registers through a module initializer.

`python scripts/preflight-project-quantity-aggregation-precision.py` is auto-discovered by aggregate preflight and pins compensated accumulation before final publication while rejecting restoration of pairwise grouped accumulation for representative metrics.

## Validation

Run:

```text
python scripts/preflight-project-quantity-aggregation-precision.py
python scripts/preflight-all.py
dotnet build src/QS3D.Core/QS3D.Core.csproj -c Release
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

Repository Shared CI is authoritative for the exact pushed candidate. This is deterministic Core reporting/commercial work; licensed BricsCAD/private-DWG runtime evidence is `NOT_APPLICABLE` and no `LOCAL_PASS` is produced.
