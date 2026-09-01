# Coordination unified workbook row integrity

Lane-Key: `issue-5181`

## Scope

This Core-only contract hardens trace lookup in the current three-sheet coordination workbook (`CLASHES`, `DUPLICATES`, `TRACE_MODEL`). It is separate from the legacy two-sheet reader hardened by #5179 and does not change native BricsCAD clash/duplicate calculation or licensed runtime behavior.

## Integrity contract

- Requested source rows are restricted to Excel data rows `2..1,048,576`.
- Every worksheet `<row r="...">` encountered during source/trace lookup is validated against canonical XLSX row coordinates `1..1,048,576`, including unrelated rows.
- `CLASHES` / `DUPLICATES` lookup retains only unique row `1` plus the requested source row and rejects missing or duplicated selected rows.
- `TRACE_MODEL` validates a unique header, scans data rows without retaining the whole worksheet, and retains only the unique matching `TRACE_KEY` projection; a second matching projection fails immediately.
- CLASH/DUPLICATE identity, canonical `MATCH_KINDS`, formula-vs-literal identity cells, exact three-sheet membership, shared-string bounds, canonical CAD handles, drawing fingerprint, rule identity, item ID and TRACE_KEY parity remain fail-closed.
- The reader must not restore eager `document.Descendants(...).ToList()` worksheet-row materialization.

## Deterministic validation

`CoordinationUnifiedWorkbookRowIntegritySmoke` exercises stable clash and duplicate lookup, unrelated out-of-range rows in all three sheets, duplicate selected source rows, duplicate TRACE_MODEL header and duplicate matching TRACE_KEY. `scripts/preflight-coordination-unified-workbook-row-integrity.py` pins the bounded traversal source contract.

Hosted exact-head `preflight + core` is authoritative for this Core-only package. No licensed BricsCAD `LOCAL_PASS` applies.
