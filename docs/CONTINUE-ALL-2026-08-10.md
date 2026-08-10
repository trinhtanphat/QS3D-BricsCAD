# Continue-all hardening — 2026-08-10

This note records source-level work completed after the earlier implementation-status snapshot. It intentionally separates repository implementation from licensed BricsCAD V25/private-DWG runtime proof.

## Room / HT_Phòng lifecycle integrity

- Room finish identity is centralized through `RoomFinishIdentityService`; duplicate canonical/legacy finish ownership fails closed across BQ, Material Usage and HT_Phòng Schedule instead of being counted twice.
- Room provenance is centralized through `AutoRoomLifecycle.ResolveRoomReferenceId` and supports canonical `RoomSourceId`, legacy aliases and Room dependencies while rejecting conflicting Room ids.
- `RoomFinishSynchronizationService` is the shared Room -> finish update contract used by `GenerateRoomFinishes` and `SyncExistingRoomFinishes`; the BricsCAD adapter no longer owns a second metric-copy implementation.
- Existing legacy finish synchronization repairs `RoomSourceId`, keeps exactly one Room dependency, preserves unrelated dependencies and synchronizes Floor/Zone/DWG fingerprint.
- `AreaM2`, `PerimeterM`, `HeightM`, `OpeningAreaM2` and `DoorWidthM` are copied from valid Room semantic data using invariant finite/non-negative parsing. If a Room metric disappears, the old finish metric is removed rather than retained, preventing stale opening/skirting deductions after topology or explicit semantic edits.
- Both single-finish and batch-finish synchronization are project-state transactional. A parse/provenance/identity failure restores earlier mutations rather than leaving a half-synchronized project.
- Stale AutoRoom cannot be used as a refresh source. A split/merge that creates a new Room does not guess that old finishes should move to the new Room; stale/orphan state remains visible for explicit repair/regeneration.
- `RoomFinishHealthService`, `QS3DROOMFINISHHEALTH`, `QS3DHEALTHALL` and `BomReleaseGuardService` surface unlinked/orphan/wrong-parent/cross-scope/stale/conflicting/duplicate Room finish states.
- Property-only legacy finishes can trace back through Room provenance to Room boundary/source handles for Locate/BOM traceability.

Regression/static coverage includes `RoomFinishSynchronizationSmoke`, single-sync atomic rollback, batch rollback after an earlier finish mutation, and duplicate Room-dependency repair.

## Curtain wall / openings

- GlassWall LINE `QS3DBUILD3D` uses the same native curtain-frame overlay path as dedicated Curtain commands.
- Curtain frames are opening-aware: linked Door/WallOpening geometry is projected onto the GlassWall host with the same safe dimensional contracts used by opening workflows.
- Mullion/transom rectangles are deterministically interrupted around opening cutter rectangles rather than running through doors/openings.
- Guarded open/bulged WCS-XY POLYLINE Curtain path frames are source-implemented through bounded tessellation/station mapping with generated ownership/live-state checks; panel-by-panel backing glass solids are still separate product work.
- Frame state carries live CAD/configuration fingerprints so host/opening geometry drift can be surfaced by health.
- The modeless Curtain Wall editor is bound to the `Document` that opened it; save/recalculate/command dispatch fail closed after an MDI switch.

### Targeted Door/Opening physical cut

- `QS3DCUTSELECTEDOPENINGS` is source-implemented and resolves current CAD/semantic selection to only the requested Door/WallOpening semantics.
- The targeted `OpeningBooleanService` overload validates requested IDs and linked hosts before building the mutation set.
- `OpeningBooleanCutGuard` prevalidates selected targets/generated hosts, rejects stale generated solids and enforces generated ownership/source-shape readiness before BoolSubtract.
- Native subtraction remains transaction-scoped. Existing `PhysicalOpeningCutFingerprint` idempotency is retained: same state can no-op; a changed target/configuration on the same already-cut generated solid fails closed until host rebuild.
- Legacy `QS3DCUTOPENINGS` remains the broader all-linked physical-cut operation.
- Direct Draw Door/Opening still does **not** silently invoke either physical cut path; the destructive step remains explicit pending licensed V25 transaction/UX proof.

## Build3D / recognition safety

- `QS3DBUILD3D` evaluates every selected tracked semantic source before choosing a category.
- Mixed semantic categories fail closed rather than building only the first category found.
- A selection that mixes already captured sources with uncaptured CAD also fails closed rather than silently processing only part of the selection.
- Specialized WallPier dispatch and native capability checks remain category-specific instead of being flattened into a generic wall path.
- Auto-recognition no longer swallows failed captures. Each failed auto-apply is counted, written to the editor and recorded as a `recognition.skip` audit event.
- B4D generated-output exclusion remains driven by the shared generated-owner policy rather than a hard-coded legacy output list.

## Direct Draw audit

- P0: `QS3DDRAWWALL`, `QS3DDRAWBEAM`, `QS3DDRAWCOLUMN`, `QS3DDRAWSLAB`.
- P1: `QS3DDRAWGLASSWALL`, `QS3DDRAWWALLPIER`, `QS3DDRAWSTRUCTWALL`, `QS3DDRAWFOUNDATION`.
- Host-aware authoring: `QS3DDRAWDOOR`, `QS3DDRAWOPENING`.
- Family / Type is exposed through canonical `QS3DFAMILIES` in the TẠO MỚI Ribbon/Domain Hub; Direct Draw does not own a second family editor/store.
- Source → semantic → regenerate/build/host-link operations use real DWG source Handles and fail-closed rollback contracts rather than a parallel CAD model.
- P0/P1 native flows guard ownership of generated CAD before destructive cleanup; Door/Opening cleans its exact source object and restores project state if verified Auto Host cannot complete.
- Direct Draw is Model-Space-only and current plan paths use a unit-aware 5 mm vertical tolerance.
- Family/instance dimensions are finite/positive/non-negative as appropriate; malformed configured Family numerics fail closed.

### Planar current-UCS support

- The full current Direct Draw set now implements the same planar-UCS source contract, not only P0.
- Point acquisition remains in current-UCS working coordinates.
- P0/P1/Door/Opening persisted LINE/POLYLINE source geometry is transformed by `Editor.CurrentUserCoordinateSystem` before `AppendEntity`, so translated/in-plane-rotated UCS does not silently create WCS-aligned source geometry.
- Door/Opening `WidthM` is calculated in UCS-local prompt coordinates before the source LINE is transformed for database/Auto Host use.
- The guard reads `CurrentUserCoordinateSystem.CoordinateSystem3d.Zaxis` and rejects tilted/3D UCS before source creation. QS3D does not reset/mutate the user's UCS.
- `scripts/preflight-direct-draw-ucs.py` protects P0 and `scripts/preflight-direct-draw-ucs-extended.py` protects P1 + Door/Opening.
- Source implementation does **not** equal runtime qualification: World/translated/30°/45°/90° planar UCS still needs exact-current-SHA BricsCAD V25 interactive proof.

See `docs/DIRECT-DRAW-UCS.md` for the coordinate-system contract and runtime matrix.

## Generated-output stale semantics

- Generated host solid, longitudinal/shape/tie/stirrup rebar, slab mesh, wall mesh, foundation mesh and curtain frames use per-output stale snapshots.
- Health services do not treat any non-zero element dirty flag as proof that a particular generated output is stale.
- Generated-dependent invalidation covers host/opening-cut solids, all policy-listed rebar/mesh outputs and curtain frames, and refuses destructive erase when a claimed object is not a live owned `Solid3d`.

## Material / Level workflows

- Project material rename preserves references: Family and Instance `Material` / `CurtainFrameMaterial` names are propagated.
- Inherited Family consumers are dirtied/staled when a referenced material is renamed; true instance overrides remain unchanged.
- A custom material cannot be deleted while any Family or Instance still references it.
- Modeless Material Catalog and Level Picker windows are bound to the `Document` that opened them. Selection-mutating operations require that same DWG to be active, preventing cross-DWG edits after switching MDI tabs.
- HT_Phòng Material Usage follows the same domain quantity precedence as Room Finish Schedule rather than silently preferring stale legacy quantities.

## Modeless multi-DWG safety

- Zone, Family, Material, Floor/Level, Curtain Wall and Rebar Mesh editors are drawing-bound before project/CAD mutation.
- Rebar Mesh Setup re-resolves its semantic element by ID at save time, so a modeless window cannot mutate a detached `ProjectElement` after project reload/replacement.
- Quantity, BBS, Revision, Door/Opening and Room-Finish review windows keep locate/recalculate/export operations on the source DWG and fail closed when a different MDI document is active.
- Static preflight contracts guard drawing-affinity behavior.

## UI / HiDPI source hardening

- The shared dark theme uses system Segoe UI, layout rounding, device-pixel snapping and display text formatting; no proprietary BLT assets/fonts are copied.
- Keyboard focus is explicit for buttons/editors, and large Tree/List/DataGrid controls use virtualization where supported.
- Workspace keyboard/context shortcuts and premium-dark refinements preserve source-level focus/HiDPI guards.
- Right-panel Layer/Xref data remains live BricsCAD data instead of decorative fake state.
- `scripts/preflight-ui-hidpi.py` protects source-level focus/virtualization/live-layer contracts, but screenshot/DPI acceptance still requires real V25.

## Generated handle ownership

- Ownership health distinguishes ownership from provenance/evidence.
- Semantic `SourceHandles` are owners.
- Generated owner slots are `Generated*Handle` / `Generated*Handles` plus `PhysicalOpeningCutSolidHandle`.
- Provenance such as Auto Room `BoundarySourceHandles` is not an owner and may legitimately be shared.
- `GeneratedSolidHandle` and `PhysicalOpeningCutSolidHandle` can be logical aliases when one semantic element references the same post-cut host solid; cross-element/family ambiguity remains an error.
- `QS3DOWNERSHIPHEALTH` exposes provenance-safe review directly.

## Project context / persistence lifecycle

- Drawing identity remains fail-closed on `.qsdb` fingerprint mismatch and save remains protected by `ProjectFileLock` plus atomic QSDB persistence.
- `Database.FingerprintGuid` is normalized without assuming a specific TD_Mgd managed wrapper type; path fallback remains available when needed.
- Unsaved documents receive session-unique sidecar keys instead of sharing `Drawing1.qsdb`.
- Successful native DWG Save/SaveAs persists pending semantic state to the matching `.qsdb`; close-time pending state requires an explicit Save/Discard/Cancel choice and a failed canonical save vetoes close while attempting a detached LocalAppData recovery copy.
- Forget/document-close cleanup first detaches native save/close handlers, then removes persistence stamps, unsaved-document keys and in-memory context.
- QSDB deserialization rejects duplicate map keys.
- Dependency/source-handle traversals used by regeneration/review are iterative where deep graphs could overflow recursion.

## Release readiness

- `QS3DRELEASECHECK` aggregates semantic model health, provenance-safe handle ownership, current generated rebar/mesh/curtain health, generated-output stale state, live curtain CAD drift and BOM release guards.
- `READY` means no Error/Warning issues in those source/runtime metadata checks.
- It does **not** replace licensed BricsCAD V25/private-DWG runtime qualification.

## Validation policy

- Main GitHub Actions workflows remain manual-only (`workflow_dispatch`).
- No GitHub Actions workflow was dispatched as part of this continue-all batch.
- `scripts/preflight-all.py` discovers `preflight-*.py`, including Direct Draw P0/P1/opening, P0/extended UCS, targeted opening cut/readiness, Curtain, ownership, UI and release-readiness contracts.
- The current remote source review does **not** claim a fresh local aggregate-preflight/Core build/V25 compile. The execution container previously could not resolve `github.com`; current final-head compile/runtime proof remains pending.

## Remaining gates that must not be claimed from remote source review alone

- Exact compile against installed BricsCAD V25 `BrxMgd.dll` / `TD_Mgd.dll`.
- Real NETLOAD/DemandLoad, Ribbon/palette interaction and V25 command execution.
- P0/P1/Door/Opening World/translated/30°/45°/90° planar-UCS runtime qualification; tilted/3D UCS intentionally remains fail-closed.
- `QS3DCUTSELECTEDOPENINGS` runtime regression for selected-target readiness, multiple hosts, same/different fingerprint behavior and legacy all-linked compatibility.
- Private-DWG save/reopen, multi-DWG, opening/curtain, wall-junction, structure/BQ/BBS/rebar regression.
- Unicode/HiDPI and screenshot-based UI review on real BricsCAD V25.
- Transient thickness/profile DrawJig preview and repeated authoring mode until exact V25 managed-API/runtime behavior is proven.
- Production certificate possession/signing operation and production licensing/updater backend operations.
- More aggressive multi-owner wall-solid union/reconciliation at complex L/T/X/Multi junctions until a safe per-element ownership/unmerge/rebuild contract is defined and proven in V25.
