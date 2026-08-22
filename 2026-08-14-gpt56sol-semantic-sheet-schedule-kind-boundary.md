# Work claim — Semantic Sheet schedule-kind boundary

- Status: `COMPLETED`
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

## Completion evidence

- Claim commit: `b39af8ad82aae7e7c3f4c757626f8d9715914269`; claim-only PR #1202 merged as `427d029ad834197a43ddfa302e36128334af5ae4` before source edits.
- Implementation commit: `4b43d103390db7da6ce9e39848b88306424d340b`.
- Implementation PR #1211 merged normally to `main` as `8b21bb73e328b27ad6aac9ac4301e9d7765c7dde`.
- Exact implementation diff: `SemanticSheetPlanner` referenced-view kind guard, new auto-registered `SemanticSheetScheduleKindBoundarySmoke`, and the existing semantic-sheet-planner preflight only.
- PASS: `preflight-semantic-sheet-planner.py`, `preflight-semantic-view-kind-validation.py`, `preflight-semantic-sheet-catalog-view-index-reuse.py`, `preflight-semantic-sheet-definition-bounds.py`, and `preflight-semantic-schedule-placement.py` via `py -3`.
- PASS: `QS3D.Core` Release build and `QS3D.Core.SmokeTests` Release build, each with 0 warnings / 0 errors.
- Full Core smoke was executed but does not have a PASS claim: the first out-of-lane failure is the stale `ModelHealthElementRelationCanonicalitySmoke.PaddedFamilyFailsVisible` expectation for `FAMILY_REFERENCE_NON_CANONICAL` after canonical relation setter trimming.
- `QS3D.sln` is not claimed as a remote build PASS because its V25 project intentionally requires external licensed `BRICSCAD_V25_DIR` references, which were unavailable and excluded from this no-BricsCAD lane.
- Issue #77 source update: https://github.com/trinhtanphat/QS3D-BricsCAD/issues/77#issuecomment-5291223986.
- No BricsCAD runtime, private data, GitHub Actions, LOCAL, #1005, or #1106 surface was used or changed.
