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
- the currently wired native adapter path accepts straight segments only;
- polygon is plan-view with normal `+Z`;
- Core polygon validation accepts the footprint as a bounded simple polygon.

Polygon mesh directions use drawing/world X and Y, not an inferred local polygon axis. New polygon meshes record `GeneratedSlabMeshFootprintMode=PolygonGlobalXY` while retaining `GeneratedSlabMeshMode=SlabMeshXY` so existing generated-mode health/report contracts do not fork.

The Core planner clips every X/Y scanline to the actual polygon and subtracts cover plus bar radius from the polygon boundary. Concave footprints may split one scanline into multiple physical bar segments.

## Bulged boundary Core preparation

Core now also contains `BulgedPolygonFootprintTessellator`. It is a CAD-independent bridge for a reviewed ordered closed polyline represented as `BulgedPolygonVertex2(Point, BulgeToNext)`:

- every straight or bulged edge, including the closing edge, is tessellated through the existing bounded `BulgeArcTessellator`;
- the caller supplies an explicit maximum sagitta; non-finite/non-positive tolerances fail closed;
- total tessellated output is capped at 4096 vertices;
- the closing point is not duplicated;
- the completed tessellated footprint must pass `PolygonScanlineClipper.NormalizeAndValidate`, including finite coordinates, non-zero area, non-degenerate edges and simple-polygon/self-intersection checks;
- the resulting `IReadOnlyList<Point2>` can be passed directly to `PolygonalSlabMeshPlanner` without creating another mesh engine.

This Core slice is `REMOTE_DONE`. It **does not** mean `QS3DSLABREBAR3D` or `QS3DFOUNDATIONREBAR3D` currently accepts native bulged POLYLINE footprints. Native extraction, OCS/plane interpretation, ownership-safe wiring and exact-SHA BricsCAD V25 proof are `LOCAL_ONLY` under `docs/REMOTE-AGENT-SCOPE.md`. Remote agents must not reimplement or re-audit that V25 wiring.

## Count semantics on concave footprints

For notation expressed as a count, such as `20D10`, the count controls the number of **distributed scanlines** in that direction. A concavity can split one scanline into multiple physical bars, so `GeneratedSlabMeshCount` can be greater than the notation count. This is intentional geometry behavior, not a fabrication mark count.

## Foundation parity

Foundation now reuses the same guarded polygon engine through `QS3DFOUNDATIONREBAR3D` while retaining separate Foundation ownership/stale/health metadata. True rectangles keep `RectangleLocalXY`; non-rectangular closed straight simple polygons use `PolygonGlobalXY`. See `docs/FOUNDATION-REBAR3D.md` for the Foundation-specific property, ownership, health and runtime contract.

This is source parity for the supported simple-polygon subset, not proof that Slab/Foundation native bars have passed the exact-SHA licensed BricsCAD V25 matrix.

## Safety and release boundary

The adapter preserves generated-handle ownership checks, destructive replacement refusal on wrong ownership/type, project snapshot rollback before CAD commit, batch bar limits, generated stale clearing and `geometry.rebar.slab.mesh` audit.

This feature creates native reinforcement geometry only. It does not infer fabrication hooks, laps, anchorage, splice policy, bend radii or code-specific cutting rules. Polygonal bar segments must not be advertised as fabrication/BBS-qualified merely because native cylinders exist; the existing fabrication qualification gate remains authoritative.

Native bulged-footprint wiring, holes/islands, multiple outer loops and arbitrary local-axis inference remain separate work. Of those, native bulged extraction/wiring and its runtime evidence are explicitly `LOCAL_ONLY`; holes/islands/multiple loops still require an explicit source geometry/ownership contract before implementation. Exact-SHA native geometry/runtime qualification also remains local-only.
