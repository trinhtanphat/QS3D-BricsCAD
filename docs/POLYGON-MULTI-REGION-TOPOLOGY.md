# Polygon multi-region topology contract

Status: `REMOTE_DONE` for the bounded Core topology primitive. Native Slab/Foundation source-loop ownership and BricsCAD V25 runtime use remain `LOCAL_ONLY`.

`PolygonRegionSetTopology` is the explicit Core contract for **multiple disconnected polygon islands**. It builds on `PolygonRegion2` instead of flattening islands into one polygon.

## Core rules

Every island requires a stable region ID supplied by the caller plus its own:

- simple outer boundary;
- zero or more simple holes validated by `PolygonRegionScanlineClipper`;
- independent region identity in all tagged scanline output.

The implementation is deliberately conservative:

- island IDs are required, bounded and unique case-insensitively;
- each region is independently normalized/validated through the existing outer+holes contract;
- the set is capped at 256 islands and 65,536 total vertices;
- outer boundaries of distinct islands must not intersect or touch;
- overlapping outer regions fail closed;
- nested outer regions fail closed pending an explicit ownership/topology policy;
- scanline output retains `RegionId` on every segment and is globally bounded.

A caller must not concatenate vertices from separate islands merely to feed a single-polygon API, and it must not reinterpret an island as a hole to bypass the topology decision.

## Why region identity is mandatory

Native Slab/Foundation support eventually needs to associate every disconnected outer loop and its hole loops with stable CAD/source ownership. Geometry without identity is insufficient for reconcile, stale detection, save/reopen and generated-rebar ownership.

This Core contract therefore requires the identity up front but does **not** invent native source-loop ownership. The BricsCAD adapter must later bind each region ID to reviewed source-loop identities/handles under its own transaction and ownership rules.

## Tagged clipping

`PolygonRegionSetTopology.Clip(...)` runs the existing hole-aware `PolygonRegionScanlineClipper` independently for every island. It returns `PolygonRegionTaggedScanSegment` values containing the stable region ID and segment endpoints.

This means two disconnected slab islands remain two independent regions even when the same global X/Y scanline crosses both. Hole splitting also remains local to the owning island.

The primitive intentionally does not define one global reinforcement distribution count across all islands. A future multi-region mesh planner must state whether counts/spacing are per region, per reinforcement zone or governed by another explicit engineering policy. It may not silently reuse single-region count semantics across disconnected owners.

## Nested islands

An outer loop geometrically inside another outer loop is currently rejected even if a future product could interpret it as an island inside a void. That case needs explicit semantic/source ownership and orientation/topology rules. Failing closed is safer than guessing whether the nested loop is a hole, an independent slab, a separate elevation or invalid source geometry.

## Native boundary

Still `LOCAL_ONLY` / product work:

- native outer-loop + hole-loop source identity and association;
- straight/bulged POLYLINE extraction and OCS/WCS handling;
- native creation/materialization for multiple disconnected Slab/Foundation regions;
- ownership, reconcile, stale and Health lifecycle for every source loop;
- generated rebar owner-slot mapping per region;
- Undo, Save/SaveAs/reopen and multi-DWG behavior;
- exact-SHA licensed BricsCAD V25 geometry proof.

The Core topology primitive must not be described as native multi-region Slab/Foundation support until those gates are implemented and qualified.
