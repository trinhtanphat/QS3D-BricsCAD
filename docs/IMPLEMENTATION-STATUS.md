# Implementation status — 2026-08-10

## Implemented in source

- BricsCAD V25 `net48/x64` adapter with external `BrxMgd.dll` / `TD_Mgd.dll` references.
- Clean-room WPF UI design system, left workspace, right Drawing/Layer manager, Full Domain Hub and audit-log review UI.
- Native Ribbon bootstrapper with QS3D workflow tabs; it fails closed and leaves palettes available if V25 ribbon runtime differs.
- Multi-document lifecycle refresh with project cache keyed by live `Document` identity instead of mutable drawing names; Save As synchronizes the sidecar drawing identity and unsaved drawing filenames are sanitized.
- Project / Zone / Floor / Family / semantic Element model with finite floor elevation validation.
- Data-driven Family property editor with active Zone/Floor/Family context; edits to a Family property are propagated to existing member elements and mark derived quantities dirty.
- QSDB schema v3 with deterministic v1 → v2 → v3 migration. Project `QuantityRule` definitions and audit provenance persist inside `.qsdb`.
- validated `.qsdb` temp-save, atomic replace where supported, `.bak` recovery, single-writer project lock, 64 MiB size guard and DTD/external-entity blocking.
- corrupted or missing primary QSDB fallback to a valid backup; unrecoverable existing project data enters protected recovery state and is not silently overwritten.
- element dirty flags and UTC update timestamps persist across `.qsdb` save/reopen; invalid persisted numeric/timestamp/dirty-state data is rejected.
- dependency graph + bounded fixed-point regeneration. Matching project quantity rules run after semantic regeneration using numeric Family properties, element properties and current quantities as deterministic variables.
- `QS3DREGEN` is available explicitly; BQ, BBS and Refresh regenerate dirty deterministic semantic quantities before consuming them.
- semantic capture for Room, Tường KT, Opening, Door, structural categories and custom takeoff.
- deterministic planar room-boundary discovery source: `RoomBoundaryEngine` subdivides segment intersections/T-junctions, snaps near endpoints, removes graph bridges/dangling edges, traverses bounded faces and returns stable keys, area, perimeter and provenance. `QS3DROOMAUTO` adapts selected straight LINE/POLYLINE networks into Room semantics, writes audit provenance and deliberately stores boundary handles outside `SourceHandles` to avoid duplicate semantic ownership.
- Quick Takeoff deterministic Length/Area/Volume/Count path and drawing-unit conversion from BricsCAD `INSUNITS` rather than a hard-coded millimeter fallback.
- deterministic structural quantity regeneration for Beam, Slab, Column, StructuralWall, Foundation, Stair, Railing and Earthwork.
- BricsCAD semantic capture commands and Ribbon/Domain Hub actions for Dầm/Sàn/Cột/Vách BTCT/Móng/Cầu thang/Lan can/Đào đất.
- source-level native 3D adapters cover Tường KT, Beam, Slab, Column, StructuralWall, Foundation plus Stair footprint mass, Railing line-prism and downward Earthwork footprint mass, using guarded two-phase generated-geometry replacement.
- host linking supports Door/Opening deduction, safe re-host dirty propagation and persisted audit events for link/unlink operations.
- HT_Phòng semantic generation for floor finish, waterproofing, skirting, wall finish and ceiling finish.
- live Xref/Layer listing, controls, selection inspection and handle-based Locate/select.
- semantic BQ groups by stable Floor/Family IDs, supports filtering/Locate/XLSX, has a real recalculate callback, and persists visible-column preferences in project metadata.
- deterministic rebar notation/BBS calculation; `QS3DBBS` exports XLSX, `QS3DBBSVIEW` opens review/Locate UI, and `QS3DBBSCSV` exports UTF-8 CSV with spreadsheet formula-injection, control-character and non-finite-number guards.
- revision snapshot persistence (`.qsrev`) plus `QS3DREVBASE` / `QS3DREVDIFF` wiring to the revision comparison UI.
- deterministic recognition core is wired to `QS3DRECOGNIZE` review UI and `QS3DRECOGNIZEAUTO`; auto mode only applies high-confidence/margin results, rejects ambiguous mappings/invalid confidence and refuses semantic category collisions.
- project/company layer mappings can override recognition deterministically before fallback heuristics.
- `.qstemplate` import/export is implemented for Families, QuantityRules, layer mappings and BQ column layout with rollback/confirmation safety for destructive apply.
- generic Family properties can carry material/classification codes, so company classification data round-trips through templates without hard-coding a vendor classification schema.
- V25 runtime probe source verifies actual palette visibility rather than treating command dispatch alone as UI success.
- V25 release packaging generates a command manifest from `CommandMethod` declarations, package metadata, SHA-256 hashes for shipped payloads, installer/uninstaller helpers and a release ZIP while excluding BricsCAD-owned runtime assemblies.
- per-user BricsCAD V25 DemandLoad installer source is implemented with OnCommand default / optional OnStartup, command registration, payload hash verification, optional Authenticode enforcement, staged file replacement, `-WhatIf`/confirmation semantics and safe uninstall. It intentionally does not weaken BricsCAD security settings.
- expanded generic and full-domain/release preflight guards cover schema/persistence, command uniqueness, generated geometry, full-domain quantities, BBS CSV safety, planar room discovery, DemandLoad wiring and PowerShell syntax.
- `main` GitHub Actions workflows remain `workflow_dispatch` only.

## Verified in GitHub-hosted CI

- Full-domain integration gates were run repeatedly while concurrent hardening was merged; the final PR #1 integration gate passed generic preflight, full-domain preflight, Core Release build and the complete deterministic smoke suite before merge.
- Release-candidate run `31346731964` passed generic preflight, full-domain/release preflight, PowerShell AST parsing for package/install/uninstall scripts, Core Release build and the complete deterministic smoke suite.
- Integrated release-tree run `31346906413` repeated those checks after merging Audit/Template UI work and also passed generic preflight, full-domain/release preflight, PowerShell parsing, Core Release build and the complete deterministic smoke suite.
- Those runs predate the newest room-boundary batch; the room-boundary head must not be called CI-verified until a later explicitly approved run covers it.
- GitHub-hosted checks validate repository/Core/release-script logic only; they are not substitutes for BricsCAD V25 plugin/runtime execution.

## Gate C blocker

Historical BricsCAD V25 integration probe run `31341184031` remained queued because no matching `[self-hosted, windows, x64, bricscad-v25]` runner was assigned. The repository includes a V25 build/package/NETLOAD/runtime/screenshot harness, but actual plugin/runtime verification still requires a licensed interactive Windows runner.

## Runtime-gated / not yet claimed complete

These still require the actual BricsCAD V25 environment or external release infrastructure:

- full plugin compile against the exact installed V25 `BrxMgd.dll` / `TD_Mgd.dll` after the newest source changes;
- real DemandLoad install/uninstall and `NETLOAD`, Ribbon/palette plus recognition/template/revision/BBS/domain/audit/`QS3DROOMAUTO` commands on V25.1/V25.2;
- private sample-DWG regression and Unicode/HiDPI visual comparison;
- robust polyline wall corners/joins/T-junctions/freeform profiles;
- physical opening/door boolean subtraction from host wall solids;
- V25/private-DWG proof, curved/bulged boundary support and performance tuning for large automatic room-boundary networks;
- geometric rebar placement/shape generation tied to BBS;
- transient highlight/isolate UX beyond the existing implied-selection/Locate path;
- Authenticode production code signing, signed updater and optional commercial licensing backend.

The source intentionally distinguishes deterministic implementation from BricsCAD/runtime claims that cannot be proved by repository inspection alone.
