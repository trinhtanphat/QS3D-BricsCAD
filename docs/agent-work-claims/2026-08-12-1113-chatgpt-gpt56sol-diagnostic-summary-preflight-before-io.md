# Work claim — Diagnostic Summary preflight before IO

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-diagnostic-summary-preflight-before-io-20260812`
- Registered: `2026-08-12T11:13:00+07:00`
- Baseline main SHA: `428cad9e13c8ec5486aaae7f7cf3321215778221`
- Task Key: `CORE-DIAGNOSTIC-SUMMARY-PREFLIGHT-BEFORE-IO`

## Defect

`ProjectDiagnosticSummaryExporter.Export(...)` creates the destination directory and temp path before `Build(project, issues)` validates and snapshots diagnostic content. Invalid or throwing issue input can therefore fail after filesystem mutation and leave a destination directory behind even though no export was produced.

## Scope

- `src/QS3D.Core/Diagnostics/ProjectDiagnosticSummaryExporter.cs`
- `tests/QS3D.Core.SmokeTests/ProjectDiagnosticSummaryPreflightSmoke.cs`
- this claim file

## Contract

Build and validate the complete diagnostic summary before directory/temp-file mutation, then write only the stable content snapshot. Preserve path validation, JSON schema, strict UTF-8, atomic replace and temp cleanup behavior.

No GitHub Actions/full build/executable smoke/BricsCAD runtime PASS is claimed unless actually executed.
