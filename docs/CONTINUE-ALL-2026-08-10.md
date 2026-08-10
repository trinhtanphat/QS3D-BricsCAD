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

## Generated-output stale semantics

- Generated host solid, longitudinal/shape/tie/stirrup rebar, slab mesh, wall mesh and curtain frames use per-output stale snapshots.
- Health services no longer treat any non-zero element dirty flag as proof that a particular generated output is stale.
- Regression coverage explicitly rejects dirty-only stale false positives.

## Material / Level workflows

- Project material rename preserves references: Family and Instance `Material` / `CurtainFrameMaterial` names are propagated.
- Inherited Family consumers are dirtied/staled when a referenced material is renamed; true instance overrides remain unchanged.
- A custom material cannot be deleted while any Family or Instance still references it.
- Modeless Material Catalog and Level Picker windows are bound to the `Document` that opened them. Selection-mutating operations require that same DWG to be active, preventing cross-DWG edits after switching MDI tabs.

## Generated handle ownership

- Ownership health distinguishes ownership from provenance/evidence.
- Semantic `SourceHandles` are owners.
- Generated owner slots are `Generated*Handle` / `Generated*Handles` plus `PhysicalOpeningCutSolidHandle`.
- Provenance such as Auto Room `BoundarySourceHandles` is not an owner and may legitimately be shared.
- The early broad scanner source remains in history but is excluded from the Core compile; its public API is compiled through a compatibility shim that delegates to the provenance-safe scanner.
- `QS3DOWNERSHIPHEALTH` exposes the provenance-safe review directly.

## Release readiness

- `QS3DRELEASECHECK` aggregates semantic model health, provenance-safe handle ownership, all generated rebar/mesh/curtain health, generated-output stale state, live curtain CAD drift and BOM release guards.
- `READY` means there are no Error/Warning issues in those source/runtime metadata checks.
- It does **not** replace the licensed BricsCAD V25/private-DWG runtime gate.

## Validation policy

- Main GitHub Actions workflows remain manual-only (`workflow_dispatch`).
- No GitHub Actions workflow was dispatched as part of this continue-all batch.
- Source preflights were extended for curtain opening/live-state behavior, generated stale semantics, Material/Level document affinity, provenance-safe ownership and release-readiness.

## Remaining gates that must not be claimed from remote source review alone

- Exact compile against the installed BricsCAD V25 `BrxMgd.dll` / `TD_Mgd.dll` set.
- Real NETLOAD/DemandLoad, Ribbon/palette interaction and V25 command execution.
- Private-DWG save/reopen, multi-DWG, opening/curtain, wall-junction, structure/BQ/BBS/rebar regression.
- Unicode/HiDPI and screenshot-based UI parity review on real BricsCAD V25.
- Production certificate possession/signing operation and production licensing/updater backend operations.
- More aggressive multi-owner wall-solid union/reconciliation at complex L/T/X/Multi junctions should stay gated until a safe per-element ownership/rebuild contract is defined and proven in V25.
