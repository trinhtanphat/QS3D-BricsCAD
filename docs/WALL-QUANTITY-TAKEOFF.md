# Wall Quantity Takeoff

`QS3DWALLQTY` opens a BricsCAD-hosted, modeless wall quantity workspace for the owner-requested wall/takeoff workflow.

## Scope

The window is intentionally separate from `QS3DBQ`:

- the left pane is a wall browser with text, floor and wall-category filters;
- the selected-wall panel shows semantic identity, Family, floor, material, CAD provenance and the quantity facts already present in QS3D;
- the lower grid is one semantic wall per row and shows length, optional thickness/height metadata, gross concrete, deduction, net concrete and formwork;
- the footer totals always describe the same currently visible rows;
- `Tính lại` creates a detached `ProjectStateSnapshot`, regenerates that detached copy, then reads `ProjectQuantityReportBuilder.Detail`;
- `Xuất Excel` reuses `XlsxQuantityExporter` for the currently visible wall rows.

Supported wall categories are `ArchitecturalWall`, `StructuralWall`, `GlassWall` and `WallPier`.

## Safety / source-of-truth boundary

This is a read-only reporting surface. It does **not** create a project, mutate semantic state, save `.qsdb`, or write the CAD database.

The window is bound to the BricsCAD `Document` that opened it and pins that project's `ProjectId`. Refresh/export fail closed if another drawing is active, the project disappears, or the source project identity changes.

No wall quantity formula is duplicated in the UI. Gross/deduction/net concrete, length and formwork come from `ProjectQuantityReportBuilder.Detail`. Thickness/height are display-only metadata: the window shows explicit finite non-negative `*Mm`/`*M` instance or Family values when available and otherwise shows `—`; it never derives missing dimensions from volume.

## Validation

Source-safe guard:

```powershell
python scripts/preflight-wall-quantity-window.py
```

Native BricsCAD V25 UI/layout/runtime qualification is LOCAL_ONLY and is tracked in `docs/LOCAL-AGENT-INBOX.md` as `LOCAL-015`. A remote source/static pass must not be promoted to `LOCAL_PASS`.
