# Room Finish Schedule Generation Fence

## Scope

This feature hardens `RoomFinishScheduleBuilder` against mixed-generation reporting when mutable project objects or collection slots change during a report build without a corresponding `ProjectState.ChangeVersion` advance.

## Contract

The builder captures one exact project generation before aggregation. The capture freezes source element/floor/family instance identity and order, detached semantic snapshots for the project data consumed by Room Finish reporting, the material-unit view, and detached work items used for row aggregation. Rows are built from those detached work items rather than by repeatedly reading mutable live project objects.

Before publication, and while processing work items, the builder revalidates the live project against the capture. It fails closed when project identity/version changes, a source collection entry is replaced or reordered, relevant element/floor/family semantics drift, or the material-unit view changes.

The existing Room Finish behavior remains authoritative: room provenance resolution, family/category compatibility, effective material and unit semantics, compensated length/area/primary aggregation, stable grouping order, source-handle provenance, and public row shape are preserved.

## Deterministic regression

`RoomFinishScheduleGenerationFenceSmoke` is a `ModuleInitializer` smoke. It verifies a stable report still builds, then reaches the private capture/revalidation boundary and proves both value-equivalent `ProjectElement` replacement and direct quantity mutation are rejected while `ChangeVersion` remains unchanged.

`scripts/preflight-room-finish-schedule-generation-fence.py` is auto-discovered by the aggregate feature-source guard and pins the source-instance, detached-work-item, semantic and deterministic regression contracts.

## Runtime boundary

Managed Core only. No licensed BricsCAD runtime result or `LOCAL_PASS` is required or claimed.
