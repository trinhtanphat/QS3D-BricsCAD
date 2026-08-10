# QS3D Direct Draw — UCS contract

Updated: 2026-08-10 (UTC+7)

## Scope

This note records the coordinate-system contract for the current Direct Draw authoring set:

- P0: `QS3DDRAWWALL`, `QS3DDRAWBEAM`, `QS3DDRAWCOLUMN`, `QS3DDRAWSLAB`;
- guarded P1: `QS3DDRAWGLASSWALL`, `QS3DDRAWWALLPIER`, `QS3DDRAWSTRUCTWALL`, `QS3DDRAWFOUNDATION`;
- host-aware opening authoring: `QS3DDRAWDOOR`, `QS3DDRAWOPENING`.

Direct Draw runs inside BricsCAD. Point prompts follow the current editor UCS, while persistent DWG/database geometry is transformed into the database/world coordinate context expected by existing QS3D geometry, host matching, quantities and native builders.

## Implemented source behavior

The current Direct Draw source paths follow this boundary:

1. Require Model Space.
2. Read `Editor.CurrentUserCoordinateSystem`.
3. Require the UCS XY plane to remain parallel to WCS XY. Translation and in-plane rotation are allowed; tilted/3D UCS is rejected before any Direct Draw source entity is created.
4. Acquire and validate prompt points in current-UCS coordinates. The existing 5 mm planarity rule therefore remains relative to the author's working UCS plane.
5. Build the source `LINE` or `POLYLINE` in UCS-local coordinates.
6. Call `TransformBy(document.Editor.CurrentUserCoordinateSystem)` before appending the entity to Model Space.
7. Continue through the existing source -> semantic -> regeneration/build/host-link workflow for that category.

For P0 Column, the rectangular footprint is built in current-UCS local X/Y around the picked center and passes through the same POLYLINE transform, so a planar rotated UCS rotates the footprint with the user's working axes instead of keeping it aligned to WCS X/Y.

For P1, the persisted GlassWall/WallPier/StructuralWall LINE source and Foundation/GlassWall path POLYLINE source now use the same transform-before-append contract before canonical `QS3DBUILD3D` consumes them.

For Door/Opening Direct Draw, `WidthM` remains the plan distance between the two prompt points in UCS-local coordinates. The persisted opening source LINE is transformed into WCS before append, so downstream Auto Host sees database geometry in the same coordinate context as existing walls. The operation still performs source + semantic + verified Auto Host only; physical boolean remains explicit through the selected/all-linked cut commands.

World UCS remains an identity transform.

## Fail-closed boundary

Tilted/3D UCS is intentionally not generalized. Current wall/structure/opening host and native builders still contain WCS-planar assumptions whose extrusion/boolean behavior must be proven before arbitrary UCS planes are accepted.

Direct Draw therefore rejects a UCS whose normalized Z axis is not aligned with positive WCS Z. It does not mutate or reset the user's UCS.

This is preferable to accepting a tilted UCS and silently creating a source, generated solid or host relation in an unintended plane.

## Static guards

`scripts/preflight-direct-draw-ucs.py` protects the P0 contract.

`scripts/preflight-direct-draw-ucs-extended.py` extends the same contract to P1 and Door/Opening source creation. Together they require:

- Model-Space -> UCS guard ordering;
- `CurrentUserCoordinateSystem.CoordinateSystem3d` inspection;
- explicit tilted-UCS rejection;
- LINE transform before `AppendEntity`;
- POLYLINE transform before `AppendEntity` where the category creates one;
- no assignment that resets `CurrentUserCoordinateSystem`.

The aggregate `scripts/preflight-all.py` auto-discovers both guards.

## Runtime gate still required

Source implementation is not runtime certification. Licensed BricsCAD V25 validation still needs at least:

- World UCS baseline;
- translated UCS;
- 30/45/90-degree planar rotated UCS;
- P0 Wall/Beam/Column/Slab creation in each supported planar UCS;
- P1 GlassWall/WallPier/StructuralWall/Foundation creation in each supported planar UCS;
- Door/WallOpening creation and Auto Host in each supported planar UCS;
- Door/Opening picked width, host selection, sill/offset and targeted physical-cut review after UCS transform;
- generated solid direction, source Handle provenance and semantic length/area checks;
- cancel/failure rollback;
- save/reopen/regenerate;
- tilted UCS rejection before source creation;
- screenshot/selection verification in the native BricsCAD viewport.

Do not describe planar-UCS Direct Draw as V25-runtime-verified until that interactive gate passes on the exact release SHA.