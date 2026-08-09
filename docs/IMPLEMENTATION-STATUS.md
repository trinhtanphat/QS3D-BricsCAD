# Implementation status — 2026-08-10

## Implemented in source

- BricsCAD V25 `net48/x64` adapter with external `BrxMgd.dll` / `TD_Mgd.dll` references.
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
- recovery metadata is cleared before a successful save and restored in memory if the save fails, preventing stale backup warnings from being persisted.
- Model Health reports backup recovery and protected project-load failures.
- dependency graph, dirty propagation, deterministic regenerators, formula/rule foundation.
- semantic capture for Room, Tường KT, Opening, Door and custom takeoff.
- Tường KT native `Solid3d` generation for selected plan-view LINE entities.
- Opening/Door → Host Wall linking command and linked opening area deduction.
- HT_Phòng semantic generation for floor finish, waterproofing, skirting, wall finish and ceiling finish.
- live Xref listing and LayerTable listing/search/show/hide.
- selection inspection and handle-based Locate/select.
- semantic BQ groups by stable Floor/Family IDs while displaying their names, preventing unrelated records with duplicate display names from being merged.
- BQ column visibility, filters, Locate and real `.xlsx` export.
- Excel exporter headers match `SideAreaM2`, `BottomAreaM2`, `TopAreaM2`, `OtherAreaM2`; first row is frozen and the quantity range has AutoFilter.
- Model Health checks missing host/family/floor/zone/material, orphan/duplicate handles and dirty elements; material may inherit from Family.
- bulk edit, revision snapshots/diff, audit, feature flags, template profile and unit/tolerance policies in core.
- expanded source preflight + deterministic hardening smoke tests, including backup recovery, dirty-state persistence, invalid numeric rejection and stable-ID BQ grouping.
- `main` GitHub Actions workflows remain `workflow_dispatch` only.

## Verified in GitHub-hosted CI before the post-CI review patch

- baseline Core CI run `31341101835`: PASS.
- persistence/export hardening run `31341548469`: PASS.
- hardening snapshot run `31341704360`: PASS.
- those runs passed preflight, Release build of `QS3D.Core`, and deterministic smoke tests for their respective commits.

The persistence/BQ/Family-propagation review patch committed after those runs has **not** triggered or rerun GitHub Actions automatically. Its workflows remain manual-only, so the historical green runs must not be presented as verification of the newer commit.

## Gate C blocker

BricsCAD V25 integration probe run `31341184031` was recorded as queued because no matching self-hosted runner was assigned for `[self-hosted, windows, x64, bricscad-v25]`. Therefore the V25 plugin build has not yet executed and is not claimed successful or failed.

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
