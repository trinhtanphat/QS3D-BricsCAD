# Customer workbook worksheet-row materialization bound

## Scope

`QsCustomerWorkbookTraceReader` accepts customer XLSX workbooks whose XML parts are already byte/character bounded. Worksheet traversal has an additional XLSX semantic ceiling of 1,048,576 row elements. Both the selected business worksheet and `TRACE_MODEL` must pass through the same bounded row materializer before row lookup or trace projection.

## Contract

- The reader discovers a possible surplus row with `MoveNext()` and rejects it before reading or retaining that surplus `Current` value.
- The public production ceiling remains `MaxRows = 1,048,576`; the helper's smaller ceiling parameter exists only so deterministic Core smoke can prove exact-limit and first-over-limit behavior without constructing a million-row fixture.
- Duplicate/missing row lookup retains at most two matching rows because only the cardinalities zero, one and more-than-one are semantically relevant.
- Existing row-number validation, XLSX column limit, formula rejection on identity cells, shared-string validation, exact worksheet set, TRACE_KEY integrity, CAD-handle canonicalization and drawing-fingerprint provenance remain unchanged.
- This is deterministic Core hostile-input validation. Licensed BricsCAD runtime evidence is not applicable and must not be reported as `LOCAL_PASS`.

## Deterministic validation

```text
python scripts/preflight-customer-workbook-row-materialization-bound.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

The focused source guard is auto-discovered by the aggregate feature-preflight lane. Protected acceptance remains exact-candidate Shared CI `preflight` plus `core`, followed by strict-freshness reconciliation and protected PR merge.
