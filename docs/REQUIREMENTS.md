# Requirements baseline from the supplied BRC document

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
- Full-screen modeless summary window.
- Filter by floor/category/search.
- Group elements by floor + type + family.
- Quantity columns include count, gross/deduction/net concrete, formwork, length, perimeter and finish/opening areas.
- Column visibility controls.
- Export to real `.xlsx` without requiring Microsoft Excel to be installed.
- Preserve a bidirectional identity bridge: QS3D Element ID ↔ CAD Handle in each exported aggregate row.
- Read a selected QS3D export row back into BricsCAD and select/zoom the referenced entities.
- Support the supplied legacy BLT workbook convention where hidden cells contain one or more decimal handles prefixed with `$`; convert those decimal values to BricsCAD hexadecimal handles without modifying the source workbook.
- Treat a CAD Handle as valid only together with the owning DWG fingerprint; a copied/mismatched `.qsdb` must fail closed instead of silently rebinding Handles.

## Generated 3D safety

- Mark every QS3D-generated entity with project/element/category ownership data inside the DWG.
- Never erase or boolean-modify a Solid3d solely because its hexadecimal Handle matches persisted text; the live ownership marker must also match.
- Quantity regeneration must preserve a stale `Geometry` flag. Only a successfully committed CAD build/replacement may mark generated geometry clean.

## B4D-style drawing scan

- Scan the entire active Current Space without requiring a manual selection.
- Read length, closed area and native Region/Hatch/Solid3d area/volume where the BricsCAD entity exposes it.
- Auto-capture only unambiguous high-confidence classifications; retain ambiguous results for human review.
- Never mutate or package BLT binaries/private DWG/XLSX reference files.

The raw user DOCX and DWG are deliberately **not committed** to this public repository.
