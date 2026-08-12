# Work claim — Generated Semantic Tag health null-element fail-visible

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-semantic-tag-null-health`
- Registered: `2026-08-12T07:36:00+07:00`
- Baseline main SHA: `b308710488d14669473bfa3f20d533d0184b6ee0`
- Priority: P1 — standalone generated health must not silently treat malformed ProjectState entries as clean.
- Task Key: `CORE-SEMANTIC-TAG-NULL-HEALTH`

## Confirmed defect

`GeneratedSemanticTagHealthService.Inspect(ProjectState)` currently executes `if (element == null) continue;`. A malformed project containing a null semantic element can therefore produce a false-clean result from this provider. The repository's newer fail-visible provider contract is now explicit in `GeneratedGeometryStaleHealthService`: direct inspection rejects null entries with `InvalidOperationException`, while `ComprehensiveModelHealthService.AddSafely(...)` converts the bounded diagnostic-data failure into a stable Error-level `HEALTH_PROVIDER_FAILED` issue.

The older null-safety commit for Semantic Tag predates that fail-visible contract and only prevented a null dereference; this lane updates the standalone provider to the newer diagnostic-integrity behavior.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedSemanticTagHealthService.cs`
- one focused auto-discovered `scripts/preflight-*.py` regression gate
- this claim file

Do not modify `ComprehensiveModelHealthService`, `SemanticTagRenderer`, native/runtime tag code, tag build/refresh/remove behavior, or the completed render-redaction contract.

## Intended contract

- Direct Semantic Tag health inspection must throw `InvalidOperationException` when `project.Elements` contains a null entry instead of silently skipping it.
- Valid projects retain all existing handle/ownership/template/render/size/position diagnostics.
- Composite health reuses existing `AddSafely` handling and remains fail-visible via `HEALTH_PROVIDER_FAILED` without aggregate changes.
- Inspection remains read-only.
- No GitHub Actions/build/release dispatch and no executable Core/full-build/BricsCAD runtime PASS claim from this remote lane.

## Completion condition

Standalone Semantic Tag health can no longer return clean solely because a null semantic element was skipped, focused regression coverage pins direct fail-closed behavior and aggregate compatibility, and this claim is closed after merged-main readback.
