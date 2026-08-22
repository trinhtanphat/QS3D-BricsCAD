# Work claim — Model Health baseline severity smoke reconciliation

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-model-health-baseline-severity-fixture-20260812-0839`
- Registered: `2026-08-12T08:39:00+07:00`
- Completed: `2026-08-12T08:47:00+07:00`
- Baseline main SHA: `4d1a7b53d90db490c70fd02c1ab11c8ca8fc47b9`
- Initial test commit: `1d7845be7337b2924059541a8d229c6be2bbfc23`
- Initial PR `#657`: closed unmerged after `main` advanced under the branch.
- Rebased test commit: `7547a34dd8d6f407240c4aef885207a6fa9b53e1`
- Moving-main reconciliation head: `d802bfaed540fb79c9b7637aa26c29b672d55e6e`
- Merged PR: `#659`
- Main squash SHA: `6bdeb6b52be262ae523c98da7b3db0e43303bcb0`
- Priority: repair deterministic Core smoke after completed domain severity validation

## Confirmed regression

The completed Model Health baseline integrity smoke constructed `new ModelHealthIssue(..., (HealthSeverity)999, ...)` inside `Throws<InvalidOperationException>`. Current `ModelHealthIssue` rejects undefined severities in its constructor with `ArgumentOutOfRangeException`, so the fixture no longer reached `ModelHealthBaselineService.Capture()` and the smoke failed for the wrong reason. The concurrent health-severity fixture reservation explicitly covered `ProjectDiagnosticSummarySmoke.cs` and `HealthSummaryReadinessSmoke.cs`, not this baseline smoke.

## Reserved scope

- `tests/QS3D.Core.SmokeTests/ModelHealthBaselineSmoke.cs`
- this claim file

## Completed contract

- production `ModelHealthIssue` and `ModelHealthBaselineService` remain unchanged;
- the smoke now creates a valid diagnostic issue and mutates only its private severity backing field with test-local reflection so baseline defense-in-depth is exercised;
- null rejection, duplicate identity, stale identity, diff and semantic read-only coverage remain unchanged;
- the two smoke files reserved by the concurrent health-severity fixture reconciliation were not touched.

## Validation evidence

- Claim commit was visible on `main`: `642beb0ef5ea89e359bc0ef03a97900693eb7378`.
- The concurrent fixture reservation `3702743fa1b0067d8a6955492d071aa8f72ebd0e` explicitly reserved only `ProjectDiagnosticSummarySmoke.cs` and `HealthSummaryReadinessSmoke.cs`, so this lane remained non-overlapping.
- PR `#657` was closed unmerged when protected `main` advanced. The patch was rebased onto newer `main` without force-push through PR `#659` and reconciled again with moving `main` before merge.
- PR `#659` squash-merged as `6bdeb6b52be262ae523c98da7b3db0e43303bcb0`.
- Re-fetched merged `ModelHealthBaselineSmoke.cs` from `main` and confirmed `System.Reflection`, `CorruptSeverity(...)`, and valid-constructor-then-corrupt severity coverage are present.
- GitHub Actions were not manually dispatched.
- The smoke source was not executed from this web session, and no BricsCAD V25/V26 runtime PASS is claimed.
