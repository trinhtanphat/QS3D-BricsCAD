# Implementation status — 2026-08-10

## Implemented in source

- BricsCAD V25 `net48/x64` adapter with external `BrxMgd.dll` / `TD_Mgd.dll` references.
- Clean-room WPF UI design system, left workspace, right Drawing/Layer manager, Full Domain Hub and audit-log review UI.
- Native Ribbon bootstrapper with QS3D workflow tabs; it fails closed and leaves palettes available if V25 ribbon runtime differs.
- Multi-document lifecycle refresh with project cache keyed by live `Document` identity instead of mutable drawing names; Save As synchronizes the sidecar drawing identity and unsaved drawing filenames are sanitized.
- DWG identity now uses the BricsCAD database `FingerprintGuid`. Same-path legacy sidecars migrate once, while a copied/mismatched `.qsdb` fails closed before Handle-based work instead of silently overwriting its identity.
- Project / Zone / Floor / Family / semantic Element model with finite floor elevation validation.
- Data-driven Family property editor with active Zone/Floor/Family context; common geometry/rebar fields now have BLT-style Vietnamese display labels/groups/units, finite-number validation is not dependent on whether a unit label exists, and edits propagate to existing member elements while marking derived quantities dirty.
- BLT-style category workflow in the main palette: select semantic group + Family, select CAD objects, press **Bóc chọn**, then edit grouped properties / build 3D / review BQ without memorizing category command names.
- QSDB schema v3 with deterministic v1 → v2 → v3 migration. Project `QuantityRule` definitions and audit provenance persist inside `.qsdb`.
- validated `.qsdb` temp-save, atomic replace where supported, `.bak` recovery, single-writer project lock, 64 MiB size guard and DTD/external-entity blocking.
- corrupted or missing primary QSDB fallback to a valid backup; unrecoverable existing project data enters protected recovery state and is not silently overwritten.
- element dirty flags and UTC update timestamps persist across `.qsdb` save/reopen; invalid persisted numeric/timestamp/dirty-state data is rejected.
- dependency graph + bounded fixed-point regeneration. Matching project quantity rules run after semantic regeneration using deterministic dependency ordering and numeric Family/instance/quantity variables.
- semantic quantity regeneration no longer clears `Geometry` for native-solid categories; dimension/family edits keep geometry dirty until a committed BricsCAD builder replacement marks it clean.
- semantic regeneration arithmetic is guarded against non-finite values/overflow before derived quantities are committed.
- `QS3DREGEN` is available explicitly; BQ, BBS and Refresh regenerate dirty deterministic semantic quantities before consuming them.
- semantic capture for Room, Tường Gạch/ArchitecturalWall, Vách Kính/GlassWall, Trụ Tường/WallPier, Opening, Door, structural categories and custom takeoff. `QS3DGLASSWALL` / `QS3DWALLPIER` create safe starter Family properties when that category has no Family yet.
- deterministic planar room-boundary discovery source: `RoomBoundaryEngine` subdivides segment intersections/T-junctions, snaps near endpoints, removes graph bridges/dangling edges, traverses bounded faces and returns stable keys, area, perimeter and provenance. Bridge discovery is iterative and per-edge source evidence uses a lookup instead of rescanning the full edge list during face traversal.
- `BulgeArcTessellator` converts polyline bulges into metric straight-segment approximations using the CAD bulge included-angle relation, configurable maximum sagitta, finite-value guards and a bounded segment count.
- `QS3DROOMAUTO` accepts selected LINE/POLYLINE networks including bulged polyline segments, converts drawing units to meters, stores boundary provenance without taking duplicate `SourceHandles` ownership, writes audit events, regenerates Room quantities, and is exposed in Ribbon + Full Domain Hub. The operation captures a project snapshot and rolls back semantic/audit mutations if the update/regeneration pipeline fails.
- Room Auto lifecycle is non-destructive: normalized source-handle provenance is stored as a signature, existing Rooms are reused when their source set remains the same after geometry edits, and topology split/merge marks superseded auto Rooms `Stale` instead of deleting them. Stale Rooms and direct dependents are excluded from project BQ while remaining in `.qsdb` for review/recovery.
- adapter reference-handle resolution supports normal `SourceHandles`, Room Auto `BoundarySourceHandles` and generated-solid fallback. HT_Phòng can resolve an auto Room when the full boundary selection is present, preventing a single shared wall from accidentally targeting both adjacent Rooms. Existing finish semantics are synchronized when an active Room Auto geometry changes.
- Quick Takeoff deterministic Length/Area/Volume/Count path and drawing-unit conversion from BricsCAD `INSUNITS` rather than a hard-coded millimeter fallback.
- deterministic structural quantity regeneration for Beam, Slab, Column, StructuralWall, Foundation, Stair, Railing and Earthwork.
<<<<<<< Updated upstream
- BricsCAD semantic capture commands and Ribbon/Domain Hub actions for Tường KT/Cửa plus Dầm/Sàn/Cột/Vách BTCT/Móng/Cầu thang/Lan can/Đào đất.
- source-level native 3D adapters cover ArchitecturalWall/Tường Gạch from LINE and open POLYLINE centerlines, plus Beam, Slab, Column, StructuralWall, Foundation, Stair footprint mass, Railing line-prism and downward Earthwork footprint mass. Polyline wall bulges are tessellated into the deterministic `WallFootprintEngine`; generated geometry uses guarded two-phase replacement and CAD geometry validation.
=======
- BricsCAD semantic capture commands and Ribbon/Domain Hub actions for Dầm/Sàn/Cột/Vách BTCT/Móng/Cầu thang/Lan can/Đào đất.
- source-level native 3D adapters cover Tường KT, Beam, Slab, Column, StructuralWall, Foundation plus Stair footprint mass, Railing line-prism and downward Earthwork footprint mass, using guarded two-phase generated-geometry replacement and CAD geometry validation.
- generated solids carry versioned QS3D XData ownership (`ProjectId`, `ElementId`, category). Replacement and physical opening boolean operations fail closed unless the live marker matches; Model Health reports missing or mismatched ownership metadata.
>>>>>>> Stashed changes
- host linking supports Door/Opening deduction, safe re-host dirty propagation and persisted audit events for link/unlink operations.
- physical Door/Opening boolean subtraction source is exposed as `QS3DCUTOPENINGS` for supported generated LINE-host solids. The service prepares cuts before mutation and fingerprints live host + opening placement/dimensions, preventing a moved opening from being silently mistaken for an already-applied cut; changed geometry on the same cut solid requires rebuilding the host first.
- HT_Phòng semantic generation for floor finish, waterproofing, skirting, wall finish and ceiling finish.
- live Xref/Layer listing, controls, selection inspection and handle-based Locate/select.
- semantic BQ groups by stable Floor/Family IDs, supports filtering/Locate/XLSX, has a real recalculate callback, persists visible-column preferences, and excludes stale auto-room records/direct dependent finishes.
- deterministic rebar notation/BBS calculation; `QS3DBBS` exports XLSX, `QS3DBBSVIEW` opens review/Locate UI, and `QS3DBBSCSV` exports UTF-8 CSV with spreadsheet formula-injection, control-character, row and non-finite-number guards plus atomic replacement.
- rectangular column rebar geometry planning + guarded native longitudinal-bar Solid3d generation is exposed as `QS3DREBAR3D`; generated bar ownership is tracked/health-checked. This is a narrow source path, not a claim of general rebar authoring.
- revision snapshot persistence (`.qsrev`) plus `QS3DREVBASE` / `QS3DREVDIFF` wiring to the revision comparison UI.
- deterministic recognition core is wired to `QS3DRECOGNIZE` review UI and `QS3DRECOGNIZEAUTO`; auto mode only applies high-confidence/margin results, rejects ambiguous mappings/invalid confidence and refuses semantic category collisions.
- project/company layer mappings can override recognition deterministically before fallback heuristics.
- `.qstemplate` import/export is implemented for Families, QuantityRules, layer mappings and BQ column layout with rollback/confirmation safety for destructive apply.
- BQ XLSX rows now include QS3D Element IDs and CAD handles; `QS3DED2` aliases the BQ/export workflow and `QS3DEXCELLOCATE` performs the reverse workbook-row → handle → live CAD selection path. The reader also supports the supplied BLT hidden `$<decimal handle>` convention.
- derived finish semantics resolve source handles transitively through their room dependency, so BQ export, Locate and finish-only untrack operate on the actual room geometry without duplicating handle ownership.
- `QS3DB4D` is a whole-Current-Space scan rather than a selection alias; the V25 adapter reads curve length, Polyline/Region/Hatch/Solid3d area and Solid3d volume before deterministic recognition.
- generic Family properties can carry material/classification codes, so company classification data round-trips through templates without hard-coding a vendor classification schema.
- V25 runtime probe source verifies actual palette visibility rather than treating command dispatch alone as UI success.
- V25 release packaging generates a command manifest from `CommandMethod` declarations, package metadata, SHA-256 hashes for shipped payloads, installer/uninstaller helpers and a release ZIP while excluding BricsCAD-owned runtime assemblies.
- per-user BricsCAD V25 DemandLoad installer source is implemented with OnCommand default / optional OnStartup, command registration, payload hash verification, optional Authenticode enforcement, staged file replacement, `-WhatIf`/confirmation semantics and safe uninstall. It intentionally does not weaken BricsCAD security settings.
- expanded generic/full-domain/geometry-completion preflight guards cover schema/persistence, command uniqueness, generated geometry, structural quantities, BBS CSV safety, wall footprint/opening boolean/rebar geometry source paths, stable planar/curved Room Auto discovery/lifecycle/rollback/UI wiring, DemandLoad wiring and PowerShell syntax.
- Ribbon + Full Domain Hub now surface Tường Gạch, Vách Kính, Trụ Tường, Door/Opening host link + physical cut and column rebar 3D workflows instead of leaving them command-line-only.
- `main` GitHub Actions workflows remain `workflow_dispatch` only.
- save hardening now rejects empty mutable map keys/Zone/Floor names before replacement; revision temp files are deep-loaded before replacement; explicit zero-valued quantity additions/removals remain visible in revision reports; malformed compound rebar notation with empty `+` segments is rejected.

## Locally verified on 2026-08-10

- Core smoke suite: `ALL PASS`.
- Exact installed BricsCAD V25.2.10 managed references: Release/x64 plugin build succeeded with 0 warnings and 0 errors.
- Read-only check of the supplied `DGKL.xlsx`: Excel row 5 resolved decimal handles `12510,12512` to hexadecimal `30DE,30E0`; row 6 resolved to `30DF,30E1`.
- Both repository preflight suites pass. No GitHub Action was dispatched.

## Verified in GitHub-hosted CI

- Full-domain integration gates were run repeatedly while earlier hardening was merged; the final PR #1 integration gate passed generic preflight, full-domain preflight, Core Release build and the complete deterministic smoke suite before merge.
- Release-candidate run `31346731964` passed generic preflight, full-domain/release preflight, PowerShell AST parsing for package/install/uninstall scripts, Core Release build and the complete deterministic smoke suite.
- Integrated release-tree run `31346906413` repeated those checks after merging the Audit/Template UI work and also passed generic preflight, full-domain/release preflight, PowerShell parsing, Core Release build and the complete deterministic smoke suite.
- Those runs predate the newest Room Auto, wall footprint/polyline, physical opening, rebar-geometry and BLT UI completion batches. These newest heads must not be called CI-verified until a later explicitly approved run covers them.
- GitHub-hosted checks validate repository/Core/release-script logic only; they are not substitutes for BricsCAD V25 plugin/runtime execution.

## Gate C blocker

Historical BricsCAD V25 integration probe run `31341184031` remained queued because no matching `[self-hosted, windows, x64, bricscad-v25]` runner was assigned. The repository includes a V25 build/package/NETLOAD/runtime/screenshot harness, but actual plugin/runtime verification still requires a licensed interactive Windows runner.

## Runtime-gated / not yet claimed complete

These still require the actual BricsCAD V25 environment or external release infrastructure:

- full plugin compile against the exact installed V25 `BrxMgd.dll` / `TD_Mgd.dll` after the newest source changes;
- real DemandLoad install/uninstall and `NETLOAD`, Ribbon/palette plus recognition/template/revision/BBS/domain/audit/`QS3DROOMAUTO`/physical-cut/rebar-3D commands on V25.1/V25.2;
- private sample-DWG regression and Unicode/HiDPI visual comparison;
- production proof/performance for polyline wall corners and curved centerlines; wall-to-wall joins/T-junction cleanup and freeform wall profiles remain product work;
- production proof of physical opening/door boolean subtraction plus generalized support beyond the current compatible LINE-host path;
- V25/private-DWG proof and large-network performance tuning for automatic room-boundary discovery; optional direct ARC/SPLINE source support beyond LINE/POLYLINE inputs;
- general geometric rebar authoring beyond the current guarded rectangular-column longitudinal-bar path (beam/slab/wall bars, stirrups, hooks/bends, shapes and editing);
- transient highlight/isolate/section-box UX beyond the existing implied-selection/Locate path;
- Authenticode production code signing, signed updater and optional commercial licensing backend.

The source intentionally distinguishes deterministic implementation from BricsCAD/runtime claims that cannot be proved by repository inspection alone.
