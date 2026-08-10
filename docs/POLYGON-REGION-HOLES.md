# QS3D polygon region / hole topology Core contract

Updated: 2026-08-10 (UTC+7)

`PolygonRegionScanlineClipper` extends the existing simple-polygon scanline foundation from one outer loop to **one simple outer loop plus zero or more simple holes**.

This is a CAD-independent topology/scanline layer. It does not yet mean Slab/Foundation native holes, openings, islands or multi-loop reinforcement are complete.

## Region model

A `PolygonRegion2` contains:

- one normalized simple outer polygon;
- zero or more normalized simple hole polygons;
- a read-only boundary-loop list for later boundary-clearance consumers.

Loop winding is not used as ownership/topology authority. Clockwise and counter-clockwise loops are both accepted after geometric validation.

## Validation

The region fails closed when:

- the outer loop or any hole is invalid under `PolygonScanlineClipper.NormalizeAndValidate`;
- more than 256 holes are supplied;
- total vertices exceed 16384;
- a hole is outside the outer polygon;
- a hole touches/intersects the outer boundary;
- holes touch/intersect each other;
- one hole contains another hole.

Nested holes are rejected because a hole-inside-hole introduces an **island**. Islands need an explicit multi-region topology model rather than treating winding order as an implicit Boolean operation.

## Scanline semantics

`Clip(region, axis, coordinate)` first clips the outer polygon, then subtracts every hole interval. Output segments remain finite, positive-length and ordered along the scan axis.

For a 10 × 10 outer square with a 2 × 2 central hole, a scanline through the hole returns two usable segments instead of one.

The result is bounded to 4096 scan segments per scanline.

## What remains before hole-aware reinforcement

This source slice deliberately stops at topology + interior clipping. `PolygonalSlabMeshPlanner` currently owns cover + bar-radius capsule clearance around its single footprint boundary. A hole-aware mesh must extend that same clearance contract to **every hole boundary**, not merely remove the mathematical hole interior.

Therefore remote/native agents must not wire `PolygonRegionScanlineClipper` directly to native Slab/Foundation rebar and call it complete until all of these are implemented:

1. cover + bar-radius clearance around outer and hole boundaries;
2. bounded bar-segment growth on concave/hole scanlines;
3. source metadata identifying outer loop vs hole loops without trusting selection order accidentally;
4. native V25 POLYLINE extraction and loop association;
5. ownership/replacement/stale/health behavior for source holes;
6. save/reopen and source-reconcile behavior when a hole is edited/deleted/replaced;
7. exact-SHA licensed BricsCAD V25 geometry proof.

## Multiple outer loops / islands

Multiple disconnected outer loops are not represented by `PolygonRegion2`. Do not fake them as holes or concatenate vertices.

A future multi-region model should explicitly represent a bounded collection of independent regions, each with its own outer loop and holes, then define whether one semantic Slab/Foundation may own multiple disconnected source regions. That ownership decision affects generated-handle replacement and quantities, so it must be reviewed separately.

## Source checks

```text
python scripts/preflight-polygon-region-holes.py
```

`PolygonRegionScanlineSmoke` covers horizontal/vertical hole subtraction and fail-closed outside, touching, overlapping and nested-hole cases.

Current status: **REMOTE_DONE for one-outer-loop + holes topology/scanline Core only**. Hole-aware mesh clearance/native integration remains open; native runtime proof remains `LOCAL_ONLY`.
