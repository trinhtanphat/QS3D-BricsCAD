# Work claim — Diagnostic Summary null-issue fail-visible

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-diagnostic-summary-null-issue`
- Registered: `2026-08-12T08:15:00+07:00`
- Completed: `2026-08-12T08:17:00+07:00`
- Baseline main SHA: `99aca605d5fb73b96bac4125c6819df5b6b04353`
- Claim commit: `1e3aa68a6fa68d4b5f0eb8fdf05e0e82006ea4aa`
- Source fix: `8462d9577de021e56d028f040a3a04264636e317`
- Smoke regression: `91ae3960cc33b584719082cb451f03500cf1d769`
- Priority: P1 — privacy-safe diagnostic export must not silently turn malformed diagnostic streams into lower/clean counts.
- Task Key: `CORE-DIAGNOSTIC-SUMMARY-NULL-ISSUE`

## Confirmed defect

`ProjectDiagnosticSummaryExporter.Build(ProjectState, IEnumerable<ModelHealthIssue>)` previously started health aggregation with `issues.Where(x => x != null)`. A malformed diagnostic stream containing a null issue was silently reduced before counts were exported. `HealthSummary` already uses a fail-visible contract for the same malformed input.

## Reserved scope

- `src/QS3D.Core/Diagnostics/ProjectDiagnosticSummaryExporter.cs`
- `tests/QS3D.Core.SmokeTests/ProjectDiagnosticSummarySmoke.cs`
- this claim file

## Completed contract

- `Build(...)` materializes the supplied diagnostics once and throws `InvalidOperationException` when any issue is null.
- `Export(...)` inherits that fail-visible behavior through `Build(...)`; the focused smoke pins that an existing destination remains unchanged when malformed input is rejected.
- Existing privacy redaction, valid health aggregation/code canonicalization, JSON format/version, project null counters and successful atomic replacement remain unchanged.
- No arbitrary global diagnostic-count cap was introduced.
- Source and smoke were read back from merged `main` after their commits.
- No GitHub Actions/build/release dispatch and no BricsCAD runtime PASS claimed from this remote lane.

## Validation evidence

Readback on merged `main` confirmed the source token `Diagnostic summary cannot contain a null health issue.` and smoke method `NullIssuesFailClosedWithoutReplacingExport()` alongside the existing privacy/count and successful atomic-export coverage. Executable Core smoke was not run by this connector.
