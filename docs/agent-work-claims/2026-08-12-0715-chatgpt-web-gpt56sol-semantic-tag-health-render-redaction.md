# Work claim — semantic tag health render failure redaction

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-semantic-tag-health-render-redaction`
- Registered: `2026-08-12T07:15:00+07:00`
- Baseline main SHA: `1ee9cd3d18c30a9549ee056e3ccff838bc4d8981`
- Priority: P1 — persisted semantic-tag diagnostic failures must remain actionable without reflecting arbitrary renderer detail or swallowing non-diagnostic failures.
- Task Key: `CORE-SEMANTIC-TAG-HEALTH-RENDER-REDACTION`

## Confirmed defect

`GeneratedSemanticTagHealthService.Inspect(...)` catches every exception from `SemanticTagRenderer.Render(...)` and appends `ex.Message` verbatim to `SEMANTIC_TAG_RENDER_INVALID`. Renderer validation errors can embed persisted/imported token, property, or quantity names in exception text (for example unsupported tokens and forbidden runtime-property keys). Because the provider catches the exception itself, aggregate `ComprehensiveModelHealthService` provider-failure redaction cannot sanitize that detail. The broad catch also converts unexpected non-diagnostic exception classes into ordinary health issues instead of allowing them to propagate.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedSemanticTagHealthService.cs`
- one focused auto-discovered `scripts/preflight-*.py` regression gate for render-failure redaction/isolation
- this claim file

Do not modify `SemanticTagRenderer`, persisted tag rendering semantics, BricsCAD runtime tag code, tag build/refresh/remove behavior, or the completed native runtime-health lane.

## Intended contract

- Preserve `SEMANTIC_TAG_RENDER_INVALID` as `HealthSeverity.Error` for diagnostic-data failures caused by invalid semantic/project/template state.
- Do not echo `Exception.Message`, template tokens, property names, or quantity names from caught renderer exceptions into health output.
- Catch only the same bounded diagnostic-data exception family used by aggregate health isolation; unexpected exception classes must not be newly swallowed.
- Preserve inspection as read-only and keep all existing handle/ownership/size/position diagnostics unchanged.
- No GitHub Actions/build/release dispatch and no executable Core or BricsCAD runtime PASS claim from this remote lane.

## Completion condition

Semantic-tag Core health converts renderer data failures into a stable redacted Error, unexpected failures are not hidden by an unfiltered catch, a focused regression gate pins the source contract, and this claim is closed after merged-main readback.
