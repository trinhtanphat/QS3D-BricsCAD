# Work claim — Comprehensive Health provider failure redaction

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-comprehensive-health-provider-redaction`
- Registered: `2026-08-12T01:33:00+07:00`
- Last Updated: `2026-08-12T01:33:00+07:00`
- Baseline main SHA: `00b05abc1149184f08d15143062e4463b11572d5`
- Priority: P1 — aggregate health diagnostics must fail visible without reflecting raw provider exception detail.
- Task Key: `CORE-COMPREHENSIVE-HEALTH-PROVIDER-REDACTION`

## Confirmed defect

`ComprehensiveModelHealthService.AddSafely(...)` intentionally converts diagnostic-data exceptions into `HEALTH_PROVIDER_FAILED`, but currently appends `ex.Message` verbatim to the user-facing issue text. Provider failures can originate from persisted/imported project state and parser/lookup paths, so aggregate health should preserve the failing provider identity without echoing arbitrary exception detail.

## Reserved scope

- `src/QS3D.Core/Diagnostics/ComprehensiveModelHealthService.cs`
- one focused Core smoke source under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Intended contract

- Keep provider isolation and `HEALTH_PROVIDER_FAILED` as `HealthSeverity.Error`.
- Keep the provider name in the message so the failed diagnostic lane remains actionable.
- Do not append raw `Exception.Message` to the health issue.
- Do not weaken `IsDiagnosticDataFailure` or swallow unexpected infrastructure/programming failures.
- Add deterministic source-level smoke coverage for stable provider-failure wording/redaction without BricsCAD dependencies.
- No GitHub Actions/build/release dispatch and no executable PASS claim from this remote lane.

## Coordination

The recent Comprehensive Health handle-normalization lane is completed and concerns live-handle normalization, not provider failure text. The Model Health recovery-redaction lane is completed and reserves a different service. Recent claim/commit searches did not surface an active provider-redaction lane.

## Validation plan

- Re-read registration rules and current source after this claim reaches `main`.
- Recheck current claim/commit activity for overlap.
- Patch only aggregate provider-failure message construction plus focused regression source.
- Review exact PR changed-file set before merge.
- Query PR-head status without dispatching workflows.
- Read merged `main` source back before closing this claim.

## Completion condition

Aggregate health still reports provider data failures as Error with provider identity, raw exception detail is absent from the emitted issue, focused regression source is merged, and the claim is closed with exact evidence.