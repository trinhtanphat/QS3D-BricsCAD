# Agent Work Claim — Semantic Schedule Placement Core

- Status: `DONE`
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

Recent concurrent commits observed before claiming were updater, curtain frame, Family lifecycle, UI and quantity work; no active schedule-placement source lane was found via repository search. Shared files were re-read before writes and no concurrent source lane was overwritten.

## Result

`SemanticSchedulePlacementPlanner` now provides a pure-Core, handle-free placement contract for semantic schedules on validated sheets. It uses persisted `SemanticScheduleDefinition.Id`, performs deterministic bounded packing in paper millimetres, treats existing semantic view placements as occupied regions, supports reserved bottom/title-block space and fails closed on missing/duplicate IDs, invalid geometry and unplaceable schedules.

A follow-up hardening keeps schedule margins scoped to new schedule candidates: an existing valid view may sit closer to the paper edge than the schedule margin without invalidating the sheet. Focused smoke coverage locks that behavior along with deterministic collision avoidance, reserved-region handling, identity failures, oversized items and non-finite geometry. A dedicated source preflight gate verifies the Core/native boundary and smoke registration. `docs/DOCUMENTATION-LAYER.md` now records the implemented contract and the remaining native V25 boundary.

## Product commits

- `4e8a6509bee2200e52f2f2135bed644ef735a40c` — `chore(agent): claim semantic schedule placement core`
- `badca854b946be8ead622daff9db5489f8a8ae53` — `feat(documentation): add semantic schedule placement planner`
- `61db18752ab9162a65d319c4c93427ffad315cd5` — `fix(documentation): accept valid existing sheet content outside placement margins`
- `63cb38e3a502c5fff6467386248113453895d728` — `test(documentation): cover semantic schedule placement`
- `71ae4df5aa55de043f217155eb70458e692654ea` — `test(documentation): register semantic schedule placement smoke`
- `8590033fd3483b3822e84b19d58b684908ed14b9` — `test: cover schedule placement edge margin`
- `dd144dc7503a6f6d2478b5d060c17a3ff079dd15` — `test: gate semantic schedule placement contract`
- `1d30993fd550bf7b89ede87d9cf0e5e3fa6103d0` — `docs: document semantic schedule placement`

## Validation

- Re-read the planner, focused smoke, smoke registration, preflight source and documentation from remote `main` during integration.
- Commit history for `SemanticSchedulePlacementPlanner.cs` confirms both the initial planner commit and the paper-edge hardening commit remain on `main`.
- The dedicated preflight was added as a source gate but was not executed in this connector-only environment; no local compile or BricsCAD V25 runtime qualification is claimed.
- Native Layout/PaperSpace/Viewport/Table/MLeader materialization remains outside this completed Core lane and must use exact V25 API evidence rather than guessed signatures.
