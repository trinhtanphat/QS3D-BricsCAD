# Work claim — XLSX worksheet row-bound integrity

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T23:35:00+07:00`
- Completed: `2026-08-11T23:42:00+07:00`
- Baseline main SHA: `c7011d557c48c8b92ecc6657f9cfd9aa1b4f93d2`
- Priority: deterministic Core XLSX validity hardening

## Reason

Material, Door/Opening, and Curtain Wall XLSX exporters derived worksheet row numbers from `rows.Count` / `index + 2` without rejecting a data-row count that exceeds the worksheet row capacity after the header row is reserved. Oversized inputs could therefore progress into invalid worksheet coordinates/package generation instead of failing before indexing rows or touching the filesystem.

## Reserved scope

Fail fast on oversized data-row lists in the three Core XLSX exporters, before row enumeration and before filesystem/package writes, while preserving existing path, null-row, XML text, numeric, atomic replacement, and package validation behavior.

## Completed surfaces

- `src/QS3D.Core/Export/MaterialUsageXlsxExporter.cs`
- `src/QS3D.Core/Export/DoorOpeningXlsxExporter.cs`
- `src/QS3D.Core/Export/CurtainWallXlsxExporter.cs`
- `tests/QS3D.Core.SmokeTests/XlsxExporterRowBoundSmoke.cs`
- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs`
- this claim file

## Result

- `272ec5aa95a9ac5d42c0e77fed06bca55cfe2ce5` — Material XLSX rejects more than 1,048,575 data rows before row inspection or filesystem work.
- `3580ab8d0c1578a7374a669f0e85f8b7d5ee9b43` — Door/Opening XLSX applies the same fail-fast worksheet bound.
- `6547a70268ee3403448bbcf145a9eb1560ddc766` — Curtain Wall XLSX applies the same fail-fast worksheet bound.
- `52aa3e29148f891972fce24b90ece1395e8fe7c7` — added focused synthetic-list regression coverage for all three exporters; the fake indexer/enumerator throws if a guard inspects rows too early.
- `d5caf3a6d65fb0ededff395c55654dd5b2c155c8` — registered the focused smoke in the deterministic Core smoke suite.
- Current `main` was re-read after concurrent commits and retained all three guards, smoke content, and registration.

## Validation boundary

- Source/static verification completed against current `main`.
- The hosted shell cannot resolve GitHub for a local checkout and does not have `gh`, so no repository `dotnet` execution is claimed from this session.
- No GitHub Actions were dispatched.
- No BricsCAD V25 runtime PASS is claimed.

## Excluded scope

- No BricsCAD command/UI changes.
- No changes to XLSX XML sanitization, cell-coordinate helpers, quantity exporters, or reporting builders.

## Coordination

Recent XLSX null-row, XML-character, and cell-coordinate lanes were completed/released before this claim. No overlapping worksheet row-bound claim appeared during implementation. This lane is now released/completed.