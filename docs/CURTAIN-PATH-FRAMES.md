# Curtain path frames — open / bulged POLYLINE contract

Status: implemented in source on the Curtain path-frame batch; real BricsCAD V25 compile/NETLOAD/runtime proof remains a separate release gate.

## Scope

`QS3DCURTAINFRAMES3D` and `QS3DCURTAIN3D` support two GlassWall frame-overlay source forms:

- existing horizontal `LINE` source;
- open `POLYLINE` source in WCS XY with +Z normal, including bulged segments.

Closed POLYLINE, tilted OCS/non-+Z paths and malformed/degenerate paths fail closed. The implementation does not silently flatten arbitrary 3D/OCS geometry.

## Deterministic station model

The existing Curtain grid/detail planners work in a 2D elevation coordinate system:

- `X` = station along the GlassWall host;
- `Z` = elevation above the curtain base.

For an open POLYLINE host:

1. bulged segments are tessellated through `BulgeArcTessellator` using the same `WallArcSagittaM` project policy used by the backing wall footprint path;
2. `CurtainPathFramePlanner` computes cumulative station along the tessellated path;
3. opening-aware frame rectangles are produced in the existing curtain `X/Z` system;
4. each frame interval is intersected with path segments;
5. a native box is created for every positive segment overlap and rotated by that segment tangent.

A frame crossing a path vertex is therefore split into deterministic native fragments. Curves are represented by bounded piecewise-linear fragments at the configured sagitta tolerance; QS3D does **not** pretend that this is one exact swept mullion solid.

## Door / Opening interruption

Linked Door/WallOpening elements keep the same curtain interruption contract.

For POLYLINE hosts, the live CAD source extents center of each linked opening is projected to the nearest tessellated path segment. The projection provides:

- nearest station along host;
- perpendicular distance to host path;
- deterministic segment tie-breaking by earliest station.

The opening is rejected when its center is farther than `host thickness / 2 + 0.25 m` from the path. A valid station is passed to the existing guarded `OpeningCutPlanner` and `CurtainFrameOpeningPlanner`, so opening rectangles interrupt mullion/transom pieces before path placement.

QS3D does not guess an opening station when CAD provenance is missing, ambiguous or too far from the host.

## Generated ownership and metadata

POLYLINE frame output continues to use the canonical owner slot:

- `GeneratedCurtainFrameHandles`

Path-specific metadata includes:

- `GeneratedCurtainFrameMode = PathFrameOverlay` or `PathFrameOverlay.OpeningAware`;
- `GeneratedCurtainFrameSourceKind = OpenPolyline`;
- `GeneratedCurtainFramePathSegmentCount`;
- `GeneratedCurtainFrameMappedFrameCount`;
- existing count/grid/opening/depth/source-length/height/config-fingerprint fields.

Destructive replacement still goes through `GeneratedCurtainFrameOwnershipGuard`; a live handle that is unowned, ambiguous or not a `Solid3d` is not erased.

## Live stale detection

`CurtainWallFrameLiveFingerprint` now supports both source kinds.

- LINE hashing remains byte-compatible with the previous LINE fingerprint schema so existing LINE overlays do not become stale only because the plugin was upgraded.
- POLYLINE hashing includes handle, closed state, elevation, normal, all vertex XY coordinates and segment bulges, plus linked-opening source/config data.

Direct grip edits or bulge edits after frame generation therefore surface through Curtain live-health stale checks.

## Safety budgets

The path planner is bounded to 8,192 tessellated points and 20,000 planned pieces. The BricsCAD path builder additionally limits one semantic element to 4,096 generated frame solids and one command batch to 8,192 generated frame solids.

If a path/grid/opening combination exceeds those budgets, the command fails instead of allocating an unbounded native-solid batch.

## Deliberate remaining boundary

This batch does **not** implement panel-by-panel backing glass solids. `QS3DCURTAIN3D` still keeps the existing single backing GlassWall host solid and adds separate generated frame overlays.

It also does not claim current BricsCAD V25 runtime validation. Exact final-SHA adapter compilation and licensed interactive NETLOAD/runtime testing remain required before release qualification.
