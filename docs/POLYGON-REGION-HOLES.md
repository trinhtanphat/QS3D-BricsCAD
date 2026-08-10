# QS3D polygon region / hole topology Core contract

Updated: 2026-08-10 (UTC+7)

`PolygonRegionScanlineClipper` extends the simple-polygon scanline foundation from one outer loop to **one simple outer loop plus zero or more simple holes**. `PolygonalSlabMeshPlanner` now consumes that region model and applies its existing cover + bar-radius boundary-clearance contract to every outer/hole edge.

This is CAD-independent Core geometry/rebar planning. It does not mean Slab/Foundation native hole extraction, ownership, islands or multi-loop reinforcement are complete.

## Region model and validation

A `PolygonRegion2` contains one normalized simple outer polygon, zero or more normalized simple hole polygons and a read-only boundary-loop list. Loop winding is not topology authority.

The region fails closed when the outer/hole polygons are invalid, limits are exceeded, a hole leaves/touches the outer boundary, holes touch/intersect, or one hole contains another. Nested holes are rejected because they introduce an island and require an explicit multi-region topology contract.

## Scanline + mesh semantics

`PolygonRegionScanlineClipper.Clip(...)` clips the outer polygon and subtracts all hole interiors. `PolygonalSlabMeshPlanner` then evaluates capsule clearance around **every loop edge** using the direction-specific `cover + bar radius` value before emitting a bar segment.

For a scanline crossing a hole, one distributed scanline can become two or more physical bar placements. This preserves the existing count semantics: `XCount` / `YCount` describe distributed scanlines, not final physical segment count. Concavity and holes may increase `PolygonalSlabMeshLayout.Count` without changing the requested distribution count.

The existing bounds remain authoritative: region hole/vertex/scan-segment limits plus the polygon mesh bar and forbidden-interval limits all fail closed instead of truncating geometry.

## Still open before native hole-aware reinforcement

Core planning now covers hole topology, scanline subtraction and boundary clearance. Native Slab/Foundation wiring still requires a reviewed source/ownership contract:

1. source metadata identifying outer loop vs hole loops without trusting selection order accidentally;
2. native V25 POLYLINE/bulged-loop extraction and loop association;
3. ownership/replacement/stale/health behavior for source holes;
4. source reconcile when a hole is edited/deleted/replaced;
5. save/reopen, undo and multi-DWG behavior;
6. exact-SHA licensed BricsCAD V25 geometry proof.

Do not pass arbitrary selected loops into the Core planner and call the native workflow complete. Drawing-local source ownership and loop identity must remain deterministic and fail closed.

## Multiple outer loops / islands

Multiple disconnected outer loops are not represented by `PolygonRegion2`. Do not fake them as holes or concatenate vertices. A future multi-region model must explicitly represent independent regions and define whether one semantic Slab/Foundation may own multiple disconnected source regions, because that decision affects generated ownership and quantities.

## Source checks

```text
python scripts/preflight-polygon-region-holes.py
python scripts/preflight-polygonal-slab-holes.py
```

`PolygonRegionScanlineSmoke` covers topology/scanline behavior. `PolygonalSlabMeshHolesSmoke` covers hole-aware cover + radius trimming, distributed-scanline semantics, elevation stability and invalid-hole rejection.

Current status: **REMOTE_DONE for one-outer-loop + holes topology and Core mesh planning**. Native source-loop integration/runtime proof remains `LOCAL_ONLY`.
