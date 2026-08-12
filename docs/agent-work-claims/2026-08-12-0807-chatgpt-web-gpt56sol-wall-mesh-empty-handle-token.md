# Work claim — Generated Wall Mesh empty handle token fail-closed

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-wall-mesh-empty-handle-token`
- Registered: `2026-08-12T08:07:15+07:00`
- Baseline main SHA: `85daf8844c99fbb6d265e28acb80b8ebe2dc00d3`
- Priority: P1 — malformed generated-handle metadata must not be silently normalized by health diagnostics.
- Task Key: `CORE-WALL-MESH-EMPTY-HANDLE-TOKEN`

## Confirmed defect

`GeneratedWallMeshHealthService.Inspect(ProjectState, ...)` explicitly treats `handle.Length == 0` as `INVALID_WALL_MESH_GENERATED_HANDLE`, but its validation loop first calls `raw.Split(..., StringSplitOptions.RemoveEmptyEntries)`. Empty semicolon tokens are removed before validation, so malformed metadata such as `AA;;BB`, `;AA`, or `AA;` can bypass the invalid-handle branch. If `GeneratedWallMeshCount` matches the surviving valid handles, count validation does not necessarily expose the malformed delimiter structure either.

## Non-overlap check

The recent Wall Mesh null-health lane is already completed and the structural-wall-mesh single-bind lane is separate. No recent claim/commit was found for Wall Mesh empty generated-handle tokens or delimiter canonicality.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedWallMeshHealthService.cs`
- one focused `scripts/preflight-*.py` regression gate for empty-token validation
- this claim file

Do not modify wall mesh builders, geometry/spacing/cover semantics, generated ownership policy, CAD runtime code, or unrelated diagnostics.

## Intended contract

- Preserve empty tokens while parsing `GeneratedWallMeshHandles` for health validation.
- Empty or whitespace-only tokens emit `INVALID_WALL_MESH_GENERATED_HANDLE` instead of being silently removed.
- Valid canonical handle lists retain all existing duplicate, ownership, live-solid, count, metadata, category, and stale behavior.
- Ownership indexing remains unchanged; this lane only hardens health validation.
- Inspection remains read-only.
- No GitHub Actions/build/release dispatch and no BricsCAD V25 runtime PASS claim from this remote lane.

## Completion condition

Malformed leading/trailing/repeated-delimiter Wall Mesh handle metadata is fail-visible, a focused static regression gate prevents restoring `RemoveEmptyEntries` in the validation loop, source + gate are read back from merged `main`, and this claim is closed with exact commit SHAs.
