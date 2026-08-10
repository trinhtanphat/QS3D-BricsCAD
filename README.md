# QS3D for BricsCAD V25

Clean-room BricsCAD V25 quantity takeoff / semantic 3D QS plugin inspired by the workflow shown in the supplied BLT3D references. The goal is a BLT-like day-to-day workflow inside BricsCAD while keeping the implementation independent. This repository does **not** contain BLT source/binaries, BricsCAD proprietary assemblies, or private drawings.

## Target

- BricsCAD V25 on Windows x64
- Plugin adapter: C# / .NET Framework 4.8 / WPF / BricsCAD .NET API
- Core engine: `netstandard2.0`
- UI: native BricsCAD viewport + QS3D Ribbon + docked WPF palettes
- Project source of truth: DWG geometry + `.qsdb` semantic metadata

## Current source status — 2026-08-10

The repository is beyond prototype stage. Source currently includes:

- BLT-style three-pane workspace: Model tree, Family/Type list, grouped property inspector, selected-object review, HT_Phòng, Xref/Drawing manager and Layer manager.
- Category-aware **Bóc chọn** flow so a user can select a model group/Family and capture the current CAD selection without memorizing commands.
- Vietnamese property labels/groups with finite-number validation and units for common geometry/rebar properties.
- Semantic Project/Zone/Floor/Family/Element model, `.qsdb` schema migration, deterministic regeneration, audit, revision baseline/diff, template import/export and Model Health.
- Tường KT workflows for Tường Gạch, Vách Kính and Trụ Tường, including explicit semantic capture commands and a shared guarded 3D pipeline.
- Tường KT 3D source paths accept LINE and open POLYLINE centerlines for all three variants; polyline bulges are tessellated and converted through the deterministic wall-footprint engine.
- Room capture plus `QS3DROOMAUTO` bounded-face discovery from planar LINE/POLYLINE/ARC networks. Direct ARC and polyline bulges are tessellated with configurable sagitta; the adapter enforces plan-view/+Z and cross-source elevation tolerance before the Core engine handles intersection/T-junction topology and non-destructive stale-room lifecycle.
- Door/Opening host linking and physical boolean subtraction source path for supported generated LINE-host solids. The cut fingerprint includes live host/opening geometry so moving an opening cannot be silently treated as an already-applied cut.
- Beam/Slab/Column/StructuralWall/Foundation/Stair/Railing/Earthwork semantic quantities and guarded native Solid3d source paths.
- Rebar notation/BBS, XLSX/CSV export, review/Locate, rectangular column rebar layout and guarded 3D bar source path.
- BQ grouping/filtering/Locate/XLSX, Quick Takeoff, recognition/review/auto-accept, Layer/Xref adapters, Ribbon, Full Domain Hub, viewport tools and release packaging/DemandLoad scripts.
- Static preflights and deterministic Core smoke coverage for the geometry/quantity workflows above, including regression guards that require the Tường Gạch/Vách Kính/Trụ Tường 3D wiring and planar ARC-aware Room Auto adapter to remain connected end-to-end.

## Main commands

### Workspace / project
- `QS3D`, `QS3DHIDE`, `QS3DDOMAIN`
- `QS3DSAVE`, `QS3DRELOAD`, `QS3DREFRESH`, `QS3DREGEN`
- `QS3DHEALTH`, `QS3DRUNTIMEPROBE`

### Semantic model
- `QS3DROOM`, `QS3DROOMAUTO`
- `QS3DWALL`, `QS3DGLASSWALL`, `QS3DWALLPIER`
- `QS3DOPENING`, `QS3DDOOR`, `QS3DLINKHOST`, `QS3DCUTOPENINGS`
- `QS3DBEAM`, `QS3DSLAB`, `QS3DCOLUMN`, `QS3DSTRUCTWALL`, `QS3DFOUNDATION`, `QS3DSTAIR`, `QS3DRAILING`, `QS3DEARTHWORK`
- `QS3DFINISH`, `QS3DTAKEOFF`, `QS3DBUILD3D`

### Quantity / rebar / review
- `QS3DB4D`
- `QS3DBQ`, `QS3DED2`, `QS3DEXCELLOCATE`
- `QS3DBBSVIEW`, `QS3DBBS`, `QS3DBBSCSV`, `QS3DREBAR3D`
- `QS3DRECOGNIZE`, `QS3DRECOGNIZEAUTO`
- `QS3DREVBASE`, `QS3DREVDIFF`

See [`docs/COMMANDS.md`](docs/COMMANDS.md) for the complete command/workflow reference.

## Architecture

- `src/QS3D.Core` — CAD-independent domain, persistence, geometry, quantity, recognition, revision, rebar and reporting logic.
- `src/QS3D.BricsCAD.V25` — BricsCAD document/database adapters, native geometry builders, commands, WPF palettes and Ribbon integration.
- `tests/QS3D.Core.SmokeTests` — deterministic Core regression/smoke suite.
- `scripts` — static preflight, V25 packaging, DemandLoad install/uninstall and runtime harness support.
- `docs` — requirements, UI specification, implementation status, runtime gate and handoff documentation.

## Release/runtime truth

Source presence is **not** the same as BricsCAD V25 runtime proof. Before calling a release production-ready, the following still require a licensed Windows x64 BricsCAD V25 environment:

1. compile the V25 adapter against the exact V25 managed assemblies;
2. NETLOAD/DemandLoad the produced DLL and run command/Ribbon/palette smoke tests;
3. test private representative DWGs, save/reopen and multi-DWG lifecycle;
4. verify native Solid3d boolean/rebar/Tường KT behavior in V25, including Tường Gạch/Vách Kính/Trụ Tường LINE and open-POLYLINE cases;
5. verify Room Auto with mixed LINE/POLYLINE/ARC plan-view boundaries and non-planar rejection;
6. capture visual regressions at 100/125/150/200% DPI with Vietnamese Unicode;
7. run performance tests on large room-boundary and quantity models.

Until those gates are green, runtime-dependent features are described as **implemented source paths**, not as verified production behavior.

## Build policy

Do not commit `BrxMgd.dll`, `TD_Mgd.dll`, BLT/BLT3D folders, or private DWG/DOCX fixtures. The BricsCAD plugin resolves V25 assemblies through `BRICSCAD_V25_DIR` with `Private=false`.

GitHub Actions on `main` are **manual-only and owner-controlled**. Documentation/Markdown, `docs:` and `chore:` commits do not need GitHub CI, and no commit/push/merge should dispatch Actions automatically. Even source changes run GitHub CI only when the repository owner explicitly requests it.

This is a multi-agent repository. Sync latest `main` before work and again before each write so concurrent changes are not overwritten. Prefer source/static work remotely; reserve licensed BricsCAD V25/private-DWG validation for a machine that actually has those resources.

Read `CI_POLICY.md` and `AGENTS.md`, then `docs/CI-READINESS.md`, before changing CI or running any GitHub Action.
