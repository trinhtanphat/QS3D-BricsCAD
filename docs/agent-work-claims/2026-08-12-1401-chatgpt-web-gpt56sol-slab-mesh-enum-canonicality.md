# Work claim — Slab mesh enum metadata canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-slab-mesh-enum-canonicality-20260812-1401`
- Registered: `2026-08-12T14:01:00+07:00`
- Completed: `2026-08-12T14:02:00+07:00`
- Baseline main SHA: `f1c82fe9a6a9bf6c90fd021aced9cbaa827d6508`
- Claim commit: `c8920ab490b6c6826fca87e07e4c0bba22d8c519`
- Product fix: `6d53dce3e8b85ff04105696dc2a229985c01f3e7`
- Regression commit: `f0c2d97ae57dc196caac5c3edd83c97cfd5306e2`
- Priority: owner-requested continue-all Core health integrity

## Confirmed defect

`SlabMeshSolidBuilder.CommitSemanticUpdate()` persists canonical generated tokens (`Bottom`/`Top`/`Both`, `SlabMeshXY`, and exact footprint-mode constants), but `GeneratedSlabMeshHealthService` used case-insensitive comparisons and trimmed present footprint mode. Case-drifted or padded snapshot metadata could therefore pass health although the writer never emits it.

## Implemented contract

Faces accepts only exact `Bottom`, `Top`, or `Both`; Mode accepts only exact `SlabMeshXY`; present FootprintMode accepts only exact `RectangleLocalXY` or `PolygonGlobalXY`. Missing FootprintMode remains valid for legacy rectangle-only metadata. Health does not normalize or mutate persisted state.

## Regression coverage

`GeneratedSlabMeshEnumCanonicalitySmoke` is auto-registered and covers canonical enum metadata, legacy missing FootprintMode compatibility, lowercase Faces, lowercase Mode, and padded FootprintMode with the existing warning/error severities.

## Explicit exclusions

Slab CAD generation, numeric snapshots, handles/count, stale lifecycle, engineering policy, persistence, and unrelated health services are out of scope.

## Validation boundary

Product and regression sources were read back from `main`; regression commit `f0c2d97ae57dc196caac5c3edd83c97cfd5306e2` was current `main` when closure began. No GitHub Actions, full build, executable smoke, release, or licensed BricsCAD V25/V26 runtime PASS is claimed.
