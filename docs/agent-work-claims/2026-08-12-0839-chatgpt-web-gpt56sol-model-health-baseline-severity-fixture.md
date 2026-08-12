# Work claim — Model Health baseline severity smoke reconciliation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-model-health-baseline-severity-fixture-20260812-0839`
- Registered: `2026-08-12T08:39:00+07:00`
- Baseline main SHA: `4d1a7b53d90db490c70fd02c1ab11c8ca8fc47b9`
- Priority: repair deterministic Core smoke after completed domain severity validation

## Confirmed regression

The completed Model Health baseline integrity smoke constructs `new ModelHealthIssue(..., (HealthSeverity)999, ...)` inside `Throws<InvalidOperationException>`. Current `ModelHealthIssue` now rejects undefined severities in its constructor with `ArgumentOutOfRangeException`, so the fixture no longer reaches `ModelHealthBaselineService.Capture()` and the smoke fails for the wrong reason. The concurrent health-severity fixture reservation explicitly covers `ProjectDiagnosticSummarySmoke.cs` and `HealthSummaryReadinessSmoke.cs`, not this baseline smoke.

## Reserved scope

- `tests/QS3D.Core.SmokeTests/ModelHealthBaselineSmoke.cs`
- this claim file for close-out

## Contract

- keep production `ModelHealthIssue` and `ModelHealthBaselineService` unchanged;
- create a valid diagnostic issue, then mutate only its private severity backing field in test-local reflection so baseline defense-in-depth is actually exercised;
- retain null rejection, duplicate identity, stale identity, diff and semantic read-only coverage unchanged;
- do not touch the two smoke files reserved by the concurrent health-severity fixture reconciliation.

## Validation plan

Read back the merged smoke and confirm the invalid-severity assertion reaches `Capture()` through a test-local corrupted issue rather than through an invalid constructor call.

No GitHub Actions dispatch and no BricsCAD V25/V26 runtime PASS claim from this web session.
