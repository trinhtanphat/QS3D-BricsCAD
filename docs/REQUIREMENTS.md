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

The raw user DOCX and DWG are deliberately **not committed** to this public repository.
