# QS3D BricsCAD V25 master plan

## V1 implemented foundation

- clean-room layered architecture: Core + V25 adapter + WPF UI;
- source-of-truth policy, `.qsdb`, project lock/backup/recovery;
- dependency/regeneration/rules/health/revision foundations;
- native Ribbon bootstrapper and BLT3D-familiar QS3D workspace;
- active Zone/Floor/Family and semantic property flow;
- Room / Tường KT / Opening / Door semantic capture;
- first native 3D Tường KT path for selected LINE geometry;
- HT_Phòng finish generation;
- host linking and wall-opening quantity deduction;
- live Layer/Xref read/control;
- BQ semantic grouping/filter/column visibility/Locate/XLSX;
- Quick Takeoff deterministic Length/Area/Count regeneration;
- fixed-point dirty regeneration via `QS3DREGEN` and automatic regeneration before BQ/BBS/Refresh;
- manual-only CI gates.

## V1.5 source implementation now present

- deterministic Beam/Slab/Column/StructuralWall/Foundation/Stair/Railing/Earthwork quantity regenerators;
- BricsCAD semantic capture commands + Ribbon entry points for those structural categories;
- StructuralWall Door/Opening host deduction and safe re-host dirty propagation;
- BBS model from semantic `Rebar*` properties: count/spacing/compound notation, positive-value validation, bar mark, shape, cutting length, lap/anchor/hook allowance, waste and kg calculations;
- real XLSX BBS export through `QS3DBBS`;
- detailed revision snapshots/diff for category/family/floor/zone/properties/source handles/quantities;
- Model Health rebar validation + structural material validation;
- smoke/preflight source guards for structural, BBS, revision and fixed-point regeneration paths.

## Remaining V1 runtime-hardening sequence

1. Gate A source/preflight review.
2. Gate B Core CI **only when explicitly approved** for the newest head.
3. Gate C licensed Windows BricsCAD V25 build.
4. Gate D `NETLOAD`, Ribbon/palette, multi-DWG and Unicode/HiDPI smoke test.
5. private sample DWG regression: wall/room/opening/finish/structural/BQ/BBS/save/reopen.
6. harden wall polylines/corners/joins and opening solid boolean transaction after real V25 geometry results are known.
7. visual screenshot comparison against the approved QS3D target UI.
8. only after A-D + regression are green, consider automatic PR CI.

## Runtime-dependent / future V2

- native 3D Beam/Slab/Column/StructuralWall/Foundation/Stair authoring and geometric rebar placement;
- automatic room-boundary discovery from arbitrary intersecting wall networks;
- physical opening/door boolean subtraction in generated wall solids;
- revision visualization overlays and richer persisted audit provenance;
- template/material/classification import/export ecosystem;
- recognition engine + optional AI suggestion layer;
- installer, signed updater, optional Cloudflare license/update backend;
- future AutoCAD adapter reusing `QS3D.Core`.

The core/semantic implementation can continue without BricsCAD installed, but any claim about native geometry, NETLOAD behavior, Ribbon runtime, installer autoload or physical rebar must wait for the licensed V25 runner/session.
