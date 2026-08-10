# Implementation status — 2026-08-10

This file distinguishes **implemented source paths** from behavior that still requires licensed BricsCAD V25/private-DWG runtime proof.

## Product form

The current and intended shipping form is a **BricsCAD V25 x64 plugin**:

- `QS3D.BricsCAD.V25` builds as a .NET Framework **Library/DLL**, not an EXE;
- BricsCAD loads the plugin through DemandLoad or `NETLOAD` and remains the DWG/database/editor/viewport host;
- QS3D UI is Ribbon/palette/modeless WPF UI hosted from BricsCAD;
- `QS3D.Core` is CAD-independent for deterministic logic and testing, but is not a standalone CAD application;
- no standalone `QS3D.exe` is implemented or required by the current product scope;
- BLT-style terminology in this file refers to workflow/UX familiarity only.

See `docs/PRODUCT-BOUNDARY.md`. Any future standalone product requires a separate explicit owner decision.

## Implemented in source

### Platform / persistence / project integrity

- BricsCAD V25 `net48/x64` plugin adapter with external `BrxMgd.dll` / `TD_Mgd.dll` references and `Private=false`.
- Project / Zone / Floor / Family / semantic Element model with `.qsdb` schema migration, dirty flags, deterministic regeneration, audit, revision and template persistence.
- Multi-document project cache bound to live Document/DWG identity with fingerprint and Save As guards.
- Floor/Zone/Family assignment and object-based Bulk Edit require the actual `ProjectElement` instance owned by the project; same-ID foreign objects fail closed.
- Project Tools, Floor, Zone, Family and Material editors are drawing-bound; project mutation/activation/selection-assignment/export actions require the window's bound DWG to still be the active BricsCAD document.
- Stored custom Material Catalog metadata fails closed when a legacy/tampered record attempts to shadow a built-in material ID or name.
- `.qsdb` intentionally continues to load dangling semantic Family/Floor/Zone/Active/DependsOn references for repair; Model Health reports these references instead of making older recoverable projects unreadable.
- Semantic dependency self-references and true dependency cycles are detected before regeneration; `QS3DHEALTHALL` reports them and `QS3DRELEASECHECK` treats them as release blockers.
- Semantic capture is transactional at project-state level: single recognition/manual capture and multi-selection capture snapshot the complete `ProjectState` before mutation, reject QS3D-generated output handles as semantic input, and restore Families/Elements/Rules/Audit/Metadata/timestamps when conversion or regeneration fails. If rollback itself fails, the original and rollback errors are preserved together instead of hiding the first failure.
- HT_Phòng generation and synchronization use the same full-project snapshot/rollback pattern so a failed finish regenerator cannot leave a partially-created five-family finish batch in memory.

### BLT-style UI / semantic selection

- Three-pane Workspace, typed Family/Instance property scopes, override reset, Bóc chọn, selected-object review and semantic selection synchronization.
- Active Family is explicit and category-safe; GlassWall/WallPier capture respects the selected active Family when categories match.
- `QS3DFAMILIES` is exposed as the canonical **Family / Type** entry point from the Direct Draw authoring UI; Direct Draw consumes the active compatible Family instead of creating a second family store/editor.
- Locate/Zoom, Highlight, Focus, Isolate/Restore, Section Box, Section Plane and clip-display review flows.
- Full Domain Hub, Project Tools, Schedule Hub, Rebar 3D Hub, Curtain Hub and Geometry Extensions provide discoverable workflow entry points.
- Semantic handle ownership resolution includes current generated channels, including Slab mesh, StructuralWall mesh, Foundation mesh and Curtain frame overlays, and consumes shared generated-owner enumeration instead of a hard-coded family list so future `Generated*Handle(s)` slots resolve automatically.
- Current UI/source hardening keeps post-commit focus/selection/palette synchronization best-effort where a UI failure must not undo otherwise-valid CAD/project state; exact focus/HiDPI behavior remains a V25 runtime gate.

### Direct Draw / new-geometry authoring

#### P0

- BLT-style Direct Draw is source-implemented for `QS3DDRAWWALL`, `QS3DDRAWBEAM`, `QS3DDRAWCOLUMN` and `QS3DDRAWSLAB` and is exposed through the **TẠO MỚI** Ribbon workflow plus Full Domain Hub.
- New geometry starts from native BricsCAD point acquisition, creates a real LINE/closed-or-open POLYLINE source with a stable DWG Handle, then converges through `SemanticCaptureService` and the existing wall/structural builders rather than a second geometry/model stack.
- Wall prompts/inherits thickness, height and bottom offset; Beam prompts/inherits width, height and bottom offset; Column prompts/inherits width, depth, height and bottom offset; Slab prompts/inherits thickness and bottom offset.
- P0 runs in Model Space only and fails closed in PaperSpace/Layout before persistent source creation.
- Wall/Beam/Slab picked paths use a unit-aware **5 mm** vertical planarity tolerance; raw drawing-unit epsilon is not used as the product tolerance.
- Direct Draw regenerates semantic state before native builder mutation so dependency/rule failures are surfaced before Solid3d commit whenever possible.
- The outer authoring rollback snapshots project state and pre-existing generated ownership, discovers QS3D XData-tagged output for the just-created project/element/category, restores/cleans according to the guarded operation contract, and verifies requested cleanup handles are no longer live.
- Successful Direct Draw selects the generated host when available. `QS3DBUILD3D`/Workspace rebuild paths resolve semantic/generated selections back to complete live source geometry where supported and reject mixed-category/mixed wall-source atomicity hazards before native commit.
- Planar current-UCS authoring is source-implemented: translated/in-plane-rotated UCS is allowed, prompt points remain in the user's working UCS, and persisted LINE/POLYLINE source geometry is transformed by `Editor.CurrentUserCoordinateSystem` before Model Space append. Tilted/3D UCS is rejected before source creation and QS3D does not reset the user's UCS.

#### Guarded P1 native subset

- `QS3DDRAWGLASSWALL` extends the same source → semantic → native flow to a GlassWall backing host. It accepts the currently guarded LINE/open-POLYLINE source family, prompts/inherits thickness/height/source-relative bottom offset and delegates native generation to canonical `QS3DBUILD3D`; Curtain frames remain a dedicated Curtain workflow.
- `QS3DDRAWWALLPIER` is deliberately **two-point LINE-only** in Direct Draw. This keeps native dispatch on `WallPierProfileSolidBuilder` and preserves current Rectangular/Chamfered profile semantics instead of silently falling back to a generic multi-segment wall prism. Multi-segment WallPier Direct Draw remains a separate future profile-around-corners problem.
- `QS3DDRAWSTRUCTWALL` uses a two-point LINE source with thickness/height/source-relative bottom offset and canonical structural native generation.
- `QS3DDRAWFOUNDATION` uses a closed POLYLINE source with thickness/source-relative bottom offset and canonical structural native generation.
- P1 writes user-confirmed geometry values through `ProjectElement.SetProperty()`, re-checks active-DWG affinity immediately before nested `QS3DBUILD3D`, requires a live `GeneratedSolidHandle` after the build, and keeps post-commit UI synchronization non-destructive.
- P1 rollback is ownership/XData-scoped: operation-created source/generated CAD is erased and verified while ownership information is still available, then the project snapshot is restored. Foreign/ambiguous generated geometry must not be erased from a textual handle collision.
- Persisted P1 paths finite-check drawing coordinates/elevation before entering the DWG database.
- P1 now shares the P0 planar-UCS contract: GlassWall/WallPier/StructuralWall LINE source and applicable GlassWall/Foundation POLYLINE source are transformed from current planar UCS into database/WCS coordinates before append; tilted/3D UCS fails closed before source creation.

#### Door / Opening Direct Draw

- `QS3DDRAWDOOR` and `QS3DDRAWOPENING` are source-implemented as host-aware authoring commands rather than being folded into the native-solid P1 wrapper.
- The user picks two plan-view edge points; QS3D creates one real Model-Space LINE and uses its unit-converted plan length as authoritative `WidthM`.
- The commands prompt/inherit positive `HeightM`, non-negative sill/bottom offset and non-negative `BooleanClearanceM`. Explicit malformed/non-finite/invalid Family numeric configuration fails closed rather than being silently masked by fallback data.
- Instance writes use canonical `ProjectElement.SetProperty()`; deterministic semantic regeneration runs before host linking.
- Only the newly created source is selected before the established `QS3DAUTOLINKHOSTS` path. Active-DWG affinity is rechecked around the nested command, and a valid authoring commit requires a non-empty `HostWallId` plus a second deterministic post-link regeneration.
- Unmatched or ambiguous Auto Host placement rolls the new operation back instead of leaving an orphan Door/WallOpening. The exact source `ObjectId` is cleaned before project snapshot restore, and post-commit UI synchronization remains best-effort/non-destructive.
- Door/Opening uses the same planar-UCS source contract as P0/P1: `WidthM` is derived from UCS-local prompt geometry, the persistent source LINE is transformed through the current UCS before database append, and downstream Auto Host sees database/WCS geometry. Tilted/3D UCS is rejected before source creation.
- Door/Opening Direct Draw intentionally stops at **source + semantic + verified Auto Host**; physical host mutation remains explicit.
- `QS3DCUTSELECTEDOPENINGS` is source-implemented as the explicit-target physical-cut path. It resolves the current CAD/semantic selection to a deduplicated Door/WallOpening ID set, prevalidates every selected target and generated host before native mutation, and calls the targeted `OpeningBooleanService` overload so unrelated linked openings are not included by this command. Same-fingerprint reruns are idempotent; a different cut state on the same already-cut generated host fails closed until the host is rebuilt.
- Legacy `QS3DCUTOPENINGS` remains the broader all-linked physical-cut operation. Neither selected nor all-linked physical cut is silently auto-invoked by Direct Draw.

Existing `QS3DWALL`, `QS3DGLASSWALL`, `QS3DWALLPIER`, `QS3DBEAM`, `QS3DCOLUMN`, `QS3DSLAB`, `QS3DSTRUCTWALL`, `QS3DFOUNDATION`, `QS3DDOOR`, `QS3DOPENING` and `QS3DBUILD3D` capture/rebuild workflows remain supported for pre-existing drawings.

Detailed source/runtime boundaries are in `docs/DIRECT-DRAW-P0-IMPLEMENTATION.md`, `docs/DIRECT-DRAW-P1-IMPLEMENTATION.md`, `docs/DIRECT-DRAW-OPENINGS.md` and `docs/DIRECT-DRAW-UCS.md`.

### Room / finishes / schedules

- Deterministic `RoomBoundaryEngine` and bounded LINE/POLYLINE/ARC/SPLINE Room Auto sampling/topology.
- Non-destructive stale-room lifecycle with provenance and audit retention.
- HT_Phòng generation/synchronization for floor finish, waterproofing, skirting, wall finish and ceiling finish, with batch rollback on failure.
- Room Finish schedule and XLSX workflow.

### Tường KT / wall geometry / Door-Opening

- ArchitecturalWall, GlassWall and WallPier capture with category-specific starter defaults.
- LINE and open-POLYLINE Tường KT centerlines; bulges use deterministic tessellation/footprint planning.
- WallPier LINE specialized rectangular/chamfered profile builder; legacy captured open POLYLINE remains on the separate guarded generic footprint path. Direct Draw WallPier is LINE-only as described above.
- L/T/X/Straight/End/Multi junction analysis.
- Fingerprinted `QS3DWALLSNAPPREVIEW` → `QS3DWALLSNAPAPPLY` for supported source-centerline endpoint cleanup.
- Manual and automatic Door/Opening host linking with compatibility, surface-gap, Floor/Zone, ambiguity and elevation guards. Current Auto Host apply/regeneration is project-atomic while ambiguity/unmatched review remains non-mutating.
- Straight physical opening cuts and dedicated curved/bulged open-POLYLINE cut planning; curved cutting fingerprints before mutation and identical reruns are idempotent.
- Selection-scoped `QS3DCUTSELECTEDOPENINGS` keeps explicit physical mutation limited to requested Door/WallOpening semantics and rejects stale/non-owned/incompatible generated hosts before BoolSubtract.
- Door/Opening schedule + XLSX with host provenance.

Physical multi-owner wall-solid union/reconciliation is **not** implemented by guessing. Current safe workflow changes source centerlines and invalidates/rebuilds owned generated geometry.

### Curtain Wall / Vách Kính

- Deterministic panel grid, schedule and XLSX.
- `QS3DCURTAIN3D` keeps one backing GlassWall host solid and generates separate perimeter/mullion/transom overlays for supported source paths.
- LINE Curtain frames remain opening-aware: linked Door/Opening rectangles interrupt frame runs deterministically.
- **Guarded open/bulged WCS-XY POLYLINE path-frame support is source-implemented**: bounded tessellation/station mapping creates ownership-protected native frame fragments and supports linked-opening interruption according to the current path planner.
- Frame state carries dedicated handles/count/grid/opening/path metadata, configuration fingerprint, generated ownership and live-geometry validation.
- Current frame-builder hardening keeps native LINE/path builders free of UI side effects; semantic/precommit validation happens before native rebuild and post-commit UI synchronization is non-fatal.
- Opening property changes and link/re-host/unlink changes stale dependent frame overlays without unnecessarily stale-marking the backing host.
- Curtain destructive ownership and dedicated ownership health are policy-driven through the shared generated-handle ownership definition rather than a manual generated-slot list.

Remaining Curtain product/runtime work includes panel-by-panel backing glass solids, broader unsupported/freeform path parity and exact V25 runtime qualification of current LINE/open/bulged frame paths. Whole-command host+frame rollback must remain conservative until proven under the final runtime contract.

### Structure / recognition / quantities

- Semantic quantities and guarded native paths for Beam, Slab, Column, StructuralWall, Foundation, Stair, Railing and Earthwork.
- Quick Takeoff uses drawing unit conversion.
- BQ grouping/filtering/Locate/XLSX and ED2 Excel/Handle round-trip with DWG fingerprint safety.
- Deterministic recognition/review/auto-apply.
- `QS3DB4D` bounded Current Space scan excludes **all generated owner-slot handles through the shared ownership policy**, preventing QS3D-generated geometry from feeding back into source recognition when new generated families are introduced. Recognition/B4D apply still flows through guarded transactional semantic capture, so an output handle cannot be recaptured if a future scanner regression reaches that stage.

### Schedules / exports

- Document-bound `QS3DSCHEDULES` Schedule Hub.
- BQ, Room Finish, Material, Curtain, Door/Opening and rebar schedule/export entry points.
- Material usage/catalog + XLSX with lazy primary/fallback quantity evaluation; an invalid unused fallback no longer breaks a valid primary quantity export.
- Door/Opening schedule + XLSX.
- Curtain XLSX.
- BBS review/XLSX/UTF-8 CSV.

### Rebar 3D

Implemented generated families:

1. Column longitudinal — `QS3DREBAR3D`.
2. Beam longitudinal — `QS3DBEAMREBAR3D`.
3. BBS-shape-driven — `QS3DREBAR3DSHAPE`.
4. Beam stirrup — `QS3DREBARSTIRRUP3D`.
5. Column tie — `QS3DREBARTIES3D`.
6. Slab X/Y mesh — `QS3DSLABREBAR3D`.
7. StructuralWall horizontal/vertical mesh — `QS3DWALLREBAR3D`.
8. Foundation X/Y mesh — `QS3DFOUNDATIONREBAR3D`.

- Slab/Foundation X/Y and StructuralWall direction-specific notation support independent diameters/distribution.
- `QS3DREBARMESHSETUP` edits semantic mesh setup and stale-marks existing generated output.
- Foundation has dedicated handles, stale state, invalidation, health and mode semantics.
- Foundation dedicated ownership health is order-independent and uses the shared owner-slot policy, including future generated families.
- Beam Stirrup has explicit-data bend/hook planning, endpoint-safe V25 path joining, exact generated metadata/health invariants and dedicated smoke coverage; absent detailing data is not invented.
- `QS3DREBARHEALTHALL` includes current generated rebar families including Foundation mesh and cross-family ownership diagnostics.
- `QS3DHEALTHALL` and `QS3DRELEASECHECK` add semantic/source/generated/live/stale/mode/BOM/dependency checks.
- Rebar, Tie and Curtain destructive ownership guards protect foreign/future generated owner slots through the shared policy and refuse ambiguous/unowned destructive erase.

Fabrication-grade hook/bend-radius/anchorage/code-specific detailing is not inferred without explicit configured dimensions/rules.

### Generated ownership / stale / health hardening

- One canonical `GeneratedHandleOwnershipHealthService` facade is compiled directly; obsolete duplicate shim/type and compile exclusion were removed.
- Shared `GeneratedHandleOwnershipPolicy` is the single classification contract for `PhysicalOpeningCutSolidHandle` and `Generated*Handle` / `Generated*Handles`, while provenance/reference metadata such as `HostHandle` remains non-owner state.
- The Core policy exposes `RebarHandleKeys` / `IsRebarOwnerSlot` for destructive rebar families plus normalized `EnumerateOwnerHandles`, project-wide `CollectOwnerHandles` and fail-closed `TryFindOwner` for cross-feature consumers. A handle claimed by different owner slots is ambiguous even when the semantic element ID is the same.
- The BricsCAD adapter policy is a delegation facade only; it no longer duplicates generated-slot classification semantics.
- Semantic selection, B4D exclusion, safe ownership health, BOM release liveness, semantic capture and Release Readiness consume the shared owner contract.
- Rebar, Tie and Curtain destructive guards use policy-driven ownership instead of stale manual foreign-family lists.
- Curtain and Foundation dedicated ownership health use shared policy-driven generated-slot discovery.
- Generated dependent invalidation clears dedicated metadata for mass, opening cuts, rebar families and opening-aware Curtain frames.
- Synthetic sample DWG/DXF fixtures are allowed only at the explicitly reviewed `samples/generated` paths; all other committed DWG/DXF and DOCX/private-reference artifacts remain blocked by preflight.

### Release readiness / packaging / secure update

- `QS3DRELEASECHECK` includes Model Health, Dependency Health, safe generated ownership, longitudinal/shape/tie/stirrup/slab/wall/Foundation mesh health, Curtain health/live state, stale state, generated-rebar mode semantics and BOM/live-solid release guards.
- Release generated-handle collection and Locate use the shared owner enumeration instead of a separate property parser, so future generated owner families participate in live-solid validation without another release-code list update.
- `scripts/package-v25.ps1` packages the x64 Release/net48 **plugin adapter** output, requires QS3D adapter/Core DLLs, excludes BricsCAD-owned assemblies, includes installer/updater/sample assets, generates hashes and creates `COMMANDS.txt` directly from `[CommandMethod]` source declarations. It does not expect a standalone `QS3D.exe`.
- Secure update source is HTTPS-only, package-host bounded, archive traversal/size/entry guarded, SHA-256 verified and Authenticode signer-pinned for executable payloads.
- Update manifest/package metadata version is bound to the Authenticode-verified `QS3D.BricsCAD.V25.dll` assembly version before install, closing the signed-payload replay/relabel gap.
- V25 installer snapshots targeted DemandLoad registration and rolls back registry plus payload atomically if an install/upgrade step fails; fresh-install failures remove the newly committed payload.
- Installer/updater do not weaken BricsCAD `SECURELOAD`.
- Manual V25 release workflow can run source gates, Core build/smoke, V25 x64 compile, optional real runtime validation, packaging/checksum and GitHub Release publication after explicit owner approval.

## Static/preflight coverage

Current source preflights cover, among other things:

- manual-only GitHub Actions policy and per-job manual guards;
- product-boundary documentation/source markers that keep the shipping target explicitly a BricsCAD plugin;
- command uniqueness and key XAML contracts;
- Direct Draw P0 command uniqueness, BLT-style prompts, Model-Space/unit-aware planarity guards, semantic-regeneration-before-native-build ordering, generated-XData orphan discovery, verified rollback cleanup, Ribbon/Hub exposure and semantic/generated rebuild-source resolution;
- Direct Draw native P1 command uniqueness, source-relative parameter writes through `SetProperty()`, active-DWG revalidation, live-generated verification, ownership/XData-aware cleanup-before-restore ordering, finite persisted paths, non-destructive UI synchronization and WallPier two-point LINE-only profile authoring;
- Door/Opening Direct Draw command uniqueness, Model-Space/unit-aware source creation, fail-closed Family numerics, `SetProperty()` lifecycle, selection-scoped Auto Host, post-link semantic verification, exact source cleanup before project restore, Ribbon/Hub exposure and prohibition on implicit physical cutting;
- planar-UCS transform-before-append and tilted-UCS fail-closed behavior for P0, P1 and Door/Opening Direct Draw, with no user-UCS mutation;
- targeted Door/Opening physical-cut selection, requested-ID validation, all-target readiness/stale-host prevalidation, ownership checks, transaction-scoped BoolSubtract and explicit-vs-auto-cut boundary;
- Family/Type authoring discoverability and Direct Draw integration synchronization;
- project-editor active-DWG affinity and project-owned mutation integrity;
- semantic selection including dynamic future generated owner slots;
- B4D future-proof generated-source exclusion through the Core owner policy;
- transactional semantic capture, generated-input rejection and HT_Phòng batch rollback;
- canonical generated owner-slot compilation/enumeration and future-family BOM liveness;
- persisted Material Catalog built-in-shadow rejection;
- dependency self/cycle health and release blocking;
- Beam Stirrup explicit bend/hook metadata plus repo-wide smoke registration protection;
- wall junction/snap, project-atomic Auto Host and opening cuts;
- Curtain opening-aware lifecycle, guarded LINE/open/bulged path-frame source contracts, precommit validation, UI-free native frame builders and policy-driven ownership;
- Slab/Wall/Foundation mesh lifecycle/health/ownership;
- unified Release Readiness including Dependency/Foundation/mode/BOM contracts;
- signed updater version binding and transactional installer rollback;
- schedule/export hub wiring;
- synthetic sample provenance/private-file policy.

`scripts/preflight-all.py` auto-discovers feature preflights, including product-boundary, Direct Draw P0/P1, Door/Opening Direct Draw, P0/extended UCS, targeted opening cut/readiness, Auto Host and Curtain path-frame gates.

## Manual GitHub Actions policy

Current workflow inventory is owner-triggered/manual-only:

- `ci.yml`;
- `bricscad-v25.yml`;
- `curved-opening.yml`;
- `geometry-extensions.yml`;
- `project-data-gate.yml`;
- `schedule-gate.yml`;
- `release-v25.yml`.

Every workflow must remain `workflow_dispatch` only and every executable job must guard `github.event_name == 'workflow_dispatch'`. Release additionally requires `confirm_release=RELEASE`.

No GitHub Action was dispatched as part of the current continue-all review/source/docs batch.

## Validation history — do not confuse with current head

An **earlier** integrated snapshot based on `b00d03f` was compiled against BricsCAD V25.2.10 managed assemblies in Release/x64 and the then-current Core/preflight suite passed. That proof predates many current Curtain/rebar/Foundation/Schedule/ownership/project-editor/updater/Beam-Stirrup/Direct-Draw changes.

Earlier GitHub-hosted runs also predate the newest batches. They are historical evidence only and **must not** be described as validation of the current `main` head.

The current final SHA still requires an explicitly approved build/runtime validation before it can be called current V25-verified. During the current source hardening work, the execution container could not resolve `github.com`, so this batch does **not** claim a local `dotnet build`, Core smoke execution or aggregate Python preflight pass.

## Runtime/product work still remaining

- compile the exact final SHA against the exact target BricsCAD V25 managed assemblies;
- NETLOAD/DemandLoad command/Ribbon/palette regression in a licensed interactive session, including Family / Type activation and current Direct Draw/selected-cut buttons;
- Direct Draw P0 runtime qualification for Wall/Beam/Column/Slab, including prompted parameters, Model/Paper Space behavior, millimeter/meter units, 5 mm planarity boundary, World/translated/rotated planar UCS, source/semantic/generated ownership, rebuild, ESC/failure cleanup and a private copy of owner-provided `MB MONG.dwg` without committing it;
- Direct Draw native P1 runtime qualification for GlassWall backing host, WallPier two-point LINE Rectangular/Chamfered behavior plus explicit multi-segment rejection, StructuralWall and Foundation, including World/translated/rotated planar UCS, forced native-build failure/rollback and post-commit UI-failure isolation;
- Door/Opening Direct Draw runtime qualification for mm/m width, Family values, World/translated/rotated planar UCS, valid/no-host/ambiguous-host matching, Floor/Zone/elevation/gap gates, sill/bottom-offset/boolean-clearance persistence, save/reopen, schedule/export and explicit `QS3DCUTSELECTEDOPENINGS` after authoring;
- selected-opening physical-cut regression with one/multiple targets, multiple hosts, mixed unrelated CAD selection, stale generated hosts, same-fingerprint rerun and different-fingerprint fail-closed behavior, plus legacy all-linked `QS3DCUTOPENINGS` compatibility;
- representative private-DWG save/reopen/multi-DWG regression;
- Room Auto mixed-curve regression;
- wall snap, Auto Host and straight/curved opening-cut regression;
- Curtain host + opening-aware frame regression for current LINE and guarded open/bulged WCS-XY path-frame source paths; panel-by-panel backing glass solids and unsupported broader/freeform path parity remain product work;
- legacy captured WallPier open-POLYLINE specialized profile product work and any future deterministic multi-segment WallPier Direct Draw profile-around-corners contract;
- physical multi-owner wall-solid L/T/X/Multi reconciliation product work;
- transient thickness/profile DrawJig preview and repeated authoring mode only after exact V25 managed-API compile/runtime proof;
- all generated rebar families including Foundation mesh and explicit-data Beam Stirrup paths on real drawings;
- Schedule Hub/export/traceability and `QS3DRELEASECHECK` on representative data;
- Unicode/HiDPI and large-model performance regression;
- production code-signing certificate/key custody, timestamp/publication infrastructure and a real signed package/install/update/rollback exercise;
- optional commercial licensing/backend work if that product requirement is pursued.

A standalone QS3D CAD executable is **not** a remaining gap; it is outside the current product boundary.

See `docs/PRODUCT-BOUNDARY.md`, `docs/DIRECT-DRAW-WORKFLOW.md`, `docs/DIRECT-DRAW-P0-IMPLEMENTATION.md`, `docs/DIRECT-DRAW-P1-IMPLEMENTATION.md`, `docs/DIRECT-DRAW-OPENINGS.md`, `docs/DIRECT-DRAW-UCS.md`, `docs/CURTAIN-PATH-FRAMES.md`, `docs/CONTINUE-ALL-DEEP-AUDIT-2026-08-10.md`, `docs/DEEP-AUDIT-2026-08-10.md`, `docs/FULL-REPO-AUDIT-2026-08-10.md` and `docs/REVIEW-2026-08-10-CONTINUE-ALL-AUDIT.md` for the product boundary, Direct Draw/Curtain source status, deep passes and broader runtime/product boundary.