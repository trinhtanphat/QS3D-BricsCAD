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
- the original DWG entity remains the source of truth and keeps its stable CAD Handle provenance;
- the command does not create or claim native Grid 3D geometry.

## Product boundary

This is the first guarded Grid reference surface. It does **not** yet mean the following are complete:

- automatic Grid naming/renumbering bubbles;
- rectangular/radial Grid systems;
- Grid intersection constraints;
- dimensions/annotations tied to Grid IDs;
- Direct Draw Grid with transient jig/repeat authoring;
- automatic snapping/hosting of structure to Grid intersections.

Those features should build on the existing `ElementCategory.Grid` semantic model rather than adding a competing Grid store.

## Floor / Level relation

QS3D already has `FloorDefinition` with stable ID/name/elevation and element `FloorId`; do not introduce a duplicate `LevelDefinition` merely to resemble another product. Future top/bottom reference semantics should extend the current Floor model with an explicit migration and regeneration contract.

## Local V25 validation

A local-capable agent should add Grid to the exact-SHA runtime matrix:

1. capture one LINE and one ARC;
2. verify Grid semantic ownership and `LengthM`/`Count` after `QS3DREGEN`;
3. save/reopen and verify source Handle provenance;
4. select generated/unrelated QS3D output and verify it cannot be recaptured as Grid;
5. select POLYLINE/Solid3d/text and verify the entire Grid operation fails before mutation;
6. test millimeter and metre drawings;
7. verify UI/selection sync and Locate behavior.

Until that runtime pass exists, describe `QS3DGRID` as source-implemented/statically guarded, not V25-runtime-certified.
