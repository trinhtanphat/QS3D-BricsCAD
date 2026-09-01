# Customer workbook business-row integrity

## Scope

`QsCustomerWorkbookTraceReader.Read` resolves a selected DGKL, COP_PHA, or CHI_TIET row back to canonical TRACE_MODEL provenance. Business-sheet trace lookup must not accept malformed worksheet row metadata merely because the malformed row is unrelated to the requested row.

## Contract

The selected business worksheet is scanned once with the XLSX row ceiling of 1,048,576. The scan validates every declared row number before the trace key is accepted, rejects malformed or out-of-range row metadata, rejects duplicate header row 1 and duplicate requested rows, and retains only the unique header plus requested target row.

The ceiling check occurs after `MoveNext()` but before reading an unexpected surplus `Current`, preserving the no-surplus-Current contract established by the historical worksheet-row materialization guard.

TRACE_MODEL keeps its existing bounded full-row materializer because trace-key lookup intentionally scans the trace projection. Formula/literal validation, TRACE_KEY canonicality, exact worksheet-set validation, shared-string handling, drawing fingerprint, Element ID and CAD Handle provenance remain unchanged.

## Deterministic validation

`CustomerWorkbookBusinessRowIntegritySmoke` covers stable selection, duplicate header/target rows, malformed unrelated row metadata, out-of-range unrelated row metadata, and the no-surplus-Current ceiling ordering.

`preflight-customer-workbook-business-row-integrity.py` pins the selective single-pass business-sheet contract. `preflight-customer-workbook-row-materialization-bound.py` continues to pin the TRACE_MODEL bounded materializer and explicitly recognizes the business-sheet selective scan.

Runtime classification: `NOT_APPLICABLE`. This contract is deterministic Core XLSX parsing/data-integrity behavior and requires no licensed BricsCAD runtime evidence.
