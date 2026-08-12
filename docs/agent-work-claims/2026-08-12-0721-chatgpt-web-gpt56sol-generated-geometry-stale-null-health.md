# Work claim — Generated Geometry Stale health null-element fail-visible

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-generated-geometry-stale-null-health-20260812-0721`
- Registered: `2026-08-12T07:21:00+07:00`
- Baseline main SHA: `a70ccd6b966fbbf18816d152f18cb0092586005b`
- Priority: evidence-driven diagnostic integrity during owner-requested `continue all`

## Confirmed defect

`GeneratedGeometryStaleHealthService.Inspect(ProjectState)` currently executes `if (element == null) continue;`. A malformed project with a null semantic element can therefore be reported clean by this provider. This is inconsistent with the repository's fail-visible health pattern: `ComprehensiveModelHealthService` deliberately converts diagnostic data failures such as `InvalidOperationException` into `HEALTH_PROVIDER_FAILED` errors instead of allowing invalid state to disappear from health output.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedGeometryStaleHealthService.cs`
- isolated focused Core smoke regression for this provider
- this claim file for close-out

## Contract

- direct stale-health inspection must reject a null semantic element instead of silently skipping it;
- the failure must occur before returning a possibly false-clean result;
- valid projects retain all existing generated-output stale warning codes/messages;
- composite health must surface the provider failure through its existing `HEALTH_PROVIDER_FAILED` handling;
- no generated-output stale semantics, CAD handles, mutation behavior, WPF, native BricsCAD code, persistence format, release/update code, or unrelated health providers are changed.

## Validation plan

Add isolated Core smoke coverage proving direct inspection fails closed for a null element and `ComprehensiveModelHealthService` returns an Error-level `HEALTH_PROVIDER_FAILED` instead of silently treating the provider as clean, while a valid stale element still returns its existing stale warning.

No GitHub Actions dispatch and no BricsCAD V25 runtime PASS claim from this web session.
