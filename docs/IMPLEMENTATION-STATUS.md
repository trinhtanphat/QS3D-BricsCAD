# Implementation status — 2026-08-10

## Implemented in source

- BricsCAD V25 `net48/x64` adapter with external `BrxMgd.dll` / `TD_Mgd.dll` references.
- Clean-room WPF UI design system, left workspace and right Drawing/Layer manager.
- Native Ribbon bootstrapper with QS3D workflow tabs; it fails closed and leaves palettes available if V25 ribbon runtime differs.
- Multi-document lifecycle refresh with project cache keyed by live `Document` identity instead of mutable drawing names; Save As synchronizes the sidecar drawing identity and unsaved drawing filenames are sanitized.
- Project / Zone / Floor / Family / semantic Element model with finite floor elevation validation.
- Data-driven Family property editor with active Zone/Floor/Family context; edits to a Family property are propagated to existing member elements and mark derived quantities dirty.
- QSDB schema v3 with deterministic v1 → v2 → v3 migration. Project `QuantityRule` definitions and audit provenance now persist inside `.qsdb` instead of existing only in memory.
- validated `.qsdb` temp-save, atomic replace where supported, `.bak` recovery, single-writer project lock, 64 MiB size guard and DTD/external-entity blocking.
- corrupted or missing primary QSDB fallback to a valid backup; unrecoverable existing project data enters protected recovery state and is not silently overwritten.
- element dirty flags and UTC update timestamps persist across `.qsdb` save/reopen; invalid persisted numeric/timestamp/dirty-state data is rejected.
- dependency graph + bounded fixed-point regeneration. Matching project quantity rules run after semantic regeneration using numeric Family properties, element properties and current quantities as deterministic variables.
- `QS3DREGEN` is available explicitly; BQ, BBS and Refresh regenerate dirty deterministic semantic quantities before consuming them.
- semantic capture for Room, Tường KT, Opening, Door, structural categories and custom takeoff.
- Quick Takeoff deterministic Length/Area/Volume/Count path and drawing-unit conversion from BricsCAD `INSUNITS` rather than a hard-coded millimeter fallback.
- deterministic structural quantity regeneration for Beam, Slab, Column, StructuralWall, Foundation, Stair, Railing and Earthwork.
- BricsCAD semantic capture commands and Ribbon actions for Dầm/Sàn/Cột/Vách BTCT/Móng/Cầu thang/Lan can/Đào đất.
- host linking supports Door/Opening deduction, safe re-host dirty propagation and persisted audit events for link/unlink operations.
- Tường KT/native structural `Solid3d` source paths use the guarded two-phase generated-geometry replacement architecture already present in the V25 adapter.
- HT_Phòng semantic generation for floor finish, waterproofing, skirting, wall finish and ceiling finish.
- live Xref/Layer listing, controls, selection inspection and handle-based Locate/select.
- semantic BQ groups by stable Floor/Family IDs, supports filtering/Locate/XLSX, has a real recalculate callback, and persists visible-column preferences in project metadata.
- deterministic rebar notation/BBS calculation; `QS3DBBS` exports XLSX and `QS3DBBSVIEW` opens the existing review/Locate UI.
- revision snapshot persistence (`.qsrev`) plus `QS3DREVBASE` / `QS3DREVDIFF` wiring to the existing revision comparison UI.
- deterministic recognition core is wired to `QS3DRECOGNIZE` review UI and `QS3DRECOGNIZEAUTO`; auto mode only applies high-confidence/margin results and refuses semantic category collisions.
- project/company layer mappings can override recognition deterministically at confidence 0.99 before fallback heuristics.
- `.qstemplate` import/export is implemented for Families, QuantityRules, layer mappings and BQ column layout. Template files use size/DTD guards, validated temp writes and `.bak` replacement. Import marks affected elements dirty, regenerates deterministically, records audit provenance, and deliberately does not auto-save `.qsdb` before review.
- generic Family properties can carry material/classification codes, so company classification data round-trips through templates without hard-coding a vendor classification schema.
- expanded preflight and smoke-source guards cover schema v3 migration, rule/audit roundtrip, rule-driven regeneration, template roundtrip/apply, project layer recognition overrides and missing Ribbon command wiring.
- `main` GitHub Actions workflows remain `workflow_dispatch` only.

## Previously verified in GitHub-hosted CI

Historical Core CI runs `31341101835`, `31341548469` and `31341704360` passed their respective older snapshots. Those runs predate the current workflow/persistence/template/recognition changes and **must not be presented as verification of the newest head**. A new GitHub-hosted Core CI run remains owner-controlled/manual-only.

## Gate C blocker

Historical BricsCAD V25 integration probe run `31341184031` remained queued because no matching `[self-hosted, windows, x64, bricscad-v25]` runner was assigned. The repository now includes a V25 NETLOAD/runtime/screenshot harness, but actual plugin/runtime verification still requires a licensed interactive Windows runner.

## Runtime-gated / not yet claimed complete

These still require the actual BricsCAD V25 environment or external release infrastructure:

- full plugin compile against the exact installed V25 `BrxMgd.dll` / `TD_Mgd.dll` after the newest source changes;
- `NETLOAD`, Ribbon/palette and all newly wired recognition/template/revision/BBS commands on V25.1/V25.2;
- private sample-DWG regression and Unicode/HiDPI visual comparison;
- robust polyline wall corners/joins/T-junctions/freeform profiles;
- physical opening/door boolean subtraction from host wall solids;
- automatic room-boundary discovery from arbitrary intersecting wall networks;
- geometric rebar placement/shape generation tied to BBS;
- transient highlight/isolate UX beyond the existing implied-selection/Locate path;
- installer/autoload release package, code signing/signed updater and optional commercial licensing backend.

The source intentionally distinguishes deterministic implementation from BricsCAD/runtime claims that cannot be proved by repository inspection alone.
