# QS3D Direct Draw — UCS contract

Updated: 2026-08-10 (UTC+7)

## Scope

This note records the coordinate-system contract for `QS3DDRAWWALL`, `QS3DDRAWBEAM`, `QS3DDRAWCOLUMN` and `QS3DDRAWSLAB`.

Direct Draw runs inside BricsCAD. Point prompts follow the current editor UCS, while persistent DWG/database geometry must be created in the database/world coordinate context expected by the existing QS3D native builders.

## Implemented source behavior

`DirectDrawCommands` now follows this boundary:

1. Require Model Space.
2. Read `Editor.CurrentUserCoordinateSystem`.
3. Require the UCS XY plane to remain parallel to WCS XY. Translation and in-plane rotation are allowed; tilted/3D UCS is rejected before any Direct Draw source entity is created.
4. Acquire and validate prompt points in current-UCS coordinates. The existing 5 mm planarity rule therefore remains relative to the author's working UCS plane.
5. Build the temporary/persistent source `LINE` or `POLYLINE` in UCS-local coordinates.
6. Call `TransformBy(document.Editor.CurrentUserCoordinateSystem)` before appending the entity to Model Space.
7. Continue through the existing atomic source -> semantic capture -> semantic regeneration -> native 3D builder workflow.

Column footprints are also built in current-UCS local X/Y around the picked center and pass through the same POLYLINE transform, so a planar rotated UCS rotates the rectangular footprint with the user's working axes instead of keeping it aligned to WCS X/Y.

World UCS remains an identity transform.

## Fail-closed boundary

Tilted/3D UCS is intentionally not generalized in this batch. The current wall/structure native builders still contain WCS planarity assumptions and their real Boolean/extrusion behavior must be proven before arbitrary UCS planes are accepted.

Direct Draw therefore rejects a UCS whose normalized Z axis is not aligned with positive WCS Z. It does not mutate or reset the user's UCS.

This is preferable to accepting a tilted UCS and silently creating a source/solid in an unintended plane.

## Static guard

`scripts/preflight-direct-draw-ucs.py` requires:

- the Model-Space -> UCS guard ordering;
- `CurrentUserCoordinateSystem.CoordinateSystem3d` inspection;
- explicit tilted-UCS rejection;
- LINE transform before `AppendEntity`;
- POLYLINE transform before `AppendEntity`;
- no assignment that resets `CurrentUserCoordinateSystem`.

The aggregate `scripts/preflight-all.py` auto-discovers this guard.

## Runtime gate still required

Source implementation is not runtime certification. Licensed BricsCAD V25 validation still needs at least:

- World UCS baseline;
- translated UCS;
- 30/45/90-degree planar rotated UCS;
- Wall/Beam/Column/Slab creation in each supported planar UCS;
- generated solid direction, source Handle provenance and semantic length/area checks;
- cancel/failure rollback;
- save/reopen/regenerate;
- tilted UCS rejection before source creation;
- screenshot/selection verification in the native BricsCAD viewport.

Do not describe planar UCS Direct Draw as V25-runtime-verified until that interactive gate passes on the exact release SHA.
