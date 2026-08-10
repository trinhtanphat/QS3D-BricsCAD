# Implementation status — 2026-08-10

## Implemented in source

### Platform / persistence / regeneration

- BricsCAD V25 `net48/x64` adapter with external `BrxMgd.dll` / `TD_Mgd.dll` references.
- Project / Zone / Floor / Family / semantic Element model, `.qsdb` schema v3 migration, validated temp-save, atomic replacement where supported, `.bak` recovery, project locking, size/XML safety guards and protected recovery mode.
- persisted dirty flags/timestamps, dependency graph + bounded fixed-point regeneration, project QuantityRules, audit provenance, revision baseline/diff and `.qstemplate` import/export.
- multi-document project cache keyed by live `Document` identity; Save As/unsaved drawing handling is guarded. Live identity uses BricsCAD `Database.FingerprintGuid`; copied/mismatched `.qsdb` Handle identities fail closed instead of being silently rebound.
- generated native outputs carry explicit stale snapshots. Property/geometry-affecting edits mark existing generated mass/rebar/tie/stirrup/slab-mesh/wall-mesh/curtain-frame outputs stale; successful rebuild or guarded invalidation clears the matching stale state.
- project mutation integrity is hardened across Floor/Zone/Family assignment and object-based Bulk Edit APIs: an operation must target the exact `ProjectElement` instance owned by the `ProjectState`; a foreign/spoofed object that merely reuses an existing ID is rejected. Duplicate semantic IDs in the project are treated as an integrity error rather than silently choosing one object.

### BLT-style UI / project editors / selection / property editing

- clean-room dark WPF design system, three-pane main workspace, separate Drawing/Xref + Layer manager, Full Domain Hub, Project Tools, Curtain Wall Hub, Geometry Extensions and native Ribbon bootstrapper.
- `QS3DPROJECTTOOLS` consolidates drawing-bound project administration. `QS3DZONES` exposes Zone CRUD/active-zone/assignment, `QS3DFAMILIES` exposes inheritance-safe Family CRUD/property/assignment, and Floor/Level + Material Catalog editors remain drawing-bound so switching active DWGs does not silently mutate another project.
- model tree → Family/Type → **Bóc chọn** workflow removes the need to memorize most semantic capture commands.
- grouped Vietnamese property labels/units with typed editors: text/numeric, boolean checkbox and editable choice controls.
- explicit **Family / Type** vs **Đối tượng / Instance** property scope. Exactly one semantic selection switches to Instance scope; edits affect only that element and a reset action restores the current Family value.
- Family edits update values that still inherit the previous Family value while preserving true instance overrides. Family reassignment removes old inherited defaults, applies new inherited defaults and retains real instance overrides.
- explicit Active Family semantics are available for future semantic capture. Category-aware capture only consumes the active Family when its category matches; GlassWall/WallPier variant capture now preserves the selected active Family instead of blindly switching to the first Family of that category.
- semantic selection synchronization uses the shared `SemanticReferenceHandles` resolver, including Auto Room boundary provenance and generated-solid fallback; ambiguous multi-element matches do not silently open Instance editing.
- selected-object review exposes Locate/Zoom, transient highlight, `QS3DFOCUS`, `QS3DISOLATE` and `QS3DUNISOLATE`; guarded Section Box/Section Plane/Clip workflows are exposed through Ribbon/Hub.
- the primary Workspace exposes **Giao tường**, **Snap xem**, **Snap áp** and **Auto Host** beside the main Family/modeling actions. Ribbon and Full Domain Hub additionally expose Project Tools, Curtain 3D, slab/wall mesh and unified health entry points.

### Room / finishes

- deterministic planar `RoomBoundaryEngine`: intersection/T-junction subdivision, snapping, iterative bridge removal, bounded-face traversal, stable boundary keys, area/perimeter and source evidence lookup.
- `BulgeArcTessellator` provides bounded sagitta-based tessellation with finite/segment-count guards.
- `QS3DROOMAUTO` accepts planar LINE/POLYLINE/ARC/SPLINE networks. ARC/polyline curves use `RoomBoundaryArcSagittaM`; SPLINE uses bounded `RoomBoundarySplineChordM` sampling with a hard segment cap. Selected source elevations/planarity are checked before topology discovery.
- Auto Room lifecycle is non-destructive: stable provenance reuses compatible Rooms; topology split/merge can mark superseded Rooms `Stale`; stale Rooms/direct dependents are excluded from BQ but remain in `.qsdb` for audit/recovery.
- HT_Phòng generation/synchronization covers floor finish, waterproofing, skirting, wall finish and ceiling finish.

### Tường KT / wall geometry / Cửa-Lỗ

- semantic capture for Tường Gạch/ArchitecturalWall, Vách Kính/GlassWall and Trụ Tường/WallPier, with category-specific starter Family defaults.
- generic Tường KT source path accepts LINE and open POLYLINE centerlines; polyline bulges are tessellated into the deterministic `WallFootprintEngine`; generated geometry uses guarded replacement and finite geometry validation.
- **WallPier LINE** can use the specialized deterministic rectangular/chamfered profile builder. Open POLYLINE WallPier remains on the generic footprint path.
- `QS3DWALLJUNCTIONS` analyzes selected LINE/open-POLYLINE wall centerlines and classifies L/T/X/Straight/End/Multi junction nodes using guarded finite-safe `WallJunctionPlanner` math. `WallJunctionAdjustmentPlanner` additionally produces bounded, reviewable endpoint cleanup plans.
- `QS3DWALLSNAPPREVIEW` / `QS3DWALLSNAPAPPLY` implement review-gated source-centerline endpoint cleanup for tracked wall LINE/open **straight** POLYLINE geometry. Preview stores a SHA-256 signature over geometry/targets/tolerances; Apply refuses stale previews, curved/bulged polylines and nonsemantic wall source. Dependent generated geometry is invalidated only through ownership checks.
- manual Door/Opening host linking propagates dirty state and audit records.
- `OpeningHostMatcher` + `QS3DAUTOLINKHOSTS` provide safe automatic host matching for selected Door/Opening semantics. The matcher scores wall **surface gap**, groups tessellated segments by semantic host, rejects near-tie ambiguity, caps input segments, respects assigned Floor/Zone scope and adds an independent source-Z/elevation tolerance before linking. Auto Host never silently executes the physical boolean cut; `QS3DLINKHOST` remains the explicit manual override.
- `QS3DCUTOPENINGS` physically subtracts linked openings from compatible generated LINE hosts and supported straight non-bulged open-POLYLINE segments when projection is safe and does not cross a corner/junction.
- `QS3DCUTOPENINGSCURVED` adds a dedicated bulged open-POLYLINE cut path. It prepares curved footprints, vertical plans and the complete fingerprint **before** `BoolSubtract`; identical reruns are idempotent and changed geometry on the same already-cut host fails before mutation until the host is rebuilt.
- source replacement clears physical-cut handle/fingerprint/count/mode metadata so an old cut mode cannot survive host replacement.

### Vách Kính / Curtain Wall

- deterministic `CurtainWallLayoutPlanner` and `CurtainWallDetailPlanner` compute panel grid, glass/frame quantities and panel/frame rectangles; schedule and XLSX source paths are present.
- dedicated Curtain Wall Hub edits GlassWall Family controls such as max panel width/height, perimeter/mullion/transom widths, frame material and `CurtainFrameDepthM` while preserving true instance overrides.
- `QS3DCURTAINFRAMES3D` generates ownership-protected mullion/transom/perimeter `Solid3d` overlays for supported horizontal GlassWall LINE sources.
- `QS3DCURTAIN3D` is the one-shot host+frame workflow: the GlassWall backing host remains the single `GeneratedSolidHandle` used by Door/Opening booleans, while frame overlays use dedicated `GeneratedCurtainFrameHandles`.
- native curtain frame generation is capped at 4,096 frame solids/element and 8,192/selected batch even though the Core detail planner allows a larger planning cap.
- deterministic curtain config/live-geometry fingerprints snapshot relevant Family/source state. Curtain health detects missing/live/ownership/count issues and stale configuration or live CAD drift after Family/Instance/grid/source changes.
- Core opening-interruption planning can split curtain frame runs around hosted openings with deterministic bounded output. Native adapter behavior for all opening-interruption cases remains part of the V25/private-DWG runtime gate.
- frame overlays are ownership-invalidated when their host source is replaced, and rebar destructive guards treat frame handles as protected foreign geometry.
- current limitations remain explicit: no curved/open-POLYLINE frame overlay and no panel-by-panel backing glass solids.

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
- `QS3DREBAR3D` generates guarded rectangular-column longitudinal bars.
- `QS3DBEAMREBAR3D` generates supported Beam LINE longitudinal bars using the protected longitudinal-bar ownership slot.
- BBS-shape geometry includes `RebarShapePath` + `ProjectRebarShapePlanner` and `QS3DREBAR3DSHAPE`; supported source paths include straight and configured L/U/Z/custom leg/turn definitions with cutting-length consistency checks.
- `QS3DREBARSTIRRUP3D` generates bounded rectangular beam-stirrup loops along supported horizontal Beam LINE sources; `QS3DREBARTIES3D` generates rectangular Column ties.
- `QS3DSLABREBAR3D` generates rectangular Slab X/Y mesh with **independent X/Y diameters and count/spacing**, Top/Bottom/Both support and dedicated `GeneratedSlabMesh*` ownership/metadata.
- `QS3DWALLREBAR3D` generates StructuralWall horizontal/vertical Near/Far/Both mesh with **independent horizontal/vertical diameters and distribution**, using dedicated `GeneratedWallMesh*` ownership/metadata.
- dedicated health commands cover longitudinal, shape, tie, stirrup, slab mesh and wall mesh. `QS3DREBARHEALTHALL` aggregates all six generated rebar families plus cross-family ownership diagnostics.
- `QS3DHEALTHALL` additionally aggregates model/source/generated-solid health, generated stale snapshots, rebar-mode semantics and curtain-frame health with issue-specific Locate routing.
- generated rebar ownership/invalidation protects source handles, main generated hosts, curtain frame overlays and other generated-rebar families from destructive cross-role erase.
- current beam-stirrup/column-tie geometry uses guarded segmented-cylinder loops; fabrication hooks/bend radii/code-specific anchorage are not inferred without explicit dimensions.

### Packaging / guards / manual release

- V25 release packaging produces command manifest, metadata, hashes and DemandLoad install/uninstall helpers while excluding BricsCAD-owned runtime assemblies.
- per-user DemandLoad installer supports hash verification, optional Authenticode enforcement, `-WhatIf`/confirmation semantics and does not lower `SECURELOAD`.
- source/static guards cover command uniqueness, geometry/rebar safety, Room Auto lifecycle/curve sources, wall junction/snap, Auto Host, straight/curved opening cuts, WallPier profile, slab/wall mesh, curtain panel/native-frame/fingerprint lifecycle, generated ownership, unified health, project editor/assignment integrity, Family/Instance/Active-Family contracts, semantic selection sync and key XAML well-formedness.
- `scripts/preflight-blt-workspace.py` guards primary Workspace/Ribbon/Hub parity including Curtain Hub/Curtain 3D, slab/wall mesh and Health All.
- `scripts/preflight-ci-manual-only.py` requires **every** `.github/workflows/*.yml|yaml` workflow to use `workflow_dispatch` only and requires executable jobs to hard-guard `github.event_name == 'workflow_dispatch'`. The release workflow additionally requires explicit `confirm_release=RELEASE`.
- current GitHub Actions workflow inventory is manual-only: Core CI, V25 integration, curved-opening gate, geometry-extension gate, project-data gate and V25 build/release. None is authorized by commit/push/merge/`continue all` alone.
- `.github/workflows/project-data-gate.yml` is also hard-guarded to manual dispatch and covers Zone/Floor/Family/Material/Project Tools plus project-assignment-integrity checks before its Core build/smoke stage.
- `.github/workflows/release-v25.yml` prepares the owner-approved build → preflight/smoke → V25 x64 build → optional NETLOAD/runtime evidence → ZIP/SHA-256 → GitHub Release path. The integration workflow runtime/artifact paths are aligned to `bin/x64/Release/net48`, matching `package-v25.ps1`.
- `CI_POLICY.md`, `AGENTS.md`, `docs/CI.md`, `docs/MANUAL-BUILD-RELEASE.md` and `README.md` document the same manual-only policy. No GitHub Action was dispatched as part of these source/documentation changes.

## Validation history — do not confuse with current head

### Earlier local snapshot verified on 2026-08-10

An earlier integrated snapshot based on `origin/main` `b00d03f` was compiled against installed BricsCAD V25.2.10 managed assemblies in Release/x64 with 0 warnings / 0 errors. The deterministic Core smoke executable reported `ALL PASS`, and the then-existing preflight set passed locally. No GitHub Action was dispatched for that check.

That proof **predates** the newest curtain native frames/fingerprints, dedicated slab/wall mesh ownership/health, curved-opening idempotence hardening, Project Tools/Zone/Family managers, project-instance ownership hardening, Active Family capture fixes and manual release workflow. It must not be used as proof that the current `main` head has compiled or run inside BricsCAD V25.

### Earlier GitHub-hosted CI

- Earlier full-domain integration gates passed generic/full-domain preflights, Core Release build and deterministic smoke suites.
- Release-candidate run `31346731964` passed generic/full-domain/release preflight, PowerShell AST parsing, Core Release build and the then-current deterministic smoke suite.
- Integrated release-tree run `31346906413` repeated those checks after Audit/Template UI integration.
- These runs predate the newest geometry/rebar/curtain/project-editor/manual-release batches. The current head must not be described as CI-verified until a later explicitly approved run covers it.

## Gate C blocker

Historical V25 integration probe run `31341184031` remained queued because no matching `[self-hosted, windows, x64, bricscad-v25]` runner was assigned. The repository contains V25 build/package/NETLOAD/runtime/screenshot harness source, but actual final plugin/runtime proof still requires a licensed interactive Windows BricsCAD V25 environment.

## Runtime/product work still remaining

- compile the **current final SHA** against the exact target BricsCAD V25 managed assemblies and run DemandLoad/NETLOAD command/Ribbon/palette regression after the owner explicitly requests that build;
- private-DWG and save/reopen/multi-DWG regression for Room Auto, wall snap, Auto Host, straight/curved opening cuts, WallPier profile, curtain host/frame overlay, structure, BQ/BBS and all generated rebar families;
- real V25 validation of Family/Instance/Zone/Floor-Level/Material project editors, typed controls, Focus/Isolate/Section and Unicode/HiDPI behavior;
- physical wall-solid reconciliation/union at L/T/X/Multi junctions remains product work even though guarded **source centerline endpoint snap cleanup** is implemented;
- full native curtain opening-interruption proof, panel-by-panel glass solids and curved/open-POLYLINE frame overlay remain product/runtime work;
- complex corner-crossing curved opening booleans beyond the current guarded footprint planner remain product work;
- fabrication-grade rebar hooks/bend radii/code-specific detailing and richer editing remain product work;
- commercial icon/Ribbon grouping/context-menu/DPI polish should be finalized from real V25 screenshots;
- Authenticode production signing, signed updater and optional commercial licensing/backend remain release/commercial work.

The project deliberately distinguishes **implemented source paths** from behavior that requires licensed BricsCAD V25/private-DWG/runtime proof.
