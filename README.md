# QS3D for BricsCAD V25

Clean-room BricsCAD V25 quantity takeoff / BIM-QS plugin inspired by the workflow shown in the supplied BLT3D references. This repository does **not** contain BLT source, BLT binaries, BricsCAD proprietary assemblies, or private drawings.

## Target
- BricsCAD V25 on Windows x64
- Plugin: C# / .NET Framework 4.8 / WPF / BricsCAD .NET API
- Core engine: `netstandard2.0`
- UI: QS3D ribbon + docked WPF palettes around the native BricsCAD viewport
- Project source of truth: DWG geometry + `.qsdb` semantic metadata

## Implemented source domains
- Project / Zone / Floor / Family / semantic Element model
- Room / finishes / architectural wall / opening / door workflows
- Beam / Slab / Column / Structural Wall / Foundation / Earthwork quantity engines
- source-level structural `Solid3d` generation paths for LINE and closed-polyline input
- Rebar notation, spacing/count calculation, shapes, BBS aggregation, kg/m and CSV export
- rule-based recognition from layer + nearby text + entity type with confidence/margin review
- grouped BQ with concrete, formwork, length/area and steel kg + XLSX export
- persistent `.qsdb` schema migration, backup recovery and protected-load mode
- persistent `.qsrev` revision baseline + per-quantity diff
- Model Health for host/family/floor/zone/material/geometry/rebar/orphan/duplicate-handle/recovery states
- Layer/Xref manager, selection inspection, Locate, Ribbon, multi-document lifecycle
- release packaging script that excludes BricsCAD vendor assemblies

## Main commands
- `QS3D`, `QS3DHIDE`, `QS3DINSPECT`, `QS3DHEALTH`, `QS3DBQ`
- `QS3DROOM`, `QS3DWALL`, `QS3DOPENING`, `QS3DDOOR`, `QS3DFINISH`
- `QS3DBEAM`, `QS3DSLAB`, `QS3DCOLUMN`, `QS3DSTRUCTWALL`, `QS3DFOUNDATION`, `QS3DEARTHWORK`
- `QS3DREBAR`, `QS3DBBS`
- `QS3DRECOGNIZE`, `QS3DRECOGNIZEAUTO`
- `QS3DREGEN`, `QS3DREVBASE`, `QS3DREVDIFF`

See `docs/COMMANDS.md` for the complete command map.

## Verification status
GitHub-hosted Core CI validates the source tree, builds `QS3D.Core` in Release and runs deterministic regression tests. The BricsCAD V25 adapter itself remains runtime-gated until a licensed Windows self-hosted runner with the exact V25 assemblies is available; source implementation must not be confused with a successful `NETLOAD` test.

## Build policy
Do not commit `BrxMgd.dll`, `TD_Mgd.dll`, BLT/BLT3D folders, or private DWG/DOCX fixtures. The BricsCAD plugin resolves V25 assemblies through `BRICSCAD_V25_DIR` with `Private=false`.

Read `docs/CI-READINESS.md`, `docs/V25-RUNNER.md` and `docs/RUNTIME-TEST-CHECKLIST.md` before running V25 integration/release gates.
