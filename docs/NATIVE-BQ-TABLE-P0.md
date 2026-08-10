# Native BQ Tổng hợp Table P0

Status: **source-implemented; exact-SHA licensed BricsCAD V25 qualification is still required**.

This specialized native Table consumes the existing authoritative `ProjectQuantityReportBuilder`. It does not duplicate BQ formulas in the BricsCAD adapter.

## Commands

```text
QS3DBQTABLE
QS3DBQTABLEREFRESH
QS3DBQTABLEREMOVE
QS3DBQTABLEHEALTH
```

`QS3DBQTABLE` creates/replaces one project-owned native BricsCAD `Table` at a picked ModelSpace point. `...REFRESH` rebuilds at the persisted drawing-local WCS position. `...REMOVE` erases only the positively owned artifact and clears its project metadata. `...HEALTH` is read-only.

## Authoritative source and regeneration

Rows come directly from `ProjectQuantityReportBuilder.Group(project)`. That Core builder remains responsible for Room Finish identity validation, lifecycle quantity exclusion, Floor/Family grouping, source-handle traceability, quantity fallback rules and overflow/finite-safe aggregation through `QuantityReportMath`.

The native adapter only formats the existing `QuantityReportRow` fields:

- Floor, Category, Family, Count;
- Gross Concrete, Deduction, Net Concrete;
- Formwork and Length;
- Outer/Inner perimeter;
- Door/Opening area;
- Side, Bottom, Top and Other area.

The existing `QS3DBQ` workflow regenerates semantic quantities before presenting BQ. Native BQ create/refresh preserves that behavior by running `RegenerationEngine(RegeneratorCatalog.CreateDefault()).RegenerateDirty(project)` first, then performing the native Table transaction. These are two explicit transactional operations rather than a fake cross-layer transaction: if native Table creation fails, a valid semantic regeneration is not rolled back after a possible CAD commit boundary.

`QS3DBQTABLEHEALTH` never regenerates. If the BQ artifact exists while any semantic element remains dirty, it adds `BQ_TABLE_PROJECT_DIRTY` so health cannot silently bless possibly stale quantity output.

## Ownership and rollback

The BQ Table summarizes many semantic elements, so ownership is project-level through `ProjectOwnedNativeTableArtifactService`.

Artifact identity:

- RegApp/XData: `QS3DDOC`;
- document ID: `QuantityReportSchedule`;
- document kind: `BqQuantityTable`;
- metadata prefix: `GeneratedBqTable`;
- exact project ID, ownership version and displayed authoritative snapshot fingerprint.

The shared service persists handle/WCS position/row+column shape in `ProjectState.Metadata`, verifies matching live `Table` + XData before destructive replacement/removal, and uses `ProjectStateSnapshot` plus native CAD transaction rollback. Foreign/wrong-type/mismatched objects are never erased.

## Live health

BQ diagnostics are namespaced with `BQ_` before entering the shared runtime-health aggregator. Read-only checks cover:

- partial/corrupt project metadata;
- authoritative BQ snapshot stale;
- missing/wrong-type native entity;
- `QS3DDOC` ownership mismatch;
- live row/column shape drift;
- title/header/body text drift;
- persisted drawing-local WCS position drift;
- semantic dirty state while a BQ Table exists.

Cell diagnostics are bounded. The provider is fail-isolated in the shared native runtime-health aggregator consumed by Health and `QS3DRELEASECHECK`; health never repairs or rewrites the Table.

## Persistence and portability

Project-level native Table metadata is persisted in `.qsdb` so save/reopen can refresh/remove the same owned artifact. `ProjectStateSnapshot` includes Metadata so failed pre-commit native operations can restore it. Portable `QS3D.SemanticSnapshot` interchange deliberately does **not** serialize `ProjectState.Metadata`, so drawing-local BQ handles/fingerprints/positions do not become portable ownership authority.

## P0 context

Creation is ModelSpace-only. A new insertion point requires a planar UCS whose XY plane is parallel to WCS XY and is transformed to drawing-local WCS. Refresh uses the persisted WCS position and is independent of the current UCS orientation. Table dimensions are converted through `CadUnitService.MetersToDrawingUnits`.

PaperSpace/Layout/sheet/view/title-block lifecycle and standards-specific TableStyle/column formatting remain separate work.

## Local licensed BricsCAD V25 qualification

On the exact source SHA:

1. build `Release|x64` against installed BricsCAD V25 managed assemblies;
2. `NETLOAD` and verify all four BQ Table commands register exactly once;
3. prepare representative semantic walls/structure/rooms/finishes/openings with dirty quantities, then run `QS3DBQTABLE` and verify semantic regen occurs before Table output;
4. compare every native row/metric with the existing `QS3DBQ`/Excel quantity report for the same project state;
5. verify lifecycle-excluded HT_Phòng elements remain excluded;
6. verify Vietnamese/Unicode Floor/Family/Category text;
7. verify millimetre and metre drawings retain physically consistent table sizing;
8. verify planar-UCS creation, stored-WCS refresh and fail-closed tilted-UCS creation;
9. mutate semantic inputs without refresh and verify dirty/stale health appears;
10. mutate live Table text/shape/position and verify Health/Release report drift without repairing CAD;
11. corrupt persisted handle or `QS3DDOC` XData on a disposable copy and verify refresh/remove never erase foreign CAD;
12. manually erase the owned Table and verify missing health plus safe recreation;
13. save/reopen, Undo/Redo and multi-DWG switching; verify project ownership remains isolated;
14. confirm Layout/PaperSpace build/refresh refuses unsupported P0 placement;
15. run the normal local runtime/support-bundle qualification and archive sanitized evidence only.

Do not claim V25 runtime completion until these tests run on a licensed BricsCAD V25 installation.

## Still open

- BBS authoritative native Table;
- richer TableStyle/column sizing/formatting policies;
- Layout/Sheet/Viewport/title-block lifecycle;
- exact V25 Unicode/HiDPI/save-reopen/Undo/multi-DWG qualification.
