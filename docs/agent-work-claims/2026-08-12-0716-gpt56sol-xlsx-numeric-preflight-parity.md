# Work claim — XLSX numeric preflight parity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-xlsx-numeric-preflight-parity-20260812-0716`
- Registered: `2026-08-12T07:16:00+07:00`
- Baseline main SHA: `5a9f54e256954e69ff1fc5ca0366ad46eaa29707`
- Priority: evidence-driven remote-safe export atomicity hardening during owner-requested `continue all`

## Reserved scope

Fail closed on non-finite numeric cells before path resolution, directory creation, temp-package creation, or worksheet serialization for the direct schedule XLSX exporters that currently defer `NaN`/`Infinity` rejection to `NumberCell`/`AppendNumber` during package construction.

## Expected surfaces

- `src/QS3D.Core/Export/DoorOpeningXlsxExporter.cs`
- `src/QS3D.Core/Export/MaterialUsageXlsxExporter.cs`
- `src/QS3D.Core/Export/CurtainWallXlsxExporter.cs`
- `src/QS3D.Core/Export/RoomFinishXlsxExporter.cs`
- `src/QS3D.Core/Export/XlsxRebarScheduleExporter.cs`
- focused Core smoke coverage under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Excluded scope

- `XlsxQuantityExporter.cs`, which remains reserved by active claim `2026-08-12-0127-gpt56sol-quantity-xlsx-structural-limits.md`.
- XML text sanitization, text-cell limits, worksheet row limits, null-row handling, reporting/grouping/business rules, sign/domain validation beyond the exporters' existing finite-number contract, or shared XLSX package validation.
- Native BricsCAD/UI/runtime work, GitHub Actions, release packaging, or LOCAL_ONLY qualification.

## Validation plan

- Every numeric value emitted by each owned exporter is checked for `NaN`/`Infinity` during existing row preflight.
- Failure identifies `rows`, worksheet row and field, and occurs before any destination directory/file/temp-package mutation.
- Existing ordinary finite-value export behavior, row/text/null/XML guards and package validation remain unchanged.
- Add focused module-initializer smoke coverage for pre-filesystem rejection and at least one ordinary finite export path per owned exporter.
- Re-read exact branch diff and current `main` immediately before integration; do not dispatch Actions.

## Coordination

No open PRs exist at registration time. Quantity XLSX remains explicitly excluded because its older `ACTIVE` claim still reserves that exporter; repository takeover rules prohibit treating age alone as release.

## Completion condition

All five owned exporters and focused regression coverage are merged to current `main`, remote source is re-read, and this claim is marked `COMPLETED` with exact integration SHA and validation boundaries.
