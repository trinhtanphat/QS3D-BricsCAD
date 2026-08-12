# Work claim — Generated Grid Annotation health null-element fail-visible

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-grid-annotation-null-health`
- Registered: `2026-08-12T07:47:00+07:00`
- Baseline main SHA: `0696f3cbcf602e140c3cad23282160641f2e659d`
- Priority: P1 — standalone generated health must not silently treat malformed ProjectState entries as clean.
- Task Key: `CORE-GRID-ANNOTATION-NULL-HEALTH`

## Confirmed defect

`GeneratedGridAnnotationHealthService.Inspect(ProjectState)` still executes `if (element == null) continue;`. A malformed project containing a null semantic element can therefore produce a false-clean result from this standalone provider. Newer generated-health lanes use a fail-visible contract: direct inspection rejects malformed null entries with `InvalidOperationException`, while `ComprehensiveModelHealthService.AddSafely(...)` converts that bounded failure into a stable Error-level `HEALTH_PROVIDER_FAILED` issue.

The historical null-safe commit only avoided a null dereference; the newer diagnostic-integrity policy supersedes silent normalization for standalone health providers.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedGridAnnotationHealthService.cs`
- one focused auto-discovered `scripts/preflight-*.py` regression gate
- this claim file

Do not modify GridAnnotationBuilder, Grid naming, BricsCAD runtime Grid health, annotation regeneration, or `ComprehensiveModelHealthService`.

## Intended contract

- Direct Grid Annotation health inspection throws `InvalidOperationException` on a null project element instead of silently skipping it.
- Valid projects retain all existing handle/count/mode/placement diagnostics.
- Composite health reuses existing `AddSafely` handling and remains fail-visible via `HEALTH_PROVIDER_FAILED` without aggregate changes.
- Inspection remains read-only.
- No GitHub Actions/build/release dispatch and no executable Core/full-build/BricsCAD runtime PASS claim from this remote lane.

## Completion condition

Standalone Grid Annotation health can no longer return clean solely because a null semantic element was skipped, focused regression coverage pins direct fail-closed behavior and aggregate compatibility, and this claim is closed after merged-main readback.
