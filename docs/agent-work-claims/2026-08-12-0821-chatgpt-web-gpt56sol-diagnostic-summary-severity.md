# Work claim — Diagnostic Summary undefined severity fail-closed

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-diagnostic-summary-severity-20260812-0821`
- Registered: `2026-08-12T08:21:00+07:00`
- Completed: `2026-08-12T08:30:00+07:00`
- Baseline main SHA: `fd6d25c1ee5c6f1d9feec6aa42b7d1887d66fb56`
- Source commit on implementation branch: `573035a2383afbb9ad9353bc087a4681ba14b8fa`
- Smoke commit on implementation branch: `949ea72652fc195ed5c800f9b0ef068b8ce16394`
- Merged PR: `#652`
- Main squash SHA: `ae104ed106e090837a86ba8a25234cbfef88b084`
- Priority: diagnostic export integrity during owner-requested `continue all`

## Confirmed defect

`ProjectDiagnosticSummaryExporter.Build(ProjectState, IEnumerable<ModelHealthIssue>)` rejected null issues, but still accepted undefined `HealthSeverity` enum values. Those rows were grouped and written to `byCode`, while the top-level `errors` / `warnings` / `info` totals only counted the three defined enum values. A malformed stream containing only an undefined severity could therefore export zero aggregate health counts and appear false-clean to consumers that rely on the summary totals. `HealthSummary` already treats undefined severity as invalid input.

## Reserved scope

- `src/QS3D.Core/Diagnostics/ProjectDiagnosticSummaryExporter.cs`
- `tests/QS3D.Core.SmokeTests/ProjectDiagnosticSummarySmoke.cs`
- this claim file

## Completed contract

- direct diagnostic-summary `Build(...)` now rejects every undefined `HealthSeverity` before aggregation or serialization;
- `Export(...)` inherits the fail-visible behavior and does not replace an existing destination when severity validation fails;
- valid Error/Warning/Info aggregation, health-code canonicalization, privacy redaction, JSON format/version and atomic replacement remain unchanged;
- existing null-issue rejection remains unchanged;
- no Comprehensive Model Health provider, CAD mutation, persistence schema, WPF/native BricsCAD, updater/release packaging or unrelated diagnostics changed.

## Validation evidence

- Claim commit was visible on `main` before implementation: `7b9d24ddd46a29ac53ea72d633a345a9a20ae000`.
- Compared implementation base `bb4e2e884ce3d397c1d17879c4b248683685ab0b` to moving `main` before PR; concurrent changes did not touch either reserved source/test file.
- PR `#652` squash-merged as `ae104ed106e090837a86ba8a25234cbfef88b084`.
- Re-fetched merged `ProjectDiagnosticSummaryExporter.cs` from `main` and confirmed `Enum.IsDefined(typeof(HealthSeverity), issue.Severity)` fail-closed validation is present before grouping.
- Re-fetched merged `ProjectDiagnosticSummarySmoke.cs` from `main` and confirmed direct Build + Export `(HealthSeverity)999` rejection and destination-preservation coverage are present.
- GitHub Actions were not manually dispatched.
- Smoke source was not executed from this web session, and no BricsCAD V25/V26 runtime PASS is claimed.
