# Polygonal Slab Mesh 3D

`QS3DSLABREBAR3D` supports two guarded footprint paths for semantic `Slab` elements.

## Rectangle compatibility path

A valid closed four-vertex straight rectangle continues to use `RectangularSlabMeshPlanner` and its local axes. This preserves the existing behavior for rotated rectangles and existing metadata:

- `GeneratedSlabMeshMode=SlabMeshXY`
- independent X/Y notation, diameter, count/spacing and actual spacing
- Bottom / Top / Both faces
- `RebarSlabXClosestToFace`
- ownership, stale-state and audit contracts

The adapter records `GeneratedSlabMeshFootprintMode=RectangleLocalXY` for newly generated rectangle meshes.

## Simple polygon path

A non-rectangular footprint uses `PolygonalSlabMeshPlanner` when all of these conditions are true:

- source is one selected semantic Slab `POLYLINE`;
- source is closed and has at least three vertices;
- boundary consists only of straight segments; bulges/curved edges are rejected;
- polygon is plan-view with normal `+Z`;
- Core polygon validation accepts the footprint as a bounded simple polygon.

Polygon mesh directions use drawing/world X and Y, not an inferred local polygon axis. New polygon meshes record `GeneratedSlabMeshFootprintMode=PolygonGlobalXY` while retaining `GeneratedSlabMeshMode=SlabMeshXY` so existing generated-mode health/report contracts do not fork.

The Core planner clips every X/Y scanline to the actual polygon and subtracts cover plus bar radius from the polygon boundary. Concave footprints may split one scanline into multiple physical bar segments.

## Count semantics on concave footprints

For notation expressed as a count, such as `20D10`, the count controls the number of **distributed scanlines** in that direction. A concavity can split one scanline into multiple physical bars, so `GeneratedSlabMeshCount` can be greater than the notation count. This is intentional geometry behavior, not a fabrication mark count.

## Safety and release boundary

The adapter preserves generated-handle ownership checks, destructive replacement refusal on wrong ownership/type, project snapshot rollback before CAD commit, batch bar limits, generated stale clearing and `geometry.rebar.slab.mesh` audit.

This feature creates native reinforcement geometry only. It does not infer fabrication hooks, laps, anchorage, splice policy, bend radii or code-specific cutting rules. Polygonal bar segments must not be advertised as fabrication/BBS-qualified merely because native cylinders exist; the existing fabrication qualification gate remains authoritative.

Curved/bulged polygon boundaries, holes/islands, multiple outer loops, arbitrary local-axis inference and Foundation native polygon integration are separate work and must remain fail-closed until explicitly implemented and reviewed.
