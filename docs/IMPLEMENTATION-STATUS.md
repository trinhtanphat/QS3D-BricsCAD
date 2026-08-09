# Implementation status — 2026-08-09

## Implemented in source

- BricsCAD V25 `net48/x64` adapter with external `BrxMgd.dll` / `TD_Mgd.dll` references.
- Clean-room WPF UI design system, left workspace and right Drawing/Layer manager.
- Native Ribbon bootstrapper with QS3D workflow tabs; it fails closed and leaves palettes available if V25 ribbon runtime differs.
- Multi-document lifecycle refresh on document created/activated/destroyed.
- Project / Zone / Floor / Family / semantic Element model.
- Data-driven Family property editor with active Zone/Floor/Family context.
- `.qsdb` load/save, backup/temp replacement and single-writer project lock.
- dependency graph, dirty propagation, deterministic regenerators, formula/rule foundation.
- semantic capture for Room, Tường KT, Opening, Door and custom takeoff.
- Tường KT native `Solid3d` generation for selected plan-view LINE entities.
- Opening/Door → Host Wall linking command and linked opening area deduction.
- HT_Phòng semantic generation for floor finish, waterproofing, skirting, wall finish and ceiling finish.
- live Xref listing and LayerTable listing/search/show/hide.
- selection inspection and handle-based Locate/select.
- semantic BQ grouped by floor/category/family, column visibility, filters, Locate and real `.xlsx` export.
- Model Health window + checks for missing host/family/floor/zone/material, orphan/duplicate handles, dirty elements.
- bulk edit, revision snapshots/diff, audit, feature flags, template profile and unit/tolerance policies in core.
- expanded source preflight + smoke-test source.
- GitHub Actions remain `workflow_dispatch` only.

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

The source intentionally distinguishes these from implemented/testable deterministic core work so a design mock or unverified API path is never reported as a successful BricsCAD runtime test.
