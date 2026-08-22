# Work claim — Comprehensive Health null provider output

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-comprehensive-health-null-provider`
- Registered: `2026-08-12T08:10:00+07:00`
- Last Updated: `2026-08-12T08:10:00+07:00`
- Baseline main SHA: `99aca605d5fb73b96bac4125c6819df5b6b04353`
- Priority: P1 — malformed health-provider output must fail visible instead of silently disappearing from aggregate diagnostics.
- Task Key: `CORE-COMPREHENSIVE-HEALTH-NULL-PROVIDER-FAIL-VISIBLE`

## Confirmed defect

`ComprehensiveModelHealthService.Add(...)` currently uses `if (issue == null) continue;`. Every aggregate provider is invoked through `AddSafely(...)`, which already converts diagnostic-data `InvalidOperationException` failures into a stable `HEALTH_PROVIDER_FAILED` Error. A provider that returns a sequence containing a null issue therefore currently produces a false-clean partial result instead of a visible provider failure.

## Reserved scope

- `src/QS3D.Core/Diagnostics/ComprehensiveModelHealthService.cs`
- one focused regression source under `tests/QS3D.Core.SmokeTests/` or `scripts/`
- this claim file

## Intended contract

- Reject a null `ModelHealthIssue` from provider output deterministically instead of silently skipping it.
- Reuse the existing `AddSafely(...)` diagnostic-data boundary so the aggregate emits `HEALTH_PROVIDER_FAILED` as `HealthSeverity.Error`.
- Do not widen `IsDiagnosticDataFailure(...)` or swallow unrelated programming/infrastructure exceptions.
- Preserve duplicate suppression, generated-output targeting, handle normalization, and provider-redaction behavior.
- Add focused source-side regression coverage.
- No GitHub Actions/build/release dispatch and no BricsCAD V25 runtime PASS claim from this remote lane.

## Coordination

The earlier `CORE-COMPREHENSIVE-HEALTH-PROVIDER-REDACTION` claim is completed and released. Its concern was user-facing exception-detail redaction, not null issue output. Recent branch/commit searches did not surface an active `comprehensive-health-null` lane.

## Validation plan

- Re-read current `main` after this claim lands and abort/rebase if the reserved source changed incompatibly.
- Patch only null issue handling plus focused regression coverage.
- Review the exact PR changed-file set before merge.
- Query status only; do not dispatch workflows.
- Read merged `main` back before closing this claim.

## Completion condition

A provider sequence containing a null issue can no longer be silently omitted from Comprehensive Model Health; it yields a stable `HEALTH_PROVIDER_FAILED` Error through the existing isolation boundary, with focused regression source merged and this claim closed with exact evidence.
