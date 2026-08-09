# Implementation status — 2026-08-10

## Implemented in source

- BricsCAD V25 `net48/x64` adapter with external `BrxMgd.dll` / `TD_Mgd.dll` references.
- Clean-room WPF UI design system, left workspace and right Drawing/Layer manager.
- Native Ribbon bootstrapper with QS3D workflow tabs; it fails closed and leaves palettes available if V25 ribbon runtime differs.
- Multi-document lifecycle refresh on document created/activated/destroyed.
- Project / Zone / Floor / Family / semantic Element model.
- Data-driven Family property editor with active Zone/Floor/Family context.
- QSDB schema v2 with deterministic v1 → v2 migration and migration provenance metadata.
- validated `.qsdb` temp-save, atomic replace where supported, `.bak` recovery and single-writer project lock.
- corrupted primary QSDB fallback to valid backup; if both are unreadable, the BricsCAD context enters protected recovery state and refuses to overwrite the existing project file.
- recovery metadata is cleared before a successful save and restored in memory if the save fails, preventing stale backup warnings from being persisted.
- Model Health reports backup recovery and protected project-load failures.
- dependency graph, dirty propagation, deterministic regenerators, formula/rule foundation.
- semantic capture for Room, Tường KT, Opening, Door and custom takeoff.
- Tường KT native `Solid3d` generation for selected plan-view LINE entities.
- Opening/Door → Host Wall linking command and linked opening area deduction.
- HT_Phòng semantic generation for floor finish, waterproofing, skirting, wall finish and ceiling finish.
- live Xref listing and LayerTable listing/search/show/hide.
- selection inspection and handle-based Locate/select.
- semantic BQ grouped by floor/category/family, column visibility, filters, Locate and real `.xlsx` export.
- Excel exporter headers match `SideAreaM2`, `BottomAreaM2`, `TopAreaM2`, `OtherAreaM2`; first row is frozen and the quantity range has AutoFilter.
- Model Health checks missing host/family/floor/zone/material, orphan/duplicate handles and dirty elements; material may inherit from Family.
- bulk edit, revision snapshots/diff, audit, feature flags, template profile and unit/tolerance policies in core.
- expanded source preflight + deterministic hardening smoke tests.
- `main` GitHub Actions workflows remain `workflow_dispatch` only.

## Verified in GitHub-hosted CI

- baseline Core CI run `31341101835`: PASS.
- persistence/export hardening run `31341548469`: PASS.
- final hardening snapshot run `31341704360`: PASS.
- the final run passed preflight, Release build of `QS3D.Core`, and deterministic smoke tests.

## Gate C blocker

BricsCAD V25 integration probe run `31341184031` is queued because no matching self-hosted runner is assigned for `[self-hosted, windows, x64, bricscad-v25]`. Therefore the V25 plugin build has not yet executed and is not claimed successful or failed.

## Runtime-gated / not yet claimed complete

These require a licensed BricsCAD V25 Windows runner/session and are **not claimed as runtime-tested yet**:

- first full plugin compile against the exact installed V25 `BrxMgd.dll`/`TD_Mgd.dll`;
- `NETLOAD` and Ribbon runtime verification on V25.1/V25.2;
- `Solid3d` wall generation regression on the supplied private DWG;
- polyline wall corners, joins/T-junctions and freeform wall profiles;
- physical boolean subtraction of generated door/opening solids from host wall solids;
- automatic room-boundary discovery from arbitrary wall networks;
- zoom-to-extents/transient highlight beyond implied CAD selection;
- full structural Beam/Slab/Column/Foundation 3D authoring and advanced rebar/BBS;
- installer/code signing/update service and commercial licensing backend.

The source intentionally distinguishes implemented/tested deterministic core work from API paths that still require an actual BricsCAD V25 runtime.
