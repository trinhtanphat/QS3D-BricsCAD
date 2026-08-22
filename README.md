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
- Selected-object review actions for Locate/Zoom, **Focus**, **Cô lập/Khôi phục**, plus semantic-reference matching so Room Auto boundary-derived elements remain discoverable without duplicating source ownership. Guarded Section Box / Section Plane / clip display review commands are also exposed through Ribbon/Hub.
- Semantic Project/Zone/Floor/Family/Element model, `.qsdb` schema migration, deterministic regeneration, audit, revision baseline/diff, template import/export and Model Health.
- **Project Tools** entry point `QS3DPROJECTTOOLS` consolidates drawing-bound **Zone Manager (`QS3DZONES`)**, **Floor/Level**, **Family Manager (`QS3DFAMILIES`)**, Material Catalog, Template/module/health shortcuts. Modeless project editors are bound to the drawing that opened them so switching active DWGs does not silently edit the wrong project.
- Zone/Floor/Family assignment services enforce project-instance ownership instead of trusting an element ID alone. Foreign/spoofed `ProjectElement` objects with the same ID are rejected; Bulk Edit object-based APIs follow the same invariant. Family reassignment remains inheritance-safe: old inherited defaults are replaced while true instance overrides are preserved.
- Tường KT workflows for Tường Gạch, Vách Kính and Trụ Tường, including explicit semantic capture commands, wall-junction analysis and guarded native 3D paths.
- Tường Gạch and generic Tường KT source paths accept LINE and open POLYLINE centerlines; polyline bulges are tessellated and converted through the deterministic wall-footprint engine.
- **Vách Kính / Curtain Wall** has deterministic panel-grid quantities/schedule/XLSX plus a dedicated native LINE workflow. `QS3DCURTAIN3D` keeps one backing GlassWall host solid for Door/Opening booleans and adds ownership-protected mullion/transom/perimeter-frame `Solid3d` overlays. Frame depth is Family-editable, generated frame metadata carries deterministic configuration/live-geometry fingerprints, and Full Health detects missing/stale frame snapshots. Core planning can interrupt curtain-frame runs around hosted openings instead of treating the façade grid as opening-blind. Open/curved POLYLINE still uses the generic backing host and does not yet generate curved frame overlays.
- **Trụ Tường / WallPier** has deterministic rectangular/chamfered profile quantities and a specialized LINE profile builder. Open POLYLINE WallPier still falls back to the guarded generic Tường KT footprint path.
- `QS3DWALLJUNCTIONS` analyzes selected LINE/open-POLYLINE wall centerlines and classifies L/T/X/Straight/End/Multi junction nodes with finite-safe, large-coordinate-aware geometry guards.
- `QS3DWALLSNAPPREVIEW` / `QS3DWALLSNAPAPPLY` add review-gated wall centerline cleanup: Preview fingerprints endpoint moves without mutation; Apply rejects stale previews and unsupported curved/bulged/nonsemantic source. Affected generated geometry is invalidated with ownership-aware safeguards before later rebuild. This improves source-junction cleanup but is not yet complete automatic wall-solid union/reconciliation.
- Room capture plus `QS3DROOMAUTO` bounded-face discovery from planar LINE/POLYLINE/ARC/SPLINE networks. Direct ARC and polyline bulges use configurable sagitta; SPLINE uses bounded chord-length sampling with a segment cap. Planarity/elevation checks run before Core intersection/T-junction topology and non-destructive stale-room lifecycle processing.
- Door/Opening manual host linking plus `QS3DAUTOLINKHOSTS`. Auto Host uses compatible semantic wall candidates, surface gap, Floor/Zone scope, ambiguity rejection and an independent elevation gate; it only establishes the semantic host and never silently performs a physical cut.
- Physical Door/Opening subtraction supports generated LINE hosts and guarded straight, non-bulged POLYLINE wall segments where the opening can be projected safely without crossing a corner. A separate `QS3DCUTOPENINGSCURVED` path plans cutters against tessellated bulged open-POLYLINE centerlines. Curved cutting prepares all cut plans and the geometry fingerprint **before** `BoolSubtract`; identical reruns are idempotent and a changed fingerprint on the same generated host is rejected until the host is rebuilt.
- Beam/Slab/Column/StructuralWall/Foundation/Stair/Railing/Earthwork semantic quantities and guarded native Solid3d source paths.
- Rebar notation/BBS, XLSX/CSV export, review/Locate, rectangular-column longitudinal rebar 3D, beam longitudinal rebar 3D, generated-rebar ownership/health guards, deterministic linear distribution and BBS-shape-driven 3D source paths for supported straight/L/U/Z/custom leg/turn definitions.
- Beam longitudinal source path: `QS3DBEAMREBAR3D` generates guarded longitudinal bars along supported Beam LINE sources and shares protected `GeneratedRebarHandles` ownership/health with the generic longitudinal-rebar path.
- Beam stirrup source path: `QS3DREBARSTIRRUP3D` uses the deterministic beam-stirrup layout planner to generate rectangular loop solids along supported horizontal Beam LINE sources; spacing/count/cover/diameter are bounded and generated ownership is health-checkable through `QS3DREBARSTIRRUPHEALTH`.
- Column tie source path: `QS3DREBARTIES3D` generates guarded rectangular column tie loop solids along supported closed 4-vertex rectangle Column footprints; tie diameter/spacing/cover/clearances are bounded, protected ownership is enforced and `QS3DREBARTIEHEALTH` reviews generated tie state.
- Slab mesh source path: `QS3DSLABREBAR3D` generates rectangular X/Y mesh for supported closed 4-vertex Slab footprints. X/Y notation can use independent diameters/count/spacing; generated state uses dedicated `GeneratedSlabMesh*` ownership rather than the generic longitudinal slot. `QS3DSLABREBARHEALTH` checks count, X/Y diameters, spacing, cover, faces, category, ownership and live solids.
- Structural-wall mesh source path: `QS3DWALLREBAR3D` generates horizontal/vertical Near/Far/Both mesh for supported StructuralWall LINE hosts. Horizontal/vertical diameters and distribution are independent and stored under dedicated `GeneratedWallMesh*` metadata; `QS3DWALLREBARHEALTH` performs dedicated health checks.
- `QS3DREBARHEALTHALL` aggregates longitudinal, BBS-shape, column-tie, beam-stirrup, slab-mesh and wall-mesh health plus cross-family ownership diagnostics. `QS3DHEALTHALL` additionally aggregates core model/generated-solid/stale-state/mode and curtain-frame diagnostics in one review window with Locate support.
- Current beam stirrup/column tie geometry intentionally uses segmented-cylinder rectangular loops. Production fabrication hooks, bend radii and code-specific detailing are **not** inferred without explicit dimensions.
- BQ grouping/filtering/Locate/XLSX, Quick Takeoff, recognition/review/auto-accept, Layer/Xref adapters, Ribbon, Full Domain Hub, Project Tools, Zone/Family/Floor/Material managers, Curtain Hub, Geometry Extensions, viewport tools and release packaging/DemandLoad scripts.
- Ribbon/Workspace/Domain Hub expose the major BLT-style workflows consistently: Tường KT, Vách Kính Hub/Curtain 3D, Giao tường, Snap xem/áp, Auto/Manual Host, Cửa/Lỗ, Room Auto, Focus/Isolate, Section review, BQ/BBS, column/beam longitudinal rebar, BBS shape rebar, beam stirrups, column ties, slab mesh, wall mesh and unified health.
- Static preflights and deterministic Core smoke coverage include geometry/rebar/Room Auto/Auto Host/wall-snap/curtain guards, command uniqueness, typed Family/Instance inspector contracts, generated ownership/invalidation, project-editor drawing affinity/assignment integrity and XAML well-formedness checks. `scripts/preflight-all.py` auto-discovers feature preflights, including `scripts/preflight-ci-manual-only.py`, which rejects any GitHub Actions trigger other than `workflow_dispatch`.

## Main commands

### Workspace / project
- `QS3D`, `QS3DHIDE`, `QS3DDOMAIN`, `QS3DPROJECTTOOLS`
- `QS3DZONES`, `QS3DFAMILIES`
- `QS3DSAVE`, `QS3DRELOAD`, `QS3DREFRESH`, `QS3DREGEN`
- `QS3DHEALTH`, `QS3DHEALTHALL`, `QS3DRUNTIMEPROBE`

### Semantic model / geometry
- `QS3DROOM`, `QS3DROOMAUTO`
- `QS3DWALL`, `QS3DGLASSWALL`, `QS3DWALLPIER`, `QS3DWALLJUNCTIONS`
- `QS3DCURTAIN`, `QS3DCURTAIN3D`, `QS3DCURTAINFRAMES3D`, `QS3DCURTAINFRAMEHEALTH`, `QS3DCURTAINXLSX`
- `QS3DWALLSNAPPREVIEW`, `QS3DWALLSNAPAPPLY`
- `QS3DOPENING`, `QS3DDOOR`, `QS3DAUTOLINKHOSTS`, `QS3DLINKHOST`, `QS3DCUTOPENINGS`, `QS3DCUTOPENINGSCURVED`
- `QS3DBEAM`, `QS3DSLAB`, `QS3DCOLUMN`, `QS3DSTRUCTWALL`, `QS3DFOUNDATION`, `QS3DSTAIR`, `QS3DRAILING`, `QS3DEARTHWORK`
- `QS3DFINISH`, `QS3DTAKEOFF`, `QS3DBUILD3D`

### Quantity / rebar / review
- `QS3DB4D`
- `QS3DBQ`, `QS3DED2`, `QS3DEXCELLOCATE`
- `QS3DBBSVIEW`, `QS3DBBS`, `QS3DBBSCSV`
- `QS3DREBAR3D`, `QS3DBEAMREBAR3D`, `QS3DREBARHEALTH`
- `QS3DREBAR3DSHAPE`, `QS3DREBARSHAPEHEALTH`
- `QS3DREBARSTIRRUP3D`, `QS3DREBARSTIRRUPHEALTH`
- `QS3DREBARTIES3D`, `QS3DREBARTIEHEALTH`
- `QS3DSLABREBAR3D`, `QS3DSLABREBARHEALTH`
- `QS3DWALLREBAR3D`, `QS3DWALLREBARHEALTH`
- `QS3DREBARHEALTHALL`
- `QS3DHIGHLIGHT`, `QS3DUNHIGHLIGHT`, `QS3DFOCUS`, `QS3DISOLATE`, `QS3DUNISOLATE`
- `QS3DSECTIONBOX`, `QS3DSECTIONPLANE`, `QS3DCLIPDISPLAY`
- `QS3DRECOGNIZE`, `QS3DRECOGNIZEAUTO`
- `QS3DREVBASE`, `QS3DREVDIFF`

See [`docs/COMMANDS.md`](docs/COMMANDS.md) and [`docs/ADVANCED-GEOMETRY.md`](docs/ADVANCED-GEOMETRY.md) for detailed workflow constraints.

## Architecture

- `src/QS3D.Core` — CAD-independent domain, persistence, geometry, quantity, recognition, revision, rebar and reporting logic.
- `src/QS3D.BricsCAD.V25` — BricsCAD document/database adapters, native geometry builders, commands, WPF palettes and Ribbon integration.
- `tests/QS3D.Core.SmokeTests` — deterministic Core regression/smoke suite.
- `samples/generated` — repository-owned synthetic DXF/DWG, QSDB, Excel Handle lookup and `.qstemplate` fixtures; no BLT/private/vendor content.
- `scripts` — static preflight, V25 packaging, DemandLoad install/uninstall and runtime harness support.
- `docs` — requirements, UI specification, implementation status, runtime gate, manual release and handoff documentation.

## Release/runtime truth

Source presence is **not** the same as BricsCAD V25 runtime proof. Before calling a release production-ready, the following still require a licensed Windows x64 BricsCAD V25 environment:

1. compile the V25 adapter against the exact V25 managed assemblies;
2. NETLOAD/DemandLoad the produced DLL and run command/Ribbon/palette smoke tests;
3. test private representative DWGs, save/reopen and multi-DWG lifecycle;
4. verify native Solid3d wall/opening/rebar/curtain behavior, including LINE/open-POLYLINE Tường KT, WallPier profile LINE, curtain backing host + frame overlay/opening interruption, wall snap preview/apply, straight/curved opening cuts, beam/column longitudinal bars, BBS shape bars, beam stirrups, column ties, slab mesh and wall mesh;
5. verify Auto Host against ambiguous/nearby/multi-level real drawings without accidental host assignment;
6. verify Room Auto with mixed LINE/POLYLINE/ARC/SPLINE plan-view boundaries, chord/sagitta controls and non-planar rejection;
7. verify Focus/Isolate/Section review lifecycle and Family/Instance/Zone/Floor-Level/Material editing in the real V25 palette host;
8. verify curtain-frame rebuild/fingerprint behavior after panel-grid/Family/source/opening changes;
9. capture visual regressions at 100/125/150/200% DPI with Vietnamese Unicode;
10. run performance tests on large room-boundary, wall-junction/Auto Host, curtain-grid, quantity and rebar models.

Until those gates are green, runtime-dependent features are described as **implemented source paths**, not as verified production behavior.

## Manual CI/CD and release policy

GitHub Actions are deliberately **idle by default**. All workflows under `.github/workflows/` must remain **`workflow_dispatch` only**. A commit, push, PR, merge, documentation update, source fix, review or `continue all` request must **not** start CI/CD.

Current manual workflows:

- `ci.yml` — Core/static validation;
- `bricscad-v25.yml` — V25 integration build/runtime validation;
- `curved-opening.yml` — focused curved-opening gate;
- `geometry-extensions.yml` — focused geometry gate;
- `project-data-gate.yml` — focused Zone/Floor/Family/Material/Project Tools/integrity gate;
- `release-v25.yml` — owner-approved build/package/GitHub Release workflow.

`scripts/preflight-ci-manual-only.py` fails if any workflow adds an automatic/event trigger such as `push`, `pull_request`, `schedule`, `workflow_run`, `workflow_call`, `repository_dispatch`, release/deployment events, or anything other than `workflow_dispatch`. Executable jobs are additionally hard-guarded to `github.event_name == 'workflow_dispatch'`; the release job also requires `confirm_release=RELEASE`.

When the repository owner explicitly asks to **build and release**, use `release-v25.yml` for the chosen commit/tag. Publishing additionally requires `confirm_release=RELEASE`. The workflow runs source gates, Core build/smoke tests, V25 adapter build, optional real V25 runtime validation, packaging, SHA-256 generation and GitHub Release publication. Merely preparing the workflow or pushing code never authorizes running it.

See [`CI_POLICY.md`](CI_POLICY.md), [`AGENTS.md`](AGENTS.md), [`docs/CI.md`](docs/CI.md) and [`docs/MANUAL-BUILD-RELEASE.md`](docs/MANUAL-BUILD-RELEASE.md).

## Build policy

<<<<<<< HEAD
Do not commit `BrxMgd.dll`, `TD_Mgd.dll`, BLT/BLT3D folders, or private DWG/DOCX fixtures. The BricsCAD plugin resolves V25 assemblies through `BRICSCAD_V25_DIR` with `Private=false`. Use the new `samples/generated` fixture set for public tests; the whitelist in `.gitignore` permits only the named synthetic DXF/DWG/XLSX binaries.

GitHub Actions on `main` are **manual-only and owner-controlled**. Documentation/Markdown, `docs:` and `chore:` commits do not need GitHub CI, and no commit/push/merge should dispatch Actions automatically. Even source changes run GitHub CI only when the repository owner explicitly requests it.
=======
Do not commit `BrxMgd.dll`, `TD_Mgd.dll`, BLT/BLT3D folders, private DWG/DOCX fixtures, signing certificates or private runtime assets. The BricsCAD plugin resolves V25 assemblies through `BRICSCAD_V25_DIR` with `Private=false`.
>>>>>>> origin/main

This is a multi-agent repository. Sync latest `main` before work and again before each write so concurrent changes are not overwritten. Prefer source/static work remotely; reserve licensed BricsCAD V25/private-DWG validation for a machine that actually has those resources.

Read `CI_POLICY.md` and `AGENTS.md`, then `docs/CI-READINESS.md` and `docs/MANUAL-BUILD-RELEASE.md`, before changing CI or running any GitHub Action.
