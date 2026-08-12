# Work claim — Diagnostic Summary undefined severity fail-closed

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-diagnostic-summary-severity-20260812-0821`
- Registered: `2026-08-12T08:21:00+07:00`
- Baseline main SHA: `fd6d25c1ee5c6f1d9feec6aa42b7d1887d66fb56`
- Priority: diagnostic export integrity during owner-requested `continue all`

## Confirmed defect

`ProjectDiagnosticSummaryExporter.Build(ProjectState, IEnumerable<ModelHealthIssue>)` now rejects null issues, but still accepts undefined `HealthSeverity` enum values. Those rows are grouped and written to `byCode`, while the top-level `errors` / `warnings` / `info` totals only count the three defined enum values. A malformed stream containing only an undefined severity can therefore export zero aggregate health counts and appear false-clean to consumers that rely on the summary totals. `HealthSummary` already treats undefined severity as invalid input.

## Reserved scope

- `src/QS3D.Core/Diagnostics/ProjectDiagnosticSummaryExporter.cs`
- `tests/QS3D.Core.SmokeTests/ProjectDiagnosticSummarySmoke.cs`
- this claim file for close-out

## Contract

- direct diagnostic-summary `Build(...)` rejects every undefined `HealthSeverity` before aggregation or serialization;
- `Export(...)` inherits the fail-visible behavior and must not replace an existing destination when severity validation fails;
- valid Error/Warning/Info aggregation, health-code canonicalization, privacy redaction, JSON format/version and atomic replacement remain unchanged;
- no changes to Comprehensive Model Health providers, CAD mutation, persistence schema, WPF/native BricsCAD, updater/release packaging or unrelated diagnostics.

## Validation plan

Extend the existing deterministic `ProjectDiagnosticSummarySmoke` to prove undefined severity fails closed for direct Build and Export, while the existing valid privacy/count and atomic replacement scenarios remain intact.

No GitHub Actions dispatch and no BricsCAD V25/V26 runtime PASS claim from this web session.
