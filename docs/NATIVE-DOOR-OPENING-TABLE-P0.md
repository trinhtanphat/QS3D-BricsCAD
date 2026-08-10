# Native Door / Opening Schedule Table P0

Status: **source-implemented; exact-SHA licensed BricsCAD V25 qualification is still required**.

This is the first specialized native schedule Table built on the reusable project-owned `QS3DDOC` Table artifact service. It consumes the existing authoritative `DoorOpeningScheduleBuilder`; it does not reproduce Door/Opening calculations with generic documentation templates.

## Commands

```text
QS3DDOOROPENINGTABLE
QS3DDOOROPENINGTABLEREFRESH
QS3DDOOROPENINGTABLEREMOVE
QS3DDOOROPENINGTABLEHEALTH
```

`QS3DDOOROPENINGTABLE` builds/replaces one project-owned native BricsCAD `Table` at a picked ModelSpace point. `...REFRESH` keeps the persisted drawing-local WCS insertion point and rebuilds from current authoritative schedule rows. `...REMOVE` erases only a positively owned live Table and clears its project metadata. `...HEALTH` is read-only.

## Authoritative data source

Rows come directly from `DoorOpeningScheduleBuilder.Build(project)`. The native adapter only formats the already-computed row fields:

- Floor;
- Door/WallOpening category;
- Family;
- Material;
- Width, Height, Sill Height and Thickness in metres;
- Count;
- authoritative grouped `OpeningAreaM2`;
- Host count.

The schedule builder remains responsible for grouping, Family/Instance value resolution, stored `OpeningAreaM2` use, Door/WallOpening membership and `HostWallId` traceability. The native Table adapter does not run a second geometry/quantity formula.

## Project-level ownership

A schedule Table summarizes multiple semantic elements, so it must not pretend to belong to the first Door/Opening row. The shared `ProjectOwnedNativeTableArtifactService` uses:

- RegApp/XData: `QS3DDOC`;
- document ID: `DoorOpeningSchedule`;
- document kind: `DoorOpeningTable`;
- metadata prefix: `GeneratedDoorOpeningTable`;
- exact project ID;
- ownership version;
- authoritative snapshot fingerprint;
- persisted drawing-local WCS position, row count and column count.

Replacement/removal is destructive only after persisted metadata is complete and the live object resolves to `Table` with matching project/document/kind/fingerprint ownership. Wrong type, foreign XData or partial metadata fails closed.

Build/remove use `ProjectStateSnapshot`, native CAD transaction rollback and project audit/revision mutation while CAD is still rollback-capable.

## Live health

The shared read-only runtime health compares the current authoritative `DoorOpeningScheduleBuilder` snapshot against the generated artifact and live native Table. It detects:

- partial/corrupt project metadata;
- semantic schedule fingerprint stale;
- missing or wrong-type native handle;
- `QS3DDOC` ownership mismatch;
- live row/column shape drift;
- title/header/body text drift;
- drawing-local WCS position drift.

Cell-detail diagnostics are bounded. Health never repairs or erases native CAD. The Door/Opening provider is registered in the shared native runtime-health aggregator, which is consumed by normal Health and `QS3DRELEASECHECK`.

## P0 context

The current source contract is deliberately limited to ModelSpace and planar UCS with XY parallel to WCS XY. Picked UCS coordinates are transformed to drawing-local WCS. Text/row/column sizes are converted from metres through `CadUnitService.MetersToDrawingUnits`.

PaperSpace/Layout placement, sheet sets, viewport scales, standards-specific TableStyle presets and automatic column fitting remain separate documentation work.

## Local licensed BricsCAD V25 qualification

On the exact source SHA:

1. build `Release|x64` against installed V25 managed assemblies;
2. `NETLOAD` and verify the four commands register once;
3. create semantic Door and WallOpening elements with multiple floors/families/materials/hosts;
4. compare native Table rows/count/area/host values with the existing Door/Opening schedule/XLSX output;
5. verify Vietnamese/Unicode text and realistic long Family/Material values;
6. verify millimetre and metre drawings produce physically consistent table sizing;
7. verify World UCS + rotated planar UCS and fail-closed tilted UCS;
8. edit a Door/Opening semantic value and verify health reports stale before refresh;
9. edit native cells, insert/remove rows/columns, move the Table and verify health/release report live drift without repair;
10. corrupt the persisted handle or `QS3DDOC` XData on a disposable copy and verify refresh/remove never erase the foreign object;
11. manually erase the owned Table and verify health reports missing; refresh should recreate from persisted authoritative state;
12. save/reopen, Undo/Redo and multi-DWG switch; verify project ownership never crosses drawings;
13. confirm Layout/PaperSpace build/refresh refuses the unsupported P0 context;
14. run the normal local runtime/support bundle qualification and record only sanitized evidence.

Do not claim V25 runtime completion until these tests run on a licensed BricsCAD V25 installation.

## Still open

- specialized native Tables for BQ, BBS, Room Finish, Material Usage and other authoritative schedules;
- richer TableStyle/column sizing/formatting policies;
- Layout/Sheet/Viewport/title-block lifecycle;
- exact V25 Unicode/HiDPI/save-reopen/Undo/multi-DWG qualification.
