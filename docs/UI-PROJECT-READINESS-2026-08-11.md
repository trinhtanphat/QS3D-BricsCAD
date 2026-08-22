# QS3D Project Tools readiness dashboard — 2026-08-11

## Goal

Bring the existing document-bound Project Tools closer to the supplied BLT3D project-setup reference without creating a second application shell. The window should answer, at a glance: which project/Zone/Floor is active, how much semantic catalog data exists, and whether current semantic elements still carry dirty work.

## UX changes

The top of Project Tools is split into two compact read-only bands:

1. **Project Snapshot** — project name, active Zone, active Floor/elevation, drawing unit, plus Zone/Floor/Family/Element counts.
2. **Project Readiness** — dirty element count, geometry-dirty count, quantity-dirty count, project `ChangeVersion`, and last `UpdatedUtc` timestamp.

The existing project-data, interchange, module, maintenance and Workspace commands remain available below the dashboard. No command is replaced by a decorative control.

## Read-only safety boundary

Project Tools continues to bind to the DWG that opened the window. Open/refresh uses only `ProjectContextCoordinator.TryGetReadOnly` and does not create or cache a missing project.

Readiness calculation reads persisted state only:

- `ProjectState.ActiveZoneId` / `ActiveFloorId` and existing Zone/Floor definitions;
- `Zones`, `Floors`, `Families`, `Elements` counts;
- `ProjectElement.Dirty` flags;
- `ProjectState.ChangeVersion` and `UpdatedUtc`.

The refresh path must not call `Touch`, `MarkDirty`, `MarkClean`, `IsGeneratedGeometryStale`, save/reload, regeneration, or any Core mutation service. In particular, `IsGeneratedGeometryStale()` is intentionally not used because that helper can normalize/remove generated-state metadata while answering the query; a UI snapshot must not mutate project state.

## Status semantics

`CLEAN` means only that no current element has a non-zero persisted `ElementDirtyFlags` value. It is not a release-certification claim. The UI still points users to Health for deeper validation.

When dirty elements exist, the dashboard reports total dirty plus Geometry and Quantity subsets. It does not automatically regenerate anything.

If `ActiveZoneId` or `ActiveFloorId` references a definition that cannot be resolved, the dashboard shows the stored ID with a `thiếu định nghĩa` marker rather than silently substituting another Zone/Floor.

When no QS3D project exists, all project metrics reset to a neutral empty state while drawing-unit inspection remains best-effort. No replacement project is created.

## Validation

`scripts/preflight-project-tools.py` guards:

- presence of the readiness UI fields;
- read-only `TryGetReadOnly` lookup;
- Zone/Floor/count/dirty/version/timestamp data wiring;
- absence of project-creation and semantic-mutation calls from the Project Tools code-behind;
- existing document-bound command dispatch and Project Tools command/ribbon wiring.

Native WPF layout, Vietnamese text clipping, HiDPI behavior, activation/document-switch behavior and real BricsCAD V25 rendering remain part of the existing LOCAL_ONLY V25 qualification process. No remote source review is treated as a native runtime PASS.
