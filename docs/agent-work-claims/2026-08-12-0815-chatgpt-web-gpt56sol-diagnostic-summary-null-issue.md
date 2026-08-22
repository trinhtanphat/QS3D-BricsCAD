# Work claim — Diagnostic Summary null-issue fail-visible

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-diagnostic-summary-null-issue`
- Registered: `2026-08-12T08:15:00+07:00`
- Baseline main SHA: `99aca605d5fb73b96bac4125c6819df5b6b04353`
- Priority: P1 — privacy-safe diagnostic export must not silently turn malformed diagnostic streams into lower/clean counts.
- Task Key: `CORE-DIAGNOSTIC-SUMMARY-NULL-ISSUE`

## Confirmed defect

`ProjectDiagnosticSummaryExporter.Build(ProjectState, IEnumerable<ModelHealthIssue>)` currently starts the health aggregation with `issues.Where(x => x != null)`. A malformed diagnostic stream containing a null issue is therefore silently reduced before counts are exported. `HealthSummary` has just adopted the fail-visible contract for the same malformed input: null diagnostic issues are rejected instead of disappearing from readiness/count calculations.

## Reserved scope

- `src/QS3D.Core/Diagnostics/ProjectDiagnosticSummaryExporter.cs`
- `tests/QS3D.Core.SmokeTests/ProjectDiagnosticSummarySmoke.cs`
- this claim file

Do not change privacy/redaction fields, JSON format/version, atomic file replacement, project null-element counters, Comprehensive Model Health, or introduce a new global diagnostic-count capacity in this lane.

## Intended contract

- Direct `Build(...)` rejects a null diagnostic issue with `InvalidOperationException` instead of skipping it.
- `Export(...)` inherits the same fail-visible behavior through `Build(...)` and does not replace the destination with a false-clean summary.
- Valid diagnostic aggregation, code canonicalization, privacy redaction, and atomic replacement remain unchanged.
- No arbitrary enumerable cap is introduced without an established repository contract.
- No GitHub Actions/build/release dispatch and no BricsCAD runtime PASS claim from this remote lane.

## Completion condition

Focused Core smoke covers null issue rejection plus existing valid privacy/count and atomic export behavior; source/test are read back from merged `main`, then this claim is closed.
