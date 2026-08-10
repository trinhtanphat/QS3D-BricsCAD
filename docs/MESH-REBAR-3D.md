# QS3D slab / structural-wall mesh 3D

This document describes the current source-level two-direction mesh planners and native BricsCAD adapters. Runtime behavior still requires the licensed BricsCAD V25 gate.

## Slab mesh

Command: `QS3DSLABREBAR3D`.

### Source geometry

- semantic category: `Slab`;
- one selected closed four-vertex rectangular `POLYLINE`;
- footprint must be planar in XY, orthogonal and without bulges.

### Input properties

- `ThicknessM`;
- `BottomOffsetM`;
- `RebarSlabXNotation` — one count-or-spacing group, e.g. `D10@200` or `20D10`;
- `RebarSlabYNotation` — one count-or-spacing group;
- `RebarSlabCoverM` — fallback `RebarCoverM`;
- `RebarSlabFaces` — `Bottom`, `Top`, or `Both`;
- `RebarSlabXClosestToFace` — boolean direction stacking choice.

`RectangularSlabMeshPlanner` validates cover, bar radii, two-direction layer stacking, top/bottom separation, count/spacing ambiguity and an 8,192-bar planning cap before CAD mutation.

The first native adapter requires X and Y bars to use the same diameter. Core planning already supports separate diameters; the native restriction exists so the first release can reuse the established `GeneratedRebarHandles` ownership/health/stale lifecycle without creating an unsafe new handle family.

### Generated metadata

- `GeneratedRebarHandles`;
- `GeneratedRebarCount`;
- `GeneratedRebarDiameterMm`;
- `GeneratedRebarCoverM`;
- `GeneratedRebarMode = SlabMeshXY`;
- `GeneratedRebarSlabXActualSpacingM`;
- `GeneratedRebarSlabYActualSpacingM`;
- `GeneratedRebarSlabFaces`.

The adapter refuses to overwrite `GeneratedRebarHandles` when `GeneratedRebarMode` belongs to another workflow.

## Structural-wall mesh

Command: `QS3DWALLREBAR3D`.

### Source geometry

- semantic category: `StructuralWall`;
- one selected nearly-horizontal `LINE` centerline;
- current adapter requires source endpoint elevation difference within 0.005 m.

### Input properties

- `HeightM`;
- `ThicknessM`;
- `BottomOffsetM`;
- `RebarWallHorizontalNotation`;
- `RebarWallVerticalNotation`;
- `RebarWallCoverM` — fallback `RebarCoverM`;
- `RebarWallFaces` — `Near`, `Far`, or `Both`;
- `RebarWallHorizontalClosestToFace` — boolean layer stacking choice.

`RectangularWallMeshPlanner` validates horizontal/vertical distribution, cover, radii, near/far face separation and an 8,192-bar planning cap before CAD mutation.

The first native adapter also requires horizontal and vertical bars to share one diameter so it can reuse `GeneratedRebarHandles` safely.

### Generated metadata

- `GeneratedRebarHandles`;
- `GeneratedRebarCount`;
- `GeneratedRebarDiameterMm`;
- `GeneratedRebarCoverM`;
- `GeneratedRebarMode = StructuralWallMesh`;
- `GeneratedRebarWallHorizontalActualSpacingM`;
- `GeneratedRebarWallVerticalActualSpacingM`;
- `GeneratedRebarWallFaces`.

## Shared ownership / health

Slab and StructuralWall mesh bars use the existing `GeneratedRebarHandles` family. Therefore they inherit:

- cross-set destructive ownership checks;
- live `Solid3d` handle validation;
- generated stale snapshots after semantic/source changes;
- host-rebuild dependent invalidation;
- `QS3DHEALTHALL` / rebar-health integration for the generic handle set.

`GeneratedRebarModeHealthService` additionally validates mode-to-category mapping and mode-specific metadata. Command: `QS3DREBARMODEHEALTH`.

## Rebar 3D Hub

`QS3DREBARHUB` opens the dedicated Rebar 3D Hub with current column, beam, slab, wall, BBS-shape and health workflows. This keeps advanced reinforcement accessible even when the shared main workspace is being evolved independently.

## Current limitations

Source implementation does not yet claim:

- arbitrary/non-rectangular slab mesh clipping;
- openings/void-aware slab bar trimming;
- wall openings causing automatic local rebar trimming or jamb bars;
- different X/Y or H/V diameters in the first native adapter;
- engineering design/checking, code compliance or automatic reinforcement sizing;
- bend radius, hooks, laps and anchorage visualization for every mesh edge condition;
- runtime proof on a licensed V25 workstation/private DWGs.

The generated geometry is a deterministic takeoff/review representation driven by explicit semantic reinforcement inputs; it is not a structural-design calculation engine.
