# Polygonal slab multi-region mesh planning

Status: `REMOTE_DONE` for the standards-neutral Core planning slice. Native Slab/Foundation source ownership, rebar materialization and BricsCAD V25 runtime qualification remain `LOCAL_ONLY`.

`PolygonalSlabMultiRegionMeshPlanner` applies the existing single-region `PolygonalSlabMeshPlanner` **per-region** after validating all disconnected islands through `PolygonRegionSetTopology`.

## Contract

Every region supplies a stable RegionId, one outer footprint and optional holes. Reinforcement parameters are shared by the request, but distribution is solved independently inside each validated region.

The planner:

- rejects invalid/touching/overlapping/nested region topology before bar planning;
- preserves stable RegionId on every returned region layout;
- reuses the existing cover + bar-radius, hole clipping, face and direction logic from `PolygonalSlabMeshPlanner`;
- preserves each region's own `XActualSpacingM` and `YActualSpacingM`;
- does not combine distribution counts across disconnected regions;
- caps aggregate physical output at 32,768 bars in addition to the existing single-region limits.

This matters especially in count mode: two islands with different extents can legitimately have different actual spacings even when both receive the same requested X/Y count. Treating the disconnected geometry as one span would change reinforcement semantics and ownership.

## Hole behavior

Holes remain attached to the stable RegionId that owns them. If a scanline is split around a hole, the resulting physical bar segments remain in that region's layout. No hole can subtract geometry from another disconnected island.

## Engineering boundary

This planner is deliberately standards-neutral geometry/distribution infrastructure. It does not select bar diameter, spacing, count, lap, anchorage, hook, bend radius, reinforcement zone or any other engineering value. Those values must come from explicit user/project inputs and, where code compliance is claimed, an approved governing engineering standard and revision.

The planner also does not invent a cross-island reinforcement zone. If a future product needs one logical zone spanning multiple islands, that requires a separate explicit semantic/engineering contract.

## Native ownership boundary

The stable RegionId is a Core identity, not a BricsCAD native owner by itself. The adapter still needs a reviewed mapping from RegionId to native source-loop ownership and generated owner slots.

Still `LOCAL_ONLY`:

- capture/association of every native outer and hole loop;
- straight/bulged POLYLINE extraction and coordinate-system proof;
- native owner-slot schema per region;
- creation/replacement/cleanup of physical rebar entities per region;
- stale/reconcile/Health/Release lifecycle;
- Undo, Save/SaveAs/reopen and multi-DWG behavior;
- exact-SHA licensed BricsCAD V25 qualification.

Therefore this Core planner must not be presented as completed native multi-region Slab/Foundation reinforcement until those native/runtime gates pass.
