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

Regression/static coverage now includes `RoomFinishSynchronizationSmoke`, single-sync atomic rollback, batch rollback after an earlier finish mutation, and duplicate Room-dependency repair.

## Curtain wall / openings

- GlassWall LINE `QS3DBUILD3D` uses the same native curtain-frame overlay path as the dedicated curtain commands.
- Curtain frames are opening-aware: linked Door/WallOpening geometry is projected onto the GlassWall LINE with the same safe host-distance and `OpeningCutPlanner` contract used by physical opening cuts.
- Mullion/transom rectangles are deterministically interrupted around opening cutter rectangles rather than running through doors/openings.
- Actual frame fragments, base grid count and linked-opening count are stored separately so health does not report false count mismatches after frame splitting.
- Linked opening semantic changes stale only the dependent curtain-frame output where appropriate.
- Curtain frames carry a live CAD fingerprint over host LINE endpoints plus linked-opening dimensions, clearance, live entity handles and extents. Direct CAD MOVE/ROTATE/geometry drift can therefore be detected even before a semantic regenerate.
- Dedicated curtain health and full health include the live CAD drift check.
- The modeless Curtain Wall editor is now bound to the `Document` that opened it. Save, recalculate and command dispatch fail closed after an MDI switch instead of mixing a Family from one project with another active DWG.

## Build3D / recognition safety

- `QS3DBUILD3D` now evaluates every selected tracked semantic source before choosing a category.
- Mixed semantic categories fail closed rather than building only the first category found.
- A selection that mixes already captured sources with uncaptured CAD also fails closed rather than silently processing only part of the selection.
- Auto-recognition no longer swallows failed captures. Each failed auto-apply is counted, written to the editor and recorded as a `recognition.skip` audit event.
- B4D generated-output exclusion remains driven by the shared generated-owner policy rather than a hard-coded legacy output list.

## Direct Draw P0 audit

- `QS3DDRAWWALL`, `QS3DDRAWBEAM`, `QS3DDRAWCOLUMN` and `QS3DDRAWSLAB` use an atomic source -> semantic capture -> semantic regenerate -> native 3D builder path.
- Failure cleanup discovers generated output through both semantic owner slots and owned CAD/XData, requires matching generated ownership before destructive erase, erases the newly-created source, restores the project snapshot and verifies no cleanup handles remain live.
- Direct Draw is Model-Space-only and source path input is limited to a 5 mm planarity tolerance before native builders run.
- Family/instance dimensions are validated as finite/positive where required; invalid existing Family values fail closed rather than being silently replaced.
- Planar current-UCS authoring is now handled explicitly in source. `LINE` and `POLYLINE` are authored from current-UCS prompt coordinates and transformed by `Editor.CurrentUserCoordinateSystem` before `AppendEntity`, so translated/rotated planar UCS does not silently create WCS-aligned source geometry.
- The UCS guard reads `CurrentUserCoordinateSystem.CoordinateSystem3d.Zaxis` and rejects tilted/3D UCS before source creation. QS3D does not reset or mutate the user's UCS.
- `scripts/preflight-direct-draw-ucs.py` guards the Model-Space/UCS ordering, transform-before-append behavior and no-UCS-mutation contract.
- Source implementation does **not** equal runtime qualification: World/translated/30°/45°/90° planar UCS creation still needs exact-current-sha BricsCAD V25 interactive proof. Tilted/3D UCS remains intentionally unsupported until native builders are generalized and tested.

See `docs/DIRECT-DRAW-UCS.md` for the coordinate-system contract and runtime matrix.

## Generated-output stale semantics

- Generated host solid, longitudinal/shape/tie/stirrup rebar, slab mesh, wall mesh, foundation mesh and curtain frames use per-output stale snapshots.
- Health services no longer treat any non-zero element dirty flag as proof that a particular generated output is stale.
- Regression coverage explicitly rejects dirty-only stale false positives.
- Generated-dependent invalidation covers host/opening-cut solids, all policy-listed rebar/mesh outputs and curtain frames, and refuses destructive erase when a claimed object is not a live owned `Solid3d`.

## Material / Level workflows

- Project material rename preserves references: Family and Instance `Material` / `CurtainFrameMaterial` names are propagated.
- Inherited Family consumers are dirtied/staled when a referenced material is renamed; true instance overrides remain unchanged.
- A custom material cannot be deleted while any Family or Instance still references it.
- Modeless Material Catalog and Level Picker windows are bound to the `Document` that opened them. Selection-mutating operations require that same DWG to be active, preventing cross-DWG edits after switching MDI tabs.
- HT_Phòng Material Usage follows the same domain quantity precedence as Room Finish Schedule (`NetFinishAreaM2`/`SideAreaM2`, `BottomAreaM2`, `TopAreaM2`, `SkirtingLengthM`) rather than silently preferring a stale legacy `AreaM2`.

## Modeless multi-DWG safety

- Zone, Family, Material, Floor/Level, Curtain Wall and Rebar Mesh editors are drawing-bound before project/CAD mutation.
- Rebar Mesh Setup re-resolves its semantic element by ID at save time, so a modeless window cannot mutate a detached `ProjectElement` after project reload/replacement.
- Quantity, BBS, Revision, Door/Opening and Room-Finish review windows keep their locate/recalculate/export operations on the source DWG and fail closed when a different MDI document is active.
- Static preflight contracts guard the drawing-affinity behavior so future UI refactors cannot silently restore cross-DWG mutations.

## UI / HiDPI source hardening

- The shared dark theme uses system Segoe UI, `UseLayoutRounding`, device-pixel snapping and display text formatting; no proprietary BLT assets/fonts are copied.
- Keyboard focus is explicit for buttons and editors, and large Tree/List/DataGrid controls use recycling/row/column virtualization where supported.
- The current premium-dark palette refinement preserved those focus/HiDPI/virtualization guards while increasing contrast and hierarchy.
- Right-panel Layer/Xref data remains live BricsCAD data; layer color/lock state is read from the DWG instead of displaying a decorative fixed-color swatch as if it were native state.
- `scripts/preflight-ui-hidpi.py` protects the source-level focus/virtualization/live-layer contract, but screenshot/DPI acceptance still requires a real V25 session.

## Generated handle ownership

- Ownership health distinguishes ownership from provenance/evidence.
- Semantic `SourceHandles` are owners.
- Generated owner slots are `Generated*Handle` / `Generated*Handles` plus `PhysicalOpeningCutSolidHandle`.
- Provenance such as Auto Room `BoundarySourceHandles` is not an owner and may legitimately be shared.
- `GeneratedSolidHandle` and `PhysicalOpeningCutSolidHandle` are logical aliases when the same semantic element references the same post-cut host solid. This valid alias does not report a false conflict.
- The same handle claimed by another element or another generated-output family remains an ownership error/ambiguity.
- `QS3DOWNERSHIPHEALTH` exposes the provenance-safe review directly.

## Project context / persistence lifecycle

- Drawing identity remains fail-closed on `.qsdb` fingerprint mismatch and save remains protected by `ProjectFileLock` plus atomic QSDB persistence.
- `Database.FingerprintGuid` is normalized through `Convert.ToString(...)` instead of assuming a particular TD_Mgd managed wrapper type; the path-based fallback remains available when the host does not expose a usable fingerprint.
- Unsaved documents no longer share a name-only LocalAppData sidecar such as `Drawing1.qsdb`. Each live unsaved `Document` receives a session-unique project sidecar key, avoiding stale project collisions across fresh untitled drawings.
- Forget/document-close cleanup removes the unsaved document key together with the in-memory project context.
- QSDB deserialization rejects duplicate map keys instead of silently overwriting earlier entries.
- Dependency/source-handle traversals used by regeneration/review are iterative where deep project graphs could otherwise overflow the process stack.

## Release readiness

- `QS3DRELEASECHECK` aggregates semantic model health, provenance-safe handle ownership, all generated rebar/mesh/curtain health, generated-output stale state, live curtain CAD drift and BOM release guards.
- `READY` means there are no Error/Warning issues in those source/runtime metadata checks.
- It does **not** replace the licensed BricsCAD V25/private-DWG runtime gate.

## Validation policy

- Main GitHub Actions workflows remain manual-only (`workflow_dispatch`).
- No GitHub Actions workflow was dispatched as part of this continue-all batch.
- Source preflights were extended for curtain opening/live-state behavior, generated stale semantics, modeless document affinity, provenance-safe ownership, Build3D/recognition safety, project-context lifecycle, Room/HT_Phòng integrity, schedule arithmetic, Direct Draw planar-UCS source behavior, UI HiDPI/focus and release-readiness.
- `scripts/preflight-all.py` discovers the `preflight-*.py` contracts.
- The current remote source review does **not** claim a fresh local aggregate-preflight/Core build/V25 compile. The execution container previously could not resolve `github.com`; current final-head compile/runtime proof remains pending.

## Remaining gates that must not be claimed from remote source review alone

- Exact compile against the installed BricsCAD V25 `BrxMgd.dll` / `TD_Mgd.dll` set.
- Real NETLOAD/DemandLoad, Ribbon/palette interaction and V25 command execution.
- Direct Draw World/translated/rotated planar-UCS runtime qualification; tilted/3D UCS remains unsupported until native builders are generalized and proven.
- Private-DWG save/reopen, multi-DWG, opening/curtain, wall-junction, structure/BQ/BBS/rebar regression.
- Unicode/HiDPI and screenshot-based UI parity review on real BricsCAD V25.
- Production certificate possession/signing operation and production licensing/updater backend operations.
- More aggressive multi-owner wall-solid union/reconciliation at complex L/T/X/Multi junctions should stay gated until a safe per-element ownership/rebuild contract is defined and proven in V25.
