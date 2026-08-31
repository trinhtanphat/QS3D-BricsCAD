# Quantity report totals hostile row bound

## Scope

`QuantityReportTotals.FromRows` is a deterministic Core reporting boundary. It accepts either counted collections or pure streaming `IEnumerable<QuantityReportRow>` input and must not permit caller-controlled enumeration to consume unbounded work.

## Contract

- At most **10,000** report rows are supported.
- A known Count greater than 10,000 fails before `GetEnumerator()` is called.
- Existing known-Count stability is retained before/after caller `MoveNext()`, immediately after `Current`, and after traversal.
- Known-Count over-yield retains precedence over the generic row ceiling.
- For a source with no Count surface, the 10,001st successful `MoveNext()` fails before `Current` is read and before any total is mutated for that row.
- Exactly 10,000 streaming rows remain accepted.
- Existing null-row diagnostics and compensated total arithmetic remain unchanged.

## Deterministic regression

`QuantityReportTotalsBoundSmoke` covers:

1. counted input advertising 10,001 rows, proving admission fails without starting enumeration;
2. hostile streaming input producing 10,001 rows, proving row 10,001 is rejected before `Current`;
3. an exact-bound 10,000-row streaming control, proving ordinary totals and traversal remain valid.

The auto-discovered `scripts/preflight-quantity-report-totals-bound.py` pins the production ordering and smoke evidence so the ceiling cannot silently move after `Current` or after enumeration starts for an over-limit known Count.

## Validation

```text
python scripts/preflight-quantity-report-totals-bound.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

Runtime classification: **NOT_APPLICABLE**. This package is host-neutral deterministic Core reporting integrity and does not claim licensed BricsCAD runtime evidence.
