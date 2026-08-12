# Work claim — Generated Slab Mesh empty handle token fail-closed

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-slab-mesh-empty-handle-token`
- Registered: `2026-08-12T08:03:00+07:00`
- Baseline main SHA: `7c3f6b96cf3de9369c6ff1819eca6eef724972a9`
- Priority: P1 — malformed generated-handle metadata must not be silently normalized by health diagnostics.
- Task Key: `CORE-SLAB-MESH-EMPTY-HANDLE-TOKEN`

## Confirmed defect

`GeneratedSlabMeshHealthService.Inspect(ProjectState, ...)` explicitly treats `handle.Length == 0` as `INVALID_SLAB_MESH_GENERATED_HANDLE`, but its validation loop first calls `raw.Split(..., StringSplitOptions.RemoveEmptyEntries)`. Empty semicolon tokens are therefore removed before validation, making that branch unreachable for malformed metadata such as `AA;;BB`, `;AA`, or `AA;`. When `GeneratedSlabMeshCount` matches the surviving valid handles, the malformed handle-list structure can avoid both the invalid-handle diagnostic and count mismatch.

## Non-overlap check

Recent Slab Mesh work covers null-element fail-visible diagnostics and single-bind behavior; those claims are already closed. No recent claim/commit was found for empty generated-handle tokens or handle-list delimiter canonicality.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedSlabMeshHealthService.cs`
- one focused `scripts/preflight-*.py` regression gate for empty-token validation
- this claim file

Do not modify slab mesh builders, geometry/spacing/cover semantics, generated ownership policy, CAD runtime code, or unrelated diagnostics.

## Intended contract

- Preserve empty tokens while parsing `GeneratedSlabMeshHandles` for health validation so every delimiter-delimited token is inspected.
- Empty or whitespace-only tokens emit `INVALID_SLAB_MESH_GENERATED_HANDLE` and cannot be silently normalized away.
- Valid canonical handle lists retain existing behavior, including duplicate, ownership, live-solid, count, metadata, category, and stale checks.
- Ownership indexing remains unchanged; this lane only hardens health validation.
- Inspection remains read-only.
- No GitHub Actions/build/release dispatch and no BricsCAD V25 runtime PASS claim from this remote lane.

## Completion condition

Malformed leading/trailing/repeated-delimiter handle metadata is fail-visible, a focused static regression gate prevents restoring `RemoveEmptyEntries` in the validation loop, source + gate are read back from merged `main`, and this claim is closed with exact commit SHAs.
