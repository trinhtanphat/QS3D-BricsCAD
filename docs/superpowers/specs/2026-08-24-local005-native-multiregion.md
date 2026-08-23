# LOCAL-005 Native Multi-Region Reinforcement Design

## Goal

Complete the source-safe portion of issue #83 through canonical child carrier #3647 by adding native BricsCAD V25/V26 source association, materialization, ownership, reconcile, stale detection, and read-only Health for Slab/Foundation reinforcement spanning multiple disconnected polygon regions and holes.

## Product boundary

This design does not invent structural detailing rules. Anchorage, lap, hook, bend, development-length, fabrication, or code-specific detailing remains `ENGINEERING_REQUIRED`. Licensed save/reopen, Undo/Redo, multi-DWG, and exact-SHA geometry qualification remains `LOCAL_ONLY/PENDING_LOCAL` and must not be reported as remotely passed.

## Architecture

A semantic Slab or Foundation remains the single project owner. A dedicated native multi-region adapter reads the complete selected closed-loop source set, converts supported BricsCAD polylines to bounded Core 2D loops, deterministically groups disconnected outer loops and holes, and passes the resulting stable `RegionId` inputs to the existing `PolygonalSlabMultiRegionMeshPlanner`. There is no second reinforcement planning engine.

The existing rectangle and single-polygon builders remain valid compatibility paths. Multi-region output preserves the existing aggregate generated-handle slot so cleanup/invalidation continues to see generated rebar, while additional region metadata and a dedicated native XData marker provide per-region provenance.

## Source association

A multi-region command operates on a complete desired set of selected closed polylines for exactly one semantic Slab or Foundation. The set must be anchored by exactly one element already associated with at least one selected source handle or by that element's previously persisted multi-region source manifest. Ambiguous ownership fails before any destructive write.

Each source loop receives a stable source identity from its CAD handle. Core `PolygonSourceLoopRegionAssembler` validates every loop, assigns each hole to exactly one containing outer loop, rejects unsupported deeper nesting and touching/intersecting/overlapping topology, and derives each stable region identifier from the canonical outer-loop source identity. Reordering the selection therefore does not change region identity.

The persisted source manifest records, in deterministic region/source order, every region id, outer source handle, and hole source handles. It is association metadata only; the structural element's primary `SourceHandles` collection is not destructively rewritten just to represent reinforcement loops.

## Native loop extraction

`ClosedPolygonSourceLoopReader` is separate from the existing open-path reader. It accepts bounded closed `Polyline` objects only, rejects non-finite or unsupported geometry, tessellates bulged segments with the existing Core `BulgedPolygonFootprintTessellator`, and transforms supported OCS coordinates into WCS before converting to project metres. The current XY reinforcement planner requires a horizontal footprint; unsupported tilted planes fail closed instead of being silently flattened.

All loops in one region-set build must be coplanar within a bounded drawing tolerance. Straight and bulged loops may be mixed.

## Planning and materialization

For Slab and Foundation, the adapter resolves the same family/rebar notation, vertical placement, cover, faces, and closest-to-face semantics already used by the existing builders. It calls `PolygonalSlabMultiRegionMeshPlanner.Plan` exactly once per semantic element with all regions. Each returned `PolygonalSlabMeshRegionLayout` retains `RegionId`, and its bar start/end coordinates are materialized as native `Solid3d` cylinders in the same drawing coordinate system as the existing polygon builders.

The native batch cap remains 12,000 generated bars per operation even though Core has a larger aggregate safety cap.

## Ownership and atomic reconcile

Every generated bar carries the existing `QS3D_REBAR` ownership marker for compatibility plus a dedicated `QS3D_REBAR_REGION` marker containing version, project identity, element identity, canonical aggregate owner slot, and stable region id. Region identity is therefore independently verifiable before destructive reconciliation.

Before erasing any previous generated object, the service resolves the complete old generated-handle set and validates both aggregate ownership and, when a region manifest exists, region ownership. Missing, duplicate, malformed, mixed-project, mixed-element, mixed-slot, stale, or reused handles fail closed before the first erase. Legacy aggregate-owned single-region output may be migrated only after existing ownership validation succeeds.

Reconcile computes a full replacement in one document lock / CAD transaction and one `ProjectStateSnapshot`. New bars and all metadata are committed together. If CAD has not committed, project state is restored. No partial region may survive an exception.

## Metadata

Slab and Foundation use distinct prefixes but the same deterministic contract:

- aggregate generated handles (existing compatibility slot);
- generated bar count;
- `...MultiRegionSourceManifest`;
- `...MultiRegionGeneratedManifest` mapping RegionId to generated handles;
- `...MultiRegionTopologyFingerprint`;
- existing generated placement/cover/spacing/faces snapshots where applicable.

Manifest serialization is deterministic and bounded. Region/source/handle identifiers are validated before use.

## Health

A read-only runtime Health service verifies:

- every persisted source handle resolves to one supported live closed polyline;
- reassembly produces the same deterministic RegionIds and topology fingerprint;
- every generated handle resolves exactly once;
- aggregate count and per-region manifest counts agree;
- both aggregate and region ownership markers match project, element, owner slot, and RegionId;
- duplicate generated handles or one handle claimed by multiple regions fail Health;
- stale topology/source/generated state is localized by element and RegionId.

Health never repairs or erases geometry.

## Commands

Add explicit Slab/Foundation multi-region build/refresh entry points plus a read-only multi-region Health entry point following existing project-context/command error-handling conventions. Existing single-region commands remain backward compatible.

## Regression contract

Source/static tests must cover: two disconnected islands; one island plus a hole; mixed straight/bulged source loops; deterministic identity under selection reorder; add/remove region reconciliation; corrupt/mixed ownership refusal; rollback/no partial project mutation; 12,000-bar native cap; legacy rectangle/single-polygon paths remaining present; V26 shared-source linkage for all new V25 adapter files.

Exact-head branch/PR CI must pass `preflight` and `core`, including deterministic Core smoke tests and BricsCAD V25 plugin compilation, before merge. Parent #83 remains open after source merge for licensed runtime evidence.