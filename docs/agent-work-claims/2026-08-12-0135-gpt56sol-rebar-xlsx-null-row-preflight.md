# Work claim — rebar XLSX null-row preflight

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-rebar-xlsx-null-row-preflight-20260812-0135`
- Registered: `2026-08-12T01:35:00+07:00`
- Baseline main SHA: `4ccc29908eeff51857ea6bf8553d9e3fbbc0e3fc`
- Priority: evidence-driven remote-safe export atomicity hardening during owner-requested `continue all`

## Reserved scope

Fail closed on null BBS/Rebar XLSX rows before any destination path/directory/temp-package filesystem mutation, preserving existing worksheet row limits and serialization semantics.

## Expected surfaces

- `src/QS3D.Core/Export/XlsxRebarScheduleExporter.cs`
- focused Core smoke coverage under `tests/QS3D.Core.SmokeTests/`

## Excluded scope

- Completed Rebar XLSX XML sanitization and worksheet row-limit behavior.
- XLSX cell-length limits, CSV export, rebar scheduling/reporting business logic, or ownership guards.
- Active Quantity XLSX work and all native BricsCAD surfaces.
- GitHub Actions and local/V25 qualification.

## Validation plan

- Null row at any reviewed index is rejected before `Path.GetFullPath`, directory creation, temp package creation, or worksheet serialization.
- Rejection identifies the invalid row index.
- Ordinary non-null export behavior remains unchanged.
- Re-read exact PR diff and moving `main` before integration; do not dispatch Actions.

## Completion condition

Exporter + focused regression are merged to current `main`, remote source is re-read, and this claim is marked `COMPLETED` with exact integration SHA and validation boundaries.
