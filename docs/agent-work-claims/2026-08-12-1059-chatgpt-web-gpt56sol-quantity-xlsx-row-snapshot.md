# Work claim — Quantity XLSX standard row snapshot integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-xlsx-row-snapshot-20260812-1059`
- Registered: `2026-08-12T10:59:00+07:00`
- Baseline main SHA: `53b99cd5b89ef722bc7d51215801a4ee190a456c`
- Priority: P1 export preflight / filesystem atomicity

## Confirmed defect

`XlsxQuantityExporter.Export(...)` validates the caller-owned standard `IReadOnlyList<QuantityReportRow>` before I/O, including mutable `ElementIds` / `SourceHandles`, but then passes the original rows to `ExportCore(...)`. After directory/temp-file creation, `BuildSheet(rows)` re-reads row scalars and derived provenance text from the same mutable objects. The serialized worksheet can therefore differ from the values that passed preflight, or fail only after filesystem side effects begin.

## Reserved scope

- standard `XlsxQuantityExporter.Export(...)` path in `src/QS3D.Core/Export/XlsxQuantityExporter.cs`
- a new focused smoke file for standard Quantity XLSX row-snapshot integrity
- this claim file for close-out

## Contract

- Capture the existing standard worksheet row bound once.
- Read each caller-owned row index once before any path/directory/temp-file mutation.
- Copy every standard-sheet-emitted scalar plus `ElementIds` / `SourceHandles` provenance into detached `QuantityReportRow` values.
- Run existing standard text/numeric validation on the detached snapshot.
- Serialize only the detached standard snapshot.
- Preserve existing worksheet schema, XML sanitization, row/text/numeric limits, package validation and atomic replace semantics.

## Exclusions

- Do not change `ExportEd2(...)`, ED2 identity/parity rules, CHI_TIET/TONG_HOP semantics or ED2 worksheet schema in this claim.
- No Quantity builders/math/UI/commands/Health changes.
- No GitHub Actions/build/release dispatch and no BricsCAD V25/V26 runtime PASS claim.

## Validation plan

Add a focused smoke using a caller-owned `IReadOnlyList<QuantityReportRow>` that permits one indexed row read and rejects enumeration/second reads. Standard Quantity XLSX export must succeed from the detached snapshot and keep external row index reads at one.

## Coordination

Historical Quantity XLSX null/text/numeric/structural/XML hardening lanes are completed. No open PR exists at registration time. ED2 snapshot integrity is intentionally deferred to a separate collision-checked claim.

## Completion condition

Standard source fix and focused smoke source are integrated on current `main`, read back after merge, and this claim is marked `COMPLETED` with exact SHA/PR evidence and remote validation boundaries.