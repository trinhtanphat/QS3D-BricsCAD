# QS3D for BricsCAD V25

Clean-room BricsCAD V25 quantity takeoff / 3D QS plugin inspired by the workflow shown in the supplied BLT3D references. This repository does **not** contain BLT source, BLT binaries, BricsCAD proprietary assemblies, or the user's private drawings.

## Target
- BricsCAD V25 on Windows x64
- Plugin: C# / .NET Framework 4.8 / WPF / BricsCAD .NET API
- Core engine: `netstandard2.0`
- CI: GitHub Actions, manual-only until the reviewed V25 runtime gate passes

## Commands
- `QS3D` — show left/right work palettes
- `QS3DHIDE` — hide QS3D palettes
- `QS3DINSPECT` — inspect current/prompted selection
- `QS3DBQ` — quantity summary + Excel export for current selection
- `QS3DABOUT` — build identity

## Supplied requirement coverage
- `Tường KT`: semantic catalog + requested wall property model + UI tree/property layout
- `HT_Phòng`: semantic finish catalog + room property model + UI workflow
- `Cửa`: opening/door catalog + opening dimensions model + UI workflow
- `OUTPUT`: real dependency-free `.xlsx` writer and BQ export button

## Build policy
Do not commit `BrxMgd.dll`, `TD_Mgd.dll`, BLT/BLT3D folders, or private user DWG/DOCX files.
The BricsCAD plugin project resolves required assemblies through `BRICSCAD_V25_DIR` with `Private=false`.

Read `docs/CI-READINESS.md` before running any GitHub Action.
