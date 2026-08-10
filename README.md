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
- Property inspector with Vietnamese labels/groups, units, finite-number validation, typed controls (`TextBox`, boolean `CheckBox`, editable choice `ComboBox`) and **Family / Type vs Đối tượng / Instance** scope. A single semantic selection opens Instance scope; overrides can be reset to the Family value, and later Family edits preserve true instance overrides instead of overwriting them.
- Selected-object review actions for Locate/Zoom, **Focus**, **Cô lập/Khôi phục**, plus semantic-reference matching so Room Auto boundary-derived elements remain discoverable without duplicating source ownership.
- Semantic Project/Zone/Floor/Family/Element model, `.qsdb` schema migration, deterministic regeneration, audit, revision baseline/diff, template import/export and Model Health.
- Tường KT workflows for Tường Gạch, Vách Kính and Trụ Tường, including explicit semantic capture commands and a shared guarded 3D pipeline.
- Tường KT 3D source paths accept LINE and open POLYLINE centerlines for all three variants; polyline bulges are tessellated and converted through the deterministic wall-footprint engine.
- `QS3DWALLJUNCTIONS` analyzes selected LINE/open-POLYLINE wall centerlines and classifies L/T/X/Straight/End/Multi junction nodes with finite-safe, large-coordinate-aware geometry guards.
- `QS3DWALLSNAPPREVIEW` / `QS3DWALLSNAPAPPLY` add review-gated wall centerline cleanup: Preview fingerprints endpoint moves without mutation; Apply rejects stale previews and unsupported curved/bulged/nonsemantic source. Affected generated geometry is invalidated with ownership-aware safeguards before later rebuild. This improves source-junction cleanup but is not yet complete automatic wall-solid union/reconciliation.
- Room capture plus `QS3DROOMAUTO` bounded-face discovery from planar LINE/POLYLINE/ARC/SPLINE networks. Direct ARC and polyline bulges use configurable sagitta; SPLINE uses bounded chord-length sampling with a segment cap. Planarity/elevation checks run before Core intersection/T-junction topology and non-destructive stale-room lifecycle processing.
- Door/Opening manual host linking plus `QS3DAUTOLINKHOSTS`. Auto Host uses compatible semantic wall candidates, surface gap, Floor/Zone scope, ambiguity rejection and an independent elevation gate; it only establishes the semantic host and never silently performs a physical cut.
- Physical Door/Opening subtraction supports generated LINE hosts and guarded straight, non-bulged POLYLINE wall segments where the opening can be projected safely without crossing a corner. Supported categories include ArchitecturalWall/Tường Gạch, GlassWall/Vách Kính, WallPier/Trụ Tường and StructuralWall/Vách BTCT. Curved/bulged polyline-host cuts remain rejected rather than guessed.
- Beam/Slab/Column/StructuralWall/Foundation/Stair/Railing/Earthwork semantic quantities and guarded native Solid3d source paths.
- Rebar notation/BBS, XLSX/CSV export, review/Locate, rectangular-column rebar 3D, generated-rebar ownership/health guards, deterministic linear distribution and BBS-shape-driven 3D source paths for supported straight/L/U/Z/custom leg/turn definitions.
- BQ grouping/filtering/Locate/XLSX, Quick Takeoff, recognition/review/auto-accept, Layer/Xref adapters, Ribbon, Full Domain Hub, viewport tools and release packaging/DemandLoad scripts.
- Ribbon/Workspace/Domain Hub expose the major BLT-style workflows consistently: Tường KT, Giao tường, Snap xem/áp, Auto/Manual Host, Cửa/Lỗ, Room Auto, Focus/Isolate, BQ/BBS, column rebar and shape rebar.
- Static preflights and deterministic Core smoke coverage include geometry/rebar/Room Auto/Auto Host/wall-snap guards, command uniqueness, typed Family/Instance inspector contracts and XAML well-formedness checks. `scripts/preflight-blt-workspace.py` specifically guards the primary Workspace/Ribbon/Hub workflow parity.

## Main commands

### Workspace / project
- `QS3D`, `QS3DHIDE`, `QS3DDOMAIN`
- `QS3DSAVE`, `QS3DRELOAD`, `QS3DREFRESH`, `QS3DREGEN`
- `QS3DHEALTH`, `QS3DRUNTIMEPROBE`

### Semantic model / geometry
- `QS3DROOM`, `QS3DROOMAUTO`
- `QS3DWALL`, `QS3DGLASSWALL`, `QS3DWALLPIER`, `QS3DWALLJUNCTIONS`
- `QS3DWALLSNAPPREVIEW`, `QS3DWALLSNAPAPPLY`
- `QS3DOPENING`, `QS3DDOOR`, `QS3DAUTOLINKHOSTS`, `QS3DLINKHOST`, `QS3DCUTOPENINGS`
- `QS3DBEAM`, `QS3DSLAB`, `QS3DCOLUMN`, `QS3DSTRUCTWALL`, `QS3DFOUNDATION`, `QS3DSTAIR`, `QS3DRAILING`, `QS3DEARTHWORK`
- `QS3DFINISH`, `QS3DTAKEOFF`, `QS3DBUILD3D`

### Quantity / rebar / review
- `QS3DBQ`
- `QS3DBBSVIEW`, `QS3DBBS`, `QS3DBBSCSV`
- `QS3DREBAR3D`, `QS3DREBAR3DSHAPE`, `QS3DREBARHEALTH`, `QS3DREBARSHAPEHEALTH`
- `QS3DHIGHLIGHT`, `QS3DUNHIGHLIGHT`, `QS3DFOCUS`, `QS3DISOLATE`, `QS3DUNISOLATE`
- `QS3DRECOGNIZE`, `QS3DRECOGNIZEAUTO`
- `QS3DREVBASE`, `QS3DREVDIFF`

See [`docs/COMMANDS.md`](docs/COMMANDS.md) and [`docs/ADVANCED-GEOMETRY.md`](docs/ADVANCED-GEOMETRY.md) for detailed workflow constraints.

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
4. verify native Solid3d wall/opening/rebar behavior, including LINE/open-POLYLINE Tường KT, wall snap preview/apply, guarded straight-polyline opening cuts and supported BBS shape bars;
5. verify Auto Host against ambiguous/nearby/multi-level real drawings without accidental host assignment;
6. verify Room Auto with mixed LINE/POLYLINE/ARC/SPLINE plan-view boundaries, chord/sagitta controls and non-planar rejection;
7. verify Focus/Isolate/restore lifecycle and Family/Instance property editing in the real V25 palette host;
8. capture visual regressions at 100/125/150/200% DPI with Vietnamese Unicode;
9. run performance tests on large room-boundary, wall-junction/Auto Host, quantity and rebar models.

Until those gates are green, runtime-dependent features are described as **implemented source paths**, not as verified production behavior.

## Build policy

Do not commit `BrxMgd.dll`, `TD_Mgd.dll`, BLT/BLT3D folders, or private DWG/DOCX fixtures. The BricsCAD plugin resolves V25 assemblies through `BRICSCAD_V25_DIR` with `Private=false`.

GitHub Actions on `main` are **manual-only and owner-controlled**. Documentation/Markdown, `docs:` and `chore:` commits do not need GitHub CI, and no commit/push/merge should dispatch Actions automatically. Even source changes run GitHub CI only when the repository owner explicitly requests it.

This is a multi-agent repository. Sync latest `main` before work and again before each write so concurrent changes are not overwritten. Prefer source/static work remotely; reserve licensed BricsCAD V25/private-DWG validation for a machine that actually has those resources.

Read `CI_POLICY.md` and `AGENTS.md`, then `docs/CI-READINESS.md`, before changing CI or running any GitHub Action.
