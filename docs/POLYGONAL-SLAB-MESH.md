# Polygonal Slab Mesh 3D

`QS3DSLABREBAR3D` supports guarded rectangle and simple-polygon native footprint paths. Core mesh planning now additionally supports one validated outer polygon plus validated hole loops, but native hole-loop extraction/ownership is not yet exposed as complete.

## Rectangle compatibility path

A valid closed four-vertex straight rectangle continues to use `RectangularSlabMeshPlanner` and local axes. This preserves `GeneratedSlabMeshMode=SlabMeshXY`, independent X/Y notation/count/spacing, Bottom/Top/Both, `RebarSlabXClosestToFace`, ownership/stale/audit behavior and `GeneratedSlabMeshFootprintMode=RectangleLocalXY`.

## Simple polygon native path

A non-rectangular footprint uses `PolygonalSlabMeshPlanner` when the currently wired native adapter resolves one supported semantic Slab boundary. Polygon mesh directions use drawing/world X/Y and new polygon meshes record `GeneratedSlabMeshFootprintMode=PolygonGlobalXY` while retaining `GeneratedSlabMeshMode=SlabMeshXY`.

The native adapter path is still deliberately narrower than the Core planner: source-loop/hole association must not be guessed from arbitrary selected POLYLINE order.

## Hole-aware Core planner

`PolygonalSlabMeshInput` now has optional `HoleFootprintsM`. The planner validates the outer + hole topology through `PolygonRegionScanlineClipper`, clips each distributed X/Y scanline to the usable region, and subtracts direction-specific `cover + bar radius` clearance from **every outer and hole boundary edge** before emitting physical bar placements.

This means a scanline crossing a central opening/hole is split around the hole and the segment endpoints remain clear of the hole boundary by the same concrete-cover rule used at the outer boundary. Invalid outside/touching/overlapping/nested holes fail before layout.

## Count semantics on concavity and holes

For notation expressed as a count, the count controls the number of **distributed scanlines** in that direction. Concavity or holes can split one scanline into several physical bar segments, so final native/Core physical segment count may exceed the notation count. This is intentional geometry behavior and is not a fabrication mark count.

## Bulged boundary Core preparation

Core also contains `BulgedPolygonFootprintTessellator`, which converts a reviewed ordered closed bulged polyline into a bounded simple `Point2` footprint using the existing arc tessellator and sagitta limit. Native BricsCAD bulged-loop extraction/OCS interpretation remains separately runtime-qualified; do not infer native support merely from the Core tessellator.

## Foundation parity

Foundation reuses the same polygon mesh engine for its currently supported simple-polygon native subset while retaining Foundation ownership/stale/health metadata. The new hole-aware Core capability is reusable by Foundation as well, but Foundation native hole-loop extraction and ownership are not complete until the same explicit source-loop contract is implemented and qualified.

## Native hole-loop work still required

Before `QS3DSLABREBAR3D` or `QS3DFOUNDATIONREBAR3D` can advertise hole-aware native reinforcement, implement and qualify:

- deterministic semantic/source representation for one outer loop plus hole loops;
- native straight/bulged POLYLINE extraction and loop association without trusting pick order;
- source Handle ownership, stale/health and reconcile lifecycle for every loop;
- atomic replacement/rollback when hole geometry changes;
- save/reopen, undo and multi-DWG behavior;
- exact-SHA licensed BricsCAD V25 geometry evidence.

Multiple disconnected outer regions/islands remain a separate topology/ownership problem. Arbitrary local-axis inference is also separate work.

## Safety and release boundary

The existing native adapter contracts preserve generated-handle ownership checks, destructive replacement refusal on wrong ownership/type, project rollback boundaries, batch limits, stale clearing and audit. Hole-aware Core planning does not weaken those gates.

This feature does not infer fabrication hooks, laps, anchorage, splice policy, bend radii or code-specific cutting rules. Polygonal/hole-split bar segments must not be advertised as fabrication/BBS-qualified merely because geometry can be planned or native cylinders can be created; the fabrication qualification gate remains authoritative.

Source checks include:

```text
python scripts/preflight-polygon-region-holes.py
python scripts/preflight-polygonal-slab-holes.py
```

Current status: simple polygon native source paths remain source-implemented but runtime-gated; one-outer-loop + holes topology and mesh clearance are **REMOTE_DONE in Core**; native hole-loop integration and exact V25 proof remain `LOCAL_ONLY`.
