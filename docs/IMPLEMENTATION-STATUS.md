# Implementation status — 2026-08-10

This file distinguishes **implemented source paths** from behavior that still requires licensed BricsCAD V25/private-DWG runtime proof.

## Implemented in source

### Platform / persistence / project integrity

- BricsCAD V25 `net48/x64` adapter with external `BrxMgd.dll` / `TD_Mgd.dll` references and `Private=false`.
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
- Locate/Zoom, Highlight, Focus, Isolate/Restore, Section Box, Section Plane and clip-display review flows.
- Full Domain Hub, Project Tools, Schedule Hub, Rebar 3D Hub, Curtain Hub and Geometry Extensions provide discoverable workflow entry points.
- Semantic handle ownership resolution includes current generated channels, including Slab mesh, StructuralWall mesh, Foundation mesh and Curtain frame overlays, and consumes shared generated-owner enumeration instead of a hard-coded family list so future `Generated*Handle(s)` slots resolve automatically.

### Room / finishes / schedules

- Deterministic `RoomBoundaryEngine` and bounded LINE/POLYLINE/ARC/SPLINE Room Auto sampling/topology.
- Non-destructive stale-room lifecycle with provenance and audit retention.
- HT_Phòng generation/synchronization for floor finish, waterproofing, skirting, wall finish and ceiling finish, with batch rollback on failure.
- Room Finish schedule and XLSX workflow.

### Tường KT / wall geometry / Door-Opening

- ArchitecturalWall, GlassWall and WallPier capture with category-specific starter defaults.
- LINE and open-POLYLINE Tường KT centerlines; bulges use deterministic tessellation/footprint planning.
- WallPier LINE specialized rectangular/chamfered profile builder; open POLYLINE remains on guarded generic footprint path.
- L/T/X/Straight/End/Multi junction analysis.
- Fingerprinted `QS3DWALLSNAPPREVIEW` → `QS3DWALLSNAPAPPLY` for supported source-centerline endpoint cleanup.
- Manual and automatic Door/Opening host linking with compatibility, surface-gap, Floor/Zone, ambiguity and elevation guards.
- Straight physical opening cuts and dedicated curved/bulged open-POLYLINE cut planning; curved cutting fingerprints before mutation and identical reruns are idempotent.
- Door/Opening schedule + XLSX with host provenance.

Physical multi-owner wall-solid union/reconciliation is **not** implemented by guessing. Current safe workflow changes source centerlines and invalidates/rebuilds owned generated geometry.

### Curtain Wall / Vách Kính

- Deterministic panel grid, schedule and XLSX.
- `QS3DCURTAIN3D` keeps one backing GlassWall host solid and generates separate perimeter/mullion/transom overlays for supported LINE sources.
- Opening-aware LINE frame interruption around linked Door/Opening rectangles.
- Dedicated frame handles/count/grid/opening metadata, config fingerprint, live-state checks and stale lifecycle.
- Opening property changes and link/re-host/unlink changes stale dependent frame overlays without unnecessarily stale-marking the backing host.
- Curtain destructive ownership and dedicated ownership health are policy-driven through the shared generated-handle ownership definition rather than a manual generated-slot list.

Remaining Curtain product/runtime work: curved/open-POLYLINE frame overlay and panel-by-panel backing glass solids.

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
- `scripts/package-v25.ps1` packages the x64 Release/net48 adapter output, requires QS3D adapter/Core DLLs, excludes BricsCAD-owned assemblies, includes installer/updater/sample assets, generates hashes and creates `COMMANDS.txt` directly from `[CommandMethod]` source declarations.
- Secure update source is HTTPS-only, package-host bounded, archive traversal/size/entry guarded, SHA-256 verified and Authenticode signer-pinned for executable payloads.
- Update manifest/package metadata version is bound to the Authenticode-verified `QS3D.BricsCAD.V25.dll` assembly version before install, closing the signed-payload replay/relabel gap.
- V25 installer snapshots targeted DemandLoad registration and rolls back registry plus payload atomically if an install/upgrade step fails; fresh-install failures remove the newly committed payload.
- Installer/updater do not weaken BricsCAD `SECURELOAD`.
- Manual V25 release workflow can run source gates, Core build/smoke, V25 x64 compile, optional real runtime validation, packaging/checksum and GitHub Release publication after explicit owner approval.

## Static/preflight coverage

Current source preflights cover, among other things:

- manual-only GitHub Actions policy and per-job manual guards;
- command uniqueness and key XAML contracts;
- project-editor active-DWG affinity and project-owned mutation integrity;
- semantic selection including dynamic future generated owner slots;
- B4D future-proof generated-source exclusion through the Core owner policy;
- transactional semantic capture, generated-input rejection and HT_Phòng batch rollback;
- canonical generated owner-slot compilation/enumeration and future-family BOM liveness;
- persisted Material Catalog built-in-shadow rejection;
- dependency self/cycle health and release blocking;
- Beam Stirrup explicit bend/hook metadata plus repo-wide smoke registration protection;
- wall junction/snap/Auto Host/opening cuts;
- Curtain opening-aware lifecycle and policy-driven ownership;
- Slab/Wall/Foundation mesh lifecycle/health/ownership;
- unified Release Readiness including Dependency/Foundation/mode/BOM contracts;
- signed updater version binding and transactional installer rollback;
- schedule/export hub wiring;
- synthetic sample provenance/private-file policy.

`scripts/preflight-all.py` auto-discovers feature preflights.

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

An **earlier** integrated snapshot based on `b00d03f` was compiled against BricsCAD V25.2.10 managed assemblies in Release/x64 and the then-current Core/preflight suite passed. That proof predates many current Curtain/rebar/Foundation/Schedule/ownership/project-editor/updater/Beam-Stirrup changes.

Earlier GitHub-hosted runs also predate the newest batches. They are historical evidence only and **must not** be described as validation of the current `main` head.

The current final SHA still requires an explicitly approved build/runtime validation before it can be called current V25-verified. During the full-repository hardening audit, the execution container could not resolve `github.com`, so this batch does **not** claim a local `dotnet build`, Core smoke execution or aggregate Python preflight pass.

## Runtime/product work still remaining

- compile the exact final SHA against the exact target BricsCAD V25 managed assemblies;
- NETLOAD/DemandLoad command/Ribbon/palette regression in a licensed interactive session;
- representative private-DWG save/reopen/multi-DWG regression;
- Room Auto mixed-curve regression;
- wall snap, Auto Host and straight/curved opening-cut regression;
- Curtain host + opening-aware frame overlay regression and curved/open-POLYLINE frame product work;
- WallPier open-POLYLINE specialized profile product work;
- physical multi-owner wall-solid L/T/X/Multi reconciliation product work;
- all generated rebar families including Foundation mesh and explicit-data Beam Stirrup paths on real drawings;
- Schedule Hub/export/traceability and `QS3DRELEASECHECK` on representative data;
- Unicode/HiDPI and large-model performance regression;
- production code-signing certificate/key custody, timestamp/publication infrastructure and a real signed package/install/update/rollback exercise;
- optional commercial licensing/backend work if that product requirement is pursued.

See `docs/CONTINUE-ALL-DEEP-AUDIT-2026-08-10.md` for this deep pass, `docs/DEEP-AUDIT-2026-08-10.md` for the Beam/rebar review, `docs/FULL-REPO-AUDIT-2026-08-10.md` for shared ownership/capture hardening, and `docs/REVIEW-2026-08-10-CONTINUE-ALL-AUDIT.md` for the broader runtime/product boundary.
