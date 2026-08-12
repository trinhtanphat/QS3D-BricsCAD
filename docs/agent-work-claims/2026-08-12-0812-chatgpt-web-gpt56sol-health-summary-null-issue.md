# Work claim — HealthSummary null-issue fail-closed

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-health-summary-null-issue-20260812-0812`
- Registered: `2026-08-12T08:12:00+07:00`
- Baseline main SHA: `e6b4f50de81cec00813857f946bca48e9a699c14`
- Priority: diagnostic summary integrity during owner-requested `continue all`

## Confirmed defect

`HealthSummary(IEnumerable<ModelHealthIssue>)` currently normalizes with `issues.Where(x => x != null).ToList()`. A malformed diagnostic stream containing only a null issue is therefore converted into an empty summary where `IsHealthy == true` and `IsReleaseReady == true`. This is a false-clean boundary and is inconsistent with the same constructor already rejecting undefined `HealthSeverity` values.

## Reserved scope

- `src/QS3D.Core/Diagnostics/HealthSummary.cs`
- isolated focused Core smoke regression for this value object
- this claim file for close-out

## Contract

- null diagnostic entries are rejected before summary counts/release readiness can be computed;
- valid issue sequences preserve existing `Errors`, `Warnings`, `Info`, `IsHealthy` and `IsReleaseReady` semantics;
- undefined severity rejection remains unchanged;
- no health-provider implementation, CAD mutation, persistence, WPF/native BricsCAD, updater/release packaging or unrelated diagnostic behavior changes.

## Validation plan

Add deterministic module-initializer smoke coverage proving null issue streams fail closed, clean/Info/Warning/Error summaries retain current readiness semantics, and the source no longer silently filters null diagnostics.

No GitHub Actions dispatch and no BricsCAD V25 runtime PASS claim from this web session.
