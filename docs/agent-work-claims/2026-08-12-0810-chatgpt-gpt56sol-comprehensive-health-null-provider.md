# Work claim — Comprehensive Health null provider output

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-comprehensive-health-null-provider`
- Registered: `2026-08-12T08:10:00+07:00`
- Completed: `2026-08-12T08:19:00+07:00`
- Last Updated: `2026-08-12T08:19:00+07:00`
- Baseline main SHA: `99aca605d5fb73b96bac4125c6819df5b6b04353`
- Priority: P1 — malformed health-provider output must fail visible instead of silently disappearing from aggregate diagnostics.
- Task Key: `CORE-COMPREHENSIVE-HEALTH-NULL-PROVIDER-FAIL-VISIBLE`

## Confirmed defect

`ComprehensiveModelHealthService.Add(...)` used `if (issue == null) continue;`. Every aggregate provider is invoked through `AddSafely(...)`, which already converts diagnostic-data `InvalidOperationException` failures into a stable `HEALTH_PROVIDER_FAILED` Error. A provider sequence containing a null issue could therefore produce a false-clean partial result instead of a visible provider failure.

## Reserved scope

- `src/QS3D.Core/Diagnostics/ComprehensiveModelHealthService.cs`
- `tests/QS3D.Core.SmokeTests/ComprehensiveHealthNullProviderSmoke.cs`
- this claim file

## Implemented contract

- A null `ModelHealthIssue` from provider output now throws a deterministic `InvalidOperationException` from `Add(...)` instead of being silently skipped.
- Existing `AddSafely(...)` catches that diagnostic-data failure and emits `HEALTH_PROVIDER_FAILED` as `HealthSeverity.Error`.
- `IsDiagnosticDataFailure(...)` was not widened.
- Duplicate suppression, generated-output targeting, handle normalization, and provider-redaction behavior were left unchanged.
- Added focused auto-registered Core smoke source that feeds a null issue through `AddSafely(...)` and requires a provider-identified `HEALTH_PROVIDER_FAILED` Error.

## Coordination

The earlier `CORE-COMPREHENSIVE-HEALTH-PROVIDER-REDACTION` claim was already completed and released. During implementation, `main` advanced concurrently on unrelated XLSX, reporting, documentation, regeneration, curtain schedule, and rebar-health paths; comparison from the implementation branch base confirmed no overlap with the two changed files.

## Validation / evidence

- Claim registration commit on `main`: `812522248dabdf9e1ddd5587da27bf5efbd325fe`.
- Implementation branch commits: `f1d96ed83b87df7f640c6f2dbd0206e0bb36b0a6`, `39dc9ef5e4843e8bd41fbfb47ddb26ebabc0bc26`, and newline-cleanup `604d9d306e6b2e8bd047db516a6622f5cf73f32b`.
- PR #641 changed exactly `ComprehensiveModelHealthService.cs` and `ComprehensiveHealthNullProviderSmoke.cs`; the exact patch was reviewed before merge.
- PR-head combined-status query returned no statuses. No GitHub Actions/build/release workflow was dispatched and no executable smoke/build PASS is claimed from this remote session.
- PR #641 was squash-merged to `main` as `b98b977da70bd630d7df3b95b5b88b6cbba052ce`.
- Post-merge `main` readback confirmed the null issue now throws `InvalidOperationException` and the focused smoke source is present.

## Result

Comprehensive Model Health no longer silently drops null issue entries from a provider. Malformed provider output fails visible through the existing provider-isolation boundary as `HEALTH_PROVIDER_FAILED`, and this source-side lane is complete and released.
