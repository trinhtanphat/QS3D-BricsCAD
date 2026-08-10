# QS3D for BricsCAD V25

Clean-room quantity-takeoff / semantic 3D QS plugin for BricsCAD V25, inspired by the day-to-day workflow shown in BLT3D references while keeping the implementation independent. The repository does **not** contain BLT source/binaries, BricsCAD proprietary assemblies, customer/private drawings or vendor project data.

## Product form — BricsCAD plugin, not standalone EXE

QS3D is intentionally shipped and developed as a **BricsCAD V25 x64 plugin**. **BricsCAD V25 is required at runtime.**

- The shipping CAD adapter is `QS3D.BricsCAD.V25.dll`; it is a .NET Framework library loaded by BricsCAD through DemandLoad or `NETLOAD`.
- The package also carries `QS3D.Core.dll` and install/update/checksum/sample helpers. A standalone `QS3D.exe` is **not** a required or expected product artifact.
- BricsCAD owns the DWG database, document/editor lifecycle and native 2D/3D viewport; QS3D adds Ribbon, palettes/modeless WPF windows, commands, semantic data, takeoff/reporting and guarded generated geometry inside that host.
- `QS3D.Core` being CAD-independent is an architecture/testability choice, not evidence of a separate QS3D CAD application.
- `BLT-like`, `BLT-style` and `BLT3D-familiar` describe workflow/UX familiarity only. They do not define QS3D packaging and must not be interpreted as a standalone-EXE requirement.

The canonical product/hosting decision is documented in [`docs/PRODUCT-BOUNDARY.md`](docs/PRODUCT-BOUNDARY.md). A standalone CAD application or launcher is out of the current scope unless the repository owner explicitly reopens that product requirement.

## Target

- BricsCAD V25 on Windows x64
- Adapter: C# / .NET Framework 4.8 / WPF / BricsCAD .NET API
- Core: `netstandard2.0`
- UI: native viewport + QS3D Ribbon + docked/modeless WPF tools
- Source of truth: DWG source geometry + `.qsdb` semantic/project metadata

## Current source status — 2026-08-10

QS3D is beyond prototype stage. The current source contains the following integrated workflow families.

### BLT-style workspace and project data

- Three-pane Workspace with semantic model tree, Family/Type list, grouped property inspector and selected-object review.
- Typed Vietnamese property editors for text/numeric, boolean and editable choices.
- Explicit **Family / Type** and **Đối tượng / Instance** scopes; instance overrides can be reset to Family values and true overrides survive later Family changes/reassignment.
- Category-aware **Bóc chọn** capture flow and semantic selection synchronization from source or generated CAD handles.
- **Transactional semantic capture**: QS3D-generated handles are rejected before capture mutation; single/batch capture and room-finish generation/synchronization restore a complete `ProjectStateSnapshot` if regeneration or validation fails.
- Generic starter Families for wall categories are aligned with specialized capture defaults, including wall-axis offsets, Curtain frame depth and WallPier profile/chamfer defaults.
- Project/Zone/Floor/Family/Element model, `.qsdb` schema migration, audit trail, deterministic regeneration, revision baseline/diff and template import/export.
- Dependency cycles are reported as explicit Model Health errors and block Release Readiness instead of leaving regeneration as an unexplained stall.
- Drawing-bound Project Tools with Zone Manager, Floor/Level, Family Manager, Material Catalog and **Schedule Hub (`QS3DSCHEDULES`)**. Modeless project editors are tied to the drawing that opened them so switching DWGs does not silently mutate another project.
- Project mutation APIs require the actual project-owned element instance rather than trusting a same-ID caller object.

### Direct authoring

- BLT-style P0 Direct Draw is implemented for `QS3DDRAWWALL`, `QS3DDRAWBEAM`, `QS3DDRAWSLAB` and `QS3DDRAWCOLUMN`.
- P1 Direct Draw extends the same real-DWG-source/semantic/native pipeline to `QS3DDRAWGLASSWALL`, `QS3DDRAWWALLPIER`, `QS3DDRAWSTRUCTWALL` and `QS3DDRAWFOUNDATION`.
- Host-aware `QS3DDRAWDOOR` and `QS3DDRAWOPENING` create real source geometry, semantic Door/WallOpening data and guarded host links; physical boolean cutting remains an explicit cut operation.
- Direct Draw uses the existing Family/Instance model, generated ownership and guarded native builders rather than creating a second CAD/model system.
- Direct Draw rollback is ownership-scoped: failed new authoring attempts remove their own source/generated CAD and restore project state instead of deleting foreign generated handles.

### Review and viewport workflow

- Locate/Zoom, Highlight, Focus, Isolate/Restore, Section Box, Section Plane and clip-display review actions.
- Full Domain Hub, Project Tools, Schedule Hub, Rebar 3D Hub, Curtain Hub and Geometry Extensions expose major workflows without requiring command memorization.

### Tường KT, rooms and openings

- Tường Gạch / ArchitecturalWall, Vách Kính / GlassWall and Trụ Tường / WallPier semantic capture.
- LINE and open-POLYLINE Tường KT centerline source paths; bulged polyline segments are tessellated before deterministic wall-footprint generation.
- WallPier LINE specialized rectangular/chamfered profile builder; open POLYLINE WallPier currently uses the guarded generic wall-footprint path.
- `QS3DWALLJUNCTIONS` classifies L/T/X/Straight/End/Multi junctions.
- `QS3DWALLSNAPPREVIEW` → `QS3DWALLSNAPAPPLY` provides fingerprinted, review-gated source-centerline endpoint cleanup. Physical multi-owner wall-solid union/reconciliation is intentionally **not** guessed.
- Room capture plus `QS3DROOMAUTO` from planar LINE/POLYLINE/ARC/SPLINE networks with bounded curve sampling, planarity checks and non-destructive stale-room lifecycle.
- HT_Phòng generation/synchronization for floor finish, waterproofing, skirting, wall finish and ceiling finish.
- Manual and automatic Door/Opening host linking. Auto Host uses compatibility, wall-surface distance, Floor/Zone scope, ambiguity rejection and elevation tolerance; it never silently performs a physical boolean cut.
- `QS3DCUTOPENINGS` handles supported straight hosts; `QS3DCUTOPENINGSCURVED` uses a separate guarded curved-host planner and validates the complete fingerprint before mutation.

### Curtain Wall / Vách Kính

- Deterministic panel grid, quantities, schedule and XLSX.
- `QS3DCURTAIN3D` keeps one backing GlassWall host solid for opening booleans and adds separate ownership-protected perimeter/mullion/transom `Solid3d` overlays.
- Supported LINE Curtain frames are opening-aware: linked Door/Opening rectangles interrupt frame runs deterministically.
- Open/bulged WCS-XY POLYLINE GlassWall paths now map deterministic curtain stations onto tessellated path segments and generate ownership-protected native frame fragment solids, including linked-opening interruption.
- Frame state carries dedicated handles, counts, grid/opening/path metadata, configuration fingerprint and live-geometry validation.
- Opening property changes and link/re-host/unlink relations stale only the dependent frame overlay when appropriate.
- Curtain destructive ownership and dedicated ownership health use the shared generated-handle policy so new generated families cannot be silently erased/ignored because of a forgotten hard-coded slot.
- Panel-by-panel backing glass solids and whole-command host+frame rollback remain product/runtime work; current LINE/path frame source paths still require licensed BricsCAD V25 runtime qualification before production claims.

### Structure and rebar 3D

Semantic quantities and guarded native source paths exist for Beam, Slab, Column, StructuralWall, Foundation, Stair, Railing and Earthwork.

Generated rebar families include:

- column longitudinal bars — `QS3DREBAR3D`;
- beam longitudinal bars — `QS3DBEAMREBAR3D`;
- BBS-shape-driven bars — `QS3DREBAR3DSHAPE`;
- beam stirrups — `QS3DREBARSTIRRUP3D`;
- column ties — `QS3DREBARTIES3D`;
- slab X/Y mesh — `QS3DSLABREBAR3D`;
- StructuralWall horizontal/vertical mesh — `QS3DWALLREBAR3D`;
- **Foundation X/Y mesh — `QS3DFOUNDATIONREBAR3D`**.

Slab/Foundation X/Y directions and StructuralWall horizontal/vertical directions can use independent diameters/distribution. Generated ownership, stale state, invalidation, live-solid health, mode semantics and cross-family conflict checks are integrated. `QS3DREBARHEALTHALL` includes the current generated rebar families; `QS3DHEALTHALL` and `QS3DRELEASECHECK` add model/source/generated/live-CAD/dependency/BOM checks.

Beam stirrups can use explicitly configured bend-radius/hook-tail parameters. Fabrication-grade code-specific hooks, laps, anchorage and detailing are **not inferred** when those rules/dimensions are absent.

### Quantity, schedules and exports

- BQ grouping/filtering/Locate and one-sheet XLSX review. `QS3DED2` applies `Selection/Floor/Zone/All` before aggregation, writes one-element-per-row `CHI_TIET` plus Zone-aware `TONG_HOP`, and `QS3DEXCELLOCATE` validates Element ID ↔ Handle ↔ DWG fingerprint before changing CAD selection.
- Quick Takeoff with drawing-unit conversion.
- `QS3DB4D` bounded Current Space scan with high-confidence recognition/review. Entity type is a mandatory compatibility gate (for example DBText on `A-WALL` cannot become a wall), and `Solid3d.MassProperties` volume/total-surface metrics remain distinct from planar footprint area.
- B4D excludes generated output via canonical Core `CollectOwnerHandles(project)`, so owner classification, parsing and dedupe remain one source of truth as generated families evolve.
- BBS review/XLSX/UTF-8 CSV.
- Document-bound Schedule Hub for BQ, Room Finish, Material, Curtain, Door/Opening and rebar schedule/export workflows.
- Door/Opening schedule and XLSX with host provenance.
- Room Finish schedule/XLSX, Material Catalog/XLSX and Curtain XLSX.

### Generated ownership and release health

Generated ownership is treated as a product invariant rather than a UI convention:

- Core `GeneratedHandleOwnershipPolicy` owns classification, parsing, enumeration, project-wide collection and owner lookup;
- source handles and generated owner slots must not conflict;
- destructive rebuild/erase operations fail closed on foreign or ambiguous ownership;
- rebar, tie and curtain destructive guards consume shared ownership policy;
- semantic selection resolves generated slab/wall/Foundation mesh and Curtain-frame handles back to semantic owners;
- semantic capture rejects generated output as source before project mutation;
- host-solid ownership aliases are covered by ownership health so a generated host/cut alias cannot silently become a second owner;
- `QS3DRELEASECHECK` includes dependency-cycle health, Foundation mesh health, generated-rebar mode health, stale state, safe ownership and BOM/live-solid release guards.

See [`docs/REVIEW-2026-08-10-CONTINUE-ALL-AUDIT.md`](docs/REVIEW-2026-08-10-CONTINUE-ALL-AUDIT.md) for the deep audit and [`docs/PRODUCTION-HARDENING-PLAN-2026-08-10.md`](docs/PRODUCTION-HARDENING-PLAN-2026-08-10.md) for the current source-to-production gap map.

## Main commands

### Workspace / project / schedules

- `QS3D`, `QS3DHIDE`, `QS3DDOMAIN`, `QS3DPROJECTTOOLS`, `QS3DSCHEDULES`
- `QS3DZONES`, `QS3DLEVELS`, `QS3DFAMILIES`, `QS3DMATERIALS`
- `QS3DSAVE`, `QS3DRELOAD`, `QS3DREFRESH`, `QS3DREGEN`
- `QS3DHEALTH`, `QS3DHEALTHALL`, `QS3DRELEASECHECK`, `QS3DRUNTIMEPROBE`

### Semantic model / geometry

- `QS3DROOM`, `QS3DROOMAUTO`, `QS3DFINISH`
- `QS3DWALL`, `QS3DGLASSWALL`, `QS3DWALLPIER`, `QS3DWALLJUNCTIONS`
- `QS3DWALLSNAPPREVIEW`, `QS3DWALLSNAPAPPLY`
- `QS3DDRAWWALL`, `QS3DDRAWBEAM`, `QS3DDRAWSLAB`, `QS3DDRAWCOLUMN`
- `QS3DDRAWGLASSWALL`, `QS3DDRAWWALLPIER`, `QS3DDRAWSTRUCTWALL`, `QS3DDRAWFOUNDATION`
- `QS3DDRAWDOOR`, `QS3DDRAWOPENING`
- `QS3DCURTAIN`, `QS3DCURTAIN3D`, `QS3DCURTAINFRAMES3D`, `QS3DCURTAINFRAMEHEALTH`, `QS3DCURTAINXLSX`
- `QS3DOPENING`, `QS3DDOOR`, `QS3DAUTOLINKHOSTS`, `QS3DLINKHOST`, `QS3DCUTOPENINGS`, `QS3DCUTOPENINGSCURVED`
- `QS3DBEAM`, `QS3DSLAB`, `QS3DCOLUMN`, `QS3DSTRUCTWALL`, `QS3DFOUNDATION`, `QS3DSTAIR`, `QS3DRAILING`, `QS3DEARTHWORK`
- `QS3DBUILD3D`

### Quantity / schedules / rebar

- `QS3DB4D`, `QS3DTAKEOFF`
- `QS3DBQ`, `QS3DED2`, `QS3DEXCELLOCATE`
- `QS3DDOORSCHEDULE`, `QS3DDOORXLSX`
- `QS3DFINISHSCHEDULE`, `QS3DFINISHXLSX`, `QS3DMATERIALXLSX`
- `QS3DBBSVIEW`, `QS3DBBS`, `QS3DBBSCSV`
- `QS3DREBARMESHSETUP`, `QS3DREBARHUB`
- `QS3DREBAR3D`, `QS3DBEAMREBAR3D`, `QS3DREBAR3DSHAPE`
- `QS3DREBARSTIRRUP3D`, `QS3DREBARTIES3D`
- `QS3DSLABREBAR3D`, `QS3DWALLREBAR3D`, `QS3DFOUNDATIONREBAR3D`
- `QS3DREBARHEALTH`, `QS3DREBARSHAPEHEALTH`, `QS3DREBARSTIRRUPHEALTH`, `QS3DREBARTIEHEALTH`
- `QS3DSLABREBARHEALTH`, `QS3DWALLREBARHEALTH`, `QS3DFOUNDATIONREBARHEALTH`, `QS3DREBARHEALTHALL`

### Review / recognition / revision

- `QS3DHIGHLIGHT`, `QS3DUNHIGHLIGHT`, `QS3DFOCUS`, `QS3DISOLATE`, `QS3DUNISOLATE`
- `QS3DSECTIONBOX`, `QS3DSECTIONPLANE`, `QS3DCLIPDISPLAY`
- `QS3DRECOGNIZE`, `QS3DRECOGNIZEAUTO`
- `QS3DREVBASE`, `QS3DREVDIFF`

The release package generates `COMMANDS.txt` directly from source `[CommandMethod]` declarations, so package command inventory is not a stale hand-written list.

See [`docs/COMMANDS.md`](docs/COMMANDS.md) for detailed workflow notes.

## Architecture

- `src/QS3D.Core` — CAD-independent domain, persistence, geometry, quantities, diagnostics, recognition, revision, rebar and reporting.
- `src/QS3D.BricsCAD.V25` — BricsCAD document/database adapters, native geometry builders, commands, WPF palettes and Ribbon integration. This is the hosted plugin adapter, not a standalone executable.
- `tests/QS3D.Core.SmokeTests` — deterministic Core regression/smoke suite.
- `samples/generated` — repository-owned synthetic DXF/DWG/QSDB/XLSX/template fixtures only.
- `scripts` — static preflights, V25 packaging, DemandLoad installer/updater and runtime harness.
- `docs` — requirements, UI spec, implementation status, audits, runtime gates and manual-release runbooks.

## Release/runtime truth

Source presence is **not** BricsCAD V25 runtime proof. Before calling a release production-ready, the exact release SHA still needs a licensed interactive Windows x64 BricsCAD V25 environment for:

1. adapter compile against exact V25 managed assemblies;
2. NETLOAD/DemandLoad and command/Ribbon/palette smoke;
3. save/reopen and multi-DWG lifecycle on representative private drawings;
4. wall, WallPier, opening, Curtain host/frame, slab/wall/Foundation mesh and other rebar native Solid3d regression;
5. transactional capture/finish rollback and generated-source rejection regression;
6. Auto Host ambiguity/elevation regression;
7. Room Auto mixed LINE/POLYLINE/ARC/SPLINE topology regression;
8. Schedule Hub/export/traceability and dependency-cycle health regression;
9. `QS3DRELEASECHECK` on representative project data;
10. install/update rollback + signed-manifest version-binding qualification;
11. Unicode/HiDPI visual regression;
12. large-model performance tests.

Until those gates are green, runtime-dependent paths are described as **implemented source paths**, not verified production behavior.

## Manual CI/CD and release policy

GitHub Actions are deliberately **manual-only** and idle by default. Every workflow under `.github/workflows/` must remain `workflow_dispatch` only, and every executable job must hard-guard `github.event_name == 'workflow_dispatch'`.

A commit, push, PR, merge, review, documentation change, source fix or `continue all` request does **not** authorize Actions.

Current manual workflows:

- `ci.yml` — Core/static validation;
- `bricscad-v25.yml` — V25 integration build/runtime validation;
- `curved-opening.yml` — focused curved-opening gate;
- `geometry-extensions.yml` — focused geometry gate;
- `project-data-gate.yml` — project-data/editor/integrity gate;
- `schedule-gate.yml` — focused schedules/export gate;
- `release-v25.yml` — owner-approved build/package/GitHub Release workflow.

`release-v25.yml` additionally requires `confirm_release=RELEASE`. It runs source gates, Core build/smoke, V25 x64 build, optional real V25 runtime validation, packaging, SHA-256 generation and GitHub Release publication only after an explicit owner request.

Per-user autoload installation/replacement is transactional and rolls back prior files/registry state on failure. Updater version decisions are bound to a cryptographically verified signed manifest so unsigned version substitution/replay mismatches are rejected before installation. Production certificate/signing operations remain external release work.

No push/tag automatically publishes a release.

See [`CI_POLICY.md`](CI_POLICY.md), [`docs/CI.md`](docs/CI.md) and [`docs/MANUAL-BUILD-RELEASE.md`](docs/MANUAL-BUILD-RELEASE.md).

## Build and repository policy

- Do not commit `BrxMgd.dll`, `TD_Mgd.dll`, BLT/BLT3D folders, private DWG/DOCX fixtures, certificates or customer/vendor runtime assets.
- `BRICSCAD_V25_DIR` points to the licensed V25 installation; BricsCAD references use `Private=false`.
- The only committed DWG/DXF exceptions are the explicitly reviewed repository-owned synthetic sample fixtures under `samples/generated`; `scripts/preflight.py` keeps all other CAD/reference artifacts fail-closed.
- This is a multi-agent repository. Sync current `main` before each shared-file write and never force/revert newer concurrent work.

Read `CI_POLICY.md`, `AGENTS.md` and `docs/PRODUCT-BOUNDARY.md` before changing product architecture, CI or release behavior.
