# Implementation status — 2026-08-10

## Implemented in source

- BricsCAD V25 `net48/x64` adapter with external `BrxMgd.dll` / `TD_Mgd.dll` references.
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

## Verified in GitHub-hosted CI

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

## Runtime-gated / not yet claimed complete

These require an actual BricsCAD V25 Windows build/runtime and are not represented as passed merely because source/Core CI is green:

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
