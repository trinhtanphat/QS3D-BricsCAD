# Agent Work Claim — XLSX Handle cell-coordinate integrity

- Agent: ChatGPT Web / GPT-5.6 Sol
- Date: 2026-08-11
- Status: ACTIVE
- Branch/target: direct `main` under current `AGENTS.md` coordination policy

## Scope reservation

- `src/QS3D.Core/Export/XlsxHandleReader.cs`
- `tests/QS3D.Core.SmokeTests/XlsxHandleReaderCoordinateSmoke.cs` (new)
- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs` (shared registry; re-read immediately before update)
- this claim file

## Explicit exclusions

- No BricsCAD command/service/UI changes.
- No changes to LOCAL-013 runtime/evidence lane; that claim explicitly owns local proof rather than remote source surfaces.
- No changes to ED2 export parity, Quantity builders, persistence, rebar, or other active-agent lanes.

## Verified defect

`XlsxHandleReader` selects a worksheet `<row>` by its `r` number, but `ReadCells`/`ColumnIndex` parses only the leading letters of each cell reference and ignores the numeric row suffix. A malformed workbook such as `<row r="5"><c r="A999">...</c></row>` is therefore treated as column A data belonging to row 5. For Excel Locate this can bind semantic/provenance data to the wrong physical spreadsheet coordinate instead of failing closed.

## Plan

1. Parse every cell reference as a strict A1 coordinate using ASCII A-Z/a-z column letters followed by a positive decimal row number.
2. Require the cell reference row number to equal its containing `<row r="...">` number and reject unsupported columns before cell-value use.
3. Keep valid modern/legacy workbook behavior unchanged.
4. Add a Core smoke with a valid inline-string modern workbook control plus malformed mismatched-row and invalid-coordinate cases.
5. Register the smoke from the latest shared registry blob.
6. Re-read current `main`, inspect commit/status evidence, and close this claim with exact SHAs. No `LOCAL_PASS` will be claimed without actual execution.
