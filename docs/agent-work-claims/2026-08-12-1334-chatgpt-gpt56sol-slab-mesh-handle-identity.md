# Work claim — Slab Mesh standalone numeric handle identity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-slab-mesh-handle-identity-20260812-1334`
- Registered: `2026-08-12T13:34:00+07:00`
- Baseline main SHA: `fe7f89ed8c3925f2b247ddd81f763479c8c89355`
- Priority: P0 generated ownership/health identity parity
- Task Key: `CORE-SLAB-MESH-STANDALONE-HANDLE-IDENTITY`

## Confirmed defect

`GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(...)` canonicalizes valid positive CAD hexadecimal identities, but `GeneratedSlabMeshHealthService` still uses trimmed raw text for local duplicate/count, ownership, SourceHandles, live-handle checks and its provider-local ownership index. Numeric aliases such as `A` and `0A` can therefore represent one CAD object while being treated as distinct Slab Mesh handles.

The earlier Slab Mesh count canonicality lane is `COMPLETED`; the broader rebar-family identity lane released Slab Mesh for a separate follow-up claim. Current open-PR/history checks found no Slab Mesh standalone numeric-handle identity lane.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedSlabMeshHealthService.cs`
- `tests/QS3D.Core.SmokeTests/SlabMeshHandleIdentitySmoke.cs`
- this claim file

## Intended contract

- Preserve existing hexadecimal validity and whitespace diagnostics.
- Normalize valid handles through `GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(...)` before duplicate/count, ownership, SourceHandles and live checks.
- Use the same normalized identity in the provider-local ownership index.
- Treat `A` and `0A` as one logical CAD object without changing persisted spelling or `0x` validity behavior.
- Preserve count-token canonicality, numeric mesh metadata, faces/mode/footprint/category/stale behavior and all native/build code.

## Validation boundary

Focused auto-registered Core smoke + source/readback only. No GitHub Actions, full executable smoke, or licensed BricsCAD runtime PASS will be claimed unless actually executed.
