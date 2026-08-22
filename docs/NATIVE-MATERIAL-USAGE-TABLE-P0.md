# Native Material Usage Schedule Table P0

Status: **source-implemented; exact-SHA licensed BricsCAD V25 qualification is still required**.

This specialized native Table consumes the existing authoritative `MaterialUsageScheduleBuilder`. It does not duplicate material quantity formulas in the BricsCAD adapter.

## Commands

```text
QS3DMATERIALTABLE
QS3DMATERIALTABLEREFRESH
QS3DMATERIALTABLEREMOVE
QS3DMATERIALTABLEHEALTH
```

`QS3DMATERIALTABLE` creates/replaces one project-owned native BricsCAD `Table` at a picked ModelSpace point. `...REFRESH` rebuilds at the persisted drawing-local WCS position. `...REMOVE` erases only the positively owned artifact and clears its project metadata. `...HEALTH` is read-only.

## Authoritative source

Rows come directly from `MaterialUsageScheduleBuilder.Build(project)`. The existing Core builder remains responsible for:

- Room Finish identity validation and lifecycle quantity exclusion;
- Material resolution from Instance/Family;
- material-unit lookup through `ProjectMaterialCatalog`;
- category-specific Length/Area/Volume/Mass quantity selection;
- Curtain Wall frame material/component handling;
- overflow-safe aggregation through `QuantityReportMath`;
- authoritative `PrimaryQuantity` selection from the material unit.

The native adapter only formats the resulting row fields: Floor, Material, Unit, Component, Category, Family, Element Count, Length, Area, Volume, Mass and Primary Quantity.

## Ownership and rollback

The schedule spans many semantic elements, so ownership is project-level through `ProjectOwnedNativeTableArtifactService`.

Artifact identity:

- RegApp/XData: `QS3DDOC`;
- document ID: `MaterialUsageSchedule`;
- document kind: `MaterialUsageTable`;
- metadata prefix: `GeneratedMaterialUsageTable`;
- exact project ID, ownership version and authoritative snapshot fingerprint.

The shared service persists handle/WCS position/row+column shape in `ProjectState.Metadata`, verifies matching live `Table` + XData before destructive replacement/removal, and uses `ProjectStateSnapshot` plus native CAD transaction rollback. Foreign/wrong-type/mismatched objects are never erased.

## Live health

Material Usage diagnostics are namespaced with `MATERIAL_USAGE_` before entering the shared runtime-health aggregator. Read-only checks include:

- partial/corrupt metadata;
- authoritative schedule fingerprint stale;
- missing/wrong-type native object;
- `QS3DDOC` ownership mismatch;
- live row/column shape drift;
- title/header/body text drift;
- persisted drawing-local WCS position drift.

Cell diagnostics are bounded. The provider is included in normal native Health and `QS3DRELEASECHECK`; health never repairs or rewrites the Table.

## P0 context

Creation is ModelSpace-only. New insertion uses a planar UCS whose XY plane is parallel to WCS XY, then persists the transformed drawing-local WCS position. Refresh uses the persisted WCS position independent of current UCS orientation. Table dimensions are converted through `CadUnitService.MetersToDrawingUnits`.

PaperSpace/Layout/sheet/view/title-block lifecycle and standards-specific TableStyle/column formatting remain separate work.

## Local licensed BricsCAD V25 qualification

On the exact source SHA:

1. build `Release|x64` against installed BricsCAD V25 managed assemblies;
2. `NETLOAD` and verify all four commands register exactly once;
3. create elements across Floors/Families/Materials including walls, finishes, openings, solids and a GlassWall with CurtainFrame material quantities;
4. compare native Table rows and all Length/Area/Volume/Mass/Primary values with existing Material Usage schedule/XLSX output;
5. verify material units `m`, `m²`, `m³`, `kg` and unknown/blank-unit behavior stay identical to the Core schedule;
6. verify lifecycle-excluded HT_Phòng elements do not reappear;
7. verify Vietnamese/Unicode material/family/component labels;
8. verify millimetre and metre drawings retain physically consistent sizing;
9. verify planar UCS creation, stored-WCS refresh and fail-closed tilted-UCS creation;
10. mutate semantic/material quantities and verify stale health before refresh;
11. mutate native Table text/shape/position and verify health/release report drift without repair;
12. corrupt handle/XData on a disposable copy and verify replacement/removal never erases foreign CAD;
13. manually erase the owned Table, then verify missing health and safe refresh recreation;
14. save/reopen, Undo/Redo and multi-DWG switching; verify project ownership remains isolated;
15. confirm Layout/PaperSpace build/refresh refuses unsupported P0 placement;
16. run normal local runtime/support-bundle qualification and archive sanitized evidence only.

Do not claim V25 runtime completion until these tests run on a licensed BricsCAD V25 installation.

## Still open

- specialized native Tables for BQ, BBS and other authoritative schedules;
- richer TableStyle/column sizing/formatting policies;
- Layout/Sheet/Viewport/title-block lifecycle;
- exact V25 Unicode/HiDPI/save-reopen/Undo/multi-DWG qualification.
