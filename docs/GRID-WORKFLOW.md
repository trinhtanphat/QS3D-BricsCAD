# QS3D Grid / Trục reference workflow

Updated: 2026-08-10 (UTC+7)

## Current command

`QS3DGRID`

The command captures selected BricsCAD `LINE` or `ARC` geometry as `ElementCategory.Grid` semantic references.

## Source contract

- selection must contain only `LINE` / `ARC` sources;
- every source must expose a finite positive curve length;
- malformed/unsupported selection fails before semantic mutation;
- capture reuses `SemanticCaptureService`, including generated-output rejection, collision checks and project-state rollback;
- Grid uses the existing `GenericTakeoffRegenerator` and therefore carries semantic `LengthM` and `Count` quantities;
- the original DWG entity remains the source of truth and keeps its stable drawing-local CAD Handle provenance;
- the command does not create or claim native Grid 3D geometry.

## Semantic Grid label sequencing — Core source

`GridNamingService` now provides a CAD-independent, fail-closed naming contract over the **existing** `ElementCategory.Grid` elements. It does not create another Grid catalog.

Current semantics:

- caller supplies an explicit ordered list of Grid element IDs; Core does not pretend to infer spatial Grid order from CAD geometry;
- numeric sequences support prefix/suffix, start index and bounded zero-padding;
- alphabetic sequences use deterministic `A..Z, AA..AZ, BA...` numbering;
- labels are stored on the Grid semantic element as `GridLabel`; the resolved sequence index is stored as `GridSequenceIndex`;
- label uniqueness is case-insensitive across other Grid elements in the same project;
- the whole batch is validated before any label mutation, so a missing/non-Grid element, duplicate ID, duplicate external label or invalid sequence blocks the whole renumber operation;
- source geometry, source Handles and native CAD ownership are untouched.

This deliberately solves only the reusable semantic sequence layer. A future V25 command/UI may supply a reviewed CAD ordering and call this service, but it must not silently change the Core ordering contract.

## Product boundary

The current Grid source does **not** yet mean the following are complete:

- native Grid bubble/label drawing and ownership/replacement lifecycle;
- automatic CAD spatial ordering for renumbering;
- rectangular/radial Grid systems;
- Grid intersection constraints;
- dimensions/annotations tied to Grid IDs;
- Direct Draw Grid with transient jig/repeat authoring;
- automatic snapping/hosting of structure to Grid intersections.

Those features must extend the existing `ElementCategory.Grid` semantic model rather than adding a competing Grid store. Source-only agents may advance deterministic Core/reference semantics; real bubbles, DrawJig/editor interaction, spatial ordering and native visualization remain subject to the V25 runtime boundary in `docs/REMOTE-AGENT-SCOPE.md`.

## Floor / Level relation — current source

QS3D reuses the existing `ProjectFloor` / `FloorDefinition` catalog as its Level model; do not introduce a duplicate `LevelDefinition` merely for product parity.

Current Core now supports opt-in vertical references:

- `BottomLevelId`
- `BottomLevelOffsetM`
- `TopLevelId`
- `TopLevelOffsetM`

`ProjectFloorService` owns Level reference assignment/lifecycle and `ElementVerticalPlacementService` defines the legacy-compatible bottom/top/effective-height resolution contract. `LevelReferenceHealthService` participates in Health All and Release Check.

This semantic Level contract is **source-implemented**, but native host/opening/curtain/rebar placement and Floor/Level assignment UI are intentionally not exposed as complete until all dependent native systems consume the same resolver. That integration is parked as P0 LOCAL_ONLY/runtime-sensitive work in `docs/LOCAL-AGENT-OPEN-WORK-ADDENDUM-2026-08-10.md`.

## Native source-edit relation

If a tracked Grid `LINE`/`ARC` is edited with native BricsCAD tools, the authoritative-source reconcile path is `QS3DSYNCSOURCE`. The command refreshes tracked source-derived semantic state through the guarded source-reconcile contract; it does not turn Grid into generated native 3D geometry or imply grip/jig parity.

## Local V25 validation

A local-capable agent should include Grid in the exact-SHA runtime matrix:

1. capture one LINE and one ARC;
2. verify Grid semantic ownership and `LengthM`/`Count` after `QS3DREGEN`;
3. edit/move a tracked Grid source and verify `QS3DSYNCSOURCE` preserves ownership while refreshing semantic state;
4. save/reopen and verify drawing-local source Handle provenance;
5. select generated/unrelated QS3D output and verify it cannot be recaptured as Grid;
6. select POLYLINE/Solid3d/text and verify the entire Grid operation fails before mutation;
7. test millimeter and metre drawings;
8. verify UI/selection sync and Locate behavior;
9. when a local Grid-label UI/command exists, verify reviewed ordering, semantic label persistence, duplicate-label fail-closed behavior and save/reopen before calling native naming complete.

Until that runtime pass exists, describe `QS3DGRID` as `REMOTE_DONE` / source-implemented and statically guarded, not `LOCAL_PASS` or V25-runtime-certified. `GridNamingService` is likewise Core/source functionality only until a V25 interaction layer is explicitly implemented and qualified.
