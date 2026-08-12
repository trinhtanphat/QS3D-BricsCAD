# Work claim — Slab mesh enum metadata canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-slab-mesh-enum-canonicality-20260812-1401`
- Registered: `2026-08-12T14:01:00+07:00`
- Baseline main SHA: `f1c82fe9a6a9bf6c90fd021aced9cbaa827d6508`
- Priority: owner-requested continue-all Core health integrity

## Confirmed defect

`SlabMeshSolidBuilder.CommitSemanticUpdate()` persists canonical generated tokens (`Bottom`/`Top`/`Both`, `SlabMeshXY`, and exact footprint-mode constants), but `GeneratedSlabMeshHealthService` currently uses case-insensitive comparisons and trims present footprint mode. Case-drifted or padded snapshot metadata can therefore pass health although the writer never emits it.

## Owned scope

- `src/QS3D.Core/Diagnostics/GeneratedSlabMeshHealthService.cs`
- planned regression: `tests/QS3D.Core.SmokeTests/GeneratedSlabMeshEnumCanonicalitySmoke.cs`

## Intended contract

Require exact generated enum text for Faces, Mode, and present FootprintMode. Preserve legacy compatibility where missing FootprintMode remains valid rectangle metadata. Do not mutate/normalize persisted state.

## Explicit exclusions

Slab CAD generation, numeric snapshots, handles/count, stale lifecycle, engineering policy, persistence, and unrelated health services are out of scope.
