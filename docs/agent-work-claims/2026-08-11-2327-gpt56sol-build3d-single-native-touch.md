# Work claim — Build3D single native ownership ChangeVersion touch

- Status: `ACTIVE`
- Agent: `gpt56sol-chatgpt-web`
- Registered: `2026-08-11T23:27:00+07:00`
- Baseline main SHA: `4c4eb7f7d1fd2041ef51bd9bcb7197289adb7fa0`
- Priority: remove the redundant top-level `QS3DBUILD3D` project Touch after the selected native builder has already committed semantic ownership and advanced ChangeVersion.

## Confirmed defect

Every supported `BuildCategory(...)` native builder advances `ProjectState.ChangeVersion` inside its rollback-capable CAD/semantic commit boundary when it successfully writes generated ownership:
- `StructuralSolidBuilder.BuildSelected(...)`;
- `WallSolidBuilder.BuildSelectedLineWalls(...)`;
- `PolylineWallSolidBuilder.BuildSelected(...)`;
- `WallPierProfileSolidBuilder.BuildSelectedLinePiers(...)`.

After `BuildCategory(...)` returns `built > 0`, `Build3DCommands.Build3D()` calls `project.Touch()` again before post-commit UI. A successful native rebuild therefore receives an artificial extra ChangeVersion advancement on top of the builder-owned mutation (and any legitimate semantic regeneration revisions).

## Reserved scope

- `src/QS3D.BricsCAD.V25/Build3DCommands.cs`
- one focused auto-discovered source regression gate under `scripts/`
- this claim file

## Intended contract

- Native builders remain the ownership/ChangeVersion commit boundary; do not change their transaction/rollback logic.
- `QS3DBUILD3D` retains regeneration scope, semantic rollback, PICKFIRST handoff, generated-handle commit detection and post-commit UI isolation.
- Top-level command must not add a second project Touch after successful `BuildCategory`.

## Excluded scope

- active Grid Annotation, Native Table freshness, Quantity, Semantic View and other current agent lanes
- native builder geometry, unit/vertical placement, generated ownership and regeneration algorithms
- global `ProjectState.Touch` or `AuditTrail` behavior
- BricsCAD V25 native/runtime qualification

## Validation plan

- remove only the redundant top-level `project.Touch()`;
- add a source gate requiring all current BuildCategory target builders to retain their successful pending-update Touch while Build3D contains no post-builder explicit Touch;
- keep rollback/PICKFIRST/post-commit UI tokens guarded;
- compare latest main for overlap before PR/merge;
- do not dispatch GitHub Actions.

## Completion condition

The focused command fix and regression gate are merged to main, claim is COMPLETED, and V25 runtime remains LOCAL_ONLY unless qualified locally.
