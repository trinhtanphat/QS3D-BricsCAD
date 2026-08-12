# Work claim — Structural Wall Mesh standalone numeric handle identity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-wall-mesh-handle-identity-20260812-1341`
- Registered: `2026-08-12T13:41:00+07:00`
- Baseline main SHA: `9996ffb125f51b08f5e2d5ae6c6f6253f0763d8a`
- Priority: P0 generated ownership/health identity parity
- Task Key: `CORE-WALL-MESH-STANDALONE-HANDLE-IDENTITY`

## Confirmed defect

The shared generated-handle identity canonicalizes valid positive CAD hexadecimal identities, but `GeneratedWallMeshHealthService` still uses trimmed raw text for local duplicate/count, ownership, SourceHandles, live-handle checks and its provider-local ownership index. Numeric aliases such as `A` and `0A` can therefore represent one CAD object while being treated as distinct Structural Wall Mesh handles.

The earlier Wall Mesh count canonicality lane is `COMPLETED`; the broader rebar-family identity lane released Wall Mesh for a separate follow-up claim. Current open-PR/history checks found no Wall Mesh standalone numeric-handle identity lane.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedWallMeshHealthService.cs`
- `tests/QS3D.Core.SmokeTests/WallMeshHandleIdentitySmoke.cs`
- this claim file

## Intended contract

- Preserve existing hexadecimal validity and whitespace diagnostics.
- Normalize valid handles through `GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(...)` before duplicate/count, ownership, SourceHandles and live checks.
- Use the same normalized identity in the provider-local ownership index.
- Treat `A` and `0A` as one logical CAD object without changing persisted spelling or existing prefixed-hex validity behavior.
- Preserve count-token canonicality, numeric mesh metadata, faces/mode/category/stale behavior and all native/build code.

## Validation boundary

Focused auto-registered Core smoke + source/readback only. No GitHub Actions, full executable smoke, or licensed BricsCAD runtime PASS will be claimed unless actually executed.
