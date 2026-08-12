# Work claim — Project Browser workspace dirty tracking

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:05:00+07:00`
- Completed: `2026-08-12T00:08:00+07:00`
- Baseline main SHA: `2828143f5df24019ee6cda13f662417dfc8afafa`
- Claim commit: `ceeeb0f96eef26dd0563e59f11ca0ddd084eb47d`
- Source fix commit: `81fd6fa9ed29136e95fb7c7250a7dc4cb2688051`
- Regression commit: `ff1008e509175ad132fffc5bd9d5a7c6bc592373`
- Regression registration commit: `4cbeee4a04f0c42f7dcfe5244566ac88d9c1100e`
- Priority: evidence-driven remote-safe Core persistence correctness

## Reserved scope

Fix Project Browser workspace persistence so changes to persisted workspace metadata participate in `ProjectState.ChangeVersion` / `ProjectPersistenceStamp` dirty tracking. Preserve idempotence: saving identical serialized state and clearing an absent workspace key must remain no-ops that do not advance the project version.

## Implemented surfaces

- `src/QS3D.Core/Navigation/ProjectBrowserWorkspaceStateStore.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserWorkspaceDirtyTrackingSmoke.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserWorkspaceDirtyTrackingRegistration.cs`
- this claim file

## Implemented fix

- `Save` still returns `false` without touching the project when the serialized workspace is identical.
- A real workspace metadata write now calls `project.Touch()` exactly once before returning success.
- `Clear` now returns `false` without touching the project when the workspace key is absent.
- Removing an existing workspace key now calls `project.Touch()` exactly once before returning success.
- XML format, query validation, selection validation and virtualization semantics were not changed.

## Regression coverage

The focused smoke verifies:

- a new persistence stamp starts clean;
- first workspace save advances `ChangeVersion` and makes `ProjectPersistenceStamp.RequiresSave()` true;
- `MarkSaved` resets the stamp;
- identical save is a version no-op and remains clean;
- changed workspace save advances the version and marks dirty;
- clearing existing workspace metadata advances the version and marks dirty;
- clearing again is a version no-op and remains clean.

A dedicated module initializer registers the smoke without editing shared registration surfaces.

## Validation actually performed

- Claim was committed and re-read from `main` before substantive writes.
- Current source blob was re-fetched before the fix; the expected SHA guard was used for the update.
- Re-read current `main` after implementation and confirmed both real mutation paths call `project.Touch()` while both no-op paths return before touching.
- Re-read the focused smoke and its dedicated module initializer from current `main`.
- A transient 409 while registering the smoke was handled by re-fetching `main`; compare proved the regression commit remained the merge base with `behind_by: 0`, then the disjoint registration write was retried.
- No force push/reset was used.
- This connector-only environment did not execute the .NET smoke suite or BricsCAD V25 runtime validation; executable/runtime PASS is not claimed.
- No GitHub Actions were dispatched.

## Exclusions honored

- No Project Browser query, selection, grouping, virtualization, XML schema/format, WPF/native/runtime behavior changes.
- No `ProjectState` or `ProjectPersistenceStamp` implementation changes.
- No GitHub Actions dispatch.

## Completion condition

Satisfied for remote/source scope: persisted workspace metadata mutations now participate in project dirty tracking exactly once, no-op saves/clears remain clean, focused regression source is registered on `main`, and validation boundaries are recorded truthfully.
