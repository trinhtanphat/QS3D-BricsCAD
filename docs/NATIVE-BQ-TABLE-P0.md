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

Create/refresh regenerate dirty semantic quantities before rendering the authoritative BQ snapshot. Remove is ownership-scoped. Health is read-only and never regenerates.

## Exact BQ/XLSX parity

Rows come directly from `ProjectQuantityReportBuilder.Group(project)`. `XlsxQuantityExporter` consumes the same `QuantityReportRow` model. The native Table therefore mirrors the same **20 columns** (including `Zone`):

1. Tầng;
2. Zone;
3. Loại;
4. Tên cấu kiện / Family;
5. SL;
6. BT gộp (m³);
7. Trừ giao (m³);
8. BT còn (m³);
9. Cốp pha (m²);
10. Dài (m);
11. Chu vi ngoài (m);
12. Chu vi trong (m);
13. DT cửa (m²);
14. Thành bên (m²);
15. DT đáy (m²);
16. DT đỉnh (m²);
17. DT khác (m²);
18. QS3D Element ID;
19. CAD Handle (hex);
20. QS3D Drawing Fingerprint.

The last three columns preserve the same traceability available in the XLSX report. The adapter does not invent another quantity or traceability formula.

`ProjectQuantityReportBuilder` remains authoritative for Room Finish identity validation, lifecycle quantity exclusion, Floor/Family grouping, source-handle traceability, quantity fallback rules and finite/overflow-safe aggregation through `QuantityReportMath`.

## Regeneration boundary

The existing `QS3DBQ` flow regenerates semantic quantities before presenting BQ. `QS3DBQTABLE` and `QS3DBQTABLEREFRESH` preserve that behavior through `RegenerationEngine(RegeneratorCatalog.CreateDefault()).RegenerateDirty(project)`, followed by the native Table operation.

These are two explicit transactional operations rather than a fake transaction spanning Core semantic regeneration and native CAD. If Table creation fails, a valid completed semantic regeneration is retained; the Table service still prevents partial native commit within its own transaction.

`QS3DBQTABLEHEALTH` never mutates. If the BQ artifact exists while any semantic element remains dirty, it reports `BQ_TABLE_PROJECT_DIRTY`.

## Ownership, persistence and portability

The BQ Table summarizes many elements and therefore uses project-level `ProjectOwnedNativeTableArtifactService` ownership:

- RegApp/XData `QS3DDOC`;
- document ID `QuantityReportSchedule`;
- document kind `BqQuantityTable`;
- metadata prefix `GeneratedBqTable`;
- exact project ID, ownership version and snapshot fingerprint.

Replacement/removal requires complete metadata and matching live `Table` + XData/fingerprint. Foreign or wrong-type CAD is never erased. Native mutation uses CAD transaction rollback plus `ProjectStateSnapshot` for pre-commit project metadata/audit rollback.

`.qsdb` persists project metadata so save/reopen can refresh/remove the same artifact. Portable `QS3D.SemanticSnapshot` deliberately excludes `ProjectState.Metadata`, so drawing-local handles/fingerprint/positions do not become portable ownership authority.

## Live health and Release

BQ diagnostics are namespaced with `BQ_` and enter the fail-isolated native runtime-health aggregator. Read-only checks cover metadata corruption, authoritative snapshot stale, missing/wrong type, QS3DDOC ownership mismatch, row/column shape drift, title/header/body text drift, WCS position drift and semantic dirty state.

The shared aggregator is consumed by Health and `QS3DRELEASECHECK`. This makes BQ Table drift a release blocker without claiming that the separate licensed V25 runtime matrix has passed.

## P0 context

Creation is ModelSpace-only. A newly picked point requires a UCS whose XY plane is parallel to WCS XY and is transformed to drawing-local WCS. Refresh uses the persisted WCS position and is independent of current UCS orientation. Table dimensions use `CadUnitService.MetersToDrawingUnits`.

PaperSpace/Layout/sheet/view/title-block lifecycle and standards-specific TableStyle/column formatting remain separate work.

## Local licensed BricsCAD V25 qualification

On the exact source SHA:

1. build `Release|x64` against installed V25 managed assemblies;
2. `NETLOAD` and verify the four BQ Table commands register once;
3. prepare representative walls/structure/rooms/finishes/openings with dirty quantities and verify create/refresh regenerates before Table output;
4. compare all 19 native columns against `XlsxQuantityExporter` for the exact same `QuantityReportRow` set, including Element IDs, source handles and drawing fingerprint;
5. verify lifecycle-excluded HT_Phòng elements remain excluded;
6. verify Vietnamese/Unicode Floor/Family/Category text;
7. verify millimetre and metre drawings retain physically consistent sizing;
8. verify planar-UCS creation, stored-WCS refresh and fail-closed tilted-UCS creation;
9. mutate semantic inputs without refresh and verify dirty/stale health appears;
10. mutate live Table text/shape/position and verify Health/Release reports drift without repair;
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
