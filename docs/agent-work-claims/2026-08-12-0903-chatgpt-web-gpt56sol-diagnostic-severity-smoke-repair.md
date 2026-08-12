# Work claim — Diagnostic summary severity smoke repair

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-diagnostic-severity-smoke-repair-20260812-0903`
- Registered: `2026-08-12T09:03:00+07:00`
- Baseline main SHA: `718f1d73095afce30452d5d3c8b50f4925f8c44f`
- Priority: P0 — current Core smoke execution is blocked by a regression fixture that now violates a strengthened constructor invariant before reaching the exporter boundary it intends to test.

## Confirmed regression

The completed diagnostic-severity integrity lane added `ModelHealthIssue` constructor validation for undefined `HealthSeverity` values and also added `ProjectDiagnosticSummarySmoke.UndefinedSeverityFailsClosedWithoutReplacingExport()`. That smoke currently constructs `new ModelHealthIssue(..., (HealthSeverity)999, ...)`, so the constructor throws before the smoke enters its intended `ProjectDiagnosticSummaryExporter.Build/Export` assertions. A current local V25 PR reports the full Core smoke run stops at this fixture.

## Reserved scope

- `tests/QS3D.Core.SmokeTests/ProjectDiagnosticSummarySmoke.cs`
- this claim file for close-out

## Contract

- Preserve the strengthened `ModelHealthIssue` constructor and exporter source guards unchanged.
- Make the smoke explicitly assert the public constructor rejects undefined severity.
- If exporter defense-in-depth coverage is retained, create a malformed fixture only through a test-local reflection helper after constructing a valid issue, so normal product construction never violates its invariant.
- Preserve the existing atomic destination-replacement assertion for exporter failure.
- No production source changes, no GitHub Actions/build/release dispatch, and no BricsCAD runtime PASS claim from this remote lane.

## Completion condition

The smoke source no longer fails prematurely at malformed public construction, retains deterministic coverage of constructor/exporter boundaries, is integrated on current `main`, and this claim is marked `COMPLETED` with exact integration SHA/evidence.
