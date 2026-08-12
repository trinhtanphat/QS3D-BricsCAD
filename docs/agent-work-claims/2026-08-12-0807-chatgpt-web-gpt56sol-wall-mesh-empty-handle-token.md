# Work claim — Generated Wall Mesh empty handle token fail-closed

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-wall-mesh-empty-handle-token`
- Registered: `2026-08-12T08:07:15+07:00`
- Completed: `2026-08-12T08:09:14+07:00`
- Baseline main SHA: `85daf8844c99fbb6d265e28acb80b8ebe2dc00d3`
- Priority: P1 — malformed generated-handle metadata must not be silently normalized by health diagnostics.
- Task Key: `CORE-WALL-MESH-EMPTY-HANDLE-TOKEN`

## Confirmed defect

`GeneratedWallMeshHealthService.Inspect(ProjectState, ...)` explicitly treats `handle.Length == 0` as `INVALID_WALL_MESH_GENERATED_HANDLE`, but its validation loop previously called `raw.Split(..., StringSplitOptions.RemoveEmptyEntries)`. Empty semicolon tokens were removed before validation, so malformed metadata such as `AA;;BB`, `;AA`, or `AA;` could bypass the invalid-handle branch. If `GeneratedWallMeshCount` matched the surviving valid handles, count validation did not necessarily expose the malformed delimiter structure either.

## Non-overlap check

The recent Wall Mesh null-health lane was already completed and the structural-wall-mesh single-bind lane is separate. No recent claim/commit was found for Wall Mesh empty generated-handle tokens or delimiter canonicality before this lane was registered.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedWallMeshHealthService.cs`
- `scripts/preflight-wall-mesh-empty-handle-token.py`
- this claim file

No wall mesh builders, geometry/spacing/cover semantics, generated ownership policy, CAD runtime code, or unrelated diagnostics were modified.

## Implemented contract

- The Wall Mesh health validation loop now uses `StringSplitOptions.None`, preserving leading/trailing/repeated-delimiter empty tokens for validation.
- Empty or whitespace-only tokens flow through the existing `handle.Length == 0` branch and emit `INVALID_WALL_MESH_GENERATED_HANDLE` instead of being silently removed.
- Valid canonical handle lists retain existing duplicate, ownership, live-solid, count, metadata, category, and stale behavior.
- Ownership indexing remains unchanged and may continue normalizing empty segments because validation reports them independently.
- Inspection remains read-only.

## Completion evidence

- Claim commit: `639444e2beadb20cf6233ec548b78d1edf5b2928`
- Source fix: `0d5ea92643741c0396ee4009fb071b1efa5f8fa8`
- Focused preflight regression: `7578ec3638600c76beb54c20223a82ed37a759b2`
- Merged-main readback confirmed the source validation loop contains `StringSplitOptions.None` with the existing empty-handle invalid branch.
- Merged-main readback confirmed `scripts/preflight-wall-mesh-empty-handle-token.py` exists and forbids restoring `RemoveEmptyEntries` in the validation loop.
- `7578ec3638600c76beb54c20223a82ed37a759b2` is an ancestor of refreshed `main`; concurrent commits after it did not overwrite this lane.
- GitHub Actions/build/release were not dispatched. No BricsCAD V25 runtime PASS is claimed from this remote lane.

## Result

`COMPLETED`: malformed leading/trailing/repeated-delimiter `GeneratedWallMeshHandles` metadata is now fail-visible in standalone Wall Mesh health diagnostics, with a focused regression gate protecting the parser contract.
