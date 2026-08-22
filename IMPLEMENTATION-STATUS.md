# Implementation status — 2026-08-10

## Implemented in source

### Platform / persistence / regeneration

- BricsCAD V25 `net48/x64` adapter with external `BrxMgd.dll` / `TD_Mgd.dll` references.
- Project / Zone / Floor / Family / semantic Element model, `.qsdb` schema v3 migration, validated temp-save, atomic replacement where supported, `.bak` recovery, project locking, size/XML safety guards and protected recovery mode.
- persisted dirty flags/timestamps, dependency graph + bounded fixed-point regeneration, project QuantityRules, audit provenance, revision baseline/diff and `.qstemplate` import/export.
- multi-document project cache keyed by live `Document` identity; Save As/unsaved drawing handling is guarded. Live identity uses BricsCAD `Database.FingerprintGuid`; copied/mismatched `.qsdb` Handle identities fail closed instead of being silently rebound.

### BLT-style UI / selection / property editing

- clean-room dark WPF design system, three-pane main workspace, separate Drawing/Xref + Layer manager, Full Domain Hub and native Ribbon bootstrapper.
- model tree → Family/Type → **Bóc chọn** workflow removes the need to memorize most semantic capture commands.
- grouped Vietnamese property labels/units with typed editors: text/numeric, boolean checkbox and editable choice controls.
- explicit **Family / Type** vs **Đối tượng / Instance** property scope. Exactly one semantic selection switches to Instance scope; edits affect only that element and a reset action restores the current Family value.
- Family edits update values that still inherit the previous Family value while preserving true instance overrides. Opening the inspector no longer dirties the project just by rebinding the Family name.
- semantic selection synchronization uses the shared `SemanticReferenceHandles` resolver, including Auto Room boundary provenance and generated-solid fallback; ambiguous multi-element matches do not silently open Instance editing.
- selected-object review exposes Locate/Zoom, transient highlight, `QS3DFOCUS`, `QS3DISOLATE` and `QS3DUNISOLATE`.
- the primary Workspace now exposes **Giao tường**, **Snap xem**, **Snap áp** and **Auto Host** beside the main Family/modeling actions; Ribbon and Full Domain Hub expose the same advanced wall/host/review/rebar workflows.

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
- `QS3DB4D` performs a bounded whole-Current-Space scan, excludes generated mass/rebar/shape-rebar handles, reads curve/Polyline/Region/Hatch/Solid3d metrics and applies only high-confidence recognition. Rescan replaces stale source-derived metrics and `CAD.*` metadata while preserving an existing element's Family/Floor/Zone context.
- BQ groups by stable Floor/Family IDs, supports filtering/Locate/XLSX, real recalculation and persisted column preferences. XLSX rows carry QS3D Element IDs, CAD handles and the owning DWG fingerprint; `QS3DED2` exposes the workflow and `QS3DEXCELLOCATE` rejects a mismatched fingerprint before selection. Legacy BLT `$<decimal handle>` rows require explicit `YES` confirmation.
- source-reference resolution follows room dependencies for generated finishes, so BQ/Locate/untrack reach the room source without duplicating Handle ownership.
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
- `scripts/preflight-blt-workspace.py` specifically guards typed Family/Instance controls, reset override, semantic selection sync, Workspace/Ribbon/Hub Giao tường + Snap Preview/Apply + Auto Host + review entry-point parity and key XAML well-formedness.
- both manual-only workflows include the safe Auto Host source preflight; no workflow was dispatched as part of these source changes.
- `main` GitHub Actions workflows remain `workflow_dispatch` only.

## Locally verified on 2026-08-10

- The integrated branch based on `origin/main` `b15c02c` compiled Core and the BricsCAD V25 adapter against the installed V25.2.10 managed assemblies in Release/x64 with **0 warnings / 0 errors**.
- The deterministic Core smoke executable reported `ALL PASS`.
- All ten repository `preflight*.py` scripts passed using the installed BricsCAD Python 3.9 runtime. No GitHub Action was dispatched.
- Read-only reference check: supplied `DGKL.xlsx` row 5 decimal handles `12510,12512` resolve to hexadecimal `30DE,30E0`; row 6 resolves to `30DF,30E1`. The workbook was not modified.

## Verified in earlier GitHub-hosted CI

- Earlier full-domain integration gates passed generic/full-domain preflights, Core Release build and deterministic smoke suites.
- Release-candidate run `31346731964` passed generic/full-domain/release preflight, PowerShell AST parsing, Core Release build and the then-current deterministic smoke suite.
- Integrated release-tree run `31346906413` repeated those checks after Audit/Template UI integration.
- These runs **predate** the newest Room ARC/SPLINE lifecycle hardening, wall-junction/snap/polyline-opening/Auto-Host work, BBS-shape rebar, review commands and Family/Instance UI batches. The current head must not be described as CI-verified until a later explicitly approved run covers it.

## Gate C blocker

Historical V25 integration probe run `31341184031` remained queued because no matching `[self-hosted, windows, x64, bricscad-v25]` runner was assigned. The repository contains V25 build/package/NETLOAD/runtime/screenshot harness source, but actual plugin/runtime proof still requires a licensed interactive Windows BricsCAD V25 environment.

## Runtime/product work still remaining

- preserve the successful local V25.2.10 compile while still completing real DemandLoad/NETLOAD command/Ribbon/palette regression on the final published SHA;
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
