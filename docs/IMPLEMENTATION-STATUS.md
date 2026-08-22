# Implementation status — 2026-08-10

## Implemented in source

- BricsCAD V25 `net48/x64` adapter with external `BrxMgd.dll` / `TD_Mgd.dll` references.
<<<<<<< origin/main
- Clean-room WPF UI design system, left workspace and right Drawing/Layer manager.
- Native Ribbon bootstrapper with QS3D workflow tabs; it fails closed and leaves palettes available if V25 ribbon runtime differs.
- Multi-document lifecycle refresh on document created/activated/destroyed; lifecycle load errors are surfaced to the command line/status UI instead of escaping the document event handler.
- Project / Zone / Floor / Family / semantic Element model.
- Data-driven Family property editor with active Zone/Floor/Family context; edits to a Family property are propagated to existing member elements and mark their derived quantities dirty.
- QSDB schema v2 with deterministic v1 → v2 migration and migration provenance metadata.
- validated `.qsdb` temp-save, atomic replace where supported, `.bak` recovery and single-writer project lock.
- corrupted or missing primary QSDB fallback to a valid backup; if recovery is impossible, the BricsCAD context enters protected recovery state and refuses to overwrite the existing project data.
- element dirty flags and UTC update timestamps persist across `.qsdb` save/reopen so stale calculated quantities cannot become clean merely because the project was reloaded.
- invalid persisted numeric/timestamp/dirty-state data is rejected instead of silently coercing corrupted values to zero/clean state.
- dependency graph, dirty propagation, formula/rule foundation and a default regenerator catalog.
- regeneration is fixed-point/multi-pass with a bounded pass count: if regenerating an Opening dirties its host Wall after the Wall already ran in the same cycle, a later pass recalculates the Wall instead of leaving stale deductions.
- `QS3DREGEN` is available explicitly; BQ, BBS and Refresh regenerate dirty deterministic semantic quantities before consuming them.
- semantic capture for Room, Tường KT, Opening, Door and custom takeoff.
- Quick Takeoff has a dedicated deterministic regenerator so captured Length/Area/Count reach BQ instead of being lost behind the room-regenerator path.
- deterministic structural quantity regeneration for Beam, Slab, Column, StructuralWall, Foundation, Stair, Railing and Earthwork.
- BricsCAD semantic capture commands and Ribbon actions for Dầm/Sàn/Cột/Vách BTCT/Móng/Cầu thang/Lan can/Đào đất; selected LINE/closed-area metrics feed the core model while family defaults supply structural dimensions/material.
- structural wall host linking supports Door/Opening deduction; re-hosting removes the old dependency and dirties both old and new hosts for recalculation.
- Tường KT native `Solid3d` generation for selected plan-view LINE entities.
- HT_Phòng semantic generation for floor finish, waterproofing, skirting, wall finish and ceiling finish.
- live Xref listing and LayerTable listing/search/show/hide.
- selection inspection and handle-based Locate/select.
- semantic BQ groups by stable Floor/Family IDs while displaying their names, preventing unrelated records with duplicate display names from being merged.
- BQ column visibility, filters, Locate and real `.xlsx` export.
- Excel quantity exporter headers match `SideAreaM2`, `BottomAreaM2`, `TopAreaM2`, `OtherAreaM2`; first row is frozen and the quantity range has AutoFilter.
- rebar notation parser rejects non-positive diameter/spacing/count and supports count, compound and spacing notation.
- deterministic BBS builder calculates bar mark, shape, cutting length, lap/anchor/hook allowances, spacing-derived quantity, waste, kg/m, total length and total weight without AI.
- project BBS adapter reads `Rebar*` semantic properties from QS3D elements; `QS3DBBS` exports a real `.xlsx` Bar Bending Schedule with frozen header and AutoFilter.
- Model Health validates structural material inheritance, malformed/overflowing rebar notation and missing rebar cutting/source length.
- revision snapshots retain floor/zone/properties/source handles/quantities and produce field-level deltas instead of only Added/Removed/Changed labels.
- bulk edit, audit, feature flags, template profile and unit/tolerance policies remain available in core.
- expanded source preflight + deterministic smoke sources cover persistence, stable-ID BQ, fixed-point regeneration, structural quantities, structural opening deduction, Quick Takeoff, BBS/XLSX, rebar health and detailed revision diff.
- `main` GitHub Actions workflows remain `workflow_dispatch` only.
=======
- Clean-room WPF design system, BLT3D-inspired workflow layout, left workspace and right Drawing/Layer manager around the native CAD viewport.
- Ribbon tabs for project setup, BIM, recognition, drawing/tools, modeling, view, quantity and revisions.
- Multi-document lifecycle refresh on document create/activate/destroy.
- Project / Zone / Floor / Family / semantic Element model with data-driven family properties and engineering units.
- QSDB schema v2 with deterministic v1 → v2 migration, validated temp-save, atomic replace where supported, `.bak` recovery, single-writer lock and protected no-overwrite state when both project generations are unreadable.
- dependency graph, dirty propagation, deterministic quantity rules and category-specific regenerators.
- Room, floor finish, waterproofing, skirting, wall finish, ceiling finish, architectural wall, opening, door and custom quantity workflows.
- Beam, Slab, Column, Structural Wall, Foundation and Earthwork quantity calculators/regenerators for concrete, deductions, formwork, footprint and excavation loose-volume quantities.
- source-level structural 3D paths: LINE → Beam/Structural Wall `Solid3d`; closed Polyline → Slab/Column/Foundation extrusion.
- Rebar domain with count/spacing notation, straight/L/U/rectangular-stirrup shapes, hooks/laps, cut length, kg/m, total kg, BBS grouping and UTF-8 CSV export.
- rule-based recognition from layer + text + entity type, Vietnamese normalization, confidence score, top-two margin, review queue and high-confidence auto-apply mode.
- persistent `.qsrev` revision baseline and per-element/per-quantity Before/After/Delta/% diff with Locate UI.
- semantic BQ grouped by floor/category/family with concrete, formwork, length/area and steel kg, filters, Locate and real `.xlsx` export; header row freeze + AutoFilter retained.
- Model Health covers project recovery, host/family/floor/zone/material, structural/earthwork dimensions, rebar definition/length, dirty state, orphan handles and duplicate handles; material can inherit from Family.
- live Xref and LayerTable listing/search/show/hide, selection inspection and handle-based Locate/select.
- packaging script creates a QS3D-only V25 ZIP and explicitly rejects BricsCAD vendor assemblies.
- expanded preflight requires the complete full-domain tree, validates XML/XAML handlers/C# delimiter sanity, forbidden artifacts, net48 adapter constraints, packaging guard and manual-only release workflows.
- `main` workflows remain designed as `workflow_dispatch` only.
>>>>>>> origin/agent/full-domain-20260810

## Previously verified in GitHub-hosted CI

<<<<<<< origin/main
- baseline Core CI run `31341101835`: PASS.
- persistence/export hardening run `31341548469`: PASS.
- hardening snapshot run `31341704360`: PASS.
- those runs passed preflight, Release build of `QS3D.Core`, and deterministic smoke tests for their respective commits.

The newer persistence/structural/BBS/revision/fixed-point source patches were committed after those historical runs. They do not trigger GitHub Actions automatically and must not be presented as CI-verified until a new manual Core CI run is explicitly approved and completed.

## Gate C blocker

BricsCAD V25 integration probe run `31341184031` was recorded as queued because no matching self-hosted runner was assigned for `[self-hosted, windows, x64, bricscad-v25]`. Therefore the V25 plugin build has not yet executed and is not claimed successful or failed.
=======
Previous green gates:
- `31341101835` — baseline Core.
- `31341548469` — persistence/export hardening.
- `31341704360` — hardening snapshot.
- `31342458832` — structural/rebar/recognition/revision Core after compiler fixes.
- `31342976121` — full-domain snapshot including BQ steel, revision store, structural-solid source/preflight and packaging.

Release-tree gate:
- **`31343166796` — PASS**.
- preflight: PASS.
- `QS3D.Core` Release build: PASS.
- deterministic smoke tests: PASS, including structural quantities, column footprint fallback, generic quantity, rebar/BBS, BQ steel, recognition confidence/review behavior, revision quantity diff/store, QSDB hardening, XLSX packaging and structural/earthwork/rebar Model Health.

Earlier full-domain gates intentionally caught nullable compile errors in new Core code; those were fixed before the green release-tree gate rather than being hidden or bypassed.

## Gate C blocker

BricsCAD V25 integration probe run `31341184031` is still queued because no matching self-hosted runner is assigned for `[self-hosted, windows, x64, bricscad-v25]`. The V25 adapter build therefore has **not started**; it is not claimed successful or failed.

The V25 integration runner must provide a licensed BricsCAD V25 installation, `BRICSCAD_V25_DIR`, `BrxMgd.dll` and `TD_Mgd.dll` locally. Vendor DLLs must not be uploaded to this repository.
>>>>>>> origin/agent/full-domain-20260810

## Runtime-gated / not yet claimed complete

These require an actual BricsCAD V25 Windows build/runtime and are not represented as passed merely because source/Core CI is green:

<<<<<<< origin/main
- first full plugin compile against the exact installed V25 `BrxMgd.dll`/`TD_Mgd.dll`;
- `NETLOAD` and Ribbon runtime verification on V25.1/V25.2, including the structural/BBS/regenerate commands;
- `Solid3d` wall generation regression on the supplied private DWG;
- native 3D authoring for Beam/Slab/Column/StructuralWall/Foundation/Stair beyond the deterministic semantic quantity layer;
- polyline wall corners, joins/T-junctions and freeform wall profiles;
- physical boolean subtraction of generated door/opening solids from host wall solids;
- automatic room-boundary discovery from arbitrary intersecting wall networks;
- zoom-to-extents/transient highlight beyond implied CAD selection;
- geometric rebar placement inside BricsCAD; BBS calculation/export is implemented in core but physical rebar solids/curves still need the V25 runtime path;
- installer/code signing/update service and commercial licensing backend.

The source intentionally distinguishes implemented deterministic/core behavior from BricsCAD API paths that still require an actual V25 runtime.
=======
- first full plugin compile against the exact installed V25 managed assemblies;
- `NETLOAD`, Ribbon/Palette runtime verification and multi-DWG lifecycle on V25.1/V25.2;
- wall, beam, structural-wall, slab, column and foundation `Solid3d` regression on private sample DWGs;
- repeated structural authoring without duplicate/stale generated solids and transaction/undo regression;
- polyline architectural-wall corners, joins/T-junctions and freeform wall profiles;
- physical boolean subtraction of generated door/opening solids from host wall solids;
- automatic room-boundary discovery from arbitrary wall networks;
- transient highlight/zoom-to-extents beyond implied CAD selection;
- advanced bar-bending shape-code standards and physical rebar 3D geometry;
- installer/code signing/auto-update/commercial licensing backend;
- UI screenshot/pixel comparison, DPI matrix and performance corpus.

`docs/RUNTIME-TEST-CHECKLIST.md` is the source of truth for the next runtime gate.
>>>>>>> origin/agent/full-domain-20260810
