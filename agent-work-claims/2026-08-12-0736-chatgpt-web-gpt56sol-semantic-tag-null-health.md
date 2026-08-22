# Work claim — Generated Semantic Tag health null-element fail-visible

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-semantic-tag-null-health`
- Registered: `2026-08-12T07:36:00+07:00`
- Completed: `2026-08-12T07:36:00+07:00`
- Baseline main SHA: `b308710488d14669473bfa3f20d533d0184b6ee0`
- Priority: P1 — standalone generated health must not silently treat malformed ProjectState entries as clean.
- Task Key: `CORE-SEMANTIC-TAG-NULL-HEALTH`

## Confirmed defect

`GeneratedSemanticTagHealthService.Inspect(ProjectState)` executed `if (element == null) continue;`. A malformed project containing a null semantic element could therefore produce a false-clean result from this provider. The repository's newer fail-visible provider contract is explicit in `GeneratedGeometryStaleHealthService`: direct inspection rejects null entries with `InvalidOperationException`, while `ComprehensiveModelHealthService.AddSafely(...)` converts the bounded diagnostic-data failure into a stable Error-level `HEALTH_PROVIDER_FAILED` issue.

The older null-safety commit for Semantic Tag predated that fail-visible contract and only prevented a null dereference; this lane updates the standalone provider to the newer diagnostic-integrity behavior.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedSemanticTagHealthService.cs`
- `scripts/preflight-semantic-tag-null-health.py`
- this claim file

`ComprehensiveModelHealthService`, `SemanticTagRenderer`, native/runtime tag code, tag build/refresh/remove behavior, and the completed render-redaction contract were not modified.

## Completed implementation

- Claim registration: `25eabe095a07291b83cc52ad4a5f0e05134bf557`.
- Source fix: `5e30f03a7128d420b43e3ec9327cc30f349621c5` (`fix(health): fail visible on null semantic tag entries`).
- Focused regression gate: `603700c47f912797c9cea1ae192e2a94957ca04f` (`test(health): pin semantic tag null fail-visible`).
- Direct `GeneratedSemanticTagHealthService.Inspect(...)` now throws `InvalidOperationException` when a null project element is encountered instead of silently skipping it.
- Existing semantic-tag handle/ownership/template/render/size/position diagnostics and the filtered render-failure redaction contract remain unchanged.
- Composite health is unchanged and continues to register this provider through `AddSafely`, whose diagnostic-data filter includes `InvalidOperationException` and emits stable `HEALTH_PROVIDER_FAILED` Error diagnostics.

## Validation actually performed

- Re-fetched current `main` source after source/gate commits; relevant source blob is `04324ebe159f6286bdf3c7e9400b7aa6ce0dd5a4` with direct null fail-closed behavior and existing render-redaction logic intact.
- Re-fetched focused gate from `main`; gate blob is `b9fac8ed109bcc874d8e3e3654954ca2f661770f` and pins direct null rejection plus aggregate provider registration/`HEALTH_PROVIDER_FAILED` compatibility.
- One gate create initially received a moving-`main` 409; HEAD was refreshed and the file was created without force or overwrite.
- No GitHub Actions/build/release workflow was dispatched. No executable Core smoke, full solution build, or BricsCAD runtime PASS is claimed.

## Completion condition

Satisfied on merged source contract: standalone Semantic Tag health can no longer return clean solely because a null semantic element was skipped, focused regression coverage pins direct fail-closed behavior and aggregate compatibility, and this claim is closed `COMPLETED`.
