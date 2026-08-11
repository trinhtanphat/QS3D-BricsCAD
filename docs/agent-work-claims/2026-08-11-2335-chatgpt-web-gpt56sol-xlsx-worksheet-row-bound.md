# Work claim — XLSX worksheet row-bound integrity

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T23:35:00+07:00`
- Baseline main SHA: `c7011d557c48c8b92ecc6657f9cfd9aa1b4f93d2`
- Priority: deterministic Core XLSX validity hardening

## Reason

Material, Door/Opening, and Curtain Wall XLSX exporters derive worksheet row numbers from `rows.Count` / `index + 2` without rejecting a data-row count that exceeds the worksheet row capacity after the header row is reserved. Oversized inputs can therefore progress into invalid worksheet coordinates/package generation instead of failing before indexing rows or touching the filesystem.

## Reserved scope

Fail fast on oversized data-row lists in the three Core XLSX exporters, before row enumeration and before filesystem/package writes, while preserving existing path, null-row, XML text, numeric, atomic replacement, and package validation behavior.

## Expected surfaces

- `src/QS3D.Core/Export/MaterialUsageXlsxExporter.cs`
- `src/QS3D.Core/Export/DoorOpeningXlsxExporter.cs`
- `src/QS3D.Core/Export/CurtainWallXlsxExporter.cs`
- `tests/QS3D.Core.SmokeTests/XlsxExporterRowBoundSmoke.cs`
- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs`
- this claim file

## Excluded scope

- No BricsCAD command/UI changes.
- No changes to XLSX XML sanitization, cell-coordinate helpers, quantity exporters, or reporting builders.
- No GitHub Actions dispatch and no BricsCAD V25 runtime PASS claim.

## Validation plan

- Add a shared exporter-local row guard equivalent to one header row plus the maximum legal data rows.
- Add focused smoke coverage using synthetic `IReadOnlyList<T>` instances whose `Count` is oversized and whose indexer/enumerator throw, proving rejection occurs before row inspection and before output creation.
- Re-fetch current `main`, claim registry, and every target blob immediately before writes; never force-push.
- Preserve concurrent smoke registrations when adding the focused smoke.

## Coordination

Recent XLSX null-row, XML-character, and cell-coordinate lanes are completed/released. No current claim/search result matched worksheet row-bound scope at registration time.

## Completion condition

All three exporters fail closed on oversized row lists before row enumeration/file creation, focused regression coverage is registered, current `main` is re-read, and this claim is marked `COMPLETED`.