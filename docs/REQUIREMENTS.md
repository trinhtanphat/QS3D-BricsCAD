# Requirements baseline from the supplied BRC document

## Product/runtime boundary

QS3D's selected product target is a **BricsCAD V25 + V26 Windows x64 hosted plugin**, not a standalone CAD desktop application.

- A matching licensed BricsCAD V25 or V26 host is required at runtime and remains the native DWG/viewport/document host.
- V25 loads `QS3D.BricsCAD.V25.dll` built for .NET Framework 4.8 (`net48`); V26 loads `QS3D.BricsCAD.V26.dll` built for .NET 8 (`net8.0-windows`). Both are managed Library plugins loaded by the matching BricsCAD host through DemandLoad or `NETLOAD`; a standalone `QS3D.exe` is not part of the current requirement.
- V25 and V26 package/build/update/runtime identity must remain explicit; a V25 assembly/package must never be relabeled as V26-compatible.
- QS3D Ribbon, palettes and modeless/full-screen-style WPF windows are plugin UI launched from inside BricsCAD. “Full-screen” below describes window size/workflow, not an independent desktop shell.
- `QS3D.Core` may run deterministic tests outside CAD and is shared by both host adapters, but that does not change the shipping product into a standalone application.
- BLT/BLT3D screenshots and terminology define clean-room workflow/UX expectations only. They do not define QS3D packaging or executable form.

See `docs/PRODUCT-BOUNDARY.md`. Any future standalone/CAD-engine direction requires a separate explicit owner decision and is not implied by “giống BLT”.

The supplied document explicitly marks these areas for completion:

1. Build the missing functions.
2. Architectural wall (`TƯỜNG KT`).
3. Room finish workflow (`HT_PHÒNG`).
4. Door/opening workflow (`Cửa`).
5. Output quantities to Excel.

The embedded screenshots add the following UI/workflow expectations:

## TƯỜNG KT
- Tree children: Tường Gạch, Vách Kính, Trụ Tường.
- Family list with Add / Delete / Vẽ 3D.
- Properties: Family name, category, floor, thickness, axis-left/right offsets, close/freeform profile, top/bottom levels, display, metadata, material.

## HT_PHÒNG
- Tree children: Phòng, Sàn Hoàn Thiện, Chống Thấm, Chân Tường, Hoàn Thiện Tường, Trần Hoàn Thiện, Lan Can.
- Room detail panel with Thêm / Bỏ / Tạo hoàn thiện.
- Generated finishes remain separate semantic elements for later quantity reporting.

## Cửa
- Tree children: Lỗ Mở Vách, Cửa Đi.
- Opening properties: width, height, thickness, bottom level/sill offset, display, metadata/material.

## Quantity report / BQ
- Full-screen modeless summary window hosted by the BricsCAD plugin.
- Filter by floor/category/search.
- Group elements by floor + type + family.
- Quantity columns include count, gross/deduction/net concrete, formwork, length, perimeter and finish/opening areas.
- Column visibility controls.
- Export to real `.xlsx` without requiring Microsoft Excel to be installed.
<<<<<<< HEAD
- Preserve a bidirectional identity bridge: QS3D Element ID ↔ CAD Handle ↔ owning DWG fingerprint in each exported aggregate row.
- Read a selected QS3D export row back into BricsCAD and select/zoom the referenced entities.
- Support the supplied legacy BLT workbook convention where hidden cells contain one or more decimal handles prefixed with `$`; convert those decimal values to BricsCAD hexadecimal handles without modifying the source workbook, and require explicit confirmation because legacy workbooks have no DWG fingerprint.
- Treat a CAD Handle as valid only together with the owning DWG fingerprint; a copied/mismatched `.qsdb` must fail closed instead of silently rebinding Handles.

## Generated 3D safety

- Mark every QS3D-generated entity with project/element/category ownership data inside the DWG.
- Never erase or boolean-modify a Solid3d solely because its hexadecimal Handle matches persisted text; the live ownership marker must also match.
- Quantity regeneration must preserve a stale `Geometry` flag. Only a successfully committed CAD build/replacement may mark generated geometry clean.
=======
- Preserve a bidirectional identity bridge: QS3D Element ID ↔ CAD Handle in each exported aggregate row.
- Read a selected QS3D export row back into BricsCAD and select/zoom the referenced entities.
- Support the supplied legacy BLT workbook convention where hidden cells contain one or more decimal handles prefixed with `$`; convert those decimal values to BricsCAD hexadecimal handles without modifying the source workbook.
>>>>>>> 645b39943 (feat: add B4D scan and Excel handle round-trip)

## B4D-style drawing scan

- Scan the entire active Current Space without requiring a manual selection.
- Read length, closed area and native Region/Hatch/Solid3d area/volume where the BricsCAD entity exposes it.
- Auto-capture only unambiguous high-confidence classifications; retain ambiguous results for human review.
- Never mutate or package BLT binaries/private DWG/XLSX reference files.

The raw user DOCX and DWG are deliberately **not committed** to this public repository.
