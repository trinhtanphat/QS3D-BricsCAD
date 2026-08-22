# Work claim — Project Browser workspace revision atomicity

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T07:20:00+07:00`
- Completed: `2026-08-12T07:26:00+07:00`
- Baseline main SHA: `1ee9cd3d18c30a9549ee056e3ccff838bc4d8981`
- Claim commit: `3dc86e27db785071930110dbf710fe91554d8603`
- Source fix commit: `815b1cc6f329dbd9700583aa666431b0bef6e692`
- Regression commit: `7a5cbc490f177fffd60a93e5789ebe416e24ca30`
- Priority: P1 — deterministic Core mutation atomicity at the project revision boundary.

## Reserved scope

Fix `ProjectBrowserWorkspaceStateStore.Save()` and `Clear()` so a `ProjectState.Touch()` overflow cannot occur after workspace metadata has already been mutated. Preserve current validation, serialized format, no-op behavior, and successful revision semantics.

## Implemented surfaces

- `src/QS3D.Core/Navigation/ProjectBrowserWorkspaceStateStore.cs` — mutation ordering only for persisted workspace save/clear.
- `tests/QS3D.Core.SmokeTests/ProjectBrowserWorkspaceDirtyTrackingSmoke.cs` — focused `long.MaxValue` save/clear atomicity regression while retaining the existing dirty-tracking assertions.

## Implemented fix

- `Save()` still validates and returns `false` before touching the project when serialized state is unchanged.
- For a real save, `project.Touch()` now reserves the next project revision before writing `MetadataKey`.
- `Clear()` now checks presence without mutating; an absent key remains a no-op.
- For a real clear, `project.Touch()` now reserves the next project revision before removing `MetadataKey`.
- Successful save/clear still advance `ChangeVersion` exactly once, preserving the newer workspace dirty-tracking contract.

## Regression coverage

The existing registered workspace dirty-tracking smoke now also proves:

- with `ChangeVersion == long.MaxValue`, `Save()` throws `OverflowException` without adding workspace metadata;
- failed save preserves `ChangeVersion` and `UpdatedUtc`;
- with existing workspace metadata and `ChangeVersion == long.MaxValue`, `Clear()` throws without removing or rewriting the metadata;
- failed clear preserves `ChangeVersion` and `UpdatedUtc`;
- pre-existing assertions continue to cover successful first/changed save, clear, identical-save no-op and absent-clear no-op semantics.

The maximum-version fixture is produced through the normal QSDB persistence loader after changing only the persisted `changeVersion`, avoiding any test-only production API.

## Excluded scope honored

- Workspace XML schema/canonicality, query/grouping/primary-id rules, collection bounds, query/selection/virtualization planners, browser UI and V25/V26 adapter/runtime behavior were not changed.
- Semantic Schedule, formula parsing/reference behavior, release/package, licensing and rebar lanes were not touched.
- No LOCAL_ONLY scenario was introduced or changed.

## Coordination and concurrency

- The claim was published alone on `main` before source implementation and verified visible at `3dc86e27db785071930110dbf710fe91554d8603`.
- A first source update was rejected with GitHub 409 because a concurrent browser-workspace collection-canonicality change had moved the blob. The SHA guard prevented any overwrite.
- The winning current file was re-fetched; its new collection-canonicality validation was preserved exactly.
- The concurrent browser-workspace Load/presence claim at `38493fb44cfba32245d74ea0fed1b9cf292eb70a` explicitly recorded that it owns only `Load(ProjectState)` and excludes `Save()/Clear()`, which remain reserved by this claim. This implementation did not modify that lane.
- No force-push, reset, or history rewrite was used.

## Validation actually performed

- Re-read current `ProjectState.Touch()` and confirmed its checked version increment throws before changing `ChangeVersion`/`UpdatedUtc` at `long.MaxValue`.
- Re-read and reviewed the exact source commit diff `815b1cc6f329dbd9700583aa666431b0bef6e692`; it changes only save/clear ordering.
- Re-read and reviewed the exact regression commit diff `7a5cbc490f177fffd60a93e5789ebe416e24ca30`.
- The existing smoke registration is reused; no shared registration surface was changed.
- This connector-only pass did not execute the .NET smoke suite or licensed BricsCAD runtime qualification, so executable/runtime PASS is not claimed.
- GitHub Actions were not dispatched.

## Completion condition

Satisfied for remote/source scope: workspace save/clear now fail before metadata mutation when project revision reservation overflows, normal dirty-tracking semantics remain intact, focused regression source is pushed, concurrent browser Load work was preserved, and validation limitations are recorded truthfully.
