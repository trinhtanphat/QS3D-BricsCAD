# Wall Quantity Takeoff

`QS3DWALLQTY` opens a BricsCAD-hosted, modeless wall quantity workspace for the owner-requested wall/takeoff workflow.

## Scope

The window is intentionally separate from `QS3DBQ`:

- the left pane is a wall browser with text, floor and wall-category filters;
- the selected-wall panel shows semantic identity, Family, floor, material, CAD provenance and the quantity facts already present in QS3D;
- the lower grid is one semantic wall per row and shows length, optional thickness/height metadata, gross concrete, deduction, net concrete and formwork;
- the footer totals always describe the same currently visible rows;
- `Tính lại` creates a detached `ProjectStateSnapshot`, regenerates that detached copy, then reads `ProjectQuantityReportBuilder.Detail`;
- `Xuất Excel` reuses `XlsxQuantityExporter` for the currently visible wall rows;
- `Bám 3D` is enabled by default: selecting a wall in the browser or detail grid revalidates that semantic wall against the current project, re-resolves its current CAD Handles, selects the corresponding CAD object(s), then queues `QS3DZOOMSELECTED`;
- `Định vị 3D` performs the same guarded reveal explicitly; when `Bám 3D` is off, double-clicking either wall list or detail table also performs the guarded reveal.

Supported wall categories are `ArchitecturalWall`, `StructuralWall`, `GlassWall` and `WallPier`.

## Safety / source-of-truth boundary

This is a read-only reporting surface. It does **not** create a project, mutate semantic state, save `.qsdb`, or write the CAD database. Viewport reveal changes only the active CAD selection/view.

The window is bound to the BricsCAD `Document` that opened it and pins that project's `ProjectId`. Refresh/export/locate fail closed if another drawing is active, the project disappears, or the source project identity changes.

A displayed detached row is never trusted directly for native selection. Before every reveal, QS3D:

1. revalidates the active source `Document` and pinned `ProjectId`;
2. re-resolves the displayed ElementId in the current canonical project;
3. verifies that the current semantic category is still one of the supported wall categories;
4. creates a detached current snapshot and rebuilds exactly that wall's `ProjectQuantityReportBuilder.Detail(...)` row to confirm current semantic identity;
5. resolves current source Handles again from the canonical project;
6. only then calls `CadHandleService.Select(...)` and queues `QS3DZOOMSELECTED`.

If the wall was deleted, retyped, the project was replaced, the row is stale, or no live source Handle can be resolved, locate refuses before native selection/zoom.

No wall quantity formula is duplicated in the UI. Gross/deduction/net concrete, length and formwork come from `ProjectQuantityReportBuilder.Detail`. Thickness/height are display-only metadata: the window shows explicit finite non-negative `*Mm`/`*M` instance or Family values when available and otherwise shows `—`; it never derives missing dimensions from volume.

## Validation

Source-safe guard:

```powershell
python scripts/preflight-wall-quantity-window.py
```

The preflight checks the original detached takeoff/export contract plus the current-row revalidation → current Handle resolution → CAD select → viewport zoom ordering for `Bám 3D`/`Định vị 3D`.

Native BricsCAD V25 modeless mouse behavior, implied-selection highlight, zoom behavior, multi-DWG switching, HiDPI layout and XLSX interaction remain LOCAL_ONLY. A remote source/static pass must not be promoted to `LOCAL_PASS`.
