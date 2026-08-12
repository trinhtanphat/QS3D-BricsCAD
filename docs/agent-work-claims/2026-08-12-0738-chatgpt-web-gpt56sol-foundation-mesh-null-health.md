# Work claim — Generated Foundation Mesh health null-element fail-visible

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-foundation-mesh-null-health`
- Registered: `2026-08-12T07:38:00+07:00`
- Baseline main SHA: `7f3d6d910a405a40829b0391ac1f77280c6feff1`
- Priority: P1 — standalone generated health must not silently treat malformed ProjectState entries as clean.
- Task Key: `CORE-FOUNDATION-MESH-NULL-HEALTH`

## Confirmed defect

`GeneratedFoundationMeshHealthService.Inspect(ProjectState, ...)` currently executes `if (element == null) continue;`. A malformed project containing a null semantic element can therefore produce a false-clean result from this standalone provider. The repository's newer provider contract is fail-visible: direct generated-health inspection rejects malformed null entries with `InvalidOperationException`, while `ComprehensiveModelHealthService.AddSafely(...)` converts that bounded diagnostic-data failure into a stable Error-level `HEALTH_PROVIDER_FAILED` issue.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedFoundationMeshHealthService.cs`
- one focused auto-discovered `scripts/preflight-*.py` regression gate
- this claim file

Do not modify foundation mesh builders, quantity semantics, ownership policy/index, CAD runtime code, or `ComprehensiveModelHealthService`.

## Intended contract

- Direct Foundation Mesh health inspection throws `InvalidOperationException` on a null project element instead of silently skipping it.
- Valid projects retain all existing handle/count/diameter/spacing/cover/faces/mode/category/stale diagnostics.
- Composite health reuses existing `AddSafely` handling and remains fail-visible via `HEALTH_PROVIDER_FAILED` without aggregate changes.
- Inspection remains read-only.
- No GitHub Actions/build/release dispatch and no executable Core/full-build/BricsCAD runtime PASS claim from this remote lane.

## Completion condition

Standalone Foundation Mesh health can no longer return clean solely because a null semantic element was skipped, focused regression coverage pins direct fail-closed behavior and aggregate compatibility, and this claim is closed after merged-main readback.
