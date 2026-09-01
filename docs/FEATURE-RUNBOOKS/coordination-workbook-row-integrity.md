# Coordination workbook row integrity

Lane-Key: `issue-5179`

## Scope

This Core-only contract hardens the legacy two-sheet coordination workbook trace reader (`CLASHES` + `TRACE_MODEL`). It does not change clash calculation, native BricsCAD selection, the unified coordination workbook format, or licensed runtime behavior.

## Integrity contract

- `CoordinationWorkbookTraceReader.Read` accepts only requested data rows `2..1,048,576`.
- Every worksheet `<row r="...">` encountered by the lookup is validated against the canonical XLSX row range `1..1,048,576`, including unrelated rows.
- `CLASHES` lookup retains only row `1` and the requested row and rejects missing/duplicate selected rows.
- `TRACE_MODEL` lookup validates the header, scans data rows without retaining the whole worksheet, and retains only the unique matching `TRACE_KEY` projection.
- Formula-vs-literal identity rules, exact sheet set, shared-string validation, canonical Handle normalization, drawing fingerprint, rule identity, ClashId and TRACE_KEY parity remain fail-closed.
- The lookup must not restore eager `document.Descendants(...).ToList()` worksheet-row materialization.

## Deterministic validation

`CoordinationWorkbookRowIntegritySmoke` covers stable lookup plus unrelated out-of-range row metadata in both `CLASHES` and `TRACE_MODEL`. `scripts/preflight-coordination-workbook-row-integrity.py` pins the bounded selective-scan source contract and rejects eager all-row materialization.

No licensed BricsCAD `LOCAL_PASS` applies. Exact-head hosted `preflight + core` is authoritative for this Core data-integrity package.
