# Continue-all hardening — 2026-08-10

This note records source-level work completed after the earlier implementation-status snapshot. It intentionally separates repository implementation from licensed BricsCAD V25/private-DWG runtime proof.

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

## Modeless multi-DWG safety

- Zone, Family, Material, Floor/Level, Curtain Wall and Rebar Mesh editors are drawing-bound before project/CAD mutation.
- Rebar Mesh Setup re-resolves its semantic element by ID at save time, so a modeless window cannot mutate a detached `ProjectElement` after project reload/replacement.
- Quantity, BBS, Revision, Door/Opening and Room-Finish review windows keep their locate/recalculate/export operations on the source DWG and fail closed when a different MDI document is active.
- Static preflight contracts guard the drawing-affinity behavior so future UI refactors cannot silently restore cross-DWG mutations.

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
- Source preflights were extended for curtain opening/live-state behavior, generated stale semantics, modeless document affinity, provenance-safe ownership, Build3D/recognition safety, project-context lifecycle and release-readiness.
- `scripts/preflight-all.py` discovers the `preflight-*.py` contracts, including the newly added review-workflow and project-context gates.

## Remaining gates that must not be claimed from remote source review alone

- Exact compile against the installed BricsCAD V25 `BrxMgd.dll` / `TD_Mgd.dll` set.
- Real NETLOAD/DemandLoad, Ribbon/palette interaction and V25 command execution.
- Private-DWG save/reopen, multi-DWG, opening/curtain, wall-junction, structure/BQ/BBS/rebar regression.
- Unicode/HiDPI and screenshot-based UI parity review on real BricsCAD V25.
- Production certificate possession/signing operation and production licensing/updater backend operations.
- More aggressive multi-owner wall-solid union/reconciliation at complex L/T/X/Multi junctions should stay gated until a safe per-element ownership/rebuild contract is defined and proven in V25.
