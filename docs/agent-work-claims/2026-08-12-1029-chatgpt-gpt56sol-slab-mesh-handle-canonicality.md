# Work claim — Generated Slab Mesh handle token canonicality

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-slab-mesh-handle-canonicality`
- Registered: `2026-08-12T10:29:00+07:00`
- Last Updated: `2026-08-12T10:29:00+07:00`
- Baseline main SHA: `e8d3b8d72c18bc6ed1b11345396ebdd8ae8bf6a7`
- Priority: P1 — malformed persisted generated Slab Mesh owner handles must be fail-visible instead of silently canonicalized by diagnostics
- Task Key: `CORE-SLAB-MESH-HANDLE-CANONICALITY`

## Confirmed defect

`GeneratedSlabMeshHealthService.Inspect(...)` preserves empty delimiter tokens but trims every `GeneratedSlabMeshHandles` token before validation. A persisted token such as `" A "` therefore passes as valid hex with no canonicality Error. Sibling generated-solid/rebar/Tie/Beam Stirrup diagnostics now use a consistent writer-owned contract: surrounding token whitespace is fail-visible, while downstream ownership/live lookup still uses the trimmed handle and lowercase canonical hex remains accepted.

## Coordination

Slab Mesh null-health and empty-handle-token lanes are completed. No current commit/claim search found a Slab Mesh padded-handle canonicality lane. Recent template/ownership-command work does not overlap this file.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedSlabMeshHealthService.cs`
- `tests/QS3D.Core.SmokeTests/GeneratedSlabMeshHandleCanonicalitySmoke.cs`
- this claim file

## Intended contract

- A valid non-empty Slab Mesh hex token with surrounding whitespace emits Error `SLAB_MESH_GENERATED_HANDLE_NON_CANONICAL`.
- Continue duplicate, ownership, SourceHandles, liveness, count, mesh metadata, category and stale checks using the trimmed handle.
- Lower-case canonical hex remains accepted; no casing rule is added.
- Empty/whitespace delimiter tokens continue to emit existing `INVALID_SLAB_MESH_GENERATED_HANDLE` diagnostics.
- Do not modify slab mesh planners, footprint semantics, generated ownership policy, CAD runtime code or persistence.

## Validation plan

Add an auto-registered Core smoke covering padded handle + trimmed live lookup, lowercase canonical control, and preservation of empty-token invalid diagnostics. Review exact PR diff, squash-merge guarded, read back source/test and verify ancestry.

## Validation boundary

No GitHub Actions, full build, executable smoke or licensed BricsCAD V25/V26 runtime PASS will be claimed unless actually executed.

## Completion condition

Padded generated Slab Mesh handle tokens are fail-visible without changing downstream trimmed-handle semantics, focused regression evidence is merged to current `main`, and this claim is closed with exact commit/PR evidence.
