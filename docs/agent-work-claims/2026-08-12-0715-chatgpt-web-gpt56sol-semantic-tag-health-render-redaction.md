# Work claim — semantic tag health render failure redaction

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-semantic-tag-health-render-redaction`
- Registered: `2026-08-12T07:15:00+07:00`
- Completed: `2026-08-12T07:15:00+07:00`
- Baseline main SHA: `1ee9cd3d18c30a9549ee056e3ccff838bc4d8981`
- Priority: P1 — persisted semantic-tag diagnostic failures must remain actionable without reflecting arbitrary renderer detail or swallowing non-diagnostic failures.
- Task Key: `CORE-SEMANTIC-TAG-HEALTH-RENDER-REDACTION`

## Confirmed defect

`GeneratedSemanticTagHealthService.Inspect(...)` caught every exception from `SemanticTagRenderer.Render(...)` and appended `ex.Message` verbatim to `SEMANTIC_TAG_RENDER_INVALID`. Renderer validation errors can embed persisted/imported token, property, or quantity names in exception text. Because the provider caught the exception itself, aggregate `ComprehensiveModelHealthService` provider-failure redaction could not sanitize that detail. The broad catch also converted unexpected non-diagnostic exception classes into ordinary health issues.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedSemanticTagHealthService.cs`
- `scripts/preflight-semantic-tag-health-render-redaction.py`
- this claim file

`SemanticTagRenderer`, persisted rendering semantics, BricsCAD runtime tag code, tag build/refresh/remove behavior, and the completed native runtime-health lane were not modified.

## Completed implementation

- Claim registration: `8d975b984caa4c614030bc005be83580e524e989`.
- Source fix: `4161d46d71e4f777e3024393417b93760151af32` (`fix(health): redact semantic tag render failures`).
- Focused regression gate: `05fe63693ff7f6d0be0724a2e60f56547a492b4a` (`test(health): pin semantic tag render redaction`).
- Renderer diagnostic-data failures still emit `SEMANTIC_TAG_RENDER_INVALID` with `HealthSeverity.Error`, but the issue text is stable and no longer appends `Exception.Message`.
- The catch now uses `IsDiagnosticDataFailure(...)`, bounded to `InvalidOperationException`, `ArgumentException`, `FormatException`, `OverflowException`, `KeyNotFoundException`, and `NullReferenceException`; exception classes outside that family are not swallowed by this provider.
- Existing handle/ownership/template/size/position diagnostics and read-only inspection flow remain unchanged.

## Validation actually performed

- Re-fetched merged `main` source after the fix/gate; `GeneratedSemanticTagHealthService.cs` is blob `ccef2c6bf0ec429023c3d2874ff246cc668ad945` with the filtered catch and stable redacted message.
- Re-fetched the focused gate from `main`; gate blob is `f5c70de87ae02e1f2a7644d2219b646dcf802ea9` and pins the bounded exception family, absence of raw `ex.Message`, and read-only mutation exclusions.
- Verified `SemanticTagRenderer` was not edited; its current validation can include persisted token/property/quantity identifiers in exception text, which is why provider-side redaction is required.
- One source write initially received a moving-`main` 409; HEAD and source blob were re-fetched and the patch was retried without force or overwrite.
- No GitHub Actions/build/release workflow was dispatched. No executable Core smoke, full solution build, or BricsCAD runtime PASS is claimed.

## Completion condition

Satisfied on merged source contract: semantic-tag Core health converts renderer data failures into a stable redacted Error, unexpected failures are no longer hidden by an unfiltered catch, focused regression coverage pins the contract, and this claim is closed `COMPLETED`.
