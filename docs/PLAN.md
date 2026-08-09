# QS3D BricsCAD V25 master plan

## V1 implemented foundation

- clean-room layered architecture: Core + V25 adapter + WPF UI;
- source-of-truth policy, `.qsdb`, project lock/backup;
- dependency/regeneration/rules/health/revision foundations;
- native Ribbon bootstrapper and BLT3D-familiar QS3D workspace;
- active Zone/Floor/Family and semantic property flow;
- Room / Tường KT / Opening / Door semantic capture;
- first native 3D Tường KT path for selected LINE geometry;
- HT_Phòng finish generation;
- host linking and wall-opening quantity deduction;
- live Layer/Xref read/control;
- BQ semantic grouping/filter/column visibility/Locate/XLSX;
- manual-only CI gates.

## Remaining V1 runtime-hardening sequence

1. Gate A source/preflight review.
2. Gate B Core CI **only when explicitly approved**.
3. Gate C licensed Windows BricsCAD V25 build.
4. Gate D `NETLOAD`, Ribbon/palette, multi-DWG and Unicode/HiDPI smoke test.
5. private sample DWG regression: wall/room/opening/finish/BQ/save/reopen.
6. harden wall polylines/corners/joins and opening solid boolean transaction after real V25 geometry results are known.
7. visual screenshot comparison against the approved QS3D target UI.
8. only after A-D + regression are green, consider automatic PR CI.

## V1.5 / V2

- advanced Beam/Slab/Column/StructuralWall/Foundation/Cầu thang authoring;
- rebar geometry + BBS beyond notation/weight core;
- revision visualization and richer audit provenance;
- template/material/classification import/export ecosystem;
- recognition engine + optional AI suggestion layer;
- installer, signed updater, optional Cloudflare license/update backend;
- future AutoCAD adapter reusing `QS3D.Core`.
