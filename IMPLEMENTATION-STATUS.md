# Implementation status — 2026-08-10

## Implemented in source

### Platform / persistence / regeneration

- BricsCAD V25 `net48/x64` adapter with external `BrxMgd.dll` / `TD_Mgd.dll` references.
<<<<<<< HEAD
- Project / Zone / Floor / Family / semantic Element model, `.qsdb` schema v3 migration, validated temp-save, atomic replacement where supported, `.bak` recovery, project locking, size/XML safety guards and protected recovery mode.
- persisted dirty flags/timestamps, dependency graph + bounded fixed-point regeneration, project QuantityRules, audit provenance, revision baseline/diff and `.qstemplate` import/export.
- multi-document project cache keyed by live `Document` identity; Save As/unsaved drawing handling is guarded.

### BLT-style UI / selection / property editing

- clean-room dark WPF design system, three-pane main workspace, separate Drawing/Xref + Layer manager, Full Domain Hub and native Ribbon bootstrapper.
- model tree → Family/Type → **Bóc chọn** workflow removes the need to memorize most semantic capture commands.
- grouped Vietnamese property labels/units with typed editors: text/numeric, boolean checkbox and editable choice controls.
- explicit **Family / Type** vs **Đối tượng / Instance** property scope. Exactly one semantic selection switches to Instance scope; edits affect only that element and a reset action restores the current Family value.
- Family edits update values that still inherit the previous Family value while preserving true instance overrides. Opening the inspector no longer dirties the project just by rebinding the Family name.
- semantic selection synchronization uses the shared `SemanticReferenceHandles` resolver, including Auto Room boundary provenance and generated-solid fallback; ambiguous multi-element matches do not silently open Instance editing.
- selected-object review exposes Locate/Zoom, transient highlight, `QS3DFOCUS`, `QS3DISOLATE` and `QS3DUNISOLATE`. Ribbon and Full Domain Hub expose the same major review/model workflows.

### Room / finishes

- deterministic planar `RoomBoundaryEngine`: intersection/T-junction subdivision, snapping, iterative bridge removal, bounded-face traversal, stable boundary keys, area/perimeter and source evidence lookup.
- `BulgeArcTessellator` provides bounded sagitta-based tessellation with finite/segment-count guards.
- `QS3DROOMAUTO` accepts planar LINE/POLYLINE/ARC/SPLINE networks. ARC/polyline curves use `RoomBoundaryArcSagittaM`; SPLINE uses bounded `RoomBoundarySplineChordM` sampling with a hard segment cap. Selected source elevations/planarity are checked before topology discovery.
- Auto Room lifecycle is non-destructive: stable provenance reuses compatible Rooms; topology split/merge can mark superseded Rooms `Stale`; stale Rooms/direct dependents are excluded from BQ but remain in `.qsdb` for audit/recovery.
- HT_Phòng generation/synchronization covers floor finish, waterproofing, skirting, wall finish and ceiling finish.

### Tường KT / wall geometry / Cửa-Lỗ

- semantic capture for Tường Gạch/ArchitecturalWall, Vách Kính/GlassWall and Trụ Tường/WallPier, with safe starter Family defaults for the latter two.
- native Tường KT source path accepts LINE and open POLYLINE centerlines for all three categories. Polyline bulges are tessellated into the deterministic `WallFootprintEngine`; generated geometry uses guarded replacement and finite geometry validation.
- `QS3DWALLJUNCTIONS` analyzes selected LINE/open-POLYLINE wall centerlines and classifies L/T/X/Straight/End/Multi junction nodes using guarded finite-safe `WallJunctionPlanner` math. `WallJunctionAdjustmentPlanner` additionally produces bounded, reviewable endpoint cleanup plans.
- `QS3DWALLSNAPPREVIEW` / `QS3DWALLSNAPAPPLY` implement review-gated source-centerline endpoint cleanup for tracked wall LINE/open **straight** POLYLINE geometry. Preview stores a SHA-256 signature over geometry/targets/tolerances; Apply refuses stale previews, curved/bulged polylines and nonsemantic wall source. Edited semantic owners are marked `Geometry|Quantity` dirty after CAD mutation.
- manual Door/Opening host linking propagates dirty state and audit records.
- `OpeningHostMatcher` + `QS3DAUTOLINKHOSTS` provide safe automatic host matching for selected Door/Opening semantics. The matcher scores wall **surface gap** (`centerline distance - thickness/2`), groups tessellated segments by semantic host, rejects near-tie ambiguity, caps input segments, respects assigned Floor/Zone scope and adds an independent source-Z/elevation tolerance before linking. Auto Host never silently executes the physical boolean cut; `QS3DLINKHOST` remains the explicit manual override.
- `QS3DCUTOPENINGS` physically subtracts linked openings from compatible generated wall solids. LINE hosts are supported for ArchitecturalWall, GlassWall, WallPier and StructuralWall. Straight non-bulged POLYLINE hosts are also handled when `PolylineOpeningCutPlanner` can safely project the opening to a single segment without crossing a corner/junction. Curved/bulged polyline-host cuts are rejected rather than guessed.
- opening cut preparation/fingerprints include live host/opening placement and dimensions; changed geometry on the same already-cut solid requires a host rebuild before re-cut.

### Structure / quantity / recognition

- deterministic semantic quantities and guarded native source paths for Beam, Slab, Column, StructuralWall, Foundation, Stair, Railing and Earthwork.
- Quick Takeoff Length/Area/Volume/Count uses drawing `INSUNITS` conversion.
- BQ groups by stable Floor/Family IDs, supports filtering/Locate/XLSX, real recalculation and persisted column preferences.
- deterministic recognition + review and confident auto-apply; project/company layer mappings override fallback heuristics.
- live Xref/Layer controls, selection inspection and semantic reference-based Locate paths are wired through BQ/Health/BBS/revision workflows.

### Rebar

- deterministic notation/BBS calculation plus XLSX/UTF-8 CSV export and review/Locate UI.
- `QS3DREBAR3D` generates guarded rectangular-column longitudinal bars; ownership, protected semantic/generated handles, generated handles and count mismatches are health-checked before destructive replacement.
- linear rebar distribution planning supports count/spacing modes with bounded bar counts and deterministic offsets.
- BBS-shape geometry source includes `RebarShapePath` + `ProjectRebarShapePlanner` and `QS3DREBAR3DSHAPE`. Supported source paths include straight and configured L/U/Z/custom leg/turn definitions; cutting-length consistency and distribution placement are validated before native geometry mutation.
- shape-generated bars use separate ownership metadata and `QS3DREBARSHAPEHEALTH`; destructive replacement refuses ambiguous/protected ownership.

### Packaging / guards

- V25 release packaging produces command manifest, metadata, hashes and DemandLoad install/uninstall helpers while excluding BricsCAD-owned runtime assemblies.
- per-user DemandLoad installer supports hash verification, optional Authenticode enforcement, `-WhatIf`/confirmation semantics and does not lower `SECURELOAD`.
- generic/full-domain/geometry/room-curve/advanced-geometry static preflights cover command uniqueness, geometry/rebar safety, Room Auto lifecycle, wall junction analysis + review-gated snap apply, straight-polyline opening cuts, safe Auto Host matching/elevation/ambiguity separation, Family/Instance inspector contracts, semantic selection sync and key XAML well-formedness.
- both manual-only workflows include the safe Auto Host source preflight; no workflow was dispatched as part of these source changes.
=======
- Clean-room WPF UI design system, left workspace, right Drawing/Layer manager, Full Domain Hub and audit-log review UI.
- Native Ribbon bootstrapper with QS3D workflow tabs; it fails closed and leaves palettes available if V25 ribbon runtime differs.
- Multi-document lifecycle refresh with project cache keyed by live `Document` identity instead of mutable drawing names; Save As synchronizes the sidecar drawing identity and unsaved drawing filenames are sanitized.
- Project / Zone / Floor / Family / semantic Element model with finite floor elevation validation.
- Data-driven Family property editor with active Zone/Floor/Family context; common geometry/rebar fields now have BLT-style Vietnamese display labels/groups/units, finite-number validation is not dependent on whether a unit label exists, and edits propagate to existing member elements while marking derived quantities dirty.
- BLT-style category workflow in the main palette: select semantic group + Family, select CAD objects, press **Bóc chọn**, then edit grouped properties / build 3D / review BQ without memorizing category command names.
- QSDB schema v3 with deterministic v1 → v2 → v3 migration. Project `QuantityRule` definitions and audit provenance persist inside `.qsdb`.
- validated `.qsdb` temp-save, atomic replace where supported, `.bak` recovery, single-writer project lock, 64 MiB size guard and DTD/external-entity blocking.
- corrupted or missing primary QSDB fallback to a valid backup; unrecoverable existing project data enters protected recovery state and is not silently overwritten.
- element dirty flags and UTC update timestamps persist across `.qsdb` save/reopen; invalid persisted numeric/timestamp/dirty-state data is rejected.
- dependency graph + bounded fixed-point regeneration. Matching project quantity rules run after semantic regeneration using deterministic dependency ordering and numeric Family/instance/quantity variables.
- semantic regeneration arithmetic is guarded against non-finite values/overflow before derived quantities are committed.
- `QS3DREGEN` is available explicitly; BQ, BBS and Refresh regenerate dirty deterministic semantic quantities before consuming them.
- semantic capture for Room, Tường Gạch/ArchitecturalWall, Vách Kính/GlassWall, Trụ Tường/WallPier, Opening, Door, structural categories and custom takeoff. `QS3DGLASSWALL` / `QS3DWALLPIER` create safe starter Family properties when that category has no Family yet.
- deterministic planar room-boundary discovery source: `RoomBoundaryEngine` subdivides segment intersections/T-junctions, snaps near endpoints, removes graph bridges/dangling edges, traverses bounded faces and returns stable keys, area, perimeter and provenance. Bridge discovery is iterative and per-edge source evidence uses a lookup instead of rescanning the full edge list during face traversal.
- `BulgeArcTessellator` converts polyline bulges and ARC-derived bulges into metric straight-segment approximations using the CAD bulge included-angle relation, configurable maximum sagitta, finite-value guards and a bounded segment count.
- `QS3DROOMAUTO` accepts selected planar LINE/POLYLINE/ARC/SPLINE networks. Direct ARC inputs require plan-view normal +Z; POLYLINE plan-view orientation is checked; LINE/ARC/POLYLINE/SPLINE sample elevations are constrained by `RoomBoundaryToleranceM`. ARC/polyline bulges use `RoomBoundaryArcSagittaM`; SPLINE uses `RoomBoundarySplineChordM` with a 4096-segment cap. Boundary provenance is stored without taking duplicate `SourceHandles` ownership; the operation writes audit events, regenerates Room quantities, is exposed in Ribbon + Full Domain Hub, and rolls semantic/audit mutations back if the update/regeneration pipeline fails.
- Room Auto lifecycle is non-destructive: normalized source-handle provenance is stored as a signature, existing Rooms are reused when their source set remains the same after geometry edits, and topology split/merge marks superseded auto Rooms `Stale` instead of deleting them. Stale Rooms and direct dependents are excluded from project BQ while remaining in `.qsdb` for review/recovery.
- adapter reference-handle resolution supports normal `SourceHandles`, Room Auto `BoundarySourceHandles` and generated-solid fallback. HT_Phòng can resolve an auto Room when the full boundary selection is present, preventing a single shared wall from accidentally targeting both adjacent Rooms. Existing finish semantics are synchronized when an active Room Auto geometry changes.
- Quick Takeoff deterministic Length/Area/Volume/Count path and drawing-unit conversion from BricsCAD `INSUNITS` rather than a hard-coded millimeter fallback.
- deterministic structural quantity regeneration for Beam, Slab, Column, StructuralWall, Foundation, Stair, Railing and Earthwork.
- BricsCAD semantic capture commands and Ribbon/Domain Hub actions for Tường KT/Cửa plus Dầm/Sàn/Cột/Vách BTCT/Móng/Cầu thang/Lan can/Đào đất.
- source-level native 3D adapters cover Tường Gạch/ArchitecturalWall, Vách Kính/GlassWall and Trụ Tường/WallPier from LINE and open POLYLINE centerlines, plus Beam, Slab, Column, StructuralWall, Foundation, Stair footprint mass, Railing line-prism and downward Earthwork footprint mass. All three Tường KT variants share the guarded centerline extrusion path; polyline bulges are tessellated into the deterministic `WallFootprintEngine`; generated geometry uses guarded two-phase replacement and CAD geometry validation.
- host linking supports Door/Opening deduction, safe re-host dirty propagation and persisted audit events for link/unlink operations.
- physical Door/Opening boolean subtraction source is exposed as `QS3DCUTOPENINGS` for compatible generated LINE-host solids across ArchitecturalWall, GlassWall, WallPier and StructuralWall. The service prepares cuts before mutation and fingerprints live host + opening placement/dimensions, preventing a moved opening from being silently mistaken for an already-applied cut; changed geometry on the same cut solid requires rebuilding the host first.
- HT_Phòng semantic generation for floor finish, waterproofing, skirting, wall finish and ceiling finish.
- live Xref/Layer listing, controls, selection inspection and handle-based Locate/select; semantic review/Locate paths use the shared semantic reference resolver so Room Auto boundary provenance remains locatable without duplicating ownership handles.
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
- expanded generic/full-domain/geometry-completion/room-curve preflight guards cover schema/persistence, command uniqueness, generated geometry, structural quantities, BBS CSV safety, all three Tường KT line/polyline 3D variants, all compatible LINE-wall physical-cut host categories, wall footprint/opening boolean/rebar geometry source paths, planar LINE/POLYLINE/ARC/SPLINE Room Auto sampling/planarity/lifecycle/rollback/UI wiring, DemandLoad wiring and PowerShell syntax.
- Ribbon + Full Domain Hub now surface Tường Gạch, Vách Kính, Trụ Tường, Door/Opening host link + physical cut and column rebar 3D workflows instead of leaving them command-line-only.
>>>>>>> 1f2557c (feat: add B4D scan and Excel handle round-trip)
- `main` GitHub Actions workflows remain `workflow_dispatch` only.
- save hardening now rejects empty mutable map keys/Zone/Floor names before replacement; revision temp files are deep-loaded before replacement; explicit zero-valued quantity additions/removals remain visible in revision reports; malformed compound rebar notation with empty `+` segments is rejected.

## Locally verified on 2026-08-10

- Core smoke suite: `ALL PASS`.
- Exact installed BricsCAD V25.2.10 managed references: Release/x64 plugin build succeeded with 0 warnings and 0 errors.
- Read-only check of the supplied `DGKL.xlsx`: Excel row 5 resolved decimal handles `12510,12512` to hexadecimal `30DE,30E0`; row 6 resolved to `30DF,30E1`.
- Both repository preflight suites pass. No GitHub Action was dispatched.

## Verified in earlier GitHub-hosted CI

- Earlier full-domain integration gates passed generic/full-domain preflights, Core Release build and deterministic smoke suites.
- Release-candidate run `31346731964` passed generic/full-domain/release preflight, PowerShell AST parsing, Core Release build and the then-current deterministic smoke suite.
- Integrated release-tree run `31346906413` repeated those checks after Audit/Template UI integration.
- These runs **predate** the newest Room ARC/SPLINE lifecycle hardening, wall-junction/snap/polyline-opening/Auto-Host work, BBS-shape rebar, review commands and Family/Instance UI batches. The current head must not be described as CI-verified until a later explicitly approved run covers it.

## Gate C blocker

Historical V25 integration probe run `31341184031` remained queued because no matching `[self-hosted, windows, x64, bricscad-v25]` runner was assigned. The repository contains V25 build/package/NETLOAD/runtime/screenshot harness source, but actual plugin/runtime proof still requires a licensed interactive Windows BricsCAD V25 environment.

## Runtime/product work still remaining

- compile the newest adapter against the exact installed V25 `BrxMgd.dll` / `TD_Mgd.dll`, then real DemandLoad/NETLOAD command/Ribbon/palette regression;
- private-DWG and save/reopen/multi-DWG regression for Room Auto, wall centerlines/snap cleanup, Auto Host, straight-polyline opening cuts, structure, BQ/BBS and both rebar geometry paths;
- real V25 validation of Family/Instance scope, typed controls, Focus/Isolate/restore and Unicode/HiDPI behavior;
- production-grade Vách Kính curtain-wall framing/panels and specialized Trụ Tường profiles/material presentation beyond the generic Tường KT extrusion;
- physical wall-solid reconciliation/union at L/T/X/Multi junctions remains product work even though guarded **source centerline endpoint snap cleanup** is now implemented;
- generalized opening booleans for curved/bulged polyline wall hosts and complex corner-crossing cases;
- broader rebar authoring/editing for beam/slab/wall bars, stirrups, hooks/bend radii and richer shape manipulation beyond the current deterministic source paths;
- transient section-box and deeper isolate/highlight UX proven on V25;
- commercial icon/Ribbon grouping/context-menu/DPI polish based on real screenshots;
- Authenticode production signing, signed updater and optional commercial licensing/backend.

The project deliberately distinguishes **implemented source paths** from behavior that requires licensed BricsCAD V25/private-DWG/runtime proof.
