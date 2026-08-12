# Work claim — Generated Slab Mesh health null-element fail-visible

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-slab-mesh-null-health`
- Registered: `2026-08-12T07:40:00+07:00`
- Baseline main SHA: `6ba5a7666345c4fad6fe76441a16d3e13d453792`
- Priority: P1 — standalone generated health must not silently treat malformed ProjectState entries as clean.
- Task Key: `CORE-SLAB-MESH-NULL-HEALTH`

## Confirmed defect

`GeneratedSlabMeshHealthService.Inspect(ProjectState, ...)` and its internal ownership-index traversal currently execute `if (element == null) continue;`. A malformed project containing a null semantic element can therefore be silently normalized inside the standalone provider. The repository's newer health-provider contract is fail-visible: direct generated-health inspection rejects malformed null entries with `InvalidOperationException`, while `ComprehensiveModelHealthService.AddSafely(...)` converts that bounded failure into a stable Error-level `HEALTH_PROVIDER_FAILED` issue.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedSlabMeshHealthService.cs`
- one focused auto-discovered `scripts/preflight-*.py` regression gate
- this claim file

Do not modify slab mesh builders, footprint semantics, quantity semantics, ownership policy/index, CAD runtime code, or `ComprehensiveModelHealthService`.

## Intended contract

- Direct Slab Mesh health inspection and its ownership traversal throw `InvalidOperationException` on a null project element instead of silently skipping it.
- Valid projects retain all existing handle/count/diameter/spacing/cover/faces/footprint/mode/category/stale diagnostics.
- Composite health reuses existing `AddSafely` handling and remains fail-visible via `HEALTH_PROVIDER_FAILED` without aggregate changes.
- Inspection remains read-only.
- No GitHub Actions/build/release dispatch and no executable Core/full-build/BricsCAD runtime PASS claim from this remote lane.

## Completion condition

Standalone Slab Mesh health can no longer return clean solely because a null semantic element was skipped, focused regression coverage pins direct fail-closed behavior and aggregate compatibility, and this claim is closed after merged-main readback.
