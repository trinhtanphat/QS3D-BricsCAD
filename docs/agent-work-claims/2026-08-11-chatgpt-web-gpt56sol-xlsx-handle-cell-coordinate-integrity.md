# Agent Work Claim — XLSX Handle cell-coordinate integrity

- Agent: ChatGPT Web / GPT-5.6 Sol
- Date: 2026-08-11
- Status: COMPLETE
- Branch/target: direct `main` under current `AGENTS.md` coordination policy

## Scope reservation

This claim reserved only:

- `src/QS3D.Core/Export/XlsxHandleReader.cs`
- `tests/QS3D.Core.SmokeTests/XlsxHandleReaderCoordinateSmoke.cs`
- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs` (shared registry; re-read immediately before update)
- this claim file

## Explicit exclusions preserved

- No BricsCAD command/service/UI changes.
- No changes to LOCAL-013 runtime/evidence lane; that claim owns local proof rather than remote source surfaces.
- No changes to ED2 export parity, Quantity builders, persistence, rebar, or other active-agent lanes.

## Verified defect and fix

`XlsxHandleReader` selected worksheet rows by `<row r="N">`, but previously parsed only leading letters from each `c@r` cell coordinate and ignored the numeric row suffix. Malformed input such as `<row r="5"><c r="A999">...</c></row>` could therefore bind provenance values to the wrong spreadsheet row.

The reader now:

1. requires a valid positive containing row number before reading its cells;
2. parses cell references as ASCII A-Z/a-z column letters followed by a positive decimal row number;
3. rejects missing/invalid coordinates rather than silently skipping them;
4. requires the referenced row to equal the containing row;
5. rejects columns beyond the XLSX 16,384-column limit before cell-value use.

Valid modern/legacy parsing paths are otherwise unchanged.

## Regression

`XlsxHandleReaderCoordinateSmoke` covers:

- a valid modern inline-string CHI_TIET workbook and verifies Element ID, CAD Handle, fingerprint, and modern-schema resolution;
- a malformed target row whose cells use `A9/B9/C9` inside `<row r="2">`, which must fail closed;
- an invalid cell reference with a missing row suffix (`A`), which must fail closed.

The smoke is registered from the latest shared registry blob.

## Commits on `main`

- Claim registration: `6dc93895243bef0f26f7b5c22113977216ef38fb`
- Source hardening: `f89a154c88a659ec5449efada8b13d9c4363d885`
- Regression smoke: `fe67ae501fd6ce4cf5e6bd8e4c945d2f3e82f863`
- Smoke registry: `d8c083bcf56e413cb89ae38b78ed8eab0086c879`

## Validation evidence

- Commit diff for `f89a154c88a659ec5449efada8b13d9c4363d885` contains only the intended cell-coordinate/row-binding hardening in `XlsxHandleReader.cs`.
- Re-read current `main`: strict `ReadCells`/`ColumnIndex` logic and the full regression fixture are present.
- Shared smoke registry contains `XlsxHandleReaderCoordinateSmoke.Run();` while preserving concurrent registry additions.
- `get_commit_combined_status` for `d8c083bcf56e413cb89ae38b78ed8eab0086c879` returned no automatic statuses/checks; no CI pass is claimed.
- Earlier runtime checkout attempts in this same session were blocked before clone by `Could not resolve host: github.com`; no runtime pass or `LOCAL_PASS` is claimed.
- No BricsCAD V25/DWG proof is required for this CAD-independent parser contract.
