# Work claim — Semantic Sheet schedule-kind boundary

- Status: `ACTIVE`
- Agent: `gpt56sol /root/fix_level_curtain_frame_z`
- Registered: `2026-08-14T15:10:35+07:00`
- Baseline main SHA: `e315a94c82ea236b332eff5c85d54e01f3cbb742`
- Priority: issue #77 — enforce the documented schedule-versus-viewport identity boundary before native Sheet materialization consumes the Core plan

## Reserved scope

Make `SemanticSheetPlanner` fail closed when a sheet view placement references an available `SemanticViewPlan` whose kind is `SemanticViewKind.Schedule`. Schedule placement remains exclusively represented by `SemanticSchedulePlacementPlanner` and persisted `SemanticScheduleDefinition.Id`; ordinary Model/Plan placements remain supported. Add focused deterministic coverage for the direct planner and its existing auto-layout caller.

## Expected surfaces

- `src/QS3D.Core/Documentation/SemanticSheetPlanner.cs` — referenced-view kind validation only.
- `tests/QS3D.Core.SmokeTests/SemanticSheetScheduleKindBoundarySmoke.cs` — new focused auto-registered Core regression.
- `scripts/preflight-semantic-sheet-planner.py` — focused source-contract assertion only.
- This claim file for completion closeout.

## Excluded scope

- No changes to `SemanticSchedulePlacementPlanner`, documentation catalog persistence/editing, tags, documentation-table generation, native Layout/PaperSpace/Viewport/Table/title-block code, or runtime commands.
- No BricsCAD execution, private data, GitHub Actions, or local/runtime qualification.
- No LOCAL-002/003/004 surfaces, source issues #1005/#1106, probes, runners, inbox, or unrelated documentation.

## Validation plan

- Run `python scripts/preflight-semantic-sheet-planner.py`.
- Run the focused Core smoke entry for the new schedule-kind boundary and full `QS3D.Core.SmokeTests`.
- Build `QS3D.sln` with repository-supported configuration available remotely.
- Review the final diff and verify the implementation/closeout commits are reachable from current `origin/main`.

## Coordination

No open PR currently owns this contract. Current ACTIVE/BLOCKED claims cover unrelated Room Finish XLSX, Preview Review CDATA, atomic file publication, V25 release dispatch, SE polyline conversion, and issue #1005 source reconcile. This lane does not touch the active documentation-table structural-freshness/tag PICKFIRST history or any native runtime qualification.

## Completion condition

A normal implementation PR is merged to current `main`, the claim records exact executed validation, and issue #77 receives a concise source-contract update without claiming native BricsCAD runtime completion.
