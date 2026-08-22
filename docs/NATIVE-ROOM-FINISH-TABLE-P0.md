# Native Room Finish Schedule Table P0

Status: **source-implemented; exact-SHA licensed BricsCAD V25 qualification is still required**.

This specialized native Table consumes the existing authoritative `RoomFinishScheduleBuilder`. It does not duplicate HT_Phòng quantity, room-link, material-unit or lifecycle rules in the BricsCAD adapter.

## Commands

```text
QS3DFINISHTABLE
QS3DFINISHTABLEREFRESH
QS3DFINISHTABLEREMOVE
QS3DFINISHTABLEHEALTH
```

`QS3DFINISHTABLE` creates/replaces one project-owned native BricsCAD `Table` at a picked ModelSpace point. `...REFRESH` rebuilds at the persisted drawing-local WCS position from the current authoritative Room Finish schedule. `...REMOVE` erases only the positively owned artifact and clears its project metadata. `...HEALTH` is read-only.

## Authoritative source

Rows come directly from `RoomFinishScheduleBuilder.Build(project)` after that builder performs the existing HT_Phòng rules, including:

- `RoomFinishIdentityService.ValidateProject(project)`;
- Floor/Room/Family/Material resolution;
- quantity exclusion through `AutoRoomLifecycle.IsExcludedFromQuantity`;
- Room relationship through `AutoRoomLifecycle.ResolveRoomReferenceId`;
- Material unit compatibility through `ProjectMaterialCatalog`;
- category-specific Length/Area metrics;
- authoritative `PrimaryQuantity` and unit hint.

The native adapter only formats the resulting row fields into columns: Floor, Room, finish category, Family, Material, unit, count, Length, Area and Primary Quantity.

## Ownership and rollback

The Table summarizes multiple finish elements and rooms, so it uses project-level ownership through `ProjectOwnedNativeTableArtifactService`, not a fake semantic row owner.

Artifact identity:

- RegApp/XData: `QS3DDOC`;
- document ID: `RoomFinishSchedule`;
- document kind: `RoomFinishTable`;
- metadata prefix: `GeneratedRoomFinishTable`;
- exact project ID, ownership version and authoritative snapshot fingerprint.

Project metadata persists handle, WCS position and shape. Replacement/removal is allowed only after complete metadata and matching live `Table` + `QS3DDOC` ownership/fingerprint checks. Native CAD changes and project metadata/audit changes remain inside rollback-capable transaction boundaries backed by `ProjectStateSnapshot`.

## Live health

`RoomFinishNativeTableBuilder.Inspect` namespaces shared native Table issues with `ROOM_FINISH_` so Health/Release cannot collapse them with another schedule artifact. Read-only diagnostics cover:

- corrupt/partial metadata;
- authoritative Room Finish snapshot stale;
- missing/wrong-type native entity;
- ownership mismatch;
- row/column shape drift;
- title/header/body text drift;
- persisted WCS position drift.

Cell detail output is bounded. Health never rewrites or erases a mismatched Table. The provider is registered in the shared native runtime-health aggregator consumed by Health and `QS3DRELEASECHECK`.

## P0 context

Creation is ModelSpace-only. A new insertion point requires a UCS with XY parallel to WCS XY and is transformed to drawing-local WCS. Refresh uses the stored WCS position and therefore does not depend on current UCS orientation. Text/row/column sizes use `CadUnitService.MetersToDrawingUnits`.

PaperSpace/Layout placement, sheets/viewports, title blocks, annotation scales and standards-specific TableStyle presets remain separate work.

## Local licensed BricsCAD V25 qualification

On the exact source SHA:

1. build `Release|x64` against installed BricsCAD V25 managed assemblies;
2. `NETLOAD` and verify all four commands register exactly once;
3. create Room + FloorFinish/Waterproofing/Skirting/WallFinish/CeilingFinish examples across floors/rooms/materials;
4. compare native Table rows, units and quantities against the existing Room Finish schedule/XLSX output;
5. verify excluded lifecycle elements do not reappear in the native Table;
6. verify Vietnamese/Unicode Room/Family/Material labels;
7. verify millimetre and metre drawings retain physically consistent Table sizing;
8. verify World UCS/rotated planar UCS creation, stored-WCS refresh and fail-closed tilted-UCS creation;
9. edit semantic finish/material/room data and verify stale health before refresh;
10. edit live Table text/shape/position and verify health/release report drift without repair;
11. corrupt handle/XData on a disposable copy and verify refresh/remove do not erase foreign CAD;
12. manually erase the owned Table, then verify missing health and safe refresh recreation;
13. save/reopen, Undo/Redo and switch between two DWGs; verify ownership remains project/drawing-local;
14. confirm Layout/PaperSpace build/refresh refuses unsupported P0 placement;
15. run the normal local runtime/support-bundle qualification and archive sanitized evidence only.

Do not claim V25 runtime completion until these tests run on a licensed BricsCAD V25 installation.

## Still open

- specialized native Tables for BQ, BBS, Material Usage and other authoritative schedules;
- richer TableStyle/column sizing/formatting policies;
- Layout/Sheet/Viewport/title-block lifecycle;
- exact V25 Unicode/HiDPI/save-reopen/Undo/multi-DWG qualification.
