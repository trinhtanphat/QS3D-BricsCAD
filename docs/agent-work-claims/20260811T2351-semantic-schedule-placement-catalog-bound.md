# Agent Work Claim — Semantic Schedule Placement Input Bounds

- Status: `ACTIVE`
- Owner: ChatGPT remote agent
- Started: 2026-08-11 23:51 +07:00
- Start commit observed: `870afd545fb303036d89427952a7bf608f408630`
- Related roadmap/issue: Documentation layer / #77

## Purpose

Harden the existing source-safe semantic schedule placement planner so both public enumerable inputs are cardinality-bounded at the existing 128-schedule contract instead of being fully materialized/enumerated before the guard can fire.

## Allowed scope

- `src/QS3D.Core/Documentation/SemanticSchedulePlacementPlanner.cs`
- focused `tests/QS3D.Core.SmokeTests/SemanticSchedulePlacementSmoke.cs` regression coverage
- `scripts/preflight-semantic-schedule-placement.py` only if the static contract must be tightened
- this claim file

## Excluded scope

- semantic schedule persistence/schema changes
- native BricsCAD Layout/PaperSpace/Table mutation
- quantity/reporting engines
- UI/ribbon/updater/licensing
- local V25 qualification

## Proven defects

`SemanticScheduleCatalog` caps persisted schedules at 128, while `SemanticSchedulePlacementPlanner.BuildScheduleIndex(...)` currently enumerates the public `IEnumerable<SemanticScheduleDefinition>` with no cardinality guard. A caller can therefore force unbounded enumeration and dictionary growth.

The planner also calls `items.ToList()` before checking `materialized.Count > MaxItems`, so the nominal 128 placement-item guard does not bound enumeration or memory use for an oversized/non-terminating `items` sequence.

## Contract

- Enumerate at most `MaxItems + 1` available schedule entries and fail closed once the 129th entry is observed.
- Enumerate at most `MaxItems + 1` placement items and fail closed once the 129th entry is observed; do not call unbounded `ToList()` first.
- Preserve case-insensitive duplicate-id rejection and existing 128-item placement semantics.
- Add focused regressions proving 129 available schedules and 129 requested placement items fail closed.

## Overlap note

Recent concurrent claims observed are opening cut ownership, takeoff result integrity, ProjectSession recovery, geometry/grid bounds and other unrelated lanes. No newer schedule-placement claim was found after the prior schedule-placement claim was completed. Re-read target files immediately before each write and merge rather than overwrite concurrent changes.