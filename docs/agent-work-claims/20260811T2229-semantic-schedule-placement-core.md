# Agent Work Claim — Semantic Schedule Placement Core

- Status: `ACTIVE`
- Owner: ChatGPT remote agent
- Started: 2026-08-11 22:29 +07:00
- Start commit observed: `b2b717a4f0a09f6b11d3b28ca05a46022899018d`
- Related roadmap/issue: Documentation layer / #77

## Purpose

Add a source-safe Core P0 contract for automatic placement of persisted semantic schedules on sheets without overloading `SemanticSheetPlacementDefinition.ViewId` or inventing native BricsCAD Layout/Table APIs.

## Allowed scope

- `src/QS3D.Core/Documentation/SemanticSchedulePlacementPlanner.cs` (new)
- focused Core smoke coverage for this planner
- a focused source/preflight regression gate for this planner
- `docs/DOCUMENTATION-LAYER.md` truth/status update limited to schedule placement
- this claim file

## Excluded scope

- `SemanticSheetPlanner` / `SemanticSheetAutoLayoutPlanner` schema or persistence changes unless a proven blocker requires them
- BricsCAD native Layout/PaperSpace/Viewport/Table/MLeader mutation
- quantity/reporting engine changes
- UI/ribbon/licensing/updater/IFC work
- local V25 qualification

## Contract

- Use persisted `SemanticScheduleDefinition.Id` as schedule identity.
- Keep schedule placement separate from view placement; never masquerade a schedule as a `ViewId`.
- Use paper millimetres and deterministic bounded packing compatible with existing semantic sheet conventions.
- Fail closed on missing/duplicate schedule ids, invalid/non-finite geometry, unusable regions, out-of-bounds items and overlap/reserved-region conflicts.
- Return immutable source-safe plans only; native adapters remain future/runtime-gated.

## Overlap note

Recent concurrent commits observed before claiming were updater, curtain frame, Family lifecycle, UI and quantity work; no active schedule-placement source lane was found via repository search. Re-read shared files before every write and merge rather than overwrite on conflict.
