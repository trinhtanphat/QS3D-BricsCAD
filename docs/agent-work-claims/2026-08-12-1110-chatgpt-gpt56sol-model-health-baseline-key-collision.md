# Work claim — Model health baseline key collision

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-model-health-baseline-key-collision-20260812`
- Registered: `2026-08-12T11:10:00+07:00`
- Baseline main SHA: `bb2337087efb7bc1f80213d78073721e727b4c52`
- Task Key: `CORE-MODEL-HEALTH-BASELINE-KEY-COLLISION`

## Defect

`ModelHealthBaselineService.Key` concatenates severity, code, element id and ordinary issue message with newline separators. `ModelHealthIssue` permits embedded newlines, so distinct diagnostic fields can produce the same key. Baseline capture can drop an issue and baseline compare can classify a changed issue as persistent instead of one resolved plus one new.

## Scope

- `src/QS3D.Core/Diagnostics/ModelHealthBaselineService.cs`
- `tests/QS3D.Core.SmokeTests/ModelHealthBaselineStructuralIdentitySmoke.cs`
- this claim file

## Contract

Use collision-free field encoding while preserving severity sensitivity, case-insensitive code/element identity, exact ordinary message identity, and existing `*_STALE` message-insensitive identity. Preserve existing malformed-issue validation and sorting.

No Actions/build/runtime PASS is claimed unless actually executed.
